using Microsoft.AspNetCore.Mvc.Rendering;

namespace Training.Models.ViewModels
{
    public class EventViewModel
    {
        public List<SelectListItem> Categories { get; set; } = new();
        public int? SelectedCategory { get; set; }

        public List<Department> Departments { get; set; } = new();
        public int? SelectedDepartment { get; set; }

        public List<Company> Companies { get; set; } = new();
        public int? SelectedCompany { get; set; }

    }
}
