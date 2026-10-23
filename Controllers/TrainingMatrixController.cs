using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Models.DTO.TrainingMatrix;
using Training.Services;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Training Matrix: satu dokumen per Department (ABRV).
    /// Semua pengguna login boleh melihat. Buat/ubah/hapus hanya role "tc";
    /// submit oleh "tc", approve/reject oleh role pada dbo.Workflow (hrhead).
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class TrainingMatrixController : Controller
    {
        private const string EditorRole = "tc";

        private readonly ICurrentUserService _emp;
        private readonly ITrainingService _trainingService;
        private readonly ITrainingMatrixService _matrixService;
        private readonly ILogger<TrainingMatrixController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="TrainingMatrixController"/>.
        /// </summary>
        public TrainingMatrixController(
            ICurrentUserService emp,
            ITrainingService trainingService,
            ITrainingMatrixService matrixService,
            ILogger<TrainingMatrixController> logger)
        {
            _emp = emp;
            _trainingService = trainingService;
            _matrixService = matrixService;
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // Pages
        // ---------------------------------------------------------------

        /// <summary>Daftar Department dan status matrix-nya.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewData["CanEdit"] = IsEditor(await GetRolesAsync(cancellationToken));

            return View();
        }

        /// <summary>Halaman dokumen matrix.</summary>
        [HttpGet]
        public IActionResult Detail(int id)
        {
            ViewData["MatrixId"] = id;

            return View();
        }

        // ---------------------------------------------------------------
        // Read
        // ---------------------------------------------------------------

        /// <summary>Daftar untuk DataTables (client-side).</summary>
        [HttpGet]
        public Task<IActionResult> GetList(CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _matrixService.GetListAsync(cancellationToken),
                "load training matrix list",
                cancellationToken);

        /// <summary>Dokumen matrix lengkap.</summary>
        [HttpGet]
        public Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken) =>
            RunAsync(
                async () =>
                {
                    var roles = await GetRolesAsync(cancellationToken);
                    var detail = await _matrixService.GetDetailAsync(id, roles, cancellationToken);

                    return detail ?? throw new ArgumentException("Matrix was not found.");
                },
                "load training matrix",
                cancellationToken);

        // ---------------------------------------------------------------
        // Write (role tc)
        // ---------------------------------------------------------------

        /// <summary>Membuat dokumen matrix Draft untuk satu Department.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Create(string departmentAbrv, CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    var id = await _matrixService.CreateAsync(departmentAbrv, by, cancellationToken);

                    return (object)new { matrixId = id };
                },
                "Training matrix created.",
                "create training matrix",
                cancellationToken);

        /// <summary>Apply Course Group dan/atau course ke Sub Dept x Layer.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Apply(
            [FromForm] MatrixApplyDto request,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    var result = await _matrixService.ApplyAsync(
                        request ?? throw new ArgumentException("Invalid request payload."),
                        by,
                        cancellationToken);

                    return (object)result;
                },
                "Courses applied.",
                "apply courses to training matrix",
                cancellationToken);

        /// <summary>Menghapus baris terpilih.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RemoveRows(
            int id,
            List<int> detailIds,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async _ =>
                {
                    var n = await _matrixService.RemoveRowsAsync(id, detailIds, cancellationToken);

                    return (object)new { removed = n };
                },
                "Rows removed.",
                "remove training matrix rows",
                cancellationToken);

        /// <summary>Mengubah Requirement baris terpilih.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> SetRequirement(
            int id,
            List<int> detailIds,
            string requirement,
            CancellationToken cancellationToken) =>
            RunEditorAsync(
                async _ =>
                {
                    var n = await _matrixService.SetRequirementAsync(
                        id, detailIds, requirement, cancellationToken);

                    return (object)new { changed = n };
                },
                "Requirement updated.",
                "update training matrix requirement",
                cancellationToken);

        /// <summary>Menghapus dokumen (Draft/Rejected).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
            RunEditorAsync(
                async by =>
                {
                    await _matrixService.DeleteAsync(id, by, cancellationToken);

                    return (object)new { };
                },
                "Training matrix deleted.",
                "delete training matrix",
                cancellationToken);

        // ---------------------------------------------------------------
        // Workflow (role diperiksa oleh service terhadap dbo.Workflow.Allowed)
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

                    var status = await _matrixService.TransitionAsync(
                        id, action, remarks, by, roles, cancellationToken);

                    return new { status };
                },
                "run training matrix workflow action",
                cancellationToken,
                successMessage: "Done.");

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Seluruh role pengguna saat ini (bukan hanya role tertinggi dari GetRoleAsync).
        /// </summary>
        private async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken)
        {
            var username = _emp.CurrentEmployee?.Username;

            return string.IsNullOrWhiteSpace(username)
                ? Array.Empty<string>()
                : await _matrixService.GetRolesAsync(username, cancellationToken);
        }

        private static bool IsEditor(IEnumerable<string> roles) =>
            roles.Any(r => string.Equals(r, EditorRole, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Operasi tulis khusus role tc: memeriksa employee dan role di server, lalu menjalankan work.
        /// </summary>
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

                    if (!IsEditor(await GetRolesAsync(cancellationToken)))
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to manage training matrices.");
                    }

                    return await work(by);
                },
                logAction,
                cancellationToken,
                successMessage);

        /// <summary>
        /// Membungkus hasil menjadi JSON {success, message, data} dan memetakan exception.
        /// </summary>
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
