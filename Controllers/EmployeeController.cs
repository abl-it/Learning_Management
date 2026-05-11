using Training.Models.DataTables;
using Training.Models.ViewModels;
using Training.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Training.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAntiforgery _antiforgery;

        public EmployeeController(IEmployeeService employeeService, IAntiforgery antiforgery)
        {
            _employeeService = employeeService;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var departments = await _employeeService.GetDepartmentsAsync(cancellationToken);

            var viewModel = new EmployeeIndexViewModel
            {
                Departments = departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Name,
                        Text = d.Name
                    })
                    .Prepend(new SelectListItem
                    {
                        Value = "All",
                        Text = "All Departments"
                    })
                    .ToList()
            };

            // Generate anti-forgery token for AJAX requests
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken)
        {
            try
            {
                var request = ParseDataTableRequest(Request);
                var response = await _employeeService.GetEmployeesAsync(request, cancellationToken);
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
                DepartmentFilter = form["departmentFilter"],
                HireDateFrom = DateTime.TryParse(form["hireDateFrom"], out var dateFrom) ? dateFrom : null,
                HireDateTo = DateTime.TryParse(form["hireDateTo"], out var dateTo) ? dateTo : null,
                MinSalary = decimal.TryParse(form["minSalary"], out var minSal) ? minSal : null,
                MaxSalary = decimal.TryParse(form["maxSalary"], out var maxSal) ? maxSal : null
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





    }
}
