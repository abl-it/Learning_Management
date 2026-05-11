using Dapper;
using Microsoft.Data.SqlClient;
using Training.Services.IServices;

namespace Training.Services
{
    public class InitialService : IInitialService
    {
        private readonly string _connectionString;
        public InitialService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }


        public async Task<int> InitYearlyPlanAsync(int year, string employeeCode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@Year", year, System.Data.DbType.Int32);
                parameters.Add("@CurrentUser", employeeCode, System.Data.DbType.String);

                var result = await connection.ExecuteScalarAsync<int>(
                    "USP_Training_BulkInsertYearlyPlanByDepartment_v2", 
                    parameters, 
                    commandType: System.Data.CommandType.StoredProcedure
                    );

                return result;

            }
        }


    }
}
