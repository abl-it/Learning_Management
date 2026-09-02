namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventCreateResultDto
    {
        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public int TrainingCategoryId { get; set; }

        public int ParticipantQuota { get; set; }

        public string Workflow { get; set; } = string.Empty;

        public string Strategy { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int DocStatus { get; set; }

    }
}
