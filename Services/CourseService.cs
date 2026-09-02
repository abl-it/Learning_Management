using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System;
using System.Data;
using System.Text;
using Training.Models;
using Training.Models.DataTables;
using Training.Services.IServices;
using static Dapper.SqlMapper;

namespace Training.Services
{
    public class CourseService : ICourseService
    {
        private readonly string _connectionString;

        public CourseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }


        public async Task<bool> CourseExistsAsync(string courseCode, int? excludeId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT COUNT(*) FROM Master_Courses WHERE (CategoryCode = @code)";
                var parameters = new DynamicParameters();

                parameters.Add("code", courseCode.Trim().ToUpper());

                if (excludeId.HasValue)
                {
                    sql += " AND CourseId != @excludeId";
                    parameters.Add("excludeId", excludeId.Value);
                }

                var count = await connection.QuerySingleAsync<int>(new CommandDefinition(sql, parameters));
                return count > 0;
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error checking category existence");
                return false;
            }
        }

        public async Task<ApiResponse> CreateCourseAsync(CourseDto courseDto, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();                         

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);                            

            // ✅ DynamicParameters to support OUTPUT param
            var parameters = new DynamicParameters();
            parameters.Add("@courseName", courseDto.CourseName);
            parameters.Add("@description", courseDto.Description);
            parameters.Add("@categoryId", courseDto.CategoryId);
            parameters.Add("@duration", courseDto.DurationHours);
            parameters.Add("@provider", courseDto.TrainingProvider);
            parameters.Add("@createdBy", courseDto.CreatedBy);
            parameters.Add("@isActive", courseDto.IsActive);
            parameters.Add("@newCourseId",
                           dbType: DbType.Int32,
                           direction: ParameterDirection.Output);      

            try
            {
                
                // ✅ ExecuteAsync since result comes from OUTPUT param
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "USP_Master_CreateCourse",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: ct)
                );

                var newId = parameters.Get<int>("@newCourseId");       

                return new ApiResponse
                {
                    Success = true,
                    Message = "Course created successfully.",
                    Data = newId
                };
            }
            catch (Exception ex)
            {
                // ✅ Log the real exception
                //_logger.LogError(ex, "Error creating course for CategoryId {CategoryId}",
                //                 courseDto.CategoryId);
                return new ApiResponse
                {
                    Success = false,
                    Message = "Error creating course."
                };
            }
        }

        public async Task<ApiResponse> DeleteCourseAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Category?> GetCourseByIdAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT * FROM Master_Courses " +
                    "WHERE CourseId = @id";
                return await connection.QueryFirstOrDefaultAsync<Category>(new CommandDefinition(sql, new { id }));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DataTableResponse<Course>> GetCoursesAsync(DataTableRequest request, CancellationToken ct = default)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = "SELECT COUNT(*) FROM dbo.Master_Courses";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql, cancellationToken: ct));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (CourseName LIKE @search OR CourseCode LIKE @search OR Description LIKE @search" +
                    " OR CategoryCode LIKE @search OR TrainingProvider LIKE @search )");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(request.CategoryFilter) && request.CategoryFilter != "All")
            {
                whereClause.Append(" AND CategoryId = @category");
                parameters.Add("@category", request.CategoryFilter);
            }
            // Status filter
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) && request.StatusFilter != "")
            {
                whereClause.Append(" AND IsActive = @isActive");
                parameters.Add("@isActive", request.StatusFilter);
            }
            // Get filtered count
            var filteredSql = $"SELECT COUNT(*) FROM dbo.Master_Courses {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters, cancellationToken: ct));

            // Build ORDER BY clause
            var orderBy = "ORDER BY CourseId ASC";
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
                SELECT   CourseId, CourseCode, CourseName, Description, CategoryId, CategoryCode, DurationHours, ValidityMonths, TrainingProvider, Cost, 
                            PrerequisiteCourseId, IsActive, CreatedDate
                FROM dbo.Master_Courses 
                {whereClause}
                {orderBy}
                OFFSET @offset ROWS 
                FETCH NEXT @length ROWS ONLY";

            var courses = await connection.QueryAsync<Course>(new CommandDefinition(dataSql, parameters, cancellationToken: ct));

            return new DataTableResponse<Course>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = courses.ToList()
            };
        }

        
        private static string GetSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "CourseId",
                1 => "CourseName",
                2 => "CourseCode",
                3 => "Description",
                4 => "CategoryCode",
                5 => "DurationHours",
                6 => "TrainingProvider",

                _ => "CourseId"
            };
        }


        private static async Task<ApiResponse> ValidateCourseAsync(CourseDto courseDto)
        {
            if (string.IsNullOrWhiteSpace(courseDto.CourseName))
            {
                return new ApiResponse { Success = false, Message = "Course name is required" };
            }

            if (courseDto.CourseName.Trim().Length < 5)
            {
                return new ApiResponse { Success = false, Message = "Course name must be at least 5 characters" };
            }


            return new ApiResponse { Success = true };
        }

        public async Task<ApiResponse> UpdateCourseAsync(CourseDto courseDto, CancellationToken ct = default)
        {
            using(var connection = new SqlConnection(_connectionString)) 
            {   
                await connection.OpenAsync();
                var parameters = new
                {
                    courseId = courseDto.CourseId,
                    description = courseDto.Description,
                    duration = courseDto.DurationHours,
                    provider = courseDto.TrainingProvider,
                    isActive = courseDto.IsActive,
                    modifiedBy = courseDto.CreatedBy
                };
                try
                {
                    await connection.ExecuteAsync(new CommandDefinition("USP_Master_UpdateCourse", parameters, cancellationToken: ct));
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Course updated successfully."
                    };
                }
                catch (Exception ex)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Error updating course."
                    };
                }
            }
        }

        public async Task<List<Course>> GetAllCourseAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"SELECT a.*, b.Color, b.CategoryName 
                            FROM Master_Courses a Left Join Master_TrainingCategories b
                                    on a.CategoryId = b.CategoryId 
                            WHERE a.IsActive=1;";
                return (await connection.QueryAsync<Course>(sql)).ToList(); 
            }
            catch (Exception ex)
            {
                throw;
            }
        }


    }
}
