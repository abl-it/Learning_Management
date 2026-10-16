namespace Training.Models.DTO.Event
{
    /// <summary>
    /// Baris training plan yang bisa dipilih saat Create Event
    /// (hasil dbo.usp_TrainingEvent_GetPlanOptions).
    /// </summary>
    public sealed class TrainingEventPlanOptionDto
    {
        public int PlanDetailId { get; set; }

        public int PlanId { get; set; }

        public string PlanCode { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string? CourseCode { get; set; }

        public string? CourseName { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int PlannedMonth { get; set; }

        public int TotalSessions { get; set; }

        public int UsedSessions { get; set; }

        public int RemainingSessions { get; set; }

        public int TotalParticipants { get; set; }

        public decimal PlannedDuration { get; set; }

        public decimal EstimatedCost { get; set; }

        /// <summary>"I" = Internal, "E" = External.</summary>
        public string EventType { get; set; } = "I";
    }
}
