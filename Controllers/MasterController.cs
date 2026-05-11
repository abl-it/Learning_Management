using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;
using System.Threading;
using Training.Filters;
using Training.Models;
using Training.Models.DataTables;
using Training.Models.DTO;
using Training.Models.ViewModels;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class MasterController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ICourseService _courseService;
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _emp;
        private readonly IAntiforgery _antiforgery;

        public MasterController(
            ICurrentUserService emp, 
            ICategoryService categoryService, 
            ICourseService courseService, 
            ITrainingService trainingService, 
            IAntiforgery antiforgery)
        {
            _categoryService = categoryService;
            _courseService = courseService;
            _trainingService = trainingService;
            _emp = emp;
            _antiforgery = antiforgery;
            _antiforgery = antiforgery;
        }


        public IActionResult Index()
        {
            return View();
        }


        #region Categories 
         public IActionResult Categories()
        {
            var _employee = _emp.CurrentEmployee;
            ProfileViewModel profileViewModel = new ProfileViewModel
            {
                profile = _employee
            };

            return View(profileViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetCategories()
        {
            
            try
            {
                var request = ParseDataTableRequest(Request);
                var response = await _categoryService.GetCategoriesAsync(request);
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
        // POST: Master/CreateCategory
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryDto categoryDto)
        {
            
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }
                
                var result = await _categoryService.CreateCategoryAsync(categoryDto);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the category"
                });
            }
        }

        // POST: Master/UpdateCategory
        [HttpPost]
        public async Task<IActionResult> UpdateCategory(CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }

                var result = await _categoryService.UpdateCategoryAsync(categoryDto);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updating the category"
                });
            }
        }

        // POST: Master/DeleteCategory
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var _employee = _emp.CurrentEmployee;
                var result = await _categoryService.DeleteCategoryAsync(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error deleting category with ID: {Id}", id);
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while deleting the category"
                });
            }
        }

        // GET: Master/GetCategory/{id}
        [HttpGet]
        public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Json(new ApiResponse
                {
                    Success = true,
                    Data = category
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error getting category with ID: {Id}", id);
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while getting the category"
                });
            }
        }

        #endregion

        #region Course

        [HttpGet]
        public async Task<IActionResult> Course(CancellationToken cancellationToken)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();

            var viewModel = new CourseIndexVM
            {
                Categories = categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .Prepend(new SelectListItem
                    {
                        Value = "All",
                        Text = "All Categories"
                    })
                    .ToList()
            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }
        // ✅ Custom Antiforgery Validation for AJAX calls
        private async Task<bool> ValidateAntiForgeryTokenAsync()
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(HttpContext);
                return true;
            }
            catch (AntiforgeryValidationException)
            {
                return false;
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetCourses(CancellationToken cancellationToken)
        {

            try
            {
                var request = ParseDataTableCourseRequest(Request);
                var response = await _courseService.GetCoursesAsync(request, cancellationToken);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(CourseDto model)
        {

            try
            {
                // ✅ Manual antiforgery validation for AJAX
                if (!await ValidateAntiForgeryTokenAsync())
                {
                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid security token. Please refresh the page and try again."
                    });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }

                var course = new CourseDto()
                {
                    CategoryId = model.CategoryId,
                    CourseName = model.CourseName,
                    Description = model.Description,
                    DurationHours = model.DurationHours,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    IsActive = model.IsActive,
                    TrainingProvider = model.TrainingProvider,
                };

                var result = await _courseService.CreateCourseAsync(course);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the course"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCourse(CourseDto model)
        {

            try
            {
                // ✅ Manual antiforgery validation for AJAX
                if (!await ValidateAntiForgeryTokenAsync())
                {
                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid security token. Please refresh the page and try again."
                    });
                }
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }

                var course = new CourseDto()
                {
                    CourseId = model.CourseId,
                    CategoryId = model.CategoryId,
                    CourseName = model.CourseName,
                    Description = model.Description,
                    DurationHours = model.DurationHours,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    IsActive = model.IsActive,
                    TrainingProvider = model.TrainingProvider,
                };

                var result = await _courseService.UpdateCourseAsync(course);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the course"
                });
            }
        }

        private static DataTableRequest ParseDataTableCourseRequest(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DataTableRequest
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                CategoryFilter = form["categoryFilter"],
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


        public async Task<IActionResult> SearchCourses(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 3)
                {
                    return Json(new { success = false, message = "Query too short" });
                }

                var _courses = await _courseService.GetAllCourseAsync();
                var courses = _courses
                    .Where(c => c.IsActive == 1 &&
                           (c.CourseCode.ToLower().Contains(query.ToLower()) ||
                            c.CourseName.ToLower().Contains(query.ToLower())))
                    .Select(c => new
                    {
                        CourseId = c.CourseId,
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        CategoryCode = c.CategoryCode,
                        Color = c.Color,
                    })
                    .Take(10)
                    .ToList();

                return Json(new { success = true, data = courses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Group
        [HttpGet]
        public async Task<IActionResult> Groups()
        {
            var _companies = await _trainingService.GetAllCompaniesAsync();
            var _sCo = _companies
                        .OrderBy(c => c.CoCode)
                        .FirstOrDefault()
                        .CoCode;

            var viewModel = new GroupVM
            {
                Companies = _companies,
                SelectedCompany = _sCo,
            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetGroups()
        {

            try
            {
                var request = ParseDataTableGroupRequest(Request);
                var response = await _trainingService.GetGroupsAsync(request);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        private static DataTableRequestMaster ParseDataTableGroupRequest(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DataTableRequestMaster
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                //CategoryFilter = form["categoryFilter"],
                //StatusFilter = form["statusFilter"],
                CompanyFilter = form["companyFilter"],
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
        public async Task<IActionResult> AddGroup([FromBody] GroupDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }
                
                
                var _group = new Group()
                {
                    GroupCode = model.GroupCode.ToUpper(),
                    Description = model.Description,
                    CoCode = model.CoCode,
                    IsActive = model.IsActive,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _trainingService.CreateGroupAsync(_group);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the course"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGroup([FromBody] GroupDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }


                var _group = new Group()
                {
                    GroupId = model.GroupId,
                    GroupCode = model.GroupCode.ToUpper(),
                    Description = model.Description,
                    CoCode = model.CoCode,
                    IsActive = model.IsActive,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _trainingService.UpdateGroupAsync(_group);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the course"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup([FromBody] int groupId)
        {
            try
            {
                // check is alredy has units
                var units = await _trainingService.GetUnitByGroupIdAsync(groupId);
                if (units != null && units.Count > 0)
                {
                    return Json(new { success = false, message = "Cannot delete group with associated units." });
                }

                var result = await _trainingService.DeleteGroupAsync(groupId);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the course"
                });
            }
        }

        // Get Groups by Company
        [HttpPost]
        public async Task<IActionResult> GetGroupsByCompany(string companyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(companyCode))
                {
                    return Json(new { success = false, message = "Company code is required" });
                }

                var groups = (await _trainingService.GetAllGroupsAsync())
                    .Where(x => x.CoCode == companyCode && x.IsActive == 1)
                    .OrderBy(x => x.GroupCode)
                    .Select(x => new
                    {
                        groupId = x.GroupId,
                        groupCode = x.GroupCode,
                        description = x.Description
                    });
                
                return Json(new
                {
                    success = true,
                    data = groups,
                    message = $"Found {groups.Count()} groups for company {companyCode}"
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error loading groups for company: {CompanyCode}", companyCode);
                return Json(new { success = false, message = "Error loading groups" });
            }
        }


        #endregion

        #region Units
        public async Task<IActionResult> Units()
        {
            var _companies = await _trainingService.GetAllCompaniesAsync();
            var _sCo = _companies
                        .OrderBy(c => c.CoCode)
                        .FirstOrDefault()
                        .CoCode;
            var _groups = await _trainingService.GetAllGroupsAsync();
            var _activeGroups = _groups.Where(g => g.IsActive == 1 && g.CoCode == _sCo).ToList();
            
            var viewModel = new UnitVM
            {
                Groups = _activeGroups,
                SelectedGroup = "0",
                Companies = _companies,
                SelectedCompany = _sCo,
            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> GetUnits()
        {

            try
            {
                var request = ParseDataTableUnitRequest(Request);
                var response = await _trainingService.GetUnitsAsync(request);
                return Json(response);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }
        private static DataTableRequestMaster ParseDataTableUnitRequest(HttpRequest httpRequest)
        {
            var form = httpRequest.Form;
            var request = new DataTableRequestMaster
            {
                Draw = int.TryParse(form["draw"], out var draw) ? draw : 0,
                Start = int.TryParse(form["start"], out var start) ? start : 0,
                Length = int.TryParse(form["length"], out var length) ? length : 10,
                SearchValue = form["search[value]"],

                // Custom filters
                CompanyFilter = form["companyFilter"],
                CategoryFilter = form["categoryFilter"],
                StatusFilter = form["statusFilter"],
                GroupFilter = form["groupFilter"],

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
        public async Task<IActionResult> AddUnit([FromBody] UnitDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }


                var _unit = new Unit()
                {
                    UnitCode = model.UnitCode.ToUpper(),
                    UnitName = model.UnitName,
                    GroupId = model.GroupId,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _trainingService.CreateUnitAsync(_unit);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while creating the unit"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUnit([FromBody] UnitDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new ApiResponse
                    {
                        Success = false,
                        Message = string.Join(", ", errors)
                    });
                }


                var _unit = new Unit()
                {
                    GroupId = model.GroupId,
                    UnitId = model.UnitId,
                    UnitCode = model.UnitCode.ToUpper(),
                    UnitName = model.UnitName,
                    CreatedBy = _emp.CurrentEmployee.EmployeeCode,
                    CreatedDate = DateTime.UtcNow

                };

                var result = await _trainingService.UpdateUnitAsync(_unit);
                return Json(result);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating category");
                return Json(new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while updateting the unit"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetUnitsByCompany(string companyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(companyCode))
                {
                    return Json(new { success = false, message = "Company code is required" });
                }

                var units = (await _trainingService.GetAllUnitsAsync())
                    .Where(x => x.CoCode == companyCode)
                    .OrderBy(x => x.UnitCode)
                    .Select(x => new
                    {
                        unitId = x.UnitId,
                        unitCode = x.UnitCode,
                        unitName = x.UnitName
                    });

                return Json(new
                {
                    success = true,
                    data = units,
                    message = $"Found {units.Count()} units for company {companyCode}"
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error loading groups for company: {CompanyCode}", companyCode);
                return Json(new { success = false, message = "Error loading units" });
            }
        }
        #endregion

        #region Matrix
        [HttpGet]
        public async Task<IActionResult> Matrix(CancellationToken cancellationToken)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var viewModel = new CourseIndexVM
            {
                Categories = categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName
                    })
                    .Prepend(new SelectListItem
                    {
                        Value = "All",
                        Text = "All Categories"
                    })
                    .ToList()
            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }

        #endregion

        #region Initial
        public async Task<IActionResult> Initial()
        {
            return View();
        }
        #endregion


    }
}
