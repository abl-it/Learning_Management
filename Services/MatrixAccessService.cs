using Dapper;
using Microsoft.Data.SqlClient;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implementasi <see cref="IMatrixAccessService"/>. Memakai fungsi akses yang sudah ada
    /// (<c>dbo.fc_GetDepartmentsByAccess</c> dengan docType 'MATRIX'); tanpa perubahan DB.
    /// Hasil di-cache selama satu request (service Scoped).
    /// </summary>
    public class MatrixAccessService : IMatrixAccessService
    {
        private const string DocType = "MATRIX";

        private static readonly string[] UnrestrictedRoles = { "tc", "hrhead", "gm", "director" };

        private const string DeniedMessage = "Anda tidak memiliki akses ke Department ini.";

        private readonly string _connectionString;
        private readonly ICurrentUserService _emp;
        private readonly ITrainingMatrixService _matrixService;

        private bool _loaded;
        private IReadOnlySet<string>? _allowed;

        /// <summary>
        /// Membuat instance baru <see cref="MatrixAccessService"/>.
        /// </summary>
        public MatrixAccessService(
            IConfiguration configuration,
            ICurrentUserService emp,
            ITrainingMatrixService matrixService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _emp = emp;
            _matrixService = matrixService;
        }

        /// <inheritdoc />
        public async Task<IReadOnlySet<string>?> GetAllowedAbrvsAsync(
            CancellationToken cancellationToken = default)
        {
            if (_loaded)
            {
                return _allowed;
            }

            var code = _emp.CurrentEmployee?.EmployeeCode;
            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(code))
            {
                _allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _loaded = true;

                return _allowed;
            }

            var roles = string.IsNullOrWhiteSpace(username)
                ? Array.Empty<string>()
                : (await _matrixService.GetRolesAsync(username, cancellationToken)).ToArray();

            if (roles.Any(r => UnrestrictedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                _allowed = null;
                _loaded = true;

                return null;
            }

            await using var connection = new SqlConnection(_connectionString);

            var abrvs = await connection.QueryAsync<string>(
                new CommandDefinition(
                    """
                    SELECT DISTINCT LTRIM(RTRIM(ABRV))
                    FROM dbo.fc_GetDepartmentsByAccess(@EmployeeCode, @DocType)
                    WHERE ABRV IS NOT NULL
                    """,
                    new { EmployeeCode = code, DocType },
                    cancellationToken: cancellationToken));

            _allowed = new HashSet<string>(abrvs, StringComparer.OrdinalIgnoreCase);
            _loaded = true;

            return _allowed;
        }

        /// <inheritdoc />
        public async Task EnsureAbrvAsync(string? abrv, CancellationToken cancellationToken = default)
        {
            var allowed = await GetAllowedAbrvsAsync(cancellationToken);

            if (allowed is not null && !allowed.Contains(abrv?.Trim() ?? string.Empty))
            {
                throw new UnauthorizedAccessException(DeniedMessage);
            }
        }

        /// <inheritdoc />
        public async Task EnsureMatrixAsync(int matrixId, CancellationToken cancellationToken = default)
        {
            if (matrixId <= 0)
            {
                throw new ArgumentException("Invalid matrix.", nameof(matrixId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var abrv = await connection.QueryFirstOrDefaultAsync<string?>(
                new CommandDefinition(
                    "SELECT DepartmentAbrv FROM dbo.TM_Matrix WHERE MatrixID = @Id AND IsDeleted = 0",
                    new { Id = matrixId },
                    cancellationToken: cancellationToken))
                ?? throw new ArgumentException("Matrix was not found.");

            await EnsureAbrvAsync(abrv, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureChangeRequestAsync(
            int changeRequestId,
            CancellationToken cancellationToken = default)
        {
            if (changeRequestId <= 0)
            {
                throw new ArgumentException("Invalid change request.", nameof(changeRequestId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var abrv = await connection.QueryFirstOrDefaultAsync<string?>(
                new CommandDefinition(
                    """
                    SELECT m.DepartmentAbrv
                    FROM dbo.TM_ChangeRequest c
                    INNER JOIN dbo.TM_Matrix m ON m.MatrixID = c.MatrixID
                    WHERE c.ChangeRequestID = @Id AND c.IsDeleted = 0
                    """,
                    new { Id = changeRequestId },
                    cancellationToken: cancellationToken))
                ?? throw new ArgumentException("Change request was not found.");

            await EnsureAbrvAsync(abrv, cancellationToken);
        }
    }
}
