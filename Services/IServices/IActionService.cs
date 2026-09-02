using Training.Models;
using Training.Models.ViewModels;

namespace Training.Services.IServices
{
    public interface IActionService
    {
        Task<ApiResponse> ActionPlanningAsync(ActionVM act);

        Task<List<History>> GetHistoriesAsync(int id, string docType);

        /// <summary>
        /// Executes a workflow action for a training event.
        /// </summary>
        /// <param name="act">Training event action request.</param>
        /// <returns>The action result.</returns>
        Task<ApiResponse> ActionEventAsync(ActionVM act);

    }
}
