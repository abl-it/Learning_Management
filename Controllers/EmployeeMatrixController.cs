using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Employee Matrix (read-only): per karyawan, course yang harus dilakukan menurut matrix Approved
    /// dan riwayat training aktual. Dapat dibuka oleh pengguna yang login.
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class EmployeeMatrixController : Controller
    {
        private readonly ITrainingMatrixReportService _reportService;
        private readonly ILogger<EmployeeMatrixController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="EmployeeMatrixController"/>.
        /// </summary>
        public EmployeeMatrixController(
            ITrainingMatrixReportService reportService,
            ILogger<EmployeeMatrixController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>Halaman Employee Matrix.</summary>
        [HttpGet]
        public IActionResult Index() => View();

        /// <summary>Pilihan filter Company / Department / Sub Department.</summary>
        [HttpGet]
        public Task<IActionResult> GetFilters(CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetFiltersAsync(cancellationToken),
                "load filters",
                cancellationToken);

        /// <summary>Daftar karyawan menurut filter.</summary>
        [HttpGet]
        public Task<IActionResult> GetEmployees(
            string company,
            string? abrv,
            int? subDepartmentId,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetEmployeesAsync(
                    company, abrv, subDepartmentId, cancellationToken),
                "load employees",
                cancellationToken);

        /// <summary>Detail satu karyawan: course menurut matrix dan training aktual.</summary>
        [HttpGet]
        public Task<IActionResult> GetDetail(
            string employeeCode,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetEmployeeDetailAsync(employeeCode, cancellationToken),
                "load employee detail",
                cancellationToken);

        /// <summary>
        /// Export Excel (satu sheet): Training Matrix per Sub Department sesuai filter.
        /// Bila gagal, mengembalikan JSON {success=false, message} agar bisa ditampilkan oleh halaman.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportMatrix(
            string company,
            string? abrv,
            int? subDepartmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                var file = await _reportService.ExportMatrixAsync(
                    company, abrv, subDepartmentId, cancellationToken);

                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (ArgumentException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Json(new { success = false, message = "The request was cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export training matrix.");

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while trying to export the training matrix."
                });
            }
        }

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
