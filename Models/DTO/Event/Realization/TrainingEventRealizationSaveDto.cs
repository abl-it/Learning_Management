namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// Payload submitted from the Realization page, both for saving a draft
    /// and for the final "Post to HRD" submission.
    /// </summary>
    public sealed class TrainingEventRealizationSaveDto
    {
        public long TrainingEventId { get; set; }

        public DateTime RealizationDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string? Notes { get; set; }

        /// <summary>
        /// Optional remark recorded in the workflow history. Only meaningful
        /// when posting to HRD.
        /// </summary>
        public string? Remarks { get; set; }

        public List<TrainingEventRealizationParticipantSaveDto> Participants { get; set; } = [];
    }

    /// <summary>
    /// One attendance row as submitted from the Realization page.
    /// </summary>
    /// <remarks>
    /// Only EmployeeCode, PlannedParticipantId, AttendanceStatus and Remarks
    /// are persisted - dbo.TrainingEventActualParticipant does not store a
    /// denormalized Name/ABRV/DeptName. Display names are resolved from the
    /// employee master when the page is loaded, not from this payload.
    /// </remarks>
    public sealed class TrainingEventRealizationParticipantSaveDto
    {
        /// <summary><see langword="null"/> for a walk-in participant.</summary>
        public long? PlannedParticipantId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>PRESENT, PARTIAL, or ABSENT.</summary>
        public string AttendanceStatus { get; set; } = "ABSENT";

        public string? Remarks { get; set; }
    }
}
