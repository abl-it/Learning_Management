using Microsoft.AspNetCore.Http.HttpResults;
using System.Numerics;
using System.Reflection.Emit;
using static Azure.Core.HttpHeader;

namespace Training.Models
{
    public class PlansDetail
    {
        public string CoCode { get; set; } = "ABL";
        public int DepartmentId { get; set; }
        public string DepartmentCode { get; set; }
        public int PlanId { get; set; }
        public string PlanCode { get; set; }
        public int FiscalYearID { get; set; }
     //   public string PlanTitle { get; set; }
        public int? CourseId { get; set; }
        public string? CourseCode { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public int TotalSessions { get; set; } = 1;
        public int TotalParticipants { get; set; }
        public string TargetParticipant { get; set; }
        public int PlannedMonth { get; set; }
        public int PlannedYear { get; set; }
        public decimal PlannedDuration { get; set; }
        public string TrainingProvider { get; set; } = "Internal";
        public decimal EstimatedCost { get; set; }  = 0;
        public string PlanStatus { get; set; } = "Draft";
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovedBy { get; set; }

    }
}
