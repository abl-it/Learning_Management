namespace Training.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Training.Models.Dtos;
    using Training.Services.Interfaces;
    using System.Security.Claims;
    using Training.Services.IServices;
    using Training.Filters;

    /// <summary>
    /// Controller untuk Training Course management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(ProfileAttribute))]
    public class TrainingCourseController : ControllerBase
    {
        private readonly ITrainingCourseService _courseService;
        private readonly ILogger<TrainingCourseController> _logger;
        private readonly IWebHostEnvironment _env;

        public TrainingCourseController(
            ITrainingCourseService courseService,
            ILogger<TrainingCourseController> logger,
            IWebHostEnvironment env)
        {
            _courseService = courseService;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Import Master Course dari file Excel
        /// </summary>
        /// <remarks>
        /// POST /api/trainingcourse/import-courses
        /// 
        /// Multipart form-data dengan file Excel
        /// - Kolom: Category Code, Course Name, Description, Duration, Trainer Type, Is Active
        /// - Auto-generate Course Code berdasarkan Category Sequence
        /// - Continue jika ada error, report detail error
        /// </remarks>
        [HttpPost("import-courses")]
        [Consumes("multipart/form-data")]
        [RoleAuthorizeApi("tc", "gm", "director")]
        public async Task<IActionResult> ImportCourses(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "File tidak ditemukan atau kosong."
                    });
                }

                // Validate file extension
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

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Ukuran file terlalu besar (max 5MB)."
                    });
                }

                // Save temporary file
                var tempPath = Path.Combine(_env.ContentRootPath, "Temp");
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                var fileName = $"course_import_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(tempPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get current user
                var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

                // Process import
                var result = await _courseService.ImportCoursesFromExcelAsync(filePath, currentUser);

                // Clean up temp file
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch { /* ignore */ }

                // Return result
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
                _logger.LogError($"Import error: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Terjadi error saat import: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Download template Master Course untuk import
        /// </summary>
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Template_Master_Course.xlsx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound(new { message = "Template file tidak ditemukan." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(templatePath);
                return File(fileBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Template_Master_Course.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Download template error: {ex.Message}");
                return StatusCode(500, new { message = "Error saat download template." });
            }
        }
    }
}
