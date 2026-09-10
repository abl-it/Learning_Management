namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk KPI card (Attendance Rate, Completion Rate, etc)
    /// </summary>
    public class MetricsCardDto
    {
        /// <summary>
        /// Card title (e.g., "Attendance Rate")
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Main value (e.g., 85.5 untuk percentage)
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Unit/suffix (e.g., "%", "Hours")
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Change from previous period (e.g., +5.2)
        /// </summary>
        public decimal? ChangeFromPrevious { get; set; }

        /// <summary>
        /// Icon class for card (Bootstrap icons)
        /// </summary>
        public string IconClass { get; set; }

        /// <summary>
        /// Card color/badge style (primary, success, warning, danger, info)
        /// </summary>
        public string BadgeStyle { get; set; }

        /// <summary>
        /// Additional description
        /// </summary>
        public string Description { get; set; }
    }
}
