using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Training.Filters;
using Training.Models;
using Training.Models.DTO;
using Training.Models.DTO.Common;
using Training.Models.DTO.Event;
using Training.Models.ViewModels;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class TrainingEventController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ICourseService _courseService;
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _emp;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<TrainingEventController> _logger;
        private readonly IEventService _eventService;

        public TrainingEventController(
            ICurrentUserService emp,
            ICategoryService categoryService,
            ICourseService courseService,
            ITrainingService trainingService,
            IAntiforgery antiforgery,
            ILogger<TrainingEventController> logger,
            IEventService eventService      
            )
        {
            _categoryService = categoryService;
            _courseService = courseService;
            _trainingService = trainingService;
            _emp = emp;
            _antiforgery = antiforgery;
            _logger = logger;
            _eventService = eventService;
        }

        public IActionResult Index()
        {
            return View();
        }


        #region Event Registration
        
        [HttpGet]
        public async Task<IActionResult> GetEvents(
    string? searchTerm,
    string? coCode,
    string? abrv,
    string? status,
    int start = 0,
    int length = 10,
    string? orderColumn = null,
    string? orderDirection = null)
        {
            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Current username could not be determined."
                });
            }

            var filter = new TrainingEventFilterDto
            {
                SearchTerm = searchTerm,
                CoCode = coCode,
                Abrv = abrv,
                Status = status,

                Start = start,
                Length = length,

                OrderColumn = orderColumn,
                OrderDirection = orderDirection
            };
           
            var result =
                await _eventService.GetTrainingEventsAsync(filter, username);

            return Json(new
            {
                draw = Request.Query["draw"].FirstOrDefault(),

                recordsTotal = result.TotalRecords,

                recordsFiltered = result.FilteredRecords,

                data = result.Data
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        [FromBody] TrainingEventCreateDto request,
        CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Request data is required."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid training event data."
                });
            }

            try
            {
                var createdBy =
                    _emp.CurrentEmployee.EmployeeCode;

                if (string.IsNullOrWhiteSpace(createdBy))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Current employee could not be determined."
                    });
                }

                var result =
                    await _eventService.CreateAsync(
                        request,
                        createdBy,
                        cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Training event created successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid training event create request.");

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating training event.");

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating training event.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred."
                    });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses =
                await _courseService.GetAllCourseAsync();

            return Json(
                courses.Select(c => new
                {
                    courseId = c.CourseId,
                    courseCode = c.CourseCode,
                    courseName = c.CourseName,
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName,
                    durationHours = c.DurationHours
                }));
        }

        [HttpGet]
        public async Task<IActionResult> SearchInternalTrainers(
            string? term,
            CancellationToken cancellationToken)
        {
            var result =
                await _trainingService
                    .SearchInternalTrainersAsync(
                        term,
                        cancellationToken);

            return Json(result.Select(x => new
            {
                employeeCode = x.EmployeeCode,
                fullName = x.FullName
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var result =
                await _trainingService.GetDepartmentsAsync(null);

            return Json(result);
        }


        [HttpGet]
        public async Task<IActionResult> SearchEmployees(
            string? term)
        {
            var result =
                await _trainingService
                    .SearchEmployeesAsync(
                        term);

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(
            long id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var username =
                _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning(
                    "Unable to determine current username when opening training event detail. " +
                    "TrainingEventId: {TrainingEventId}",
                    id);

                return Unauthorized();
            }

            try
            {
                var model =
                    await _eventService.GetDetailAsync(
                        id,
                        username,
                        cancellationToken);

                if (model is null)
                {
                    return NotFound();
                }

                /*
                    * Security check.
                    *
                    * CanShow is determined by the database permission function.
                    * The browser must never be trusted to determine access.
                    */
                if (!model.CanShow)
                {
                    _logger.LogWarning(
                        "User {Username} attempted to access unauthorized training event {TrainingEventId}.",
                        username,
                        id);

                    return Forbid();
                }

                return View(model);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading training event detail. " +
                    "TrainingEventId: {TrainingEventId}, Username: {Username}",
                    id,
                    username);

                return StatusCode(
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Displays the training event edit page.
        /// </summary>
        /// <param name="id">Training event identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The edit view.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(
            long id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            try
            {
                var model = await _eventService.GetDetailAsync(
                    id,
                    username,
                    cancellationToken);

                if (model is null)
                {
                    return NotFound();
                }

                /*
                 * Server-side permission check.
                 *
                 * Jangan hanya mengandalkan tombol Edit
                 * di Detail.cshtml.
                 */
                if (!model.CanEdit)
                {
                    return Forbid();
                }

                var editModel = new TrainingEventEditDto
                {
                    TrainingEventId = model.TrainingEventId,
                    EventCode = model.EventCode,

                    CourseId = model.CourseId,
                    CourseCode = model.CourseCode,
                    
                    TrainingCategoryId = model.TrainingCategoryId,

                    TrainingTitle = model.TrainingTitle,
                    TrainingDescription = model.TrainingDescription,
                    TrainingObjective = model.TrainingObjective,

                    CoCode = model.CoCode,
                    ABRV = model.ABRV,
                    DeptName = model.DeptName,

                    EventType = model.EventType,

                    TrainerId = model.TrainerId,
                    TrainerName = model.TrainerName,

                    ParticipantQuota = model.ParticipantQuota,
                    Budget = model.Budget,
                    Venue = model.Venue,

                    EventStartDate = model.EventStartDate,
                    EventEndDate = model.EventEndDate,

                    SessionCount = model.SessionCount,
                    DurationHours = model.DurationHours,

                    Participants = model.Participants
                };

                return View(editModel);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading training event for edit. " +
                    "TrainingEventId: {TrainingEventId}, Username: {Username}",
                    id,
                    username);

                return StatusCode(
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates an existing training event.
        /// </summary>
        /// <param name="model">Training event edit model.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Redirects to the training event detail page after successful update.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            TrainingEventEditDto model,
            CancellationToken cancellationToken)
        {
            if (model is null || model.TrainingEventId <= 0)
            {
                return BadRequest();
            }

            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            try
            {
                // -----------------------------------------------------
                // Server-side permission check
                // -----------------------------------------------------

                var detail = await _eventService.GetDetailAsync(
                    model.TrainingEventId,
                    username,
                    cancellationToken);

                if (detail is null)
                {
                    return NotFound();
                }

                if (!detail.CanEdit)
                {
                    return Forbid();
                }

                // -----------------------------------------------------
                // Model validation
                // -----------------------------------------------------

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var modifiedBy =
                    _emp.CurrentEmployee?.EmployeeCode;

                if (string.IsNullOrWhiteSpace(modifiedBy))
                {
                    return Unauthorized();
                }

                // -----------------------------------------------------
                // Update
                // -----------------------------------------------------

                await _eventService.UpdateAsync(
                    model,
                    modifiedBy,
                    cancellationToken);

                TempData["SuccessMessage"] =
                    $"Training event {detail.EventCode} berhasil diperbarui.";

                return RedirectToAction(
                    nameof(Detail),
                    new
                    {
                        id = model.TrainingEventId
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid training event update request. " +
                    "TrainingEventId: {TrainingEventId}",
                    model.TrainingEventId);

                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return View(model);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while updating training event. " +
                    "TrainingEventId: {TrainingEventId}",
                    model.TrainingEventId);

                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return View(model);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating training event. " +
                    "TrainingEventId: {TrainingEventId}",
                    model.TrainingEventId);

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while updating the training event.");

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            long id,
            CancellationToken cancellationToken)
        {
            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            var detail = await _eventService.GetDetailAsync(
                id,
                username,   
                cancellationToken);

            if (detail is null)
            {
                return NotFound();
            }

            if (!detail.CanEdit)
            {
                return Forbid();
            }

            await _eventService.DeleteAsync(
                id,
                cancellationToken);

            TempData["SuccessMessage"] =
                $"Training event {detail.EventCode} berhasil dihapus.";

            return RedirectToAction(nameof(Index));
        }


        #endregion

        #region Event Realization

        #endregion



    }
}
