using Dapper;
using Microsoft.Data.SqlClient;
using Training.Models.DTO.TrainingMatrix;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implementasi <see cref="ICourseGroupService"/> dengan Dapper.
    /// Perubahan Course Group tidak memengaruhi matrix yang sudah dibuat,
    /// karena matrix menyimpan snapshot course, bukan referensi ke group.
    /// </summary>
    public class CourseGroupService : ICourseGroupService
    {
        private const int MaxGroupNameLength = 100;
        private const int MaxDescriptionLength = 500;
        private const int MaxCoursesPerGroup = 200;

        private const int SqlUniqueConstraintViolation = 2627;
        private const int SqlUniqueIndexViolation = 2601;

        private readonly string _connectionString;
        private readonly ILogger<CourseGroupService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="CourseGroupService"/>.
        /// </summary>
        /// <param name="configuration">Konfigurasi aplikasi (connection string DefaultConnection).</param>
        /// <param name="logger">Logger.</param>
        public CourseGroupService(
            IConfiguration configuration,
            ILogger<CourseGroupService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<CourseGroupListItemDto>> GetListAsync(
            CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT
                    g.CourseGroupID AS CourseGroupId,
                    g.GroupName,
                    g.Description,
                    g.IsActive,
                    COUNT(i.CourseId) AS CourseCount,
                    g.CreatedBy,
                    g.CreatedDate,
                    g.ModifiedBy,
                    g.ModifiedDate
                FROM dbo.TM_CourseGroup g
                LEFT JOIN dbo.TM_CourseGroupItem i ON i.CourseGroupID = g.CourseGroupID
                GROUP BY
                    g.CourseGroupID, g.GroupName, g.Description, g.IsActive,
                    g.CreatedBy, g.CreatedDate, g.ModifiedBy, g.ModifiedDate
                ORDER BY g.GroupName
                """;

            await using var connection = new SqlConnection(_connectionString);

            var rows = await connection.QueryAsync<CourseGroupListItemDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.AsList();
        }

        /// <inheritdoc />
        public async Task<CourseGroupDetailDto?> GetDetailAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default)
        {
            if (courseGroupId <= 0)
            {
                throw new ArgumentException(
                    "Invalid course group identifier.",
                    nameof(courseGroupId));
            }

            const string sql = """
                SELECT
                    g.CourseGroupID AS CourseGroupId,
                    g.GroupName,
                    g.Description,
                    g.IsActive
                FROM dbo.TM_CourseGroup g
                WHERE g.CourseGroupID = @CourseGroupId;

                SELECT
                    c.CourseId,
                    c.CourseCode,
                    c.CourseName,
                    c.DurationHours,
                    CAST(ISNULL(c.IsActive, 0) AS bit) AS IsActive
                FROM dbo.TM_CourseGroupItem i
                INNER JOIN dbo.Master_Courses c ON c.CourseId = i.CourseId
                WHERE i.CourseGroupID = @CourseGroupId
                ORDER BY c.CourseCode;
                """;

            await using var connection = new SqlConnection(_connectionString);

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    new { CourseGroupId = courseGroupId },
                    cancellationToken: cancellationToken));

            var header = await multi.ReadFirstOrDefaultAsync<CourseGroupDetailDto>();

            if (header is null)
            {
                return null;
            }

            header.Courses = (await multi.ReadAsync<CourseGroupItemDto>()).AsList();

            return header;
        }

        /// <inheritdoc />
        public async Task<List<CourseGroupItemDto>> GetCourseOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT
                    c.CourseId,
                    c.CourseCode,
                    c.CourseName,
                    c.DurationHours,
                    CAST(1 AS bit) AS IsActive
                FROM dbo.Master_Courses c
                WHERE c.IsActive = 1
                ORDER BY c.CourseCode
                """;

            await using var connection = new SqlConnection(_connectionString);

            var rows = await connection.QueryAsync<CourseGroupItemDto>(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            return rows.AsList();
        }

        /// <inheritdoc />
        public async Task<int> SaveAsync(
            CourseGroupSaveDto request,
            string savedBy,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(savedBy))
            {
                throw new ArgumentException("Saved by is required.", nameof(savedBy));
            }

            // ---- Validasi input (tanpa DB) --------------------------------------------
            var groupName = request.GroupName?.Trim();

            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException("Group name is required.");
            }

            if (groupName.Length > MaxGroupNameLength)
            {
                throw new ArgumentException(
                    $"Group name cannot exceed {MaxGroupNameLength} characters.");
            }

            var description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (description is not null && description.Length > MaxDescriptionLength)
            {
                throw new ArgumentException(
                    $"Description cannot exceed {MaxDescriptionLength} characters.");
            }

            var courseIds = (request.CourseIds ?? new List<int>())
                .Distinct()
                .ToList();

            if (courseIds.Count == 0)
            {
                throw new ArgumentException("Select at least one course.");
            }

            if (courseIds.Any(id => id <= 0))
            {
                throw new ArgumentException("Invalid course identifier.");
            }

            if (courseIds.Count > MaxCoursesPerGroup)
            {
                throw new ArgumentException(
                    $"A course group cannot contain more than {MaxCoursesPerGroup} courses.");
            }

            // ---- Simpan dalam satu transaksi -------------------------------------------
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var groupId = request.CourseGroupId;
                var existingCourseIds = new HashSet<int>();

                if (groupId > 0)
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

                    existingCourseIds = (await connection.QueryAsync<int>(
                        new CommandDefinition(
                            "SELECT CourseId FROM dbo.TM_CourseGroupItem WHERE CourseGroupID = @Id",
                            new { Id = groupId },
                            transaction,
                            cancellationToken: cancellationToken))).ToHashSet();
                }

                // Course harus ada di master. Course nonaktif hanya boleh bila sudah ada di group ini.
                var masterRows = (await connection.QueryAsync<CourseStatusRow>(
                    new CommandDefinition(
                        """
                        SELECT CourseId, CourseCode, CAST(ISNULL(IsActive, 0) AS bit) AS IsActive
                        FROM dbo.Master_Courses
                        WHERE CourseId IN @Ids
                        """,
                        new { Ids = courseIds },
                        transaction,
                        cancellationToken: cancellationToken)))
                    .ToDictionary(r => r.CourseId);

                var missing = courseIds.Where(id => !masterRows.ContainsKey(id)).ToList();

                if (missing.Count > 0)
                {
                    throw new ArgumentException(
                        $"Course not found in master course (CourseId: {string.Join(", ", missing)}).");
                }

                var inactive = courseIds
                    .Where(id => !masterRows[id].IsActive && !existingCourseIds.Contains(id))
                    .Select(id => masterRows[id].CourseCode)
                    .ToList();

                if (inactive.Count > 0)
                {
                    throw new ArgumentException(
                        $"Inactive course cannot be added: {string.Join(", ", inactive)}.");
                }

                // Header
                if (groupId > 0)
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            """
                            UPDATE dbo.TM_CourseGroup
                            SET GroupName    = @GroupName,
                                Description  = @Description,
                                IsActive     = @IsActive,
                                ModifiedBy   = @By,
                                ModifiedDate = GETDATE()
                            WHERE CourseGroupID = @Id
                            """,
                            new
                            {
                                Id = groupId,
                                GroupName = groupName,
                                Description = description,
                                request.IsActive,
                                By = savedBy
                            },
                            transaction,
                            cancellationToken: cancellationToken));

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            "DELETE FROM dbo.TM_CourseGroupItem WHERE CourseGroupID = @Id",
                            new { Id = groupId },
                            transaction,
                            cancellationToken: cancellationToken));
                }
                else
                {
                    groupId = await connection.ExecuteScalarAsync<int>(
                        new CommandDefinition(
                            """
                            INSERT INTO dbo.TM_CourseGroup (GroupName, Description, IsActive, CreatedBy)
                            OUTPUT INSERTED.CourseGroupID
                            VALUES (@GroupName, @Description, @IsActive, @By)
                            """,
                            new
                            {
                                GroupName = groupName,
                                Description = description,
                                request.IsActive,
                                By = savedBy
                            },
                            transaction,
                            cancellationToken: cancellationToken));
                }

                // Isi group
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.TM_CourseGroupItem (CourseGroupID, CourseId, CreatedBy)
                        VALUES (@CourseGroupId, @CourseId, @By)
                        """,
                        courseIds.Select(id => new
                        {
                            CourseGroupId = groupId,
                            CourseId = id,
                            By = savedBy
                        }),
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return groupId;
            }
            catch (SqlException ex)
                when (ex.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation)
            {
                _logger.LogWarning(
                    ex,
                    "Duplicate course group name '{GroupName}'.",
                    groupName);

                throw new ArgumentException(
                    "A course group with this name already exists.", ex);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save course group '{GroupName}' by {SavedBy}.",
                    groupName,
                    savedBy);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default)
        {
            if (courseGroupId <= 0)
            {
                throw new ArgumentException(
                    "Invalid course group identifier.",
                    nameof(courseGroupId));
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM dbo.TM_CourseGroupItem WHERE CourseGroupID = @Id",
                        new { Id = courseGroupId },
                        transaction,
                        cancellationToken: cancellationToken));

                var deleted = await connection.ExecuteAsync(
                    new CommandDefinition(
                        "DELETE FROM dbo.TM_CourseGroup WHERE CourseGroupID = @Id",
                        new { Id = courseGroupId },
                        transaction,
                        cancellationToken: cancellationToken));

                if (deleted == 0)
                {
                    throw new ArgumentException("Course group was not found.");
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete course group {CourseGroupId}.",
                    courseGroupId);

                throw;
            }
        }

        /// <summary>
        /// Baris status course pada master, dipakai untuk validasi saat simpan.
        /// </summary>
        private sealed class CourseStatusRow
        {
            public int CourseId { get; set; }

            public string CourseCode { get; set; } = string.Empty;

            public bool IsActive { get; set; }
        }
    }
}
