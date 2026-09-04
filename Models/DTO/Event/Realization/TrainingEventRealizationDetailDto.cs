namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// Full data required to render the "Entry Realization" page: the parent
    /// training event's summary, the realization header (if entered yet),
    /// the attendance list, and any uploaded attachments.
    /// </summary>
    public sealed class TrainingEventRealizationDetailDto
    {
        // ----- Parent training event (read-only context) -----

        public long TrainingEventId { get; set; }

        public string EventCode { get; set; } = string.Empty;

        public string TrainingTitle { get; set; } = string.Empty;

        public string? TrainerName { get; set; }

        public string? DeptName { get; set; }

        public string? Venue { get; set; }

        public string EventStatus { get; set; } = string.Empty;

        public int EventDocStatus { get; set; }

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public int ParticipantQuota { get; set; }

        // ----- Realization header -----

        public long? TrainingEventRealizationId { get; set; }

        public DateTime? RealizationDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string? Notes { get; set; }

        public string RealizationStatus { get; set; } = "Draft";

        public string? PostedBy { get; set; }

        public DateTime? PostedDate { get; set; }

        // ----- Related data -----

        public List<TrainingEventRealizationParticipantDto> Participants { get; set; } = [];

        public List<TrainingEventAttachmentDto> Attachments { get; set; } = [];

        public List<Training.Models.History> Histories { get; set; } = [];

        // ----- Computed permissions (set by the service, not the database) -----

        /// <summary>
        /// True when the realization form can still be edited: the parent
        /// event is Approved and realization hasn't been posted to HRD yet.
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// True when the current user holds the "tc" (HRD) role and the
        /// parent event is waiting for HRD to confirm the realization
        /// (Status == "Realization Submitted"). Set by the controller,
        /// since it requires a role lookup outside this service.
        /// </summary>
        public bool CanComplete { get; set; }

        /// <summary>
        /// True under the same condition as <see cref="CanComplete"/> - the
        /// "tc" role may either complete or reject a submitted realization.
        /// </summary>
        public bool CanReject { get; set; }

        public bool IsPosted =>
            string.Equals(RealizationStatus, "Posted", StringComparison.OrdinalIgnoreCase);
    }
}
