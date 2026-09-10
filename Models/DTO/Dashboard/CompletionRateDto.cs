namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk training completion rate metrics
    /// </summary>
    public class CompletionRateDto
    {
        /// <summary>
        /// Total training events yang completed (TrainingEventRealization WHERE Status = 'Posted')
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Total training events yang planned
        /// </summary>
        public int PlannedCount { get; set; }

        /// <summary>
        /// Calculated completion rate percentage
        /// </summary>
        public decimal CompletionRate => PlannedCount > 0 ? (decimal)CompletedCount / PlannedCount * 100 : 0;

        /// <summary>
        /// Draft/pending count
        /// </summary>
        public int PendingCount => PlannedCount - CompletedCount;
    }
}
