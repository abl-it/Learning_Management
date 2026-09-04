using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Training.Filters;
using Training.Models;
using Training.Models.DTO;
using Training.Models.DTO.Common;
using Training.Models.DTO.Event;
using Training.Models.DTO.Event.Realization;
using Training.Models.ViewModels;
using Training.Services;
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
        private readonly IAttendanceFormService _attendanceFormService;
        private readonly IRealizationService _realizationService;

        public TrainingEventController(
            ICurrentUserService emp,
            ICategoryService categoryService,
            ICourseService courseService,
            ITrainingService trainingService,
            IAntiforgery antiforgery,
            ILogger<TrainingEventController> logger,
            IEventService eventService,
            IAttendanceFormService attendanceFormService,
            IRealizationService realizationService
            )
        {
            _categoryService = categoryService;
            _courseService = courseService;
            _trainingService = trainingService;
            _emp = emp;
            _antiforgery = antiforgery;
            _logger = logger;
            _eventService = eventService;
            _attendanceFormService = attendanceFormService;
            _realizationService = realizationService;
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
                    TrainingCategory = model.TrainingCategory,

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

                // Company & Department readonly.
                // Ambil nilai asli dari database.
                model.CoCode = detail.CoCode;
                model.ABRV = detail.ABRV;
                model.DeptName = detail.DeptName;

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
        /// <summary>
        /// Generates and streams the training attendance form ("Daftar Hadir")
        /// as a PDF so it can be viewed and printed from the browser.
        /// </summary>
        /// <param name="id">Training event identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The attendance form PDF, or an appropriate error result.</returns>
        [HttpGet]
        public async Task<IActionResult> PrintAttendance(
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
                var detail = await _eventService.GetDetailAsync(
                    id,
                    username,
                    cancellationToken);

                if (detail is null)
                {
                    return NotFound();
                }

                /*
                 * Security check.
                 *
                 * Reuse the same visibility rule as the Detail page -
                 * the browser must never be trusted to determine access.
                 */
                if (!detail.CanShow)
                {
                    _logger.LogWarning(
                        "User {Username} attempted to print attendance for unauthorized training event {TrainingEventId}.",
                        username,
                        id);

                    return Forbid();
                }

                var pdfBytes = _attendanceFormService.GenerateAttendanceFormPdf(detail);

                var fileName = $"DaftarHadir_{detail.EventCode}.pdf";

                // No FileDownloadName is set on purpose: this lets the browser
                // open the PDF inline in its built-in viewer (so the user can
                // preview and print it), instead of forcing a download.
                Response.Headers.Append(
                    "Content-Disposition",
                    $"inline; filename=\"{fileName}\"");

                return File(pdfBytes, "application/pdf");
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
                    "Error generating attendance form PDF. TrainingEventId: {TrainingEventId}",
                    id);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred while generating the attendance form."
                    });
            }
        }

        /// <summary>
        /// The "Event Realization" entry point. With no <paramref name="id"/>,
        /// shows a landing list of events the current user can act on or
        /// review (this is what the navbar's "Event Realization" link opens).
        /// With an <paramref name="id"/>, shows that specific event's
        /// realization date/time, attendance list, and attachments.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Realization(
            long? id,
            CancellationToken cancellationToken)
        {
            var username = _emp.CurrentEmployee?.Username;
            var employeeCode = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(employeeCode))
            {
                return Unauthorized();
            }

            if (id is null || id <= 0)
            {
                var listRole = await _trainingService.GetRoleAsync(username);

                var list = await _realizationService.GetListAsync(
                    listRole,
                    employeeCode,
                    cancellationToken);

                return View("RealizationList", list);
            }

            var eventDetail = await _eventService.GetDetailAsync(
                id.Value,
                username,
                cancellationToken);

            if (eventDetail is null)
            {
                return NotFound();
            }

            if (!eventDetail.CanShow)
            {
                return Forbid();
            }

            var isRealizationEligible =
                string.Equals(eventDetail.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventDetail.Status, "Realization Submitted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventDetail.Status, "Realization Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventDetail.Status, "Complete", StringComparison.OrdinalIgnoreCase);

            if (!isRealizationEligible)
            {
                TempData["ErrorMessage"] =
                    "Realization can only be entered for an approved training event.";

                return RedirectToAction(nameof(Detail), new { id });
            }

            var realization = await _realizationService.GetDetailAsync(
                id.Value,
                cancellationToken);

            if (realization is null)
            {
                return NotFound();
            }

            // The "Complete"/"Reject" confirmation step is restricted to the
            // "tc" (HRD) role, and only while the event is actually waiting
            // on that confirmation.
            var role = await _trainingService.GetRoleAsync(username);

            var isWaitingForHrd =
                string.Equals(eventDetail.Status, "Realization Submitted", StringComparison.OrdinalIgnoreCase)
                && string.Equals(role, "tc", StringComparison.OrdinalIgnoreCase);

            realization.CanComplete = isWaitingForHrd;
            realization.CanReject = isWaitingForHrd;
            realization.Histories = FilterRealizationHistories(eventDetail.Histories);

            return View(realization);
        }

        /// <summary>
        /// Narrows the full event workflow history down to entries from the
        /// realization stage only (Post to HRD / Complete / Reject-from-
        /// Realization-Submitted) - excluding the earlier Draft/Approve/
        /// Reject history from the original approval workflow, which
        /// re-uses some of the same action names (e.g. "Reject").
        /// </summary>
        private static List<Training.Models.History> FilterRealizationHistories(
            List<Training.Models.History> histories)
        {
            var realizationActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Post to HRD",
                "Complete"
            };

            var realizationStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Realization Submitted",
                "Realization Rejected",
                "Complete"
            };

            return histories
                .Where(h =>
                    (h.Action != null && realizationActions.Contains(h.Action))
                    || (h.State != null && realizationStates.Contains(h.State))
                    || (h.NextState != null && realizationStates.Contains(h.NextState)))
                .ToList();
        }

        /// <summary>Saves realization data as a draft (repeatable, doesn't change event status).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRealization(
            [FromBody] TrainingEventRealizationSaveDto request,
            CancellationToken cancellationToken)
            => await ExecuteRealizationSaveAsync(request, post: false, cancellationToken);

        /// <summary>Saves realization data and posts it to HRD, locking it from further edits.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostRealizationToHrd(
            [FromBody] TrainingEventRealizationSaveDto request,
            CancellationToken cancellationToken)
            => await ExecuteRealizationSaveAsync(request, post: true, cancellationToken);

        /// <summary>
        /// The "tc" (HRD) confirmation step: advances a posted realization
        /// from "Realization Submitted" to "Complete". Restricted to users
        /// holding the "tc" role.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRealization(
            [FromBody] TrainingEventRealizationCompleteDto request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.TrainingEventId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request payload."
                });
            }

            var username = _emp.CurrentEmployee?.Username;
            var by = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(by))
            {
                return Json(new
                {
                    success = false,
                    message = "Current employee could not be determined."
                });
            }

            var role = await _trainingService.GetRoleAsync(username);

            if (!string.Equals(role, "tc", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to complete this realization."
                });
            }

            try
            {
                await _realizationService.CompleteAsync(
                    request.TrainingEventId,
                    by,
                    request.Remarks,
                    cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Realization marked as complete."
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to complete realization. TrainingEventId: {TrainingEventId}",
                    request.TrainingEventId);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return Json(new
                {
                    success = false,
                    message = "The request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while completing realization. TrainingEventId: {TrainingEventId}",
                    request.TrainingEventId);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while completing the realization."
                });
            }
        }

        /// <summary>
        /// The "tc" (HRD) rejection step: sends a submitted realization back
        /// to the creator for correction. Restricted to users holding the
        /// "tc" role. A reason is required.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRealization(
            [FromBody] TrainingEventRealizationRejectDto request,
            CancellationToken cancellationToken)
        {
            if (request is null || request.TrainingEventId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request payload."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Remarks))
            {
                return Json(new
                {
                    success = false,
                    message = "A reason is required when rejecting a realization."
                });
            }

            var username = _emp.CurrentEmployee?.Username;
            var by = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(by))
            {
                return Json(new
                {
                    success = false,
                    message = "Current employee could not be determined."
                });
            }

            var role = await _trainingService.GetRoleAsync(username);

            if (!string.Equals(role, "tc", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to reject this realization."
                });
            }

            try
            {
                await _realizationService.RejectAsync(
                    request.TrainingEventId,
                    by,
                    request.Remarks,
                    cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Realization rejected and sent back to the creator."
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reject realization. TrainingEventId: {TrainingEventId}",
                    request.TrainingEventId);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return Json(new
                {
                    success = false,
                    message = "The request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while rejecting realization. TrainingEventId: {TrainingEventId}",
                    request.TrainingEventId);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while rejecting the realization."
                });
            }
        }

        private async Task<IActionResult> ExecuteRealizationSaveAsync(
            TrainingEventRealizationSaveDto request,
            bool post,
            CancellationToken cancellationToken)
        {
            if (request is null || request.TrainingEventId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request payload."
                });
            }

            var by = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(by))
            {
                return Json(new
                {
                    success = false,
                    message = "Current employee could not be determined."
                });
            }

            try
            {
                if (post)
                {
                    await _realizationService.PostToHrdAsync(
                        request,
                        by,
                        cancellationToken);
                }
                else
                {
                    await _realizationService.SaveDraftAsync(
                        request,
                        by,
                        cancellationToken);
                }

                return Json(new
                {
                    success = true,
                    message = post
                        ? "Realization posted to HRD successfully."
                        : "Realization saved as draft."
                });
            }
            catch (ArgumentException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save realization. TrainingEventId: {TrainingEventId}, Post: {Post}",
                    request.TrainingEventId,
                    post);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return Json(new
                {
                    success = false,
                    message = "The request was cancelled."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while saving realization. TrainingEventId: {TrainingEventId}, Post: {Post}",
                    request.TrainingEventId,
                    post);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while saving the realization."
                });
            }
        }

        /// <summary>Uploads one or more attachments (scanned attendance sheet, other documents) for a training event.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadRealizationAttachment(
            long trainingEventId,
            List<IFormFile> files,
            string documentType,
            CancellationToken cancellationToken)
        {
            if (trainingEventId <= 0 || files is null || files.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No file was provided."
                });
            }

            var uploadedBy = _emp.CurrentEmployee?.EmployeeCode;

            if (string.IsNullOrWhiteSpace(uploadedBy))
            {
                return Json(new
                {
                    success = false,
                    message = "Current employee could not be determined."
                });
            }

            var uploaded = new List<TrainingEventAttachmentDto>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                try
                {
                    await using var stream = file.OpenReadStream();

                    var result = await _realizationService.UploadAttachmentAsync(
                        trainingEventId,
                        stream,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        documentType,
                        uploadedBy,
                        cancellationToken);

                    uploaded.Add(result);
                }
                catch (Exception ex)
                    when (ex is ArgumentException or InvalidOperationException)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to upload attachment {FileName} for TrainingEventId {TrainingEventId}.",
                        file.FileName,
                        trainingEventId);

                    errors.Add($"{file.FileName}: upload failed.");
                }
            }

            if (uploaded.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = string.Join(" ", errors)
                });
            }

            return Json(new
            {
                success = true,
                message = errors.Count == 0
                    ? "File uploaded successfully."
                    : "Some files were uploaded, but some failed.",
                data = uploaded,
                errors
            });
        }

        /// <summary>Deletes a previously uploaded attachment.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRealizationAttachment(
            long attachmentId,
            CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid attachment."
                });
            }

            try
            {
                await _realizationService.DeleteAttachmentAsync(
                    attachmentId,
                    cancellationToken);

                return Json(new
                {
                    success = true,
                    message = "Attachment deleted."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete attachment {AttachmentId}.",
                    attachmentId);

                return Json(new
                {
                    success = false,
                    message = "Failed to delete attachment."
                });
            }
        }

        /// <summary>Downloads a realization attachment, after confirming the user may view its parent event.</summary>
        [HttpGet]
        public async Task<IActionResult> DownloadRealizationAttachment(
            long attachmentId,
            CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
            {
                return NotFound();
            }

            var username = _emp.CurrentEmployee?.Username;

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            var trainingEventId = await _realizationService.GetAttachmentTrainingEventIdAsync(
                attachmentId,
                cancellationToken);

            if (trainingEventId is null)
            {
                return NotFound();
            }

            var eventDetail = await _eventService.GetDetailAsync(
                trainingEventId.Value,
                username,
                cancellationToken);

            if (eventDetail is null || !eventDetail.CanShow)
            {
                return Forbid();
            }

            var result = await _realizationService.OpenAttachmentAsync(
                attachmentId,
                cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
        }

        #endregion



    }
}
