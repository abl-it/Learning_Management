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
    /// Implementasi <see cref="IMatrixChangeRequestService"/> dengan Dapper (DefaultConnection).
    /// </summary>
    public class MatrixChangeRequestService : IMatrixChangeRequestService
    {
        private const string WorkflowName = "MATRIXCR";

        private const int SqlUniqueConstraintViolation = 2627;
        private const int SqlUniqueIndexViolation = 2601;

        private readonly string _connectionString;
        private readonly ILogger<MatrixChangeRequestService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="MatrixChangeRequestService"/>.
        /// </summary>
        public MatrixChangeRequestService(
            IConfiguration configuration,
            ILogger<MatrixChangeRequestService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _logger = logger;
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        /// <inheritdoc />
        public async Task<int> CreateAsync(
            int matrixId,
            string? reason,
            string by,
            CancellationToken cancellationToken = default)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.");
            }

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            var why = reason?.Trim();

            if (string.IsNullOrEmpty(why))
            {
                throw new ArgumentException("Reason is required.");
            }

            if (why.Length > 500)
            {
                throw new ArgumentException("Reason cannot exceed 500 characters.");
            }

            await using var connection = new SqlConnection(_connectionString);

            var docStatus = await connection.QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(
                    "SELECT DocStatus FROM dbo.TM_Matrix WHERE MatrixID = @Id AND IsDeleted = 0",
                    new { Id = matrixId },
                    cancellationToken: cancellationToken));

            if (docStatus is null)
            {
                throw new ArgumentException("Matrix was not found.");
            }

            if (docStatus != 2)
            {
                throw new ArgumentException(
                    "A Change Request can only be created for an Approved matrix. Edit the Draft directly instead.");
            }

            try
            {
                return await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.TM_ChangeRequest (MatrixID, Reason, CreatedBy)
                        OUTPUT INSERTED.ChangeRequestID
                        VALUES (@MatrixId, @Reason, @By)
                        """,
                        new { MatrixId = matrixId, Reason = why, By = by },
                        cancellationToken: cancellationToken));
            }
            catch (SqlException ex)
                when (ex.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation)
            {
                throw new ArgumentException(
                    "This matrix already has an active Change Request.", ex);
            }
        }

        // =====================================================================
        // DETAIL
        // =====================================================================

        /// <inheritdoc />
        public async Task<ChangeRequestDetailDto?> GetDetailAsync(
            int changeRequestId,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default)
        {
            if (changeRequestId <= 0)
            {
                throw new ArgumentException("Invalid change request.", nameof(changeRequestId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var header = await connection.QueryFirstOrDefaultAsync<HeaderRow>(
                new CommandDefinition(
                    """
                    SELECT c.ChangeRequestID, c.MatrixID, m.DepartmentAbrv, c.Strategy, c.Status,
                           c.DocStatus, c.Reason, c.Notes, c.ApprovedBy, c.ApprovedDate,
                           c.CreatedBy, c.CreatedDate
                    FROM dbo.TM_ChangeRequest c
                    INNER JOIN dbo.TM_Matrix m ON m.MatrixID = c.MatrixID
                    WHERE c.ChangeRequestID = @Id AND c.IsDeleted = 0
                    """,
                    new { Id = changeRequestId },
                    cancellationToken: cancellationToken));

            if (header is null)
            {
                return null;
            }

            const string sql = """
                SELECT s.SubDepartmentID AS SubDepartmentId,
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
                WHERE x.MatrixID = @MatrixId
                ORDER BY x.SubDepartmentID, x.Layer, c.CourseCode;

                SELECT r.ChangeRequestDetailID AS ChangeRequestDetailId,
                       r.ActionType,
                       r.SubDepartmentID AS SubDepartmentId,
                       r.Layer,
                       r.CourseId,
                       c.CourseCode,
                       c.CourseName,
                       c.DurationHours,
                       r.Requirement,
                       r.SourceCourseGroupID AS SourceCourseGroupId,
                       g.GroupName AS SourceCourseGroupName
                FROM dbo.TM_ChangeRequestDetail r
                INNER JOIN dbo.Master_Courses c ON c.CourseId = r.CourseId
                LEFT JOIN dbo.TM_CourseGroup g ON g.CourseGroupID = r.SourceCourseGroupID
                WHERE r.ChangeRequestID = @Id
                ORDER BY r.ActionType, r.SubDepartmentID, r.Layer, c.CourseCode;

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
                        Id = changeRequestId,
                        MatrixId = header.MatrixID,
                        Abrv = header.DepartmentAbrv,
                        Workflow = WorkflowName,
                        header.Strategy,
                        State = header.Status
                    },
                    cancellationToken: cancellationToken));

            var subs = (await multi.ReadAsync<MatrixSubDepartmentDto>()).AsList();
            var matrixRows = (await multi.ReadAsync<MatrixRowDto>()).AsList();
            var changes = (await multi.ReadAsync<ChangeRequestRowDto>()).AsList();
            var actions = (await multi.ReadAsync<ActionRow>())
                .Where(a => MatrixRules.FindMatchingRole(roles, a.Allowed) is not null)
                .Select(a => a.Action)
                .ToList();

            return new ChangeRequestDetailDto
            {
                ChangeRequestId = header.ChangeRequestID,
                MatrixId = header.MatrixID,
                DepartmentAbrv = header.DepartmentAbrv,
                Status = header.Status,
                DocStatus = header.DocStatus,
                Reason = header.Reason,
                Notes = header.Notes,
                ApprovedBy = header.ApprovedBy,
                ApprovedDate = header.ApprovedDate,
                CreatedBy = header.CreatedBy,
                CreatedDate = header.CreatedDate,
                IsEditable = header.DocStatus == 0 && MatrixRules.IsEditor(roles),
                Actions = actions,
                SubDepartments = subs,
                MatrixRows = matrixRows,
                Changes = changes
            };
        }

        // =====================================================================
        // ADD / REMOVE / UNDO
        // =====================================================================

        /// <inheritdoc />
        public async Task<MatrixApplyResultDto> AddCoursesAsync(
            ChangeRequestApplyDto request,
            string by,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            var requirement = MatrixRules.NormalizeRequirement(request.Requirement);
            var targets = MatrixRules.ParseTargets(request.Targets);

            var courseIds = (request.CourseIds ?? new List<int>()).Distinct().ToList();

            if (courseIds.Any(id => id <= 0))
            {
                throw new ArgumentException("Invalid course identifier.");
            }

            if (courseIds.Count > MatrixRules.MaxCourseIds)
            {
                throw new ArgumentException(
                    $"Too many courses selected (max {MatrixRules.MaxCourseIds}).");
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
                var edit = await GetEditableAsync(
                    connection, transaction, request.ChangeRequestId, cancellationToken);

                var validSubs = (await connection.QueryAsync<int>(
                    new CommandDefinition(
                        """
                        SELECT s.SubDepartmentID
                        FROM home.dbo.APP_SubDepartments s
                        INNER JOIN home.dbo.APP_Departments d ON d.DepartmentID = s.DepartmentID
                        WHERE d.ABRV = @Abrv AND d.Disabled = 0 AND s.Disabled = 0
                        """,
                        new { Abrv = edit.Abrv },
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

                // Nilai bertipe int hasil parsing, aman disisipkan langsung ke SQL.
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
                    ChangeRequestId = request.ChangeRequestId,
                    edit.MatrixId,
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
                    INSERT INTO dbo.TM_ChangeRequestDetail
                        (ChangeRequestID, ActionType, SubDepartmentID, Layer, CourseId,
                         Requirement, SourceCourseGroupID, CreatedBy)
                    SELECT @ChangeRequestId, 'Add', t.SubDepartmentID, t.Layer, s.CourseId,
                           @Requirement, s.SourceGroupId, @By
                    FROM (VALUES {valuesSql}) AS t (SubDepartmentID, Layer)
                    CROSS JOIN src s
                    WHERE NOT EXISTS (
                            SELECT 1 FROM dbo.TM_MatrixDetail d
                            WHERE d.MatrixID        = @MatrixId
                              AND d.SubDepartmentID = t.SubDepartmentID
                              AND d.Layer           = t.Layer
                              AND d.CourseId        = s.CourseId)
                      AND NOT EXISTS (
                            SELECT 1 FROM dbo.TM_ChangeRequestDetail r
                            WHERE r.ChangeRequestID = @ChangeRequestId
                              AND r.SubDepartmentID = t.SubDepartmentID
                              AND r.Layer           = t.Layer
                              AND r.CourseId        = s.CourseId);
                    """;

                var added = await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertSql,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));

                await TouchAsync(connection, transaction, request.ChangeRequestId, by, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new MatrixApplyResultDto
                {
                    Added = added,
                    Skipped = (targets.Count * sourceCount) - added
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to add courses to change request {ChangeRequestId}.",
                    request.ChangeRequestId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> MarkRemoveAsync(
            int changeRequestId,
            IReadOnlyCollection<int> matrixDetailIds,
            string by,
            CancellationToken cancellationToken = default)
        {
            var ids = MatrixRules.ValidateIds(matrixDetailIds);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            var edit = await GetEditableAsync(connection, transaction, changeRequestId, cancellationToken);

            var added = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO dbo.TM_ChangeRequestDetail
                        (ChangeRequestID, ActionType, SubDepartmentID, Layer, CourseId, Requirement, CreatedBy)
                    SELECT @ChangeRequestId, 'Remove', d.SubDepartmentID, d.Layer, d.CourseId, NULL, @By
                    FROM dbo.TM_MatrixDetail d
                    WHERE d.MatrixID = @MatrixId
                      AND d.MatrixDetailID IN @Ids
                      AND NOT EXISTS (
                            SELECT 1 FROM dbo.TM_ChangeRequestDetail r
                            WHERE r.ChangeRequestID = @ChangeRequestId
                              AND r.SubDepartmentID = d.SubDepartmentID
                              AND r.Layer           = d.Layer
                              AND r.CourseId        = d.CourseId)
                    """,
                    new { ChangeRequestId = changeRequestId, edit.MatrixId, Ids = ids, By = by },
                    transaction,
                    cancellationToken: cancellationToken));

            await TouchAsync(connection, transaction, changeRequestId, by, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return added;
        }

        /// <inheritdoc />
        public async Task<int> UndoChangesAsync(
            int changeRequestId,
            IReadOnlyCollection<int> changeDetailIds,
            CancellationToken cancellationToken = default)
        {
            var ids = MatrixRules.ValidateIds(changeDetailIds);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            await GetEditableAsync(connection, transaction, changeRequestId, cancellationToken);

            var deleted = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM dbo.TM_ChangeRequestDetail
                    WHERE ChangeRequestID = @ChangeRequestId AND ChangeRequestDetailID IN @Ids
                    """,
                    new { ChangeRequestId = changeRequestId, Ids = ids },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return deleted;
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        /// <inheritdoc />
        public async Task DeleteAsync(
            int changeRequestId,
            string by,
            CancellationToken cancellationToken = default)
        {
            if (changeRequestId <= 0)
            {
                throw new ArgumentException("Invalid change request.", nameof(changeRequestId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_ChangeRequest
                    SET IsDeleted = 1, ModifiedBy = @By, ModifiedDate = GETDATE()
                    WHERE ChangeRequestID = @Id AND IsDeleted = 0 AND DocStatus = 0
                    """,
                    new { Id = changeRequestId, By = by },
                    cancellationToken: cancellationToken));

            if (affected == 0)
            {
                throw new ArgumentException(
                    "Only a Draft or Rejected change request can be deleted.");
            }
        }

        // =====================================================================
        // WORKFLOW
        // =====================================================================

        /// <inheritdoc />
        public async Task<string> TransitionAsync(
            int changeRequestId,
            string action,
            string? remarks,
            string by,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default)
        {
            if (changeRequestId <= 0)
            {
                throw new ArgumentException("Invalid change request.", nameof(changeRequestId));
            }

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException("By is required.", nameof(by));
            }

            var act = MatrixRules.WorkflowActions.FirstOrDefault(
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

            var header = await connection.QueryFirstOrDefaultAsync<StateRow>(
                new CommandDefinition(
                    """
                    SELECT Strategy, Status
                    FROM dbo.TM_ChangeRequest
                    WHERE ChangeRequestID = @Id AND IsDeleted = 0
                    """,
                    new { Id = changeRequestId },
                    cancellationToken: cancellationToken));

            if (header is null)
            {
                throw new ArgumentException("Change request was not found.");
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
                    $"Action '{act}' is not available while the change request is '{header.Status}'.");
            }

            var matchedRole = MatrixRules.FindMatchingRole(roles, allowed);

            if (matchedRole is null)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to perform this action.");
            }

            if (act == "Request for Approve")
            {
                var changeCount = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "SELECT COUNT(1) FROM dbo.TM_ChangeRequestDetail WHERE ChangeRequestID = @Id",
                        new { Id = changeRequestId },
                        cancellationToken: cancellationToken));

                if (changeCount == 0)
                {
                    throw new ArgumentException(
                        "The change request has no changes yet. Add or remove courses before requesting approval.");
                }
            }

            try
            {
                var result = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "dbo.USP_Approve",
                        new
                        {
                            id = (long)changeRequestId,
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
                        -1 => "Change request was not found.",
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
                    "USP_Approve failed for change request {ChangeRequestId}, action {Action}.",
                    changeRequestId,
                    act);

                throw;
            }

            var newStatus = await connection.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SELECT Status FROM dbo.TM_ChangeRequest WHERE ChangeRequestID = @Id",
                    new { Id = changeRequestId },
                    cancellationToken: cancellationToken));

            return newStatus ?? string.Empty;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Mengunci baris Change Request dan memastikan masih editable (DocStatus = 0).
        /// </summary>
        private static async Task<EditRow> GetEditableAsync(
            SqlConnection connection,
            DbTransaction transaction,
            int changeRequestId,
            CancellationToken cancellationToken)
        {
            if (changeRequestId <= 0)
            {
                throw new ArgumentException("Invalid change request.");
            }

            var row = await connection.QueryFirstOrDefaultAsync<EditRow>(
                new CommandDefinition(
                    """
                    SELECT c.MatrixID AS MatrixId, m.DepartmentAbrv AS Abrv, c.DocStatus
                    FROM dbo.TM_ChangeRequest c WITH (UPDLOCK, ROWLOCK)
                    INNER JOIN dbo.TM_Matrix m ON m.MatrixID = c.MatrixID
                    WHERE c.ChangeRequestID = @Id AND c.IsDeleted = 0
                    """,
                    new { Id = changeRequestId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (row is null)
            {
                throw new ArgumentException("Change request was not found.");
            }

            if (row.DocStatus != 0)
            {
                throw new ArgumentException(
                    "The change request is locked (pending approval or already applied).");
            }

            return row;
        }

        private static Task TouchAsync(
            SqlConnection connection,
            DbTransaction transaction,
            int changeRequestId,
            string by,
            CancellationToken cancellationToken) =>
            connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE dbo.TM_ChangeRequest
                    SET ModifiedBy = @By, ModifiedDate = GETDATE()
                    WHERE ChangeRequestID = @Id
                    """,
                    new { Id = changeRequestId, By = by },
                    transaction,
                    cancellationToken: cancellationToken));

        // ---- baris pemetaan Dapper ----

        private sealed class HeaderRow
        {
            public int ChangeRequestID { get; set; }
            public int MatrixID { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string Strategy { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DocStatus { get; set; }
            public string? Reason { get; set; }
            public string? Notes { get; set; }
            public string? ApprovedBy { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        private sealed class StateRow
        {
            public string Strategy { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        private sealed class EditRow
        {
            public int MatrixId { get; set; }
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
