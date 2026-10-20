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
        private readonly IPlanningService _planningService;
        public ActionController(IActionService actionService, ICurrentUserService emp, ITrainingService trainingService, IPlanningService planningService)
        {
            _planningService = planningService;
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

                // Plan yang masih punya New Course (belum ada di Master Course)
                // tidak boleh di-Approve sebelum TC melakukan update course.
                if (string.Equals(request.ActionName.Trim(), "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var pending = await _planningService.CountPendingNewCoursesAsync(request.Id);
                    if (pending > 0)
                        return Json(new
                        {
                            success = false,
                            message = $"Plan tidak dapat di-approve: masih ada {pending} New Course yang belum terdaftar di Master Course. TC harus melakukan update course terlebih dahulu."
                        });
                }

                // [NEW - Step 4] Guard server-side: aturan sama dengan tombol Action di UI (fc_GetDocPermission.CanAction)
                var perm = await _planningService.GetPlansPermission((int)request.Id, _emp.CurrentEmployee.Username);
                if (perm == null || perm.CanAction != 1)
                    return Json(new { success = false, message = "You are not authorized to perform this action on this plan." });

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
