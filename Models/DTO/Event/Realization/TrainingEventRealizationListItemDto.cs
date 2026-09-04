namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// One row on the "Event Realization" landing page - a training event
    /// somewhere in the realization lifecycle that the current user can act
    /// on or review.
    /// </summary>
    public sealed class TrainingEventRealizationListItemDto
    {
        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public string TrainingTitle { get; set; } = string.Empty;

        public string? DeptName { get; set; }

        public string? TrainerName { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int DocStatus { get; set; }

        public DateTime? RealizationDate { get; set; }

        public string? PostedBy { get; set; }

        public DateTime? PostedDate { get; set; }
    }
}
