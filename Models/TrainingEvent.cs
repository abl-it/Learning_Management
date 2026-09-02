namespace Training.Models
{
    public sealed class TrainingEvent
    {
        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string? CoCode { get; set; }

        public string? Abrv { get; set; }

        public string? DeptName { get; set; }

        public string TrainingTitle { get; set; } = string.Empty;

        public string? TrainingDescription { get; set; }

        public string? TrainingObjective { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// I = Internal, E = External.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        public int? TrainingCategoryId { get; set; }

        public string? TrainerId { get; set; }

        public string? TrainerName { get; set; }

        public int ParticipantQuota { get; set; }

        public decimal? Budget { get; set; }

        public string? Venue { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public DateTime? RegistrationStartDate { get; set; }

        public DateTime? RegistrationEndDate { get; set; }

        public string? Workflow { get; set; }

        public string? Strategy { get; set; }

        public int? DocStatus { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public int? SessionCount { get; set; }

        public decimal? DurationHours { get; set; }


        public byte[] RowVersion { get; set; } = [];
    }
}
