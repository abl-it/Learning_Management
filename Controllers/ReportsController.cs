using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Training.Models.DTO.Report;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Hosts reporting pages. Currently just the Training Inquiry report
    /// (realized training hours/attendance by department or by person),
    /// but structured to hold additional reports later.
    /// </summary>
    public class ReportsController : Controller
    {
        private readonly ITrainingInquiryService _trainingInquiryService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ITrainingInquiryService trainingInquiryService,
            ILogger<ReportsController> logger)
        {
            _trainingInquiryService = trainingInquiryService;
            _logger = logger;
        }

        /// <summary>Landing page for Reports - currently shows the Training Inquiry report directly.</summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>Company options for the Training Inquiry filter.</summary>
        [HttpGet]
        public async Task<IActionResult> GetInquiryCompanies(CancellationToken cancellationToken)
        {
            try
            {
                var options = await _trainingInquiryService.GetCompanyOptionsAsync(cancellationToken);
                return Json(options);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry company options.");
                return Json(Array.Empty<object>());
            }
        }

        /// <summary>Department options for the Training Inquiry filter, optionally scoped to a company.</summary>
        [HttpGet]
        public async Task<IActionResult> GetInquiryDepartments(
            string? coCode,
            CancellationToken cancellationToken)
        {
            try
            {
                var options = await _trainingInquiryService.GetDepartmentOptionsAsync(coCode, cancellationToken);
                return Json(options);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry department options.");
                return Json(Array.Empty<object>());
            }
        }

        /// <summary>Training Inquiry data, aggregated per department.</summary>
        [HttpGet]
        public async Task<IActionResult> GetInquiryByDepartment(
            [FromQuery] TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken)
        {
            try
            {
                var data = await _trainingInquiryService.GetByDepartmentAsync(filter, cancellationToken);

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry (by department) report.");

                return Json(new
                {
                    success = false,
                    message = "Failed to load the report. Please try again.",
                    data = Array.Empty<object>()
                });
            }
        }

        /// <summary>Training Inquiry data, aggregated per person.</summary>
        [HttpGet]
        public async Task<IActionResult> GetInquiryByPerson(
            [FromQuery] TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken)
        {
            try
            {
                var data = await _trainingInquiryService.GetByPersonAsync(filter, cancellationToken);

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to load Training Inquiry (by person) report.");

                return Json(new
                {
                    success = false,
                    message = "Failed to load the report. Please try again.",
                    data = Array.Empty<object>()
                });
            }
        }
    }
}
