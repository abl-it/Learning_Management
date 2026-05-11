using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;
using Training.Models;
using Training.Models.DataTables;
using Training.Services.IServices;

using System.Data;

namespace Training.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly string _connectionString;

        public CategoryService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<DataTableResponse<Category>> GetCategoriesAsync(DataTableRequest request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = "SELECT COUNT(*) FROM dbo.Master_TrainingCategories";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (CategoryName LIKE @search OR CategoryCode LIKE @search OR Description LIKE @search )");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }

            // Get filtered count
            var filteredSql = $"SELECT COUNT(*) FROM dbo.Master_TrainingCategories {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY CategoryId ASC";
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
            SELECT CategoryId, CategoryName, CategoryCode, Description, Color, IsActive
            FROM dbo.Master_TrainingCategories
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var categories = await connection.QueryAsync<Category>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<Category>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = categories.ToList()
            };
        }

        private static string GetSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "CategoryId",
                1 => "CategoryName",
                2 => "CategoryCode",
                3 => "Description",
                _ => "CategoryId"
            };
        }

        

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT CategoryId, CategoryName, CategoryCode, Description, IsActive, CreatedDate FROM Master_TrainingCategories " +
                    "WHERE CategoryId = @id";
                return await connection.QueryFirstOrDefaultAsync<Category>(new CommandDefinition(sql, new { id }));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ApiResponse> CreateCategoryAsync(CategoryDto categoryDto)
        {
            try
            {
                // Validation
                var validationResult = await ValidateCategoryAsync(categoryDto);
                if (!validationResult.Success)
                    return validationResult;

                // Check for duplicates
                if (await CategoryExistsAsync(categoryDto.CategoryName, categoryDto.CategoryCode, null))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Category with this name or code already exists"
                    };
                }

                
                using var connection = new SqlConnection(_connectionString);
             
                var parameters = new
                {
                    CategoryName = categoryDto.CategoryName.Trim().ToUpper(),
                    CategoryCode = categoryDto.CategoryCode.Trim().ToUpper(),
                    Description = categoryDto.Description?.Trim(),
                    IsActive = categoryDto.IsActive,
                    CreatedBy = categoryDto.CreatedBy.ToString().Trim()
                };

                var newId = await connection.QuerySingleAsync<int>("USP_Master_CreateCategory", parameters, commandType: CommandType.StoredProcedure);

                
                return new ApiResponse
                {
                    Success = true,
                    Message = "Category created successfully",
                    Data = new { CategoryId = newId }
                };
            }
            catch (Exception ex)
            {
            
                return new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the category"
                };
            }
        }

        public async Task<ApiResponse> UpdateCategoryAsync(CategoryDto categoryDto)
        {
            try
            {
                // Validation
                var validationResult = await ValidateCategoryAsync(categoryDto);
                if (!validationResult.Success)
                    return validationResult;

                // Check if category exists
                var existingCategory = await GetCategoryByIdAsync(categoryDto.CategoryId);
                if (existingCategory == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Category not found"
                    };
                }

                // Check for duplicates (excluding current category)
                if (await CategoryExistsAsync(categoryDto.CategoryName, categoryDto.CategoryCode, categoryDto.CategoryId))
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Category with this name or code already exists"
                    };
                }

                using var connection = new SqlConnection(_connectionString);
                var sql = @"
                    UPDATE Master_TrainingCategories 
                    SET CategoryName = @CategoryName,
                        CategoryCode = @CategoryCode,
                        Description = @Description,
                        IsActive = @IsActive,
                        CreatedDate = GETDATE(),
                        CreatedBy = @CreatedBy
                    WHERE CategoryId = @CategoryId";

                var parameters = new
                {
                    CategoryId = categoryDto.CategoryId,
                    CategoryName = categoryDto.CategoryName.Trim().ToUpper(),
                    CategoryCode = categoryDto.CategoryCode.Trim().ToUpper(),
                    Description = categoryDto.Description?.Trim(),
                    IsActive = categoryDto.IsActive,
                    CreatedBy = categoryDto.CreatedBy.Trim()
                };

                var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(sql, parameters));

                if (rowsAffected == 0)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Category not found or no changes made"
                    };
                }

               
                return new ApiResponse
                {
                    Success = true,
                    Message = "Category updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating the category"
                };
            }
        }

        public async Task<ApiResponse> DeleteCategoryAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Check if category exists
                var checkSql = "SELECT COUNT(*) FROM Master_TrainingCategories WHERE CategoryId = @id";
                var exists = await connection.QuerySingleAsync<int>(new CommandDefinition(checkSql, new { id }));

                if (exists == 0)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Category not found"
                    };
                }

                var deleteSql = "DELETE FROM Categories WHERE CategoryId = @id";
                var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(deleteSql, new { id }));

                return new ApiResponse
                {
                    Success = true,
                    Message = "Category deleted successfully"
                };
            }
            catch (Exception ex)
            {
               
                return new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while deleting the category"
                };
            }
        }

        public async Task<bool> CategoryExistsAsync(string categoryName, string categoryCode, int? excludeId = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT COUNT(*) FROM Master_TrainingCategories WHERE (CategoryName = @name OR CategoryCode = @code)";
                var parameters = new DynamicParameters();

                parameters.Add("name", categoryName.Trim());
                parameters.Add("code", categoryCode.Trim().ToUpper());

                if (excludeId.HasValue)
                {
                    sql += " AND CategoryId != @excludeId";
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

        private static async Task<ApiResponse> ValidateCategoryAsync(CategoryDto categoryDto)
        {
            if (string.IsNullOrWhiteSpace(categoryDto.CategoryName))
            {
                return new ApiResponse { Success = false, Message = "Category name is required" };
            }

            if (string.IsNullOrWhiteSpace(categoryDto.CategoryCode))
            {
                return new ApiResponse { Success = false, Message = "Category code is required" };
            }

            if (categoryDto.CategoryName.Trim().Length < 3)
            {
                return new ApiResponse { Success = false, Message = "Category name must be at least 3 characters" };
            }

            if (categoryDto.CategoryCode.Trim().Length != 3)
            {
                return new ApiResponse { Success = false, Message = "Category code must be 3 characters" };
            }

            return new ApiResponse { Success = true };
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            try
            {
                using(var connection = new SqlConnection(_connectionString)) 
                { 
                    await connection.OpenAsync();
                    var query = @"Select * from Master_TrainingCategories where IsActive = 1 ;";

                    var result = await connection.QueryAsync<Category>(query,commandType: CommandType.Text);

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }



    }

}
