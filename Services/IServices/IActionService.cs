using Training.Models;
using Training.Models.ViewModels;

namespace Training.Services.IServices
{
    public interface IActionService
    {
        Task<ApiResponse> ActionPlanningAsync(ActionVM act);

        Task<List<History>> GetHistoriesAsync(int id, string docType);

    }
}
