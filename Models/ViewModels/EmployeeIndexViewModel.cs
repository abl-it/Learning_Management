using Microsoft.AspNetCore.Mvc.Rendering;

namespace Training.Models.ViewModels
{
    public class EmployeeIndexViewModel
    {
        public List<SelectListItem> Departments { get; set; } = new();
        public string? SelectedDepartment { get; set; }
    }


}
