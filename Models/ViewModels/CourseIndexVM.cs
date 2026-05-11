using Microsoft.AspNetCore.Mvc.Rendering;

namespace Training.Models.ViewModels
{
    public class CourseIndexVM
    {
        public List<SelectListItem> Categories { get; set; } = new();
        public int? SelectedCategory { get; set; }

        


    }
}
