using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using Training.Models;
using Training.Models.DataTables;
using Training.Models.DTO;
using Training.Models.ViewModels;
using Training.Services.IServices;

namespace Training.Services
{
    public class PlanningService : IPlanningService
    {
        private readonly string _connectionString;
        public PlanningService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }


        #region Needs
        public async Task<DataTableResponse<NeedDisp>> GetNeedsAsync(DataTableRequest request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = @"SELECT Count(*) 
                     FROM dbo.v_Training_Needs 
                    ";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (CourseCode LIKE @search OR CourseName LIKE @search ");
                whereClause.Append(" OR CategoryName LIKE @search  )");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }
            // Company filter
            if (!string.IsNullOrWhiteSpace(request.CompanyFilter) && request.CompanyFilter != "All")
            {
                whereClause.Append(" AND CoCode = @coCode");
                parameters.Add("@coCode", request.CompanyFilter);
            }
            // Group filter
            if (!string.IsNullOrWhiteSpace(request.GroupFilter) && request.GroupFilter != "All")
            {
                whereClause.Append(" AND GroupId = @groupId");
                parameters.Add("@groupId", request.GroupFilter);
            }
            // Unit filter
            if (!string.IsNullOrWhiteSpace(request.UnitFilter) && request.UnitFilter != "All")
            {
                whereClause.Append(" AND UnitId = @unitId");
                parameters.Add("@unitId", request.UnitFilter);
            }
            // Category filter
            if (!string.IsNullOrWhiteSpace(request.CategoryFilter) && request.CategoryFilter != "All")
            {
                whereClause.Append(" AND CategoryId = @categoryId");
                parameters.Add("@categoryId", request.CategoryFilter);
            }
            // Status filter
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) && request.StatusFilter != "All")
            {
                whereClause.Append(" AND IsActive = @isActive");
                parameters.Add("@isActive", request.StatusFilter);
            }
            // Get filtered count
            var filteredSql = $"{totalSql} {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY NeedId ASC";
            if (request.Order?.Any() == true)
            {
                var order = request.Order[0];
                var columnName = GetSortableColumn(order.Column);
                var direction = order.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderBy = $"ORDER BY {columnName} {direction}";
            }

            // Add pagination parameters
            parameters.Add("@offset", request.Start);
            parameters.Add("@length", request.Length);

            // Build final data query
            var dataSql = $@"
            SELECT *
            FROM dbo.v_Training_Needs  
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var needs = await connection.QueryAsync<NeedDisp>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<NeedDisp>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = needs.ToList()
            };
        }

        private static string GetSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "NeedId",
                1 => "GroupCode",
                2 => "UnitCode",
                3 => "CourseName",
                4 => "CategoryName",
                5 => "IsActive",
                _ => "NeedId"
            };
        }
        public async Task<ApiResponse> CreateNeedAsync(Needs needs)
        {
            using(var connection = new SqlConnection(_connectionString))
            {

                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@coCode", needs.CoCode);
                parameters.Add("@GroupId", needs.GroupId);
                parameters.Add("@UnitId", needs.UnitId);
                parameters.Add("@CourseId", needs.CourseId);
                parameters.Add("@CreatedBy", needs.CreatedBy);
                var newId = await connection.ExecuteAsync("USP_Training_CreateNeeds", parameters, commandType: System.Data.CommandType.StoredProcedure);
                
                return new ApiResponse
                {
                    Success = true,
                    Message = "Training Needs created successfully.",
                    Data = newId
                };
            }
        }

        public Task<ApiResponse> DeleteNeedAsync(int needId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Needs>> GetAllNeedsAsync()
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM dbo.Training_Needs WHERE IsActive=1";
                var needs = await connection.QueryAsync<Needs>(new CommandDefinition(sql));
                return needs.ToList();
            }        
                
        }


        

        public async Task<ApiResponse> UpdateNeedAsync(Needs needs)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@needId", needs.NeedId);
                    parameters.Add("@isActive", needs.IsActive);
                    parameters.Add("@by", needs.CreatedBy);

                    var rowsAffected = await connection.ExecuteAsync("USP_Training_UpdateNeeds", parameters, commandType: System.Data.CommandType.StoredProcedure);

                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Training Needs updated successfully.",
                        Data = rowsAffected
                    };
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        
        #endregion

        #region Plans
        public async Task<DataTableResponse<PlanDisp>> GetPlansAsync(DataTableRequest request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = @"SELECT Count(*) FROM dbo.v_Training_Plans 
                    ";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            //if (!string.IsNullOrWhiteSpace(request.SearchValue))
            //{
            //    whereClause.Append(" AND (CourseCode LIKE @search OR CourseName LIKE @search ");
            //    //whereClause.Append(" OR CategoryName LIKE @search  )");
            //    parameters.Add("@search", $"%{request.SearchValue}%");
            //}
            // Company filter
            if (!string.IsNullOrWhiteSpace(request.CompanyFilter))
            {
                whereClause.Append(" AND CoCode = @coCode");
                parameters.Add("@coCode", request.CompanyFilter);
            }
            // Year filter
            if (!string.IsNullOrWhiteSpace(request.YearFilter) )
            {
                whereClause.Append(" AND Year = @yearId");
                parameters.Add("@yearId", request.YearFilter);
            }
            // Department filter
            if (!string.IsNullOrWhiteSpace(request.DepartmentFilter) && request.DepartmentFilter != "All")
            {
                whereClause.Append(" AND ABRV = @abrv");
                parameters.Add("@abrv", request.DepartmentFilter);
            }
            // Status filter
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) && request.StatusFilter != "All")
            {
                whereClause.Append(" AND (PlanStatus = @planStatus OR @planStatus = 'All')");
                parameters.Add("@planStatus", request.StatusFilter);
            }
            // Get filtered count
            var filteredSql = $"{totalSql} {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY PlanId ASC";
            if (request.Order?.Any() == true)
            {
                var order = request.Order[0];
                var columnName = GetPlanSortableColumn(order.Column);
                var direction = order.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderBy = $"ORDER BY {columnName} {direction}";
            }

            // Add pagination parameters
            parameters.Add("@offset", request.Start);
            parameters.Add("@length", request.Length);

            // Build final data query
            var dataSql = $@"
            SELECT *
            FROM dbo.v_Training_Plans  
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var plans = await connection.QueryAsync<PlanDisp>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<PlanDisp>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = plans.ToList()
            };
        }
        private static string GetPlanSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "PlanId",
                1 => "DeptName",
                2 => "PlanStatus",
                3 => "PlansNbr",
                _ => "PlanId"
            };
        }

        public async Task<DataTableResponse<PlanDetailView>> GetPlansDetailDtAsync(DtPlansDetailRequest request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var planId = Convert.ToInt32(request.PlanId);
            var totalSql = @"SELECT Count(*) FROM dbo.Training_YearlyPlansDetail ";
            
            
            //var parameters = new DynamicParameters();
            //parameters.Add("@planId", planId);

            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();
            parameters.Add("@planId", planId);

            whereClause.Append(" AND PlanId = @planId ");
            // Global search
            //if (!string.IsNullOrWhiteSpace(request.SearchValue))
            //{
            //    whereClause.Append(" AND (CourseCode LIKE @search OR CourseName LIKE @search ");
            //    //whereClause.Append(" OR CategoryName LIKE @search  )");
            //    parameters.Add("@search", $"%{request.SearchValue}%");
            //}
            
            // Get filtered count
            var filteredSql = $"{totalSql} {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY PlannedMonth ";
            if (request.Order?.Any() == true)
            {
                var order = request.Order[0];
                var columnName = GetPlanDetailSortableColumn(order.Column);
                var direction = order.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderBy = $"ORDER BY {columnName} {direction}";
            }

            // Add pagination parameters
            parameters.Add("@offset", request.Start);
            parameters.Add("@length", request.Length);

            // Build final data query
            var dataSql = $@"
                    Select * 
                    From dbo.Training_YearlyPlansDetail 
                    {whereClause}
                    {orderBy}
                    OFFSET @offset ROWS 
                    FETCH NEXT @length ROWS ONLY";

            var plansDet = await connection.QueryAsync<PlanDetailView>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<PlanDetailView>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = plansDet.ToList()
            };
        }
        private static string GetPlanDetailSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {

                0 => "PlannedMonth",      // MONTH
                1 => "CourseCode",        // CODE
                2 => "CourseName",        // COURSE
                3 => "CategoryCode",      // ✅ Fix: Category → CategoryCode
                4 => "TotalSessions",     // ✅ Fix: TotalSession → TotalSessions
                5 => "TotalParticipants", // #PART
                6 => "PlannedDuration",   // DUR
                7 => "TrainingProvider",  // PROV
                8 => "TargetParticipant", // ✅ Tambah: TARGET
                _ => "PlannedMonth"       // default
            };
        }

        public async Task<List<PlanDetailView>> GetPlansDetailAsync(int id)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM dbo.Training_YearlyPlansDetail WHERE PlanId = @planId Order by PlannedMonth";
                var parameters = new DynamicParameters();
                parameters.Add("@planId", id);
                var details = await connection.QueryAsync<PlanDetailView>(new CommandDefinition(sql, parameters));
                return details.ToList();
            }

        }

        public async Task<List<PlanDetailView>> GetPlansDetailByDeptAsync(string dept, int year)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM dbo.v_Training_PlanDetails WHERE Year = @year and ABRV = @dept";
                var parameters = new DynamicParameters();
                parameters.Add("@year", year);
                parameters.Add("@dept", dept);
                var details = await connection.QueryAsync<PlanDetailView>(new CommandDefinition(sql, parameters));
                return details.ToList();
            }
        }

        public async Task<Plans> GetPlansByDeptAsync(string co, string dept, int year)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM dbo.v_Training_Plans WHERE Year = @year and ABRV = @dept and cocode = @co;";
                var parameters = new DynamicParameters();
                parameters.Add("@year", year);
                parameters.Add("@dept", dept);
                parameters.Add("@co", co);
                var plan = await connection.QueryFirstOrDefaultAsync<Plans>(sql,
                                                                       parameters,
                                                                       commandType: CommandType.Text);
                return plan;
            }
        }

        public async Task<Plans> GetPlansByIdAsync(int id)
        {
            using(var connection = new SqlConnection(_connectionString)) 
            {
                var sql = @"SELECT * FROM dbo.v_Training_Plans WHERE PlanId = @id;";
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                var plan = await connection.QueryFirstOrDefaultAsync<Plans>(sql,
                                                                       parameters,
                                                                       commandType: CommandType.Text);
                return plan;
            }
        }

        public async Task<int> AddPlansDetail(PlanDetailDTO planDetail)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@PlanId", planDetail.PlanId);
                parameters.Add("@PlanCode", planDetail.PlanCode);
             //   parameters.Add("@PlanTitle", planDetail.PlanTitle);
                parameters.Add("@CourseId", planDetail.CourseId);
                parameters.Add("@CourseCode", planDetail.CourseCode);
                parameters.Add("@CourseName", planDetail.CourseName);
                parameters.Add("@CategoryId", planDetail.CategoryId);
                parameters.Add("@CategoryCode", planDetail.CategoryCode);
                parameters.Add("@TotalSessions", planDetail.TotalSessions);
                parameters.Add("@TotalParticipants", planDetail.TotalParticipants);
                parameters.Add("@TargetParticipant", planDetail.TargetParticipant);
                parameters.Add("@PlannedMonth", planDetail.PlannedMonth);
                parameters.Add("@PlannedYear", planDetail.PlannedYear);
                parameters.Add("@PlannedDuration", planDetail.PlannedDuration);
                parameters.Add("@TrainingProvider", planDetail.TrainingProvider);
                parameters.Add("@EstimatedCost", planDetail.EstimatedCost);
                parameters.Add("@Notes", planDetail.Notes);
                parameters.Add("@CreatedBy", planDetail.CreatedBy);
                parameters.Add("@IsNewCourse", planDetail.IsNewCourse);

                var newId = await connection.QueryFirstOrDefaultAsync<int>("USP_Training_AddTrainingYearlyPlansDetail", 
                                                          parameters, 
                                                          commandType: CommandType.StoredProcedure);
                return newId;
            }
        }

        public async Task<int> DeletePlansDetail(int id, string by)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@by", by);

                var result = await connection.QueryFirstOrDefaultAsync<int>("USP_Training_DeleteTrainingYearlyPlansDetail", 
                                                          parameters, 
                                                          commandType: CommandType.StoredProcedure);
                return result;
            }
        }
        #endregion
    }
}
