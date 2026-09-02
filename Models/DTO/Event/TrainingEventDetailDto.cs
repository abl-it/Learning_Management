namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventDetailDto
    {
        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string? CourseCode { get; set; }

        public string? CourseName { get; set; }

        public int TrainingCategoryId { get; set; }

        public string? TrainingCategory { get; set; }

        public string TrainingTitle { get; set; } = string.Empty;

        public string? TrainingDescription { get; set; }

        public string? TrainingObjective { get; set; }

        public string CoCode { get; set; } = string.Empty;

        public string ABRV { get; set; } = string.Empty;

        public string? DeptName { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? TrainerId { get; set; }

        public string? TrainerName { get; set; }

        public int ParticipantQuota { get; set; }

        public decimal? Budget { get; set; }

        public string? Venue { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public int? SessionCount { get; set; }

        public decimal? DurationHours { get; set; }

        public string? Workflow { get; set; }

        public string? Strategy { get; set; }

        public string? Status { get; set; }

        public int? DocStatus { get; set; }

        public string? RejectReason { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public List<TrainingEventParticipantDto> Participants { get; set; } = [];

        public bool CanShow { get; set; }

        public bool CanEdit { get; set; }

        public bool CanAction { get; set; }

        /// <summary>
        /// Determines which workflow action should be displayed on the detail page.
        /// </summary>
        public string? AvailableAction { get; set; }

        public List<History> Histories { get; set; } = [];
    }

   

}
