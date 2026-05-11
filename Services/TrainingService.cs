using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;
using Training.Data;
using Training.Models;
using Training.Models.DataTables;
using Training.Services.IServices;

namespace Training.Services
{

    public class TrainingService : ITrainingService
    {
        private readonly string _connectionString;

        public TrainingService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<Company>> GetAllCompaniesAsync()
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT CompanyCode as CoCode, CompanyName FROM home..app_company;";
                var result = await connection.QueryAsync<Company>(
                    query,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }

        public async Task<List<Department>> GetDepartmentsByCoAsync(string coCode)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT DepartmentID, DepartmentCode, DepartmentName FROM home..app_departments WHERE CoCode=@coCode;";
                var parameters = new DynamicParameters();
                parameters.Add("@coCode", coCode);
                var result = await connection.QueryAsync<Department>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }
        public async Task<List<Department>> GetDepartmentsABRVAsync(string coCode)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Distinct ABRV, DeptName FROM home..app_departments WHERE CoCode=@coCode;";
                var parameters = new DynamicParameters();
                parameters.Add("@coCode", coCode);
                var result = await connection.QueryAsync<Department>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }
        public Employees GetEmployeeDataByUsername(string username)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                                SELECT  e.*, u.Username, u.Role as Roles, '' as Role, ad.ABRV, ad.DeptName
                                FROM    home..users u 
                                        JOIN home..app_employees e ON u.EmployeeCode = e.EmployeeCode
                                        LEFT JOIN home..APP_Departments ad ON ad.DepartmentID=e.DepartmentID 
                                WHERE u.Username=@username; ";
                var parameters = new DynamicParameters();
                parameters.Add("@username", username);

                var result = connection.QueryFirstOrDefault<Employees>(
                    query,
                    parameters,
                    commandType: CommandType.Text);

                return result;
            }
        }


        #region Group Management
        //Group Management
        public async Task<DataTableResponse<Group>> GetGroupsAsync(DataTableRequestMaster request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = "SELECT COUNT(*) FROM dbo.Master_Group";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (GroupCode LIKE @search OR Description LIKE @search )");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }
            // Company filter
            if (!string.IsNullOrWhiteSpace(request.CompanyFilter) && request.CompanyFilter != "0")
            {
                whereClause.Append(" AND Master_Group.CoCode = @coCode");
                parameters.Add("@coCode", request.CompanyFilter);
            }
            // Get filtered count
            var filteredSql = $"SELECT COUNT(*) FROM dbo.Master_Group {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY GroupId ASC";
            if (request.Order?.Any() == true)
            {
                var order = request.Order[0];
                var columnName = GetSortableColumnGroup(order.Column);
                var direction = order.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderBy = $"ORDER BY {columnName} {direction}";
            }

            // Add pagination parameters
            parameters.Add("@offset", request.Start);
            parameters.Add("@length", request.Length);

            // Build final data query
            var dataSql = $@"
            SELECT GroupId, GroupCode, Description, CoCode, IsActive
            FROM dbo.Master_Group
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var categories = await connection.QueryAsync<Group>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<Group>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = categories.ToList()
            };
        }

        private static string GetSortableColumnGroup(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "GroupId",
                1 => "GroupCode",
                2 => "Description",
                3 => "IsActive",
                _ => "GroupId"
            };
        }

        public async Task<ApiResponse> CreateGroupAsync(Group group)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO dbo.Master_Group (GroupCode, Description, CoCode, IsActive, CreatedBy, CreatedDate)
                    VALUES (@GroupCode, @Description, @CoCode, @IsActive, @CreatedBy, @CreatedDate);
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                var parameters = new DynamicParameters();
                parameters.Add("@GroupCode", group.GroupCode);
                parameters.Add("@Description", group.Description);
                parameters.Add("@CoCode", group.CoCode);
                parameters.Add("@IsActive", group.IsActive);
                parameters.Add("@CreatedBy", group.CreatedBy);
                parameters.Add("@CreatedDate", group.CreatedDate);
                var newGroupId = connection.QuerySingle<int>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Group created successfully",
                    Data = new { GroupId = newGroupId }
                };
                
            }
        }

        public async Task<ApiResponse> UpdateGroupAsync(Group group)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE dbo.Master_Group
                    SET GroupCode=@GroupCode,
                        Description=@Description,
                        IsActive=@IsActive,
                        CreatedBy=@ModifiedBy,
                        CreatedDate=@ModifiedDate
                    WHERE GroupId=@GroupId;";
                var parameters = new DynamicParameters();
                parameters.Add("@GroupId", group.GroupId);
                parameters.Add("@GroupCode", group.GroupCode);
                parameters.Add("@Description", group.Description);
                parameters.Add("@IsActive", group.IsActive);
                parameters.Add("@ModifiedBy", group.CreatedBy);
                parameters.Add("@ModifiedDate", group.CreatedDate);
                var rowsAffected = await connection.ExecuteAsync(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                if (rowsAffected > 0)
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Group updated successfully."
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Group not found."
                    };
                }
            }
        }

        public async Task<ApiResponse> DeleteGroupAsync(int groupId)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "DELETE FROM dbo.Master_Group WHERE GroupId=@groupId;";
                var parameters = new DynamicParameters();
                parameters.Add("@groupId", groupId);
                var rowsAffected = await connection.ExecuteAsync(
                    query,
                    parameters,
                    commandType: CommandType.Text);

                if (rowsAffected > 0)
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Group deleted successfully."
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Group not found."
                    };
                }
            }
        }

        public async Task<List<Unit>> GetUnitByGroupIdAsync(int groupId)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM dbo.Master_Unit WHERE GroupId=@groupId;";
                var parameters = new DynamicParameters();
                parameters.Add("@groupId", groupId);
                var result = await connection.QueryAsync<Unit>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }
        public async Task<List<Group>> GetAllGroupsAsync()
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT GroupId, GroupCode, Description, CoCode, IsActive FROM dbo.Master_Group;";
                var result = await connection.QueryAsync<Group>(
                    query,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }

       

        #endregion

        #region Unit Management
        public async Task<DataTableResponse<Unit>> GetUnitsAsync(DataTableRequestMaster request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = @"SELECT COUNT(*) FROM dbo.Master_Unit
                                LEFT JOIN Master_Group  ON Master_Unit.GroupId = Master_Group.GroupId
                            ";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (UnitCode LIKE @search OR UnitName LIKE @search  )");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }
            // Company filter
            if (!string.IsNullOrWhiteSpace(request.CompanyFilter) )
            {
                whereClause.Append(" AND CoCode = @coCode");
                parameters.Add("@coCode", request.CompanyFilter);
            }
            // Group filter
            if (!string.IsNullOrWhiteSpace(request.GroupFilter) && request.GroupFilter != "0")
            {
                whereClause.Append(" AND Master_Unit.GroupId = @groupId");
                parameters.Add("@groupId", request.GroupFilter);
            }
            // Get filtered count
            var filteredSql = $"{totalSql} {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters));

            // Build ORDER BY clause
            var orderBy = "ORDER BY UnitId ASC";
            if (request.Order?.Any() == true)
            {
                var order = request.Order[0];
                var columnName = GetSortableColumnUnit(order.Column);
                var direction = order.Dir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderBy = $"ORDER BY {columnName} {direction}";
            }

            // Add pagination parameters
            parameters.Add("@offset", request.Start);
            parameters.Add("@length", request.Length);

            // Build final data query
            var dataSql = $@"
            SELECT Master_Unit.*, GroupCode, Master_Group.GroupId, Master_Group.CoCode AS CoCode 
                    FROM dbo.Master_Unit LEFT JOIN Master_Group  ON Master_Unit.GroupId = Master_Group.GroupId
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var datas = await connection.QueryAsync<Unit>(new CommandDefinition(dataSql, parameters));

            return new DataTableResponse<Unit>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = datas.ToList()
            };
        }
        private static string GetSortableColumnUnit(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "UnitId",
                1 => "UnitCode",
                2 => "UnitName",
                3 => "Description",
                4 => "IsActive",
                _ => "unitId"
            };
        }
        public async Task<ApiResponse> CreateUnitAsync(Unit unit)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO Master_Unit (UnitCode, UnitName, GroupId, CreatedBy, CreatedDate)
                    VALUES (@unitCode, @unitName, @groupId, @createdBy, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                var parameters = new DynamicParameters();
                parameters.Add("@unitCode", unit.UnitCode);
                parameters.Add("@unitName", unit.UnitName);
                parameters.Add("@groupId", unit.GroupId);
                parameters.Add("@createdBy", unit.CreatedBy);
                var newUnitId = connection.QuerySingle<int>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return new ApiResponse
                {
                    Success = true,
                    Message = "Unit created successfully",
                    Data = new { UnitId = newUnitId }
                };

            }
        }

        public async Task<ApiResponse> UpdateUnitAsync(Unit unit)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE Master_Unit
                    SET UnitCode=@unitCode,
                        UnitName=@unitName,
                        GroupId=@groupId,
                        CreatedBy=@modifiedBy,
                        CreatedDate=GETDATE()
                    WHERE UnitId=@unitId;";
                var parameters = new DynamicParameters();
                parameters.Add("@unitId", unit.UnitId);
                parameters.Add("@unitCode", unit.UnitCode);
                parameters.Add("@unitName", unit.UnitName);
                parameters.Add("@groupId", unit.GroupId);
                parameters.Add("@modifiedBy", unit.CreatedBy);
                var rowsAffected = await connection.ExecuteAsync(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                if (rowsAffected > 0)
                {
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Unit updated successfully."
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Unit not found."
                    };
                }
            }
        }

        public Task<ApiResponse> DeleteUnitAsync(int unitId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"SELECT Master_Unit.*, GroupCode, Master_Group.GroupId, CoCode 
                                FROM dbo.Master_Unit LEFT JOIN Master_Group  ON Master_Unit.GroupId = Master_Group.GroupId";
                var result = await connection.QueryAsync<Unit>(
                    query,
                    commandType: CommandType.Text);
                return result.ToList();
            }  
        }

       




        #endregion

        public async Task<List<FiscalYears>> GetFiscalYearsAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT FiscalYearID, Year, StartDate, EndDate, Status FROM dbo.FiscalYears; ";
                //var parameters = new DynamicParameters();
                //parameters.Add("@status", status);
                var result = await connection.QueryAsync<FiscalYears>(
                    query,
                    //parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }

        public async Task<List<Actions>> GetActionsAsync(string workflow, string strategy, int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"  SELECT a.Action as ActionName,
                                     a.ActionCode,
                                    dbo.fc_CanAction(@workflow,@strategy, @id) AS ActionAllowed 
                                FROM dbo.fc_Action(@workflow, @strategy, @id) a;";
                var parameters = new DynamicParameters();
                parameters.Add("@workflow", workflow);
                parameters.Add("@strategy", strategy);
                parameters.Add("@id", id);
                var result = await connection.QueryAsync<Actions>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }

        public async Task<List<Department>> GetDepartmentsByAccessAsync(string workflow, string employeeCode)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"SELECT *                               
                                FROM dbo.fc_GetDepartmentsByAccess(@employeeCode, @docType)
                                Order By DeptName;";
                var parameters = new DynamicParameters();
                parameters.Add("@employeeCode", employeeCode);
                parameters.Add("@docType", workflow);
                var result = await connection.QueryAsync<Department>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }

        public async Task<List<Company>> GetCompaniesByAccessAsync(string workflow, string employeeCode)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                                SELECT DISTINCT 
                                    ac.CompanyCode AS CoCode
                                   ,ac.CompanyName AS CompanyName   
                                   ,ac.IsActive AS IsActive
                                FROM home..APP_Company ac JOIN dbo.fc_GetDepartmentsByAccess(@employeeCode,@docType) d 
                                ON ac.CompanyCode=d.CoCode;
                            ";
                var parameters = new DynamicParameters();
                parameters.Add("@employeeCode", employeeCode);
                parameters.Add("@docType", workflow);
                var result = await connection.QueryAsync<Company>(
                    query,
                    parameters,
                    commandType: CommandType.Text);
                return result.ToList();
            }
        }


    }
}
