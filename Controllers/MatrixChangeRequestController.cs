using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Models.DTO.TrainingMatrix;
using Training.Services;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Change Request atas Training Matrix yang sudah Approved.
    /// Semua pengguna login boleh melihat. Buat/ubah/hapus hanya role "tc";
    /// submit oleh "tc", approve/reject oleh role pada dbo.Workflow (hrhead).
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class MatrixChangeRequestController : Controller
    {
        private readonly ICurrentUserService _emp;
        private readonly ITrainingMatrixService _matrixService;
        private readonly IMatrixChangeRequestService _changeService;
        private readonly ILogger<MatrixChangeRequestController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="MatrixChangeRequestController"/>.
        /// </summary>
        public MatrixChangeRequestController(
            ICurrentUserService emp,
            ITrainingMatrixService matrixService,
            IMatrixChangeRequestService changeService,
            ILogger<MatrixChangeRequestController> logger)
        {
            _emp = emp;
            _matrixService = matrixService;
            _changeService = changeService;
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // Page + read
        // ---------------------------------------------------------------

        /// <summary>Halaman Change Request.</summary>
        [HttpGet]
        public IActionResult Detail(int id)
        {
            ViewData["ChangeRequestId"] = id;

            return View();
        }

        /// <summary>Change Request lengkap beserta isi matrix saat ini.</summary>
        [HttpGet]
        public Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken) =>
            RunAsync(
                async () =>
                {
                    var roles = await GetRolesAsync(cancellationToken);
                    var detail = await _changeService.GetDetailAsync(id, roles, cancellationToken);

                    return detail ?? throw new ArgumentException("Change request was not found.");
                },
                "load change request",
                cancellationToken);

        // ---------------------------------------------------------------
        // Write (role tc)
        // ---------------------------------------------------------------

        /// <summary>Membuat Change Request untuk matrix Approved.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Create(
            int matrixId,
            string? reason,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    var id = await _changeService.CreateAsync(matrixId, reason, by, cancellationToken);

                    return (object)new { changeRequestId = id };
                },
                "Change request created.",
                "create change request",
                cancellationToken);

        /// <summary>Menambahkan perubahan Add.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AddCourses(
            [FromForm] ChangeRequestApplyDto request,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    var result = await _changeService.AddCoursesAsync(
                        request ?? throw new ArgumentException("Invalid request payload."),
                        by,
                        cancellationToken);

                    return (object)result;
                },
                "Courses added to change request.",
                "add courses to change request",
                cancellationToken);

        /// <summary>Menandai baris matrix untuk dihapus (Remove).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkRemove(
            int id,
            List<int> matrixDetailIds,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    var n = await _changeService.MarkRemoveAsync(id, matrixDetailIds, by, cancellationToken);

                    return (object)new { marked = n };
                },
                "Rows marked for removal.",
                "mark rows for removal",
                cancellationToken);

        /// <summary>Membatalkan perubahan.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> UndoChanges(
            int id,
            List<int> changeDetailIds,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async _ =>
                {
                    var n = await _changeService.UndoChangesAsync(id, changeDetailIds, cancellationToken);

                    return (object)new { undone = n };
                },
                "Changes removed.",
                "undo change request changes",
                cancellationToken);

        /// <summary>Menghapus Change Request (Draft/Rejected).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    await _changeService.DeleteAsync(id, by, cancellationToken);

                    return (object)new { };
                },
                "Change request deleted.",
                "delete change request",
                cancellationToken);

        // ---------------------------------------------------------------
        // Workflow (role diperiksa service terhadap dbo.Workflow.Allowed)
        // ---------------------------------------------------------------

        /// <summary>Request for Approve, Approve, atau Reject.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Transition(
            int id,
            [FromForm(Name = "workflowAction")] string action,
            string? remarks,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () =>
                {
                    var by = _emp.CurrentEmployee?.EmployeeCode;

                    if (string.IsNullOrWhiteSpace(by))
                    {
                        throw new ArgumentException("Current employee could not be determined.");
                    }

                    var roles = await GetRolesAsync(cancellationToken);

                    var status = await _changeService.TransitionAsync(
                        id, action, remarks, by, roles, cancellationToken);

                    return new { status };
                },
                "run change request workflow action",
                cancellationToken,
                successMessage: "Done.");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken)
        {
            var username = _emp.CurrentEmployee?.Username;

            return string.IsNullOrWhiteSpace(username)
                ? Array.Empty<string>()
                : await _matrixService.GetRolesAsync(username, cancellationToken);
        }

        private Task<IActionResult> RunEditorAsync(
            Func<string, Task<object>> work,
            string successMessage,
            string logAction,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () =>
                {
                    var by = _emp.CurrentEmployee?.EmployeeCode;

                    if (string.IsNullOrWhiteSpace(by))
                    {
                        throw new ArgumentException("Current employee could not be determined.");
                    }

                    var roles = await GetRolesAsync(cancellationToken);

                    if (!roles.Any(r => string.Equals(r, "tc", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to manage change requests.");
                    }

                    return await work(by);
                },
                logAction,
                cancellationToken,
                successMessage);

        private async Task<IActionResult> RunAsync(
            Func<Task<object>> work,
            string logAction,
            CancellationToken cancellationToken,
            string? successMessage = null)
        {
            try
            {
                var data = await work();

                return Json(new { success = true, message = successMessage, data });
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
