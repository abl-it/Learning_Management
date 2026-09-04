namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// One row in the realization attendance list - either a participant who
    /// was originally registered (<see cref="PlannedParticipantId"/> set) or
    /// a walk-in added during realization entry (<see cref="IsWalkIn"/> true).
    /// </summary>
    public sealed class TrainingEventRealizationParticipantDto
    {
        /// <summary>
        /// Identifier of the original registration row in
        /// TrainingEventParticipant, or <see langword="null"/> for a walk-in.
        /// </summary>
        public long? PlannedParticipantId { get; set; }

        /// <summary>
        /// Identifier of the saved attendance row, once realization has been
        /// saved at least once. <see langword="null"/> before the first save.
        /// </summary>
        public long? TrainingEventActualParticipantId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ABRV { get; set; }

        public string? DeptName { get; set; }

        /// <summary>PRESENT, PARTIAL, or ABSENT.</summary>
        public string AttendanceStatus { get; set; } = "ABSENT";

        public string? Remarks { get; set; }

        public bool IsWalkIn { get; set; }
    }
}
