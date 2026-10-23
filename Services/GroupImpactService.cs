using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Training.Models.DTO.TrainingMatrix;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implementasi <see cref="IGroupImpactService"/> dengan Dapper (DefaultConnection).
    /// Memakai tabel yang sudah ada (TM_MatrixDetail.SourceCourseGroupID, TM_ChangeRequest[Detail]); tanpa perubahan DB.
    /// </summary>
    public class GroupImpactService : IGroupImpactService
    {
        private const int MaxMatrices = 100;
        private const int MaxReasonLength = 300;

        private const int SqlUniqueConstraintViolation = 2627;
        private const int SqlUniqueIndexViolation = 2601;

        private readonly string _connectionString;
        private readonly IMatrixAccessService _access;
        private readonly ILogger<GroupImpactService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="GroupImpactService"/>.
        /// </summary>
        public GroupImpactService(
            IConfiguration configuration,
            IMatrixAccessService access,
            ILogger<GroupImpactService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _access = access;
            _logger = logger;
        }

        // =====================================================================
        // IMPACT
        // =====================================================================

        /// <inheritdoc />
        public async Task<GroupImpactDto> GetImpactAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default)
        {
            if (courseGroupId <= 0)
            {
                throw new ArgumentException("Invalid course group.", nameof(courseGroupId));
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var group = await connection.QueryFirstOrDefaultAsync<GroupRow>(
                new CommandDefinition(
                    """
                    SELECT g.CourseGroupID AS CourseGroupId,
                           g.GroupName,
                           (SELECT COUNT(1)
                            FROM dbo.TM_CourseGroupItem i
                            INNER JOIN dbo.Master_Courses c ON c.CourseId = i.CourseId AND c.IsActive = 1
                            WHERE i.CourseGroupID = g.CourseGroupID) AS ActiveCourseCount
                    FROM dbo.TM_CourseGroup g
                    WHERE g.CourseGroupID = @Id
                    """,
                    new { Id = courseGroupId },
                    cancellationToken: cancellationToken))
                ?? throw new ArgumentException("Course group was not found.");

            var (cells, missing) = await LoadMissingAsync(
                connection, null, courseGroupId, null, cancellationToken);

            var matrixIds = cells.Select(c => c.MatrixId).Distinct().ToList();

            var matrices = matrixIds.Count == 0
                ? new List<MatrixRow>()
                : (await connection.QueryAsync<MatrixRow>(
                    new CommandDefinition(
                        """
                        SELECT m.MatrixID AS MatrixId,
                               m.DepartmentAbrv,
                               m.Status,
                               m.DocStatus,
                               cr.ChangeRequestID AS ChangeRequestId,
                               cr.DocStatus AS ChangeRequestDocStatus
                        FROM dbo.TM_Matrix m
                        LEFT JOIN dbo.TM_ChangeRequest cr
                               ON cr.MatrixID = m.MatrixID AND cr.IsDeleted = 0 AND cr.DocStatus < 2
                        WHERE m.MatrixID IN @Ids
                        ORDER BY m.DepartmentAbrv
                        """,
                        new { Ids = matrixIds },
                        cancellationToken: cancellationToken))).ToList();

            // Pengguna selain tc / hrhead hanya melihat matrix dari Department yang boleh dilihat.
            var allowed = await _access.GetAllowedAbrvsAsync(cancellationToken);

            if (allowed is not null)
            {
                matrices = matrices
                    .Where(m => allowed.Contains(m.DepartmentAbrv?.Trim() ?? string.Empty))
                    .ToList();
            }

            var dto = new GroupImpactDto
            {
                CourseGroupId = group.CourseGroupId,
                GroupName = group.GroupName,
                ActiveCourseCount = group.ActiveCourseCount,
                TotalMatrices = matrices.Count
            };

            foreach (var m in matrices)
            {
                var rows = missing.Where(x => x.MatrixId == m.MatrixId).ToList();

                if (rows.Count == 0)
                {
                    dto.UpToDateMatrices++;
                    continue;
                }

                var (mode, blocked) = ResolveMode(m);

                dto.Items.Add(new GroupImpactItemDto
                {
                    MatrixId = m.MatrixId,
                    DepartmentAbrv = m.DepartmentAbrv,
                    Status = m.Status,
                    DocStatus = m.DocStatus,
                    CellCount = cells.Count(c => c.MatrixId == m.MatrixId),
                    MissingCount = rows.Count,
                    MissingCourses = rows
                        .GroupBy(r => r.CourseId)
                        .Select(g => new GroupImpactCourseDto
                        {
                            CourseCode = g.First().CourseCode,
                            CourseName = g.First().CourseName
                        })
                        .OrderBy(c => c.CourseCode)
                        .ToList(),
                    Mode = mode,
                    ChangeRequestId = m.ChangeRequestId,
                    BlockedReason = blocked
                });
            }

            return dto;
        }

        // =====================================================================
        // APPLY
        // =====================================================================

        /// <inheritdoc />
        public async Task<GroupImpactApplyResultDto> ApplyAsync(
            int courseGroupId,
            IReadOnlyCollection<int> matrixIds,
            string? reason,
            string by,
            CancellationToken cancellationToken = default)
        {
            if (courseGroupId <= 0)
            {
                throw new ArgumentException("Invalid course group.", nameof(courseGroupId));
            }

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            var ids = (matrixIds ?? Array.Empty<int>()).Distinct().ToList();

            if (ids.Count == 0)
            {
                throw new ArgumentException("Select at least one matrix.");
            }

            if (ids.Any(id => id <= 0))
            {
                throw new ArgumentException("Invalid matrix identifier.");
            }

            if (ids.Count > MaxMatrices)
            {
                throw new ArgumentException($"Too many matrices selected (max {MaxMatrices}).");
            }

            var why = reason?.Trim();

            if (why is { Length: > MaxReasonLength })
            {
                throw new ArgumentException($"Reason cannot exceed {MaxReasonLength} characters.");
            }

            string groupName;

            await using (var probe = new SqlConnection(_connectionString))
            {
                groupName = await probe.QueryFirstOrDefaultAsync<string?>(
                    new CommandDefinition(
                        "SELECT GroupName FROM dbo.TM_CourseGroup WHERE CourseGroupID = @Id",
                        new { Id = courseGroupId },
                        cancellationToken: cancellationToken))
                    ?? throw new ArgumentException("Course group was not found.");
            }

            var result = new GroupImpactApplyResultDto();

            foreach (var id in ids)
            {
                try
                {
                    result.Results.Add(
                        await ApplyOneAsync(courseGroupId, groupName, id, why, by, cancellationToken));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ArgumentException ex)
                {
                    result.Results.Add(new GroupImpactApplyItemDto
                    {
                        MatrixId = id,
                        Success = false,
                        Message = ex.Message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to apply group {GroupId} impact to matrix {MatrixId}.",
                        courseGroupId,
                        id);

                    result.Results.Add(new GroupImpactApplyItemDto
                    {
                        MatrixId = id,
                        Success = false,
                        Message = "Terjadi kesalahan pada server untuk matrix ini."
                    });
                }
            }

            return result;
        }

        private async Task<GroupImpactApplyItemDto> ApplyOneAsync(
            int groupId,
            string groupName,
            int matrixId,
            string? reason,
            string by,
            CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var matrix = await connection.QueryFirstOrDefaultAsync<LockRow>(
                new CommandDefinition(
                    """
                    SELECT MatrixID AS MatrixId, DepartmentAbrv, DocStatus
                    FROM dbo.TM_Matrix WITH (UPDLOCK, ROWLOCK)
                    WHERE MatrixID = @Id AND IsDeleted = 0
                    """,
                    new { Id = matrixId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (matrix is null)
            {
                throw new ArgumentException("Matrix was not found.");
            }

            var item = new GroupImpactApplyItemDto
            {
                MatrixId = matrixId,
                DepartmentAbrv = matrix.DepartmentAbrv
            };

            var (_, missing) = await LoadMissingAsync(
                connection, transaction, groupId, matrixId, cancellationToken);

            if (missing.Count == 0)
            {
                item.Success = true;
                item.Message = "Sudah sinkron.";

                return item;
            }

            if (matrix.DocStatus == 0)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.TM_MatrixDetail
                            (MatrixID, SubDepartmentID, Layer, CourseId, Requirement, SourceCourseGroupID, CreatedBy)
                        VALUES (@MatrixId, @SubDepartmentId, @Layer, @CourseId, @Requirement, @GroupId, @By)
                        """,
                        missing.Select(x => new
                        {
                            MatrixId = matrixId,
                            x.SubDepartmentId,
                            x.Layer,
                            x.CourseId,
                            x.Requirement,
                            GroupId = groupId,
                            By = by
                        }),
                        transaction,
                        cancellationToken: cancellationToken));

                await TouchMatrixAsync(connection, transaction, matrixId, by, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                item.Success = true;
                item.Added = missing.Count;
                item.Message = $"Matrix Draft diperbarui: {missing.Count} baris ditambahkan.";

                return item;
            }

            if (matrix.DocStatus != 2)
            {
                throw new ArgumentException("Matrix sedang menunggu approval; tidak dapat diubah.");
            }

            var cr = await connection.QueryFirstOrDefaultAsync<CrRow>(
                new CommandDefinition(
                    """
                    SELECT ChangeRequestID AS ChangeRequestId, DocStatus
                    FROM dbo.TM_ChangeRequest WITH (UPDLOCK, ROWLOCK)
                    WHERE MatrixID = @Id AND IsDeleted = 0 AND DocStatus < 2
                    """,
                    new { Id = matrixId },
                    transaction,
                    cancellationToken: cancellationToken));

            int crId;
            string verb;

            if (cr is null)
            {
                if (string.IsNullOrEmpty(reason))
                {
                    throw new ArgumentException(
                        "Alasan wajib diisi untuk membuat Change Request.");
                }

                var fullReason = $"Sinkronisasi Course Group '{groupName}': {reason}";

                if (fullReason.Length > 500)
                {
                    fullReason = fullReason[..500];
                }

                try
                {
                    crId = await connection.ExecuteScalarAsync<int>(
                        new CommandDefinition(
                            """
                            INSERT INTO dbo.TM_ChangeRequest (MatrixID, Reason, CreatedBy)
                            OUTPUT INSERTED.ChangeRequestID
                            VALUES (@MatrixId, @Reason, @By)
                            """,
                            new { MatrixId = matrixId, Reason = fullReason, By = by },
                            transaction,
                            cancellationToken: cancellationToken));
                }
                catch (SqlException ex)
                    when (ex.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation)
                {
                    throw new ArgumentException("Matrix ini baru saja mendapat Change Request aktif.", ex);
                }

                verb = "dibuat";
            }
            else if (cr.DocStatus == 0)
            {
                crId = cr.ChangeRequestId;
                verb = "ditambah";
            }
            else
            {
                throw new ArgumentException(
                    $"Change Request #{cr.ChangeRequestId} sedang menunggu approval.");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.TM_ChangeRequestDetail
                        (ChangeRequestID, ActionType, SubDepartmentID, Layer, CourseId,
                         Requirement, SourceCourseGroupID, CreatedBy)
                    VALUES (@CrId, 'Add', @SubDepartmentId, @Layer, @CourseId, @Requirement, @GroupId, @By)
                    """,
                    missing.Select(x => new
                    {
                        CrId = crId,
                        x.SubDepartmentId,
                        x.Layer,
                        x.CourseId,
                        x.Requirement,
                        GroupId = groupId,
                        By = by
                    }),
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_ChangeRequest
                    SET ModifiedBy = @By, ModifiedDate = GETDATE()
                    WHERE ChangeRequestID = @Id
                    """,
                    new { Id = crId, By = by },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            item.Success = true;
            item.Added = missing.Count;
            item.ChangeRequestId = crId;
            item.Message = $"Change Request #{crId} {verb}: {missing.Count} baris Add.";

            return item;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Sel (Sub Dept × Layer) yang diisi dari group, dan course group aktif yang belum ada di sel tersebut
        /// (baik di matrix maupun di Change Request aktif). Requirement sel = Mandatory bila ada satu baris
        /// Mandatory dari group itu pada sel tersebut, selain itu Recommended.
        /// </summary>
        private static async Task<(List<CellRow> Cells, List<MissingRow> Missing)> LoadMissingAsync(
            SqlConnection connection,
            DbTransaction? transaction,
            int groupId,
            int? matrixId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT d.MatrixID AS MatrixId,
                       d.SubDepartmentID AS SubDepartmentId,
                       d.Layer,
                       CASE WHEN MAX(CASE WHEN d.Requirement = 'Mandatory' THEN 1 ELSE 0 END) = 1
                            THEN 'Mandatory' ELSE 'Recommended' END AS Requirement
                INTO #cells
                FROM dbo.TM_MatrixDetail d
                INNER JOIN dbo.TM_Matrix m ON m.MatrixID = d.MatrixID AND m.IsDeleted = 0
                WHERE d.SourceCourseGroupID = @GroupId
                  AND (@MatrixId IS NULL OR d.MatrixID = @MatrixId)
                GROUP BY d.MatrixID, d.SubDepartmentID, d.Layer;

                SELECT MatrixId, SubDepartmentId, Layer, Requirement FROM #cells;

                SELECT ce.MatrixId, ce.SubDepartmentId, ce.Layer, ce.Requirement,
                       c.CourseId, c.CourseCode, c.CourseName
                FROM #cells ce
                INNER JOIN dbo.TM_CourseGroupItem i ON i.CourseGroupID = @GroupId
                INNER JOIN dbo.Master_Courses c ON c.CourseId = i.CourseId AND c.IsActive = 1
                WHERE NOT EXISTS (
                        SELECT 1 FROM dbo.TM_MatrixDetail x
                        WHERE x.MatrixID        = ce.MatrixId
                          AND x.SubDepartmentID = ce.SubDepartmentId
                          AND x.Layer           = ce.Layer
                          AND x.CourseId        = c.CourseId)
                  AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.TM_ChangeRequest r
                        INNER JOIN dbo.TM_ChangeRequestDetail rd ON rd.ChangeRequestID = r.ChangeRequestID
                        WHERE r.MatrixID = ce.MatrixId AND r.IsDeleted = 0 AND r.DocStatus < 2
                          AND rd.SubDepartmentID = ce.SubDepartmentId
                          AND rd.Layer           = ce.Layer
                          AND rd.CourseId        = c.CourseId)
                ORDER BY ce.MatrixId, ce.SubDepartmentId, ce.Layer, c.CourseCode;

                DROP TABLE #cells;
                """;

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    new { GroupId = groupId, MatrixId = matrixId },
                    transaction,
                    cancellationToken: cancellationToken));

            var cells = (await multi.ReadAsync<CellRow>()).ToList();
            var missing = (await multi.ReadAsync<MissingRow>()).ToList();

            return (cells, missing);
        }

        private static (string Mode, string? Blocked) ResolveMode(MatrixRow m)
        {
            if (m.DocStatus == 0)
            {
                return ("Direct", null);
            }

            if (m.DocStatus == 2)
            {
                if (m.ChangeRequestId is null)
                {
                    return ("NewCr", null);
                }

                return m.ChangeRequestDocStatus == 0
                    ? ("AppendCr", null)
                    : ("Blocked", $"Change Request #{m.ChangeRequestId} menunggu approval");
            }

            return ("Blocked", "Matrix menunggu approval");
        }

        private static Task TouchMatrixAsync(
            SqlConnection connection,
            DbTransaction transaction,
            int matrixId,
            string by,
            CancellationToken cancellationToken) =>
            connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_Matrix
                    SET ModifiedBy = @By, ModifiedDate = GETDATE()
                    WHERE MatrixID = @Id
                    """,
                    new { Id = matrixId, By = by },
                    transaction,
                    cancellationToken: cancellationToken));

        // ---- baris pemetaan Dapper ----

        private sealed class GroupRow
        {
            public int CourseGroupId { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public int ActiveCourseCount { get; set; }
        }

        private sealed class MatrixRow
        {
            public int MatrixId { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DocStatus { get; set; }
            public int? ChangeRequestId { get; set; }
            public int? ChangeRequestDocStatus { get; set; }
        }

        private sealed class LockRow
        {
            public int MatrixId { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public int DocStatus { get; set; }
        }

        private sealed class CrRow
        {
            public int ChangeRequestId { get; set; }
            public int DocStatus { get; set; }
        }

        private sealed class CellRow
        {
            public int MatrixId { get; set; }
            public int SubDepartmentId { get; set; }
            public byte Layer { get; set; }
            public string Requirement { get; set; } = string.Empty;
        }

        private sealed class MissingRow
        {
            public int MatrixId { get; set; }
            public int SubDepartmentId { get; set; }
            public byte Layer { get; set; }
            public string Requirement { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public string CourseCode { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
        }
    }
}
