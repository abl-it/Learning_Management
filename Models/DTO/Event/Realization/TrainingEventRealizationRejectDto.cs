namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// Payload for the "tc" (HRD) rejection step that sends a submitted
    /// realization back to the creator. <see cref="Remarks"/> is required.
    /// </summary>
    public sealed class TrainingEventRealizationRejectDto
    {
        public long TrainingEventId { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
