using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Training.Models.DataTables;
using Training.Services;
using Training.Services.IServices;

namespace Training.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        //private readonly IAntiforgery _antiforgery;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
            //_antiforgery = antiforgery;
        }

        public IActionResult Index()
        {
            return View();
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

                // Custom filters
                //DepartmentFilter = form["departmentFilter"],
                //HireDateFrom = DateTime.TryParse(form["hireDateFrom"], out var dateFrom) ? dateFrom : null,
                //HireDateTo = DateTime.TryParse(form["hireDateTo"], out var dateTo) ? dateTo : null,
                //MinSalary = decimal.TryParse(form["minSalary"], out var minSal) ? minSal : null,
                //MaxSalary = decimal.TryParse(form["maxSalary"], out var maxSal) ? maxSal : null
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
