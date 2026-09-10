using Training.Models;
using Training.Models.DataTables;
using Training.Models.DTO;

namespace Training.Services.IServices
{
    public interface ITrainingService
    {
       
        Employees GetEmployeeDataByUsername(string username);

        Task<List<Company>> GetAllCompaniesAsync();
        Task<List<Department>> GetDepartmentsByCoAsync(string coCode);
        Task<List<Department>> GetDepartmentsABRVAsync(string coCode);

        //Group
        Task<DataTableResponse<Group>> GetGroupsAsync(DataTableRequestMaster request);
        Task<ApiResponse> CreateGroupAsync(Group group);
        Task<ApiResponse> UpdateGroupAsync(Group group);
        Task<ApiResponse> DeleteGroupAsync(int groupId);
        Task<List<Group>> GetAllGroupsAsync();
        Task<List<Unit>> GetUnitByGroupIdAsync(int groupId);

        

        //Unit
        Task<DataTableResponse<Unit>> GetUnitsAsync(DataTableRequestMaster request);
        Task<ApiResponse> CreateUnitAsync(Unit unit);
        Task<ApiResponse> UpdateUnitAsync(Unit unit);
        Task<ApiResponse> DeleteUnitAsync(int unitId);
        Task<List<Unit>> GetAllUnitsAsync();

        //Fiscal Year
        Task<List<FiscalYears>> GetFiscalYearsAsync();

        //Access
        Task<List<Actions>> GetActionsAsync(string workflow, string strategy, int id);
        Task<List<Department>> GetDepartmentsByAccessAsync(string workflow, string employeeCode, string? coCode = null);
        Task<List<Company>> GetCompaniesByAccessAsync(string workflow, string employeeCode);

        Task<string> GetRoleAsync(string username);

        //TypeAhead
        Task<IReadOnlyList<EmployeeLookupDto>> SearchInternalTrainersAsync(
            string? term,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DepartmentLookupModel>> GetDepartmentsAsync(
            string? coCode,
            CancellationToken cancellationToken = default);


        Task<IReadOnlyList<EmployeeLookupDto>> SearchEmployeesAsync(
            string? term,
            CancellationToken cancellationToken = default);
    }
}
