using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Training.Models.DTO.TrainingMatrix;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implementasi <see cref="ITrainingMatrixService"/> dengan Dapper (DefaultConnection).
    /// </summary>
    public class TrainingMatrixService : ITrainingMatrixService
    {
        private const string WorkflowName = "MATRIX";
        private const string EditorRole = "tc";
        private const string CreatorToken = "creator";

        private const int MaxTargets = 300;
        private const int MaxCourseIds = 500;

        private const int SqlUniqueConstraintViolation = 2627;
        private const int SqlUniqueIndexViolation = 2601;

        private static readonly string[] AllowedActions =
            { "Request for Approve", "Approve", "Reject" };

        private readonly string _connectionString;
        private readonly ILogger<TrainingMatrixService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="TrainingMatrixService"/>.
        /// </summary>
        public TrainingMatrixService(
            IConfiguration configuration,
            ILogger<TrainingMatrixService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _logger = logger;
        }

        // =====================================================================
        // ROLES
        // =====================================================================

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetRolesAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Array.Empty<string>();
            }

            await using var connection = new SqlConnection(_connectionString);

            var raw = await connection.QueryFirstOrDefaultAsync<string?>(
                new CommandDefinition(
                    "SELECT TOP 1 u.Role FROM home.dbo.Users u WHERE u.Username = @Username",
                    new { Username = username },
                    cancellationToken: cancellationToken));

            return (raw ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        // =====================================================================
        // LIST
        // =====================================================================

        /// <inheritdoc />
        public async Task<List<MatrixListItemDto>> GetListAsync(
            CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT LTRIM(RTRIM(d.ABRV)) AS DepartmentAbrv, d.DepartmentCode
                FROM home.dbo.APP_Departments d
                WHERE d.Disabled = 0
                  AND d.ABRV IS NOT NULL
                  AND LTRIM(RTRIM(d.ABRV)) <> '';

                SELECT LTRIM(RTRIM(d.ABRV)) AS DepartmentAbrv, COUNT(1) AS Cnt
                FROM home.dbo.APP_SubDepartments s
                INNER JOIN home.dbo.APP_Departments d ON d.DepartmentID = s.DepartmentID
                WHERE d.Disabled = 0
                  AND s.Disabled = 0
                  AND d.ABRV IS NOT NULL
                GROUP BY LTRIM(RTRIM(d.ABRV));

                SELECT
                    m.MatrixID,
                    m.DepartmentAbrv,
                    m.Status,
                    m.DocStatus,
                    m.CreatedDate,
                    m.ModifiedBy,
                    m.ModifiedDate,
                    (SELECT COUNT(1) FROM dbo.TM_MatrixDetail x WHERE x.MatrixID = m.MatrixID) AS DetailCount,
                    cr.ChangeRequestID AS ActiveCrId,
                    cr.Status          AS ActiveCrStatus
                FROM dbo.TM_Matrix m
                OUTER APPLY (
                    SELECT TOP 1 c.ChangeRequestID, c.Status
                    FROM dbo.TM_ChangeRequest c
                    WHERE c.MatrixID = m.MatrixID
                      AND c.IsDeleted = 0
                      AND c.DocStatus < 2
                    ORDER BY c.ChangeRequestID DESC
                ) cr
                WHERE m.IsDeleted = 0;
                """;

            await using var connection = new SqlConnection(_connectionString);

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            var depts = (await multi.ReadAsync<DeptRow>()).AsList();
            var counts = (await multi.ReadAsync<CountRow>())
                .ToDictionary(r => r.DepartmentAbrv, StringComparer.OrdinalIgnoreCase);
            var matrices = (await multi.ReadAsync<MatrixRow>())
                .ToDictionary(r => r.DepartmentAbrv, StringComparer.OrdinalIgnoreCase);

            return depts
                .GroupBy(d => d.DepartmentAbrv.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var abrv = g.Key;
                    matrices.TryGetValue(abrv, out var m);
                    counts.TryGetValue(abrv, out var c);

                    return new MatrixListItemDto
                    {
                        DepartmentAbrv = abrv,
                        DepartmentCodes = string.Join(", ",
                            g.Select(x => x.DepartmentCode).Distinct().OrderBy(x => x)),
                        SubDepartmentCount = c?.Cnt ?? 0,
                        MatrixId = m?.MatrixID,
                        Status = m?.Status,
                        DocStatus = m?.DocStatus,
                        DetailCount = m?.DetailCount ?? 0,
                        CreatedDate = m?.CreatedDate,
                        ModifiedBy = m?.ModifiedBy,
                        ModifiedDate = m?.ModifiedDate,
                        ActiveChangeRequestId = m?.ActiveCrId,
                        ActiveChangeRequestStatus = m?.ActiveCrStatus
                    };
                })
                .OrderBy(x => x.DepartmentAbrv, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        /// <inheritdoc />
        public async Task<int> CreateAsync(
            string departmentAbrv,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            var abrv = departmentAbrv?.Trim();

            if (string.IsNullOrEmpty(abrv))
            {
                throw new ArgumentException("Department is required.");
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                throw new ArgumentException("Created by is required.", nameof(createdBy));
            }

            await using var connection = new SqlConnection(_connectionString);

            var exists = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT COUNT(1) FROM home.dbo.APP_Departments
                    WHERE Disabled = 0 AND ABRV = @Abrv
                    """,
                    new { Abrv = abrv },
                    cancellationToken: cancellationToken));

            if (exists == 0)
            {
                throw new ArgumentException("Department was not found.");
            }

            try
            {
                return await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.TM_Matrix (DepartmentAbrv, CreatedBy)
                        OUTPUT INSERTED.MatrixID
                        VALUES (@Abrv, @By)
                        """,
                        new { Abrv = abrv, By = createdBy },
                        cancellationToken: cancellationToken));
            }
            catch (SqlException ex)
                when (ex.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation)
            {
                throw new ArgumentException(
                    "A training matrix for this department already exists.", ex);
            }
        }

        // =====================================================================
        // DETAIL
        // =====================================================================

        /// <inheritdoc />
        public async Task<MatrixDetailDto?> GetDetailAsync(
            int matrixId,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.", nameof(matrixId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var header = await connection.QueryFirstOrDefaultAsync<HeaderRow>(
                new CommandDefinition(
                    """
                    SELECT MatrixID, DepartmentAbrv, Strategy, Status, DocStatus,
                           Notes, ApprovedBy, ApprovedDate
                    FROM dbo.TM_Matrix
                    WHERE MatrixID = @Id AND IsDeleted = 0
                    """,
                    new { Id = matrixId },
                    cancellationToken: cancellationToken));

            if (header is null)
            {
                return null;
            }

            const string sql = """
                SELECT s.SubDepartmentID   AS SubDepartmentId,
                       s.SubDepartmentCode,
                       s.SubDepartmentName,
                       d.DepartmentCode
                FROM home.dbo.APP_SubDepartments s
                INNER JOIN home.dbo.APP_Departments d ON d.DepartmentID = s.DepartmentID
                WHERE d.ABRV = @Abrv AND d.Disabled = 0 AND s.Disabled = 0
                ORDER BY d.DepartmentCode, s.SubDepartmentCode;

                SELECT x.MatrixDetailID AS MatrixDetailId,
                       x.SubDepartmentID AS SubDepartmentId,
                       x.Layer,
                       x.CourseId,
                       c.CourseCode,
                       c.CourseName,
                       c.DurationHours,
                       x.Requirement,
                       x.SourceCourseGroupID AS SourceCourseGroupId,
                       g.GroupName AS SourceCourseGroupName
                FROM dbo.TM_MatrixDetail x
                INNER JOIN dbo.Master_Courses c ON c.CourseId = x.CourseId
                LEFT JOIN dbo.TM_CourseGroup g ON g.CourseGroupID = x.SourceCourseGroupID
                WHERE x.MatrixID = @Id
                ORDER BY x.SubDepartmentID, x.Layer, c.CourseCode;

                SELECT TOP 1 ChangeRequestID
                FROM dbo.TM_ChangeRequest
                WHERE MatrixID = @Id AND IsDeleted = 0 AND DocStatus < 2
                ORDER BY ChangeRequestID DESC;

                SELECT w.Action, w.Allowed
                FROM dbo.Workflow w
                WHERE w.Workflow = @Workflow
                  AND w.Strategy = @Strategy
                  AND w.State    = @State
                  AND w.Action  <> '-'
                ORDER BY w.Step;
                """;

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = matrixId,
                        Abrv = header.DepartmentAbrv,
                        Workflow = WorkflowName,
                        header.Strategy,
                        State = header.Status
                    },
                    cancellationToken: cancellationToken));

            var subs = (await multi.ReadAsync<MatrixSubDepartmentDto>()).AsList();
            var rows = (await multi.ReadAsync<MatrixRowDto>()).AsList();
            var activeCr = await multi.ReadFirstOrDefaultAsync<int?>();
            var actions = (await multi.ReadAsync<ActionRow>())
                .Where(a => FindMatchingRole(roles, a.Allowed) is not null)
                .Select(a => a.Action)
                .ToList();

            return new MatrixDetailDto
            {
                MatrixId = header.MatrixID,
                DepartmentAbrv = header.DepartmentAbrv,
                Status = header.Status,
                DocStatus = header.DocStatus,
                Notes = header.Notes,
                ApprovedBy = header.ApprovedBy,
                ApprovedDate = header.ApprovedDate,
                IsEditable = header.DocStatus == 0 && IsEditor(roles),
                ActiveChangeRequestId = activeCr,
                CanRequestChange = header.DocStatus == 2 && activeCr is null && IsEditor(roles),
                Actions = actions,
                SubDepartments = subs,
                Rows = rows
            };
        }

        // =====================================================================
        // APPLY / REMOVE / REQUIREMENT
        // =====================================================================

        /// <inheritdoc />
        public async Task<MatrixApplyResultDto> ApplyAsync(
            MatrixApplyDto request,
            string by,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            if (request.MatrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.");
            }

            var requirement = NormalizeRequirement(request.Requirement);
            var targets = ParseTargets(request.Targets);

            var courseIds = (request.CourseIds ?? new List<int>()).Distinct().ToList();

            if (courseIds.Any(id => id <= 0))
            {
                throw new ArgumentException("Invalid course identifier.");
            }

            if (courseIds.Count > MaxCourseIds)
            {
                throw new ArgumentException($"Too many courses selected (max {MaxCourseIds}).");
            }

            var groupId = request.CourseGroupId is > 0 ? request.CourseGroupId : null;

            if (groupId is null && courseIds.Count == 0)
            {
                throw new ArgumentException("Select a course group or at least one course.");
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var abrv = await GetEditableAbrvAsync(
                    connection, transaction, request.MatrixId, cancellationToken);

                // Sub Department harus milik Department (ABRV) matrix ini
                var validSubs = (await connection.QueryAsync<int>(
                    new CommandDefinition(
                        """
                        SELECT s.SubDepartmentID
                        FROM home.dbo.APP_SubDepartments s
                        INNER JOIN home.dbo.APP_Departments d ON d.DepartmentID = s.DepartmentID
                        WHERE d.ABRV = @Abrv AND d.Disabled = 0 AND s.Disabled = 0
                        """,
                        new { Abrv = abrv },
                        transaction,
                        cancellationToken: cancellationToken))).ToHashSet();

                var invalid = targets
                    .Select(t => t.SubDepartmentId)
                    .Distinct()
                    .Where(id => !validSubs.Contains(id))
                    .ToList();

                if (invalid.Count > 0)
                {
                    throw new ArgumentException(
                        $"Sub department does not belong to this department (SubDepartmentId: {string.Join(", ", invalid)}).");
                }

                if (groupId is not null)
                {
                    var found = await connection.ExecuteScalarAsync<int>(
                        new CommandDefinition(
                            "SELECT COUNT(1) FROM dbo.TM_CourseGroup WHERE CourseGroupID = @Id",
                            new { Id = groupId },
                            transaction,
                            cancellationToken: cancellationToken));

                    if (found == 0)
                    {
                        throw new ArgumentException("Course group was not found.");
                    }
                }

                // Nilai di bawah bertipe int hasil parsing, aman disisipkan langsung ke SQL.
                var valuesSql = string.Join(
                    ",",
                    targets.Select(t => $"({t.SubDepartmentId},{t.Layer})"));

                var idsSql = courseIds.Count == 0 ? "-1" : string.Join(",", courseIds);

                var cte = new StringBuilder()
                    .Append("WITH grp AS (")
                    .Append(" SELECT c.CourseId, @CourseGroupId AS SourceGroupId")
                    .Append(" FROM dbo.TM_CourseGroupItem i")
                    .Append(" INNER JOIN dbo.Master_Courses c ON c.CourseId = i.CourseId")
                    .Append(" WHERE i.CourseGroupID = @CourseGroupId AND c.IsActive = 1),")
                    .Append(" ind AS (")
                    .Append(" SELECT c.CourseId, CAST(NULL AS INT) AS SourceGroupId")
                    .Append(" FROM dbo.Master_Courses c")
                    .Append($" WHERE c.CourseId IN ({idsSql}) AND c.IsActive = 1")
                    .Append(" AND NOT EXISTS (SELECT 1 FROM grp g WHERE g.CourseId = c.CourseId)),")
                    .Append(" src AS (")
                    .Append(" SELECT CourseId, SourceGroupId FROM grp")
                    .Append(" UNION ALL")
                    .Append(" SELECT CourseId, SourceGroupId FROM ind) ")
                    .ToString();

                var parameters = new
                {
                    request.MatrixId,
                    CourseGroupId = groupId,
                    Requirement = requirement,
                    By = by
                };

                var sourceCount = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        cte + "SELECT COUNT(1) FROM src;",
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));

                if (sourceCount == 0)
                {
                    throw new ArgumentException("No active course to add.");
                }

                var insertSql = cte + $"""
                    INSERT INTO dbo.TM_MatrixDetail
                        (MatrixID, SubDepartmentID, Layer, CourseId, Requirement, SourceCourseGroupID, CreatedBy)
                    SELECT @MatrixId, t.SubDepartmentID, t.Layer, s.CourseId, @Requirement, s.SourceGroupId, @By
                    FROM (VALUES {valuesSql}) AS t (SubDepartmentID, Layer)
                    CROSS JOIN src s
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM dbo.TM_MatrixDetail d
                        WHERE d.MatrixID        = @MatrixId
                          AND d.SubDepartmentID = t.SubDepartmentID
                          AND d.Layer           = t.Layer
                          AND d.CourseId        = s.CourseId);
                    """;

                var added = await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertSql,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE dbo.TM_Matrix
                        SET ModifiedBy = @By, ModifiedDate = GETDATE()
                        WHERE MatrixID = @MatrixId
                        """,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return new MatrixApplyResultDto
                {
                    Added = added,
                    Skipped = (targets.Count * sourceCount) - added
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to apply courses to matrix {MatrixId}.", request.MatrixId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> RemoveRowsAsync(
            int matrixId,
            IReadOnlyCollection<int> detailIds,
            CancellationToken cancellationToken = default)
        {
            var ids = ValidateIds(detailIds);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            await GetEditableAbrvAsync(connection, transaction, matrixId, cancellationToken);

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM dbo.TM_MatrixDetail
                    WHERE MatrixID = @MatrixId AND MatrixDetailID IN @Ids
                    """,
                    new { MatrixId = matrixId, Ids = ids },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return deleted;
        }

        /// <inheritdoc />
        public async Task<int> SetRequirementAsync(
            int matrixId,
            IReadOnlyCollection<int> detailIds,
            string requirement,
            CancellationToken cancellationToken = default)
        {
            var ids = ValidateIds(detailIds);
            var req = NormalizeRequirement(requirement);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            await GetEditableAbrvAsync(connection, transaction, matrixId, cancellationToken);

            var changed = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_MatrixDetail
                    SET Requirement = @Requirement
                    WHERE MatrixID = @MatrixId AND MatrixDetailID IN @Ids
                    """,
                    new { MatrixId = matrixId, Ids = ids, Requirement = req },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return changed;
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        /// <inheritdoc />
        public async Task DeleteAsync(
            int matrixId,
            string by,
            CancellationToken cancellationToken = default)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.", nameof(matrixId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_Matrix
                    SET IsDeleted = 1, ModifiedBy = @By, ModifiedDate = GETDATE()
                    WHERE MatrixID = @Id AND IsDeleted = 0 AND DocStatus = 0
                    """,
                    new { Id = matrixId, By = by },
                    cancellationToken: cancellationToken));

            if (affected == 0)
            {
                throw new ArgumentException(
                    "Only a Draft or Rejected matrix can be deleted.");
            }
        }

        // =====================================================================
        // WORKFLOW
        // =====================================================================

        /// <inheritdoc />
        public async Task<string> TransitionAsync(
            int matrixId,
            string action,
            string? remarks,
            string by,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.", nameof(matrixId));
            }

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            var act = AllowedActions.FirstOrDefault(
                a => string.Equals(a, action?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (act is null)
            {
                throw new ArgumentException("Unknown action.");
            }

            var note = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();

            if (note is not null && note.Length > 250)
            {
                throw new ArgumentException("Remarks cannot exceed 250 characters.");
            }

            if (act == "Reject" && note is null)
            {
                throw new ArgumentException("Remarks are required when rejecting.");
            }

            await using var connection = new SqlConnection(_connectionString);

            var header = await connection.QueryFirstOrDefaultAsync<HeaderRow>(
                new CommandDefinition(
                    """
                    SELECT MatrixID, DepartmentAbrv, Strategy, Status, DocStatus,
                           Notes, ApprovedBy, ApprovedDate
                    FROM dbo.TM_Matrix
                    WHERE MatrixID = @Id AND IsDeleted = 0
                    """,
                    new { Id = matrixId },
                    cancellationToken: cancellationToken));

            if (header is null)
            {
                throw new ArgumentException("Matrix was not found.");
            }

            var allowed = await connection.QueryFirstOrDefaultAsync<string?>(
                new CommandDefinition(
                    """
                    SELECT TOP 1 w.Allowed
                    FROM dbo.Workflow w
                    WHERE w.Workflow = @Workflow
                      AND w.Strategy = @Strategy
                      AND w.State    = @State
                      AND w.Action   = @Action
                    """,
                    new
                    {
                        Workflow = WorkflowName,
                        header.Strategy,
                        State = header.Status,
                        Action = act
                    },
                    cancellationToken: cancellationToken));

            if (allowed is null)
            {
                throw new ArgumentException(
                    $"Action '{act}' is not available while the matrix is '{header.Status}'.");
            }

            // Role yang cocok dengan Workflow.Allowed; inilah yang dikirim ke USP_Approve (@role)
            var matchedRole = FindMatchingRole(roles, allowed);

            if (matchedRole is null)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to perform this action.");
            }

            if (act == "Request for Approve")
            {
                var detailCount = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "SELECT COUNT(1) FROM dbo.TM_MatrixDetail WHERE MatrixID = @Id",
                        new { Id = matrixId },
                        cancellationToken: cancellationToken));

                if (detailCount == 0)
                {
                    throw new ArgumentException(
                        "The matrix has no courses yet. Add courses before requesting approval.");
                }
            }

            try
            {
                var result = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "dbo.USP_Approve",
                        new
                        {
                            id = (long)matrixId,
                            actionName = act,
                            remarks = note ?? string.Empty,
                            by,
                            doc = WorkflowName,
                            role = matchedRole
                        },
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken));

                if (result <= 0)
                {
                    throw new ArgumentException(result switch
                    {
                        -1 => "Matrix was not found.",
                        -2 => "Workflow transition is not configured.",
                        -3 => "Document status is not configured in workflow.",
                        _ => "The action could not be completed."
                    });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "USP_Approve failed for matrix {MatrixId}, action {Action}.",
                    matrixId,
                    act);

                throw;
            }

            var newStatus = await connection.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SELECT Status FROM dbo.TM_Matrix WHERE MatrixID = @Id",
                    new { Id = matrixId },
                    cancellationToken: cancellationToken));

            return newStatus ?? string.Empty;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool IsEditor(IEnumerable<string>? roles) =>
            roles is not null
            && roles.Any(r => string.Equals(r, EditorRole, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Mencari role pengguna yang berhak atas <paramref name="allowed"/> (nilai dbo.Workflow.Allowed).
        /// 'creator' dipetakan ke role tc (hanya tc yang membuat dan submit matrix).
        /// </summary>
        /// <returns>Nama role yang cocok, atau null bila tidak ada.</returns>
        private static string? FindMatchingRole(IEnumerable<string>? roles, string? allowed)
        {
            if (roles is null || string.IsNullOrWhiteSpace(allowed))
            {
                return null;
            }

            var target = string.Equals(allowed, CreatorToken, StringComparison.OrdinalIgnoreCase)
                ? EditorRole
                : allowed.Trim();

            return roles.FirstOrDefault(
                r => string.Equals(r, target, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeRequirement(string? requirement)
        {
            if (string.Equals(requirement?.Trim(), "Mandatory", StringComparison.OrdinalIgnoreCase))
            {
                return "Mandatory";
            }

            if (string.Equals(requirement?.Trim(), "Recommended", StringComparison.OrdinalIgnoreCase))
            {
                return "Recommended";
            }

            throw new ArgumentException("Requirement must be Mandatory or Recommended.");
        }

        private static List<int> ValidateIds(IReadOnlyCollection<int>? ids)
        {
            var list = (ids ?? Array.Empty<int>()).Distinct().ToList();

            if (list.Count == 0)
            {
                throw new ArgumentException("Select at least one row.");
            }

            if (list.Any(i => i <= 0))
            {
                throw new ArgumentException("Invalid row identifier.");
            }

            return list;
        }

        private static List<(int SubDepartmentId, int Layer)> ParseTargets(IEnumerable<string>? raw)
        {
            var result = new List<(int, int)>();

            foreach (var item in (raw ?? Enumerable.Empty<string>()).Distinct())
            {
                var parts = item?.Split(':');

                if (parts is not { Length: 2 }
                    || !int.TryParse(parts[0], out var sub) || sub <= 0
                    || !int.TryParse(parts[1], out var layer) || layer is < 1 or > 3)
                {
                    throw new ArgumentException($"Invalid target '{item}'. Use SubDepartmentId:Layer (Layer 1-3).");
                }

                result.Add((sub, layer));
            }

            if (result.Count == 0)
            {
                throw new ArgumentException("Select at least one Sub Department and Layer.");
            }

            if (result.Count > MaxTargets)
            {
                throw new ArgumentException($"Too many targets selected (max {MaxTargets}).");
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Mengunci baris matrix dan memastikan dokumen masih editable (DocStatus = 0).
        /// </summary>
        /// <returns>ABRV Department matrix.</returns>
        private static async Task<string> GetEditableAbrvAsync(
            SqlConnection connection,
            DbTransaction transaction,
            int matrixId,
            CancellationToken cancellationToken)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.");
            }

            var row = await connection.QueryFirstOrDefaultAsync<EditRow>(
                new CommandDefinition(
                    """
                    SELECT DepartmentAbrv AS Abrv, DocStatus
                    FROM dbo.TM_Matrix WITH (UPDLOCK, ROWLOCK)
                    WHERE MatrixID = @Id AND IsDeleted = 0
                    """,
                    new { Id = matrixId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (row is null)
            {
                throw new ArgumentException("Matrix was not found.");
            }

            if (row.DocStatus != 0)
            {
                throw new ArgumentException(
                    "The matrix is locked. Changes after approval must use a Change Request.");
            }

            return row.Abrv;
        }

        // ---- baris pemetaan Dapper ----

        private sealed class DeptRow
        {
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string DepartmentCode { get; set; } = string.Empty;
        }

        private sealed class CountRow
        {
            public string DepartmentAbrv { get; set; } = string.Empty;
            public int Cnt { get; set; }
        }

        private sealed class MatrixRow
        {
            public int MatrixID { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DocStatus { get; set; }
            public DateTime CreatedDate { get; set; }
            public string? ModifiedBy { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public int DetailCount { get; set; }
            public int? ActiveCrId { get; set; }
            public string? ActiveCrStatus { get; set; }
        }

        private sealed class HeaderRow
        {
            public int MatrixID { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string Strategy { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DocStatus { get; set; }
            public string? Notes { get; set; }
            public string? ApprovedBy { get; set; }
            public DateTime? ApprovedDate { get; set; }
        }

        private sealed class EditRow
        {
            public string Abrv { get; set; } = string.Empty;
            public int DocStatus { get; set; }
        }

        private sealed class ActionRow
        {
            public string Action { get; set; } = string.Empty;
            public string? Allowed { get; set; }
        }
    }
}
