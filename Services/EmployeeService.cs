using Dapper;
using Training.Models;
using Training.Models.DataTables;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Training.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly string _connectionString;

        public EmployeeService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection2")!;
        }

        public async Task<List<Department>> GetDepartmentsAsync(CancellationToken ct = default)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT DISTINCT Department AS [Name] FROM dbo.Employees WHERE Department IS NOT NULL ORDER BY Department";
            var result = await connection.QueryAsync<Department>(new CommandDefinition(sql, cancellationToken: ct));
            return result.ToList();
        }

        public async Task<DataTableResponse<Employee>> GetEmployeesAsync(DataTableRequest request, CancellationToken ct = default)
        {
            using var connection = new SqlConnection(_connectionString);

            // Get total count (unfiltered)
            var totalSql = "SELECT COUNT(*) FROM dbo.Employees";
            var totalRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql, cancellationToken: ct));

            // Build WHERE clause with filters
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                whereClause.Append(" AND (FirstName LIKE @search OR LastName LIKE @search OR Email LIKE @search OR Department LIKE @search)");
                parameters.Add("@search", $"%{request.SearchValue}%");
            }

            // Department filter
            if (!string.IsNullOrWhiteSpace(request.DepartmentFilter) && request.DepartmentFilter != "All")
            {
                whereClause.Append(" AND Department = @department");
                parameters.Add("@department", request.DepartmentFilter);
            }

            // Date range filter
            if (request.HireDateFrom.HasValue)
            {
                whereClause.Append(" AND HireDate >= @hireDateFrom");
                parameters.Add("@hireDateFrom", request.HireDateFrom.Value.Date);
            }

            if (request.HireDateTo.HasValue)
            {
                whereClause.Append(" AND HireDate <= @hireDateTo");
                parameters.Add("@hireDateTo", request.HireDateTo.Value.Date);
            }

            // Salary range filter
            if (request.MinSalary.HasValue)
            {
                whereClause.Append(" AND Salary >= @minSalary");
                parameters.Add("@minSalary", request.MinSalary.Value);
            }

            if (request.MaxSalary.HasValue)
            {
                whereClause.Append(" AND Salary <= @maxSalary");
                parameters.Add("@maxSalary", request.MaxSalary.Value);
            }

            // Get filtered count
            var filteredSql = $"SELECT COUNT(*) FROM dbo.Employees {whereClause}";
            var filteredRecords = await connection.ExecuteScalarAsync<int>(new CommandDefinition(filteredSql, parameters, cancellationToken: ct));

            // Build ORDER BY clause
            var orderBy = "ORDER BY Id ASC";
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
            SELECT Id, FirstName, LastName, Email, Department, HireDate, Salary
            FROM dbo.Employees
            {whereClause}
            {orderBy}
            OFFSET @offset ROWS 
            FETCH NEXT @length ROWS ONLY";

            var employees = await connection.QueryAsync<Employee>(new CommandDefinition(dataSql, parameters, cancellationToken: ct));

            return new DataTableResponse<Employee>
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = employees.ToList()
            };
        }

        private static string GetSortableColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => "Id",
                1 => "FirstName",
                2 => "LastName",
                3 => "Email",
                4 => "Department",
                5 => "HireDate",
                6 => "Salary",
                _ => "Id"
            };
        }



    }
}
