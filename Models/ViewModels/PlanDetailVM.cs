using System.Runtime.CompilerServices;

namespace Training.Models.ViewModels
{
    public class PlanDetailVM
    {
        public List<Course> Courses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<PlanDetailView> PlanDetail { get; set; } 
        public PlanDetailParameters Parameters { get; set; }

        public Plans Plans { get; set; }

        //    public List<Company> Companies { get; set; }
        //    public List<Department> Departments { get; set; }

        //    public string SelectedDepartment { get; set; }
        //    public string SelectedCompany { get; set; }

        public List<FiscalYears> FiscalYears { get; set;} = new();
    //    public int SelectedYear { get; set; }

      

        public List<Actions> ActionList { get; set; }

        public string SelectedAction { get; set; }
        public int PlanId { get; set; }
    }

    public class PlanDetailView
    {
        public int PlanDetailId { get; set; }
        public int PlanId { get; set; }
        public string PlanCode { get; set; }
       // public string PlanTitle { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public int TotalSessions { get; set; }
        public int TotalParticipants { get; set; }
        public string? TargetParticipant { get; set; }
        public int PlannedMonth { get; set; }
        public int PlannedYear { get; set; }
        public decimal PlannedDuration { get; set; }
        public string? TrainingProvider { get; set; }
        public decimal EstimatedCost { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public int IsNewCourse { get; set; } 

    }

    public class PlanDetailParameters
    {
        public string Co { get; set; } = "ABL";
        public string Dp { get; set; } = string.Empty;
        public string St { get; set; } = string.Empty;
        public int Yr { get; set; } = 0;
        public int Id { get; set; } = 0;
    }


}
