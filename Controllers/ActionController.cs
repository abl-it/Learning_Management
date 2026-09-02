using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Models.ViewModels;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class ActionController : Controller
    {
        private readonly IActionService _actionService;
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _emp;
        public ActionController(IActionService actionService, ICurrentUserService emp, ITrainingService trainingService)
        {
            _actionService = actionService;
            _trainingService = trainingService;
            _emp = emp;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PlanningAct([FromBody] ActionVM request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Invalid request payload." });

                if (string.IsNullOrWhiteSpace(request.Remarks))
                    return Json(new { success = false, message = "Remarks is required." });

                if (string.IsNullOrWhiteSpace(request.ActionName))
                    return Json(new { success = false, message = "Action name is required." });

                var role = await _trainingService.GetRoleAsync(_emp.CurrentEmployee.Username);   
                // ✅ Map request to ActionVM
                var act = new ActionVM
                {
                    Id = request.Id,
                    ActionName = request.ActionName.Trim(),
                    Remarks = request.Remarks.Trim(),
                    By = _emp.CurrentEmployee.EmployeeCode ?? "system", 
                    Role = role,
                };

                var result = await _actionService.ActionPlanningAsync(act);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EventAct(
    [FromBody] ActionVM request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid request payload."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ActionName))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Action name is required."
                    });
                }

                var role = await _trainingService.GetRoleAsync(
                    _emp.CurrentEmployee.Username);

                var act = new ActionVM
                {
                    Id = request.Id,

                    ActionName = request.ActionName.Trim(),

                    Remarks = request.Remarks?.Trim() ?? string.Empty,

                    By = _emp.CurrentEmployee.EmployeeCode
                         ?? "system",

                    Role = role
                };

                var result =
                    await _actionService.ActionEventAsync(act);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Server error: " + ex.Message
                });
            }
        }


    }
}
