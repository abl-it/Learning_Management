using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using Training.Models.DTO;
using Training.Models.DTO.Report;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>Implements <see cref="ITrainingInquiryService"/> using Dapper against stored procedures.</summary>
    public class TrainingInquiryService : ITrainingInquiryService
    {
        private readonly string _connectionString;
        private readonly ILogger<TrainingInquiryService> _logger;

        public TrainingInquiryService(
            IConfiguration configuration,
            ILogger<TrainingInquiryService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SelectOptionDto>> GetCompanyOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<SelectOptionDto>(
                new CommandDefinition(
                    "dbo.USP_TrainingInquiry_GetCompanies",
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return result.AsList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SelectOptionDto>> GetDepartmentOptionsAsync(
            string? coCode,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<SelectOptionDto>(
                new CommandDefinition(
                    "dbo.USP_TrainingInquiry_GetDepartments",
                    new { CoCode = NullIfEmpty(coCode) },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return result.AsList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TrainingInquiryDepartmentDto>> GetByDepartmentAsync(
            TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            await using var connection = new SqlConnection(_connectionString);

            var parameters = BuildFilterParameters(filter);

            try
            {
                var result = await connection.QueryAsync<TrainingInquiryDepartmentDto>(
                    new CommandDefinition(
                        "dbo.USP_TrainingInquiry_GetByDepartment",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken));

                return result.AsList();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry (by department) report.");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TrainingInquiryPersonDto>> GetByPersonAsync(
            TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            await using var connection = new SqlConnection(_connectionString);

            var parameters = BuildFilterParameters(filter);

            try
            {
                var result = await connection.QueryAsync<TrainingInquiryPersonDto>(
                    new CommandDefinition(
                        "dbo.USP_TrainingInquiry_GetByPerson",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken));

                return result.AsList();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry (by person) report.");
                throw;
            }
        }

        private static DynamicParameters BuildFilterParameters(TrainingInquiryFilterDto filter)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@CoCode", NullIfEmpty(filter.CoCode), DbType.String);
            parameters.Add("@ABRV", NullIfEmpty(filter.ABRV), DbType.String);
            parameters.Add("@StartDate", filter.StartDate?.Date, DbType.Date);
            parameters.Add("@EndDate", filter.EndDate?.Date, DbType.Date);

            return parameters;
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
