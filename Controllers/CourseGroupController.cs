using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Models;
using Training.Models.DTO.TrainingMatrix;
using Training.Services;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Master Course Group untuk Training Matrix.
    /// Semua pengguna yang login boleh melihat; tambah, ubah, dan hapus hanya untuk role "tc".
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class CourseGroupController : Controller
    {
        private const string EditorRole = "tc";

        private readonly ICurrentUserService _emp;
        private readonly ITrainingService _trainingService;
        private readonly ICourseGroupService _courseGroupService;
        private readonly ILogger<CourseGroupController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="CourseGroupController"/>.
        /// </summary>
        public CourseGroupController(
            ICurrentUserService emp,
            ITrainingService trainingService,
            ICourseGroupService courseGroupService,
            ILogger<CourseGroupController> logger)
        {
            _emp = emp;
            _trainingService = trainingService;
            _courseGroupService = courseGroupService;
            _logger = logger;
        }

        /// <summary>
        /// Halaman daftar Course Group. <c>ViewData["CanEdit"]</c> bernilai true untuk role "tc".
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["CanEdit"] = await IsEditorAsync();

            return View();
        }

        /// <summary>Daftar Course Group untuk DataTables (client-side).</summary>
        [HttpGet]
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            try
            {
                var list = await _courseGroupService.GetListAsync(cancellationToken);

                return Json(new { success = true, data = list });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load course group list.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred while loading course groups."
                    });
            }
        }

        /// <summary>Detail satu Course Group beserta course di dalamnya.</summary>
        [HttpGet]
        public async Task<IActionResult> GetDetail(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Invalid course group." });
            }

            try
            {
                var detail = await _courseGroupService.GetDetailAsync(id, cancellationToken);

                if (detail is null)
                {
                    return Json(new { success = false, message = "Course group was not found." });
                }

                return Json(new { success = true, data = detail });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load course group {CourseGroupId}.", id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred while loading the course group."
                    });
            }
        }

        /// <summary>Course aktif dari master course untuk picker.</summary>
        [HttpGet]
        public async Task<IActionResult> GetCourseOptions(CancellationToken cancellationToken)
        {
            try
            {
                var courses = await _courseGroupService.GetCourseOptionsAsync(cancellationToken);

                return Json(new { success = true, data = courses });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load course options.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred while loading courses."
                    });
            }
        }

        /// <summary>
        /// Membuat (CourseGroupId = 0) atau memperbarui Course Group. Hanya role "tc".
        /// Dikirim sebagai form post (CourseIds berulang) agar token antiforgery dari form tervalidasi.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            [FromForm] CourseGroupSaveDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var savedBy = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(savedBy))
            {
                return Json(new
                {
                    success = false,
                    message = "Current employee could not be determined."
                });
            }

            if (!await IsEditorAsync())
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to manage course groups."
                });
            }

            try
            {
                var id = await _courseGroupService.SaveAsync(
                    request,
                    savedBy,
                    cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Course group saved.",
                    data = new { courseGroupId = id }
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return Json(new { success = false, message = "The request was cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while saving course group.");

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while saving the course group."
                });
            }
        }

        /// <summary>Menghapus Course Group. Hanya role "tc".</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Invalid course group." });
            }

            if (!await IsEditorAsync())
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to manage course groups."
                });
            }

            try
            {
                await _courseGroupService.DeleteAsync(id, cancellationToken);

                return Json(new { success = true, message = "Course group deleted." });
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return Json(new { success = false, message = "The request was cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting course group {CourseGroupId}.", id);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the course group."
                });
            }
        }

        /// <summary>
        /// True bila pengguna saat ini memegang role "tc" (keputusan server-side, bukan dari browser).
        /// </summary>
        private async Task<bool> IsEditorAsync()
        {
            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            var role = await _trainingService.GetRoleAsync(username);

            return string.Equals(role, EditorRole, StringComparison.OrdinalIgnoreCase);
        }
    }
}
