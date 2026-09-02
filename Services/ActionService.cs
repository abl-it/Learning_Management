using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using Training.Models;
using Training.Models.ViewModels;
using Training.Services.IServices;
using static System.Net.Mime.MediaTypeNames;

namespace Training.Services
{
    public class ActionService : IActionService
    {
        private readonly string _connectionString;
        public ActionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ApiResponse> ActionPlanningAsync(ActionVM act)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@id", act.Id, DbType.Int32);
                parameters.Add("@actionName", act.ActionName, DbType.String);
                parameters.Add("@remarks", act.Remarks, DbType.String);
                parameters.Add("@by", act.By, DbType.String);
                parameters.Add("@doc", "PLAN", DbType.String);
                parameters.Add("@role", act.Role, DbType.String);
                var result = await connection.ExecuteScalarAsync<int>(
                    "USP_Approve",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Action successfully",
                    Data = new { Result = result }
                };
            }
        }

        public async Task<List<History>> GetHistoriesAsync(int id, string docType)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@id", id, DbType.Int32);
                parameters.Add("@docType", docType, DbType.String);

                var result = await connection.QueryAsync<History>(
                    "USP_GetHistory",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
        }

        public async Task<ApiResponse> ActionEventAsync(ActionVM act)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();

                parameters.Add(
                    "@id",
                    act.Id,
                    DbType.Int64);

                parameters.Add(
                    "@actionName",
                    act.ActionName,
                    DbType.String);

                parameters.Add(
                    "@remarks",
                    act.Remarks ?? string.Empty,
                    DbType.String);

                parameters.Add(
                    "@by",
                    act.By,
                    DbType.String);

                parameters.Add(
                    "@doc",
                    "EVENT",
                    DbType.String);

                parameters.Add(
                    "@role",
                    act.Role,
                    DbType.String);

                var result = await connection.ExecuteScalarAsync<int>(
                    "USP_Approve",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return new ApiResponse
                {
                    Success = result > 0,
                    Message = $"USP_Approve result = {result}",
                    Data = new
                    {
                        Result = result
                    }
                };
                //return new ApiResponse
                //{
                //    Success = result > 0,
                //    Message = result switch
                //    {
                //        > 0 => "Action successfully.",
                //        -1 => "Training event not found.",
                //        -2 => "Workflow transition is not configured.",
                //        -3 => "Document status is not configured.",
                //        _ => "Action was not executed."
                //    },
                //    Data = new
                //    {
                //        Result = result
                //    }
                //};
            }
        }

    }
}
