using Training.Models;
using Training.Models.DataTables;
using Training.Models.DTO;
using Training.Models.ViewModels;

namespace Training.Services.IServices
{
    public interface IPlanningService
    {
        //Needs
        Task<DataTableResponse<NeedDisp>> GetNeedsAsync(DataTableRequest request);
        Task<ApiResponse> CreateNeedAsync(Needs needs);
        Task<ApiResponse> UpdateNeedAsync(Needs needs);
        Task<ApiResponse> DeleteNeedAsync(int needId);
        Task<List<Needs>> GetAllNeedsAsync();

       //Yearly Plans
        Task<DataTableResponse<PlanDisp>> GetPlansAsync(DataTableRequest request);

        Task<Plans> GetPlansByDeptAsync(string co, string dept, int year); 
        Task<Plans> GetPlansByIdAsync(int id);
        //Task<ApiResponse> CreatePlanAsync(Plans plan);
        //Task<ApiResponse> UpdatePlanAsync(Plans plan);
        //Task<ApiResponse> DeletePlanAsync(int planId);
        //Task<List<Plans>> GetAllPlansAsync();
        //ViewModels
        //Task<NeedVM> GetNeedVMAsync();
        //Task<PlanVM> GetPlanVMAsync();

        //Yearly Plan Detail
        Task<DataTableResponse<PlanDetailView>> GetPlansDetailDtAsync(DtPlansDetailRequest request);
        Task<List<PlanDetailView>> GetPlansDetailAsync(int id);
        Task<List<PlanDetailView>> GetPlansDetailByDeptAsync(string dept, int year);
        Task<int> AddPlansDetail(PlanDetailDTO planDetail);
        Task<int> DeletePlansDetail(int id, string by);
    }
}
