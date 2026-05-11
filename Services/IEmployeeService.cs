using Training.Models;
using Training.Models.DataTables;

namespace Training.Services
{
    public interface IEmployeeService
    {
        Task<DataTableResponse<Employee>> GetEmployeesAsync(DataTableRequest request, CancellationToken ct = default);
        Task<List<Department>> GetDepartmentsAsync(CancellationToken ct = default);
    }
}
