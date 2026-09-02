using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Training.Filters;
using Training.Models;
using Training.Models.DTO;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _emp;

        public HomeController(ILogger<HomeController> logger, ITrainingService trainingService, ICurrentUserService emp)
        {
            _logger = logger;
            _trainingService = trainingService;
            _emp = emp;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Common
        [HttpGet]
        public async Task<IActionResult> GetCompanies(string type)
        {
            try
            {
                if (_emp.CurrentEmployee == null)
                {
                    return StatusCode(
                        StatusCodes.Status401Unauthorized,
                        new
                        {
                            success = false,
                            message = "Current employee tidak ditemukan."
                        });
                }

                var employeeCode =
                    _emp.CurrentEmployee.EmployeeCode;

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "EmployeeCode kosong."
                    });
                }

                var companies =
                    await _trainingService
                        .GetCompaniesByAccessAsync(
                            type,
                            employeeCode);

                var result = companies
                    .Select(x => new SelectOptionDto
                    {
                        Value = x.CoCode,
                        Text = x.CompanyName
                    })
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading companies. Type: {Type}",
                    type);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDepartments(string type)
        {
            try
            {
                if (_emp.CurrentEmployee == null)
                {
                    return StatusCode(
                        StatusCodes.Status401Unauthorized,
                        new
                        {
                            success = false,
                            message = "Current employee tidak ditemukan."
                        });
                }

                var employeeCode =
                    _emp.CurrentEmployee.EmployeeCode;

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "EmployeeCode kosong."
                    });
                }

                var departments =
                    await _trainingService
                        .GetDepartmentsByAccessAsync(
                            type,
                            employeeCode);

                var result = departments
                    .Select(x => new SelectOptionDto
                    {
                        Value = x.ABRV,
                        Text = x.DeptName
                    })
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading departments. Type: {Type}",
                    type);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message
                    });
            }
        }
        //[HttpGet]
        //public async Task<IActionResult> GetDepartments(string type )
        //{
        //    var _departments = await _trainingService.GetDepartmentsByAccessAsync(type, _emp.CurrentEmployee.EmployeeCode);
        //    var result = _departments
        //        .Select(x => new SelectOptionDto
        //        {
        //            Value = x.ABRV,
        //            Text = x.DeptName
        //        })
        //        .ToList();
        //    return Json(result);
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetCompanies(string type)
        //{
        //    var _companies = await _trainingService.GetCompaniesByAccessAsync(type, _emp.CurrentEmployee.EmployeeCode);
        //    var result = _companies
        //        .Select(x => new SelectOptionDto
        //        {
        //            Value = x.CoCode,
        //            Text = x.CompanyName
        //        })
        //        .ToList();
        //    return Json(result);
        //}

        [HttpGet]
        public async Task<IActionResult> SearchEmployees(
            string? term,
            CancellationToken cancellationToken)
        {
            var employees =
                await _trainingService.SearchEmployeesAsync(
                    term,
                    cancellationToken);

            var result = employees
                .Select(x => new
                {
                    employeeCode = x.EmployeeCode,
                    fullName = x.FullName,
                    abrv = x.ABRV,
                    deptName = x.DeptName,
                })
                .ToList();

            return Json(result);
        }

        #endregion
    }
}
