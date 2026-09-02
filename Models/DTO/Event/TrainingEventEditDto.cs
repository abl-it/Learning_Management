namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventEditDto
    {
        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string? CourseCode { get; set; }

        public int TrainingCategoryId { get; set; }

        public string TrainingTitle { get; set; } = string.Empty;

        public string? TrainingDescription { get; set; }

        public string? TrainingObjective { get; set; }

        public string CoCode { get; set; } = string.Empty;

        public string ABRV { get; set; } = string.Empty;

        public string? DeptName { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? TrainerId { get; set; }

        public string? TrainerName { get; set; }

        public decimal? Budget { get; set; }

        public string? Venue { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public int? SessionCount { get; set; }

        public decimal? DurationHours { get; set; }

        public int ParticipantQuota { get; set; }

        

        public List<TrainingEventParticipantDto> Participants { get; set; } = [];
    }
}
