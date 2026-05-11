namespace Training.Models.DTO
{
    public class PlanDetailDTO
    {
        public int PlanDetailId { get; set; }
        public int PlanId { get; set; }
        public string PlanCode { get; set; }
     //   public string PlanTitle { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; } = string.Empty;
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
}
