using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Services.IServices;

namespace Training.Controllers
{
    /// <summary>
    /// Controller untuk import Training Event + Participant dari Excel.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(ProfileAttribute))]
    public class EventImportController : ControllerBase
    {
        private readonly IEventImportService _importService;
        private readonly ICurrentUserService _emp;
        private readonly ILogger<EventImportController> _logger;
        private readonly IWebHostEnvironment _env;

        public EventImportController(
            IEventImportService importService,
            ICurrentUserService emp,
            ILogger<EventImportController> logger,
            IWebHostEnvironment env)
        {
            _importService = importService;
            _emp = emp;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Import Training Event + Participant dari file Excel.
        /// </summary>
        /// <remarks>
        /// POST /api/eventimport/import-events
        ///
        /// Multipart form-data dengan file Excel berisi 2 sheet: "Events" dan "Participants",
        /// dihubungkan lewat kolom EventNo (key lokal di file, bukan EventCode database).
        /// Satu event + seluruh peserta-nya diproses sebagai satu unit (atomic) lewat
        /// <c>dbo.usp_TrainingEvent_Create</c>. Error di satu event tidak menghentikan event lain.
        /// </remarks>
        [HttpPost("import-events")]
        [Consumes("multipart/form-data")]
        [RoleAuthorizeApi("tc", "gm", "director")]
        public async Task<IActionResult> ImportEvents(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "File tidak ditemukan atau kosong."
                    });
                }

                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Format file tidak valid. Gunakan Excel (.xlsx atau .xls)."
                    });
                }

                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ukuran file terlalu besar (max 5MB)."
                    });
                }

                var tempPath = Path.Combine(_env.ContentRootPath, "Temp");
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                var fileName = $"event_import_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(tempPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                var createdBy = _emp.CurrentEmployee?.EmployeeCode;

                if (string.IsNullOrWhiteSpace(createdBy))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Current employee could not be determined."
                    });
                }

                var result = await _importService.ImportEventsFromExcelAsync(filePath, createdBy, cancellationToken);

                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch { /* ignore */ }

                return Ok(new
                {
                    success = result.SuccessCount > 0,
                    message = result.Message,
                    successCount = result.SuccessCount,
                    errorCount = result.Errors.Count,
                    errors = result.Errors.Any() ? result.Errors : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event import error.");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi error saat import: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Download template Training Event + Participant untuk import.
        /// </summary>
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Template_Training_Event.xlsx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound(new { message = "Template file tidak ditemukan." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(templatePath);
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Template_Training_Event.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download event template error.");
                return StatusCode(500, new { message = "Error saat download template." });
            }
        }
    }
}
