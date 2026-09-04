namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// Payload for the "tc" (HRD) confirmation step that marks a posted
    /// realization as complete.
    /// </summary>
    public sealed class TrainingEventRealizationCompleteDto
    {
        public long TrainingEventId { get; set; }

        public string? Remarks { get; set; }
    }
}
