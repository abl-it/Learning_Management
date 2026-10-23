using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Services;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Dampak perubahan Course Group terhadap matrix yang memakainya. Semua pengguna login boleh melihat;
    /// penerapan hanya untuk role "tc". Pembuatan Change Request tetap melalui approval HRGA Head.
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class GroupImpactController : Controller
    {
        private readonly ICurrentUserService _emp;
        private readonly ITrainingMatrixService _matrixService;
        private readonly IGroupImpactService _impactService;
        private readonly ILogger<GroupImpactController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="GroupImpactController"/>.
        /// </summary>
        public GroupImpactController(
            ICurrentUserService emp,
            ITrainingMatrixService matrixService,
            IGroupImpactService impactService,
            ILogger<GroupImpactController> logger)
        {
            _emp = emp;
            _matrixService = matrixService;
            _impactService = impactService;
            _logger = logger;
        }

        /// <summary>Matrix yang masih kurang course dari group.</summary>
        [HttpGet]
        public Task<IActionResult> GetImpact(int groupId, CancellationToken cancellationToken) =>
            RunAsync(
                async () => (object)await _impactService.GetImpactAsync(groupId, cancellationToken),
                "load group impact",
                cancellationToken);

        /// <summary>Menerapkan penambahan course dari group ke matrix terpilih. Hanya role "tc".</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Apply(
            int groupId,
            List<int> matrixIds,
            string? reason,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () =>
                {
                    var by = _emp.CurrentEmployee?.EmployeeCode;

                    if (string.IsNullOrWhiteSpace(by))
                    {
                        throw new ArgumentException("Current employee could not be determined.");
                    }

                    var username = _emp.CurrentEmployee?.Username;

                    var roles = string.IsNullOrWhiteSpace(username)
                        ? Array.Empty<string>()
                        : (await _matrixService.GetRolesAsync(username, cancellationToken)).ToArray();

                    if (!roles.Any(r => string.Equals(r, "tc", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to apply group changes.");
                    }

                    return (object)await _impactService.ApplyAsync(
                        groupId, matrixIds ?? new List<int>(), reason, by, cancellationToken);
                },
                "apply group impact",
                cancellationToken);

        private async Task<IActionResult> RunAsync(
            Func<Task<object>> work,
            string logAction,
            CancellationToken cancellationToken)
        {
            try
            {
                var data = await work();

                return Json(new { success = true, data });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Json(new { success = false, message = "The request was cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Action}.", logAction);

                return Json(new
                {
                    success = false,
                    message = $"An unexpected error occurred while trying to {logAction}."
                });
            }
        }
    }
}
