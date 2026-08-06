using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Training.Filters;
using Training.Models;
using Training.Models.DataTables;
using Training.Models.DTO;
using Training.Models.ViewModels;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class PlanningController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ICourseService _courseService;
        private readonly ITrainingService _trainingService;
        private readonly IPlanningService _planningService;
        private readonly IActionService _actionService;
        private readonly ICurrentUserService _emp;
        private readonly IAntiforgery _antiforgery;
        public PlanningController(
            ICurrentUserService emp,
            ICategoryService categoryService,
            ICourseService courseService,
            ITrainingService trainingService,
            IPlanningService planingService,
            IActionService actionService,
            IAntiforgery antiforgery
            )
        {
            _categoryService = categoryService;
            _courseService = courseService;
            _trainingService = trainingService;
            _emp = emp;
            _planningService = planingService;
            _actionService = actionService;
            _antiforgery = antiforgery;
        }
        public IActionResult Index()
        {
            return View();
        }

        #region Needs
        public async Task<IActionResult> Needs()
        {
            var _co = _emp.CurrentEmployee.CoCode;
            var _groups = await _trainingService.GetAllGroupsAsync();
            var _activeGroups = _groups.Where(g => g.IsActive == 1 && g.CoCode==_co).ToList();
            var _categories = await _categoryService.GetAllCategoriesAsync();
            var _units = await _trainingService.GetAllUnitsAsync();
            var _filteredUnits = _units.Where(u => u.CoCode == _co).ToList();
            var _companies = await _trainingService.GetAllCompaniesAsync();
            var _companiesByEmp = _companies.Where(c => c.CoCode == _co).ToList();
            var _selectedCompany = _companiesByEmp.FirstOrDefault()?.CoCode?.ToString() ?? "ABL";


            var viewModel = new NeedVM
            {
                Groups = _activeGroups,
                SelectedGroup = "All",
                Categories = _categories,
                SelectedCategory = "All",
                Units = _filteredUnits,
                SelectedUnit = "All",
                Companies = _companies, //_companiesByEmp,
                SelectedCompany = _selectedCompany,

            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetNeeds()
        {

            try
            {
                var request = ParseDataTableRequest(Request);
                var response = await _planningService.GetNeedsAsync(request);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }
        private static DataTableRequest ParseDataTableRequest(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DataTableRequest
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                CompanyFilter = form["companyFilter"],
                CategoryFilter = form["categoryFilter"],
                UnitFilter = form["unitFilter"],
                GroupFilter = form["groupFilter"],
                StatusFilter = form["statusFilter"],

                //CategoryId = int.TryParse(form["category"], out var categoryId) ? categoryId : 0,
            };

            // Parse columns
            var columnIndex = 0;
            while (!string.IsNullOrEmpty(form[$"columns[{columnIndex}][data]"]))
            {
                request.Columns.Add(new DataTableColumn
                {
                    Data = form[$"columns[{columnIndex}][data]"],
                    Name = form[$"columns[{columnIndex}][name]"],
                    Searchable = bool.TryParse(form[$"columns[{columnIndex}][searchable]"], out var searchable) && searchable,
                    Orderable = bool.TryParse(form[$"columns[{columnIndex}][orderable]"], out var orderable) && orderable,
                    Search = new DataTableSearch
                    {
                        Value = form[$"columns[{columnIndex}][search][value]"],
                        Regex = bool.TryParse(form[$"columns[{columnIndex}][search][regex]"], out var regex) && regex
                    }
                });
                columnIndex++;
            }

            // Parse ordering
            var orderIndex = 0;
            while (!string.IsNullOrEmpty(form[$"order[{orderIndex}][column]"]))
            {
                request.Order.Add(new DataTableOrder
                {
                    Column = int.TryParse(form[$"order[{orderIndex}][column]"], out var orderColumn) ? orderColumn : 0,
                    Dir = form[$"order[{orderIndex}][dir]"]
                });
                orderIndex++;
            }

            return request;
        }

        [HttpPost]
        public async Task<IActionResult> SaveTrainingNeeds([FromBody] SaveTrainingNeedsRequest request)
        {
            try
            {
                // Validate input
                if (request == null || request.TrainingNeeds == null || !request.TrainingNeeds.Any())
                {
                    return Json(new { success = false, message = "No training needs data provided" });
                }

                // Validate all entries
                foreach (var need in request.TrainingNeeds)
                {
                    if (need.CoCode == "" || need.CoCode == null )
                    {
                        return Json(new { success = false, message = "Invalid company selected" });
                    }
                    if (need.GroupId <= 0)
                    {
                        return Json(new { success = false, message = "Invalid group selected" });
                    }
                    if (need.UnitId <= 0)
                    {
                        return Json(new { success = false, message = "Invalid unit selected" });
                    }
                    if (need.CourseId == "" || need.CourseId == null)
                    {
                        return Json(new { success = false, message = "Invalid course selected" });
                    }
                }

                // Save to database
                var savedCount = 0;
                var duplicateCount = 0;

                foreach (var needDto in request.TrainingNeeds)
                {
                    // Check if already exists
                    var exists = (await _planningService.GetAllNeedsAsync())
                        .Where(tn =>
                            tn.UnitId == needDto.UnitId &&
                            tn.CourseId == needDto.CourseId &&
                            tn.IsActive == 1)
                        .Select(n => new { n.GroupId, n.UnitId, n.CourseId })
                        .ToList();

                    if (exists.Any())
                    {
                        duplicateCount++;
                        continue;
                    }

                    // Create new training need
                    var trainingNeed = new Needs
                    {
                        CoCode = needDto.CoCode,
                        GroupId = needDto.GroupId,
                        UnitId = needDto.UnitId,
                        CourseId = needDto.CourseId,
                        IsActive = 1,
                        CreatedAt = DateTime.Now,
                        CreatedBy = _emp.CurrentEmployee.EmployeeCode ?? "System"
                                            };
                    var res = _planningService.CreateNeedAsync(trainingNeed);
                    savedCount++;
                }

                

                // Build response message
                var message = $"{savedCount} training need(s) saved successfully";
                if (duplicateCount > 0)
                {
                    message += $". {duplicateCount} duplicate(s) skipped";
                }

                return Json(new
                {
                    success = true,
                    message = message,
                    savedCount = savedCount,
                    duplicateCount = duplicateCount
                });
            }
            catch (Exception ex)
            {
                // Log the error
                //_logger.LogError(ex, "Error saving training needs");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving training needs. Please try again."
                });
            }
        }

        #endregion

        #region Planning
        public async Task<IActionResult> Plans(string co = "", string dp = "", int yr = 0, string st = "")
        {
            var _co = _emp.CurrentEmployee.CoCode;
            
            var _companies = await _trainingService.GetCompaniesByAccessAsync("PLAN", _emp.CurrentEmployee.EmployeeCode);

            var _departments = (await _trainingService.GetDepartmentsByCoAsync(_co)).OrderBy(x=>x.DepartmentCode).ToList();
            var _departmentsGrp = await _trainingService.GetDepartmentsByAccessAsync("PLAN", _emp.CurrentEmployee.EmployeeCode);
            //var _selectedDepartment = dp == ""? _departmentsGrp.FirstOrDefault()?.DepartmentCode?.ToString() ?? dp;
            var _selectedDepartment = _departmentsGrp.Count() > 1
                                        ? (dp == "" ? "All" : dp)
                                        : _departmentsGrp.FirstOrDefault()?.ABRV?.ToString() ?? dp;

            //var _companiesByEmp = _companies.Where(c => c.CoCode == _co).ToList();
            var _selectedCompany = _companies.FirstOrDefault()?.CoCode?.ToString() ?? "ABL";
            var _year = await _trainingService.GetFiscalYearsAsync();
            var _yearActive = _year.Where(y => y.Status == "Active").Select(y=>y.Year).FirstOrDefault();
            var _cUser = _emp.CurrentEmployee.EmployeeCode ?? "";
            var _uName = _emp.CurrentEmployee.Username ?? "";

            var viewModel = new PlanVM
            {
                Companies = _companies, //_companiesByEmp,
                SelectedCompany = co==""?_selectedCompany:co,
                Departments = _departmentsGrp,
                SelectedDepartment = _selectedDepartment, // dp==""?"All":dp,
                SelectedStatus = st==""?"All":st,
                //YearList = Enumerable.Range(DateTime.Now.Year - 1, 5).ToList(),
                YearList = _year.OrderBy(y=>y.Year).ToList(),
                SelectedYear = yr==0? _yearActive : yr,
                CurrentUser = _cUser,
                Username = _uName,
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetPlans()
        {

            try
            {
                var request = ParseDataTablePlanRequest(Request);
                var response = await _planningService.GetPlansAsync(request);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }
        private static DataTableRequest ParseDataTablePlanRequest(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DataTableRequest
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                CompanyFilter = form["companyFilter"],
                //CategoryFilter = form["categoryFilter"],
                YearFilter = form["yearFilter"],
                DepartmentFilter = form["departmentFilter"],
                StatusFilter = form["statusFilter"],
                EmployeeCode = form["currentUser"],
                Username = form["username"],
                
                //CategoryId = int.TryParse(form["category"], out var categoryId) ? categoryId : 0,
            };

            // Parse columns
            var columnIndex = 0;
            while (!string.IsNullOrEmpty(form[$"columns[{columnIndex}][data]"]))
            {
                request.Columns.Add(new DataTableColumn
                {
                    Data = form[$"columns[{columnIndex}][data]"],
                    Name = form[$"columns[{columnIndex}][name]"],
                    Searchable = bool.TryParse(form[$"columns[{columnIndex}][searchable]"], out var searchable) && searchable,
                    Orderable = bool.TryParse(form[$"columns[{columnIndex}][orderable]"], out var orderable) && orderable,
                    Search = new DataTableSearch
                    {
                        Value = form[$"columns[{columnIndex}][search][value]"],
                        Regex = bool.TryParse(form[$"columns[{columnIndex}][search][regex]"], out var regex) && regex
                    }
                });
                columnIndex++;
            }

            // Parse ordering
            var orderIndex = 0;
            while (!string.IsNullOrEmpty(form[$"order[{orderIndex}][column]"]))
            {
                request.Order.Add(new DataTableOrder
                {
                    Column = int.TryParse(form[$"order[{orderIndex}][column]"], out var orderColumn) ? orderColumn : 0,
                    Dir = form[$"order[{orderIndex}][dir]"]
                });
                orderIndex++;
            }

            return request;
        }

        public async Task<IActionResult> PlanDetails( PlanDetailParameters param, int pId = 0)
        {
            var _plans = await _planningService.GetPlansByIdAsync(pId);


            var _co = _plans.CoCode; //pCo==""?_emp.CurrentEmployee.CoCode:pCo;
            var _dept = _plans.ABRV; //pDept==""?_emp.CurrentEmployee.ABRV:pDept;
            var _status = _plans.PlanStatus; //pSt==""?"Pending":pSt;   
            var _docStatus = _plans.DocStatus; //pSt==""?"Pending":pSt; 

           var _year = (await _trainingService.GetFiscalYearsAsync()).Where(y=>y.Status == "Active" || y.Status == "Planning").ToList();
           var _yearSel = _plans.Year; //pYear==0? _yearActive: pYear;

            var _categories = await _categoryService.GetAllCategoriesAsync();
            var _courses = await _courseService.GetAllCourseAsync();

            if (_plans == null)
            {
                TempData["Error"] = $"Plan with ID {pId} was not found.";
                return RedirectToAction("Plans");
            }

            var planDet = await _planningService.GetPlansDetailAsync(_plans.PlanId);  // ✅ Returns List<PlanDetailView>
            var _actions = await _trainingService.GetActionsAsync("PLAN",_plans.Strategy, _plans.PlanId);

            return View(new PlanDetailVM
            {
                PlanDetail = planDet,  // ✅ Use the first plan detail or empty if none
                Parameters = param,
                Categories = _categories,
                Plans = _plans,
                FiscalYears = _year,
                ActionList = _actions,
                Courses = _courses,
                //PlanId = _plans.PlanId,

            });
        }

        //Data tables
        public async Task<IActionResult> PlanDetail(PlanDetailParameters param, int pId = 0) 
        {
            //var _plans = await _planningService.GetPlansByIdAsync(pId);
            var _plans = await _planningService.GetPlansPermission(pId, _emp.CurrentEmployee.Username);

            var _co = _plans.CoCode; //pCo==""?_emp.CurrentEmployee.CoCode:pCo;
            var _dept = _plans.ABRV; //pDept==""?_emp.CurrentEmployee.ABRV:pDept;
            var _status = _plans.PlanStatus; //pSt==""?"Pending":pSt;   
            var _docStatus = _plans.DocStatus; //pSt==""?"Pending":pSt; 

            var _year = (await _trainingService.GetFiscalYearsAsync()).Where(y => y.Status == "Active" || y.Status == "Planning").ToList();
            var _yearSel = _plans.Year; //pYear==0? _yearActive: pYear;

            var _categories = await _categoryService.GetAllCategoriesAsync();
            var _courses = await _courseService.GetAllCourseAsync();

            if (_plans == null)
            {
                TempData["Error"] = $"Plan with ID {pId} was not found.";
                return RedirectToAction("Plans");
            }

            //var planDet = await _planningService.GetPlansDetailAsync(_plans.PlanId);  // ✅ Returns List<PlanDetailView>
            var _actions = await _trainingService.GetActionsAsync("PLAN", _plans.Strategy, _plans.PlanId);
            var _histories = await _actionService.GetHistoriesAsync(_plans.PlanId, "PLAN");

            PlanDetailVM model = new PlanDetailVM() 
            {
                //PlanDetail = planDet,  // ✅ Use the first plan detail or empty if none
                Parameters = param,
                Categories = _categories,
                Plans = _plans,
                FiscalYears = _year,
                ActionList = _actions,
                Courses = _courses,
                Username = _emp.CurrentEmployee.Username ?? "",
                Histories = _histories,
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> GetPlansDetail()
        {

            try
            {
                var request = DataPlansDetail(Request);
                var response = await _planningService.GetPlansDetailDtAsync(request);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }
        private static DtPlansDetailRequest DataPlansDetail(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DtPlansDetailRequest
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                PlanId = form["planId"],
                Username = form["username"],
                //CategoryFilter = form["categoryFilter"],
                //YearFilter = form["yearFilter"],
                //DepartmentFilter = form["departmentFilter"],
                //StatusFilter = form["statusFilter"],

                //CategoryId = int.TryParse(form["category"], out var categoryId) ? categoryId : 0,
            };

            // Parse columns
            var columnIndex = 0;
            while (!string.IsNullOrEmpty(form[$"columns[{columnIndex}][data]"]))
            {
                request.Columns.Add(new DataTableColumn
                {
                    Data = form[$"columns[{columnIndex}][data]"],
                    Name = form[$"columns[{columnIndex}][name]"],
                    Searchable = bool.TryParse(form[$"columns[{columnIndex}][searchable]"], out var searchable) && searchable,
                    Orderable = bool.TryParse(form[$"columns[{columnIndex}][orderable]"], out var orderable) && orderable,
                    Search = new DataTableSearch
                    {
                        Value = form[$"columns[{columnIndex}][search][value]"],
                        Regex = bool.TryParse(form[$"columns[{columnIndex}][search][regex]"], out var regex) && regex
                    }
                });
                columnIndex++;
            }

            // Parse ordering
            var orderIndex = 0;
            while (!string.IsNullOrEmpty(form[$"order[{orderIndex}][column]"]))
            {
                request.Order.Add(new DataTableToggleOrder
                {
                    Column = int.TryParse(form[$"order[{orderIndex}][column]"], out var orderColumn) ? orderColumn : 0,
                    Dir = form[$"order[{orderIndex}][dir]"]
                });
                orderIndex++;
            }

            return request;
        }


        [HttpPost]
        public async Task<IActionResult> AddPlanDetail([FromBody] AddCourseRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Request is null" });

                if (string.IsNullOrWhiteSpace(request.CourseName))
                    return Json(new { success = false, message = "Course name is required." });

                if (request.Month == 0)
                    return Json(new { success = false, message = "Month is required." });

                var _plan = await _planningService.GetPlansByIdAsync(request.PlanId);
                if (_plan == null)
                    return Json(new { success = false, message = "Plan Id is invalid." });
                
                // ✅ Cek IsNewCourse dengan int
                //if (request.IsNewCourse == 1)
                //{
                //    request.CourseId = await _planningService.InsertNewCourseAsync(new CourseModel
                //    {
                //        CourseName = request.CourseName,
                //        CategoryId = request.CategoryId
                //    });
                //}

                var newDetail = new PlanDetailDTO()
                {
                    PlanId = request.PlanId,
                    PlanCode = _plan.PlanCode,
                    CourseId = request.CourseId,
                    CourseName = request.CourseName.ToUpper(),
                    CategoryId = request.CategoryId,
                    CategoryName = request.CategoryName,
                    PlannedMonth = request.Month,
                    PlannedYear = request.Year,
                    TotalSessions = request.Session,
                    TotalParticipants = request.Participant,
                    PlannedDuration = request.Duration,
                    TrainingProvider = request.Provider,
                    EstimatedCost = request.EstCost,
                    TargetParticipant = request.TargetParticipant,
                    IsNewCourse = request.IsNewCourse,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode ?? "System",

                };
                var result = await _planningService.AddPlansDetail(newDetail);
                if (result > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Plan detail added successfully."
                    });
                }
                else {                     
                    return Json(new
                    {
                        success = false,
                        message = "Failed to add plan detail."
                    });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePlanDetail(int detailId)
        {
            try
            {
                var planDetail = await _planningService.DeletePlansDetail(detailId, _emp.CurrentEmployee.EmployeeCode ?? "System");
                if (planDetail > 0)
                {
                    //await _planningService.DeletePlanDetailAsync(detailId);
                    TempData["ToastMessage"] = "Plan detail deleted successfully.";
                    TempData["ToastMode"] = "success";
                    return Json(new { success = true, message = "Deleted successfully." });
                }
                else
                {
                    TempData["ToastMessage"] = "Failed to delete plan detail.";
                    TempData["ToastMode"] = "error";
                    return Json(new { success = false, message = "Failed to delete plan detail." });
                }
                ;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNewCourse([FromBody] UpdateNewCourseRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Invalid request." });

                if (string.IsNullOrWhiteSpace(request.CourseName))
                    return Json(new { success = false, message = "Course name is required." });

                if (request.CategoryId <= 0)
                    return Json(new { success = false, message = "Category is required." });

                var username = User.Identity?.Name ?? "system";

                var result = await _planningService.UpdateNewCourseAsync(request, username);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        #endregion


        #region Function
        [HttpPost]
        public async Task<IActionResult> GetUnitsByGroup(int groupId)
        {
            try
            {
                if (groupId <= 0)
                {
                    return Json(new { success = false, message = "Invalid Group ID" });
                }

                // Get units filtered by group - adjust this query based on your database structure
                var units = (await _trainingService.GetUnitByGroupIdAsync(groupId))
                    .OrderBy(u => u.UnitCode) // Order by code for better UX
                    .Select(u => new
                    {
                        UnitId = u.UnitId,
                        UnitCode = u.UnitCode,
                        UnitName = u.UnitName
                    })
                    .ToList();

                return Json(new { success = true, data = units });
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging
                // _logger.LogError(ex, "Error getting units for group {GroupId}", groupId);

                return Json(new { success = false, message = "Error loading units" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNeed([FromBody] NeedDTO model)
        {
            try
            {

                Needs  need = new Needs() 
                { 
                    NeedId = model.NeedId,
                    IsActive = model.IsActive,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode ?? "System",
                };
                var result = await _planningService.UpdateNeedAsync(need);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating the needs"
                });
            }
        }

        #endregion




    }
}
