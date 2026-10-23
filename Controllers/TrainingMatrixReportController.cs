using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Laporan Compliance Training Matrix (read-only). Dapat dibuka oleh pengguna yang login;
    /// pembatasan role bisa ditambahkan di sini bila diperlukan.
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class TrainingMatrixReportController : Controller
    {
        private readonly ITrainingMatrixReportService _reportService;
        private readonly ILogger<TrainingMatrixReportController> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="TrainingMatrixReportController"/>.
        /// </summary>
        public TrainingMatrixReportController(
            ITrainingMatrixReportService reportService,
            ILogger<TrainingMatrixReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>Halaman laporan.</summary>
        [HttpGet]
        public IActionResult Index() => View();

        /// <summary>Ringkasan Compliance untuk satu perusahaan (ABL/NKM): KPI, per Department, alasan Unmapped.</summary>
        [HttpGet]
        public Task<IActionResult> GetSummary(
            string company,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetSummaryAsync(company, cancellationToken),
                "load compliance summary",
                cancellationToken);

        /// <summary>Karyawan satu Department beserta gap.</summary>
        [HttpGet]
        public Task<IActionResult> GetDepartmentEmployees(
            string abrv,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetDepartmentEmployeesAsync(abrv, cancellationToken),
                "load department employees",
                cancellationToken);

        /// <summary>Karyawan Unmapped (Data Quality).</summary>
        [HttpGet]
        public Task<IActionResult> GetUnmapped(
            string company,
            CancellationToken cancellationToken) =>
            RunAsync(
                async () => await _reportService.GetUnmappedAsync(company, cancellationToken),
                "load unmapped employees",
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
