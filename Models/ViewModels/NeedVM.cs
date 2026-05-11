namespace Training.Models.ViewModels
{
    public class NeedVM
    {
        public List<Group> Groups { get; set; } = new();
        public string SelectedGroup { get; set; }
        public List<Unit> Units { get; set; } = new();
        public string SelectedUnit { get; set; }
        public List<Category> Categories { get; set; } = new();
        public string SelectedCategory { get; set; }

        public List<Company> Companies { get; set; } = new();
        public string SelectedCompany { get; set; }



    }

    public class NeedDisp 
    {
        public string NeedId { get; set; }
        public int Year { get; set; }
        public int UnitId { get; set; }
        public string CourseId { get; set; }
        public string CoCode { get; set; }
        public string UnitCode { get; set; }
        public string UnitName { get; set; }

        public int GroupId { get; set; }
        public string GroupCode { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public string Color { get; set; }
        public int IsActive { get; set; }

    }

}
