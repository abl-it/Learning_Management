namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk chart data (Chart.js compatible)
    /// </summary>
    public class ChartDataDto
    {
        /// <summary>
        /// Chart labels (x-axis)
        /// </summary>
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// Chart datasets
        /// </summary>
        public List<ChartDatasetDto> Datasets { get; set; } = new List<ChartDatasetDto>();

        /// <summary>
        /// Chart type (line, bar, pie, doughnut, etc)
        /// </summary>
        public string ChartType { get; set; }

        /// <summary>
        /// Chart title
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// Single dataset untuk chart
    /// </summary>
    public class ChartDatasetDto
    {
        /// <summary>
        /// Dataset label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Data values
        /// </summary>
        public List<decimal> Data { get; set; } = new List<decimal>();

        /// <summary>
        /// Border color
        /// </summary>
        public string BorderColor { get; set; }

        /// <summary>
        /// Background color
        /// </summary>
        public string BackgroundColor { get; set; }

        /// <summary>
        /// Fill untuk line chart
        /// </summary>
        public bool Fill { get; set; } = false;

        /// <summary>
        /// Tension untuk line chart
        /// </summary>
        public decimal Tension { get; set; } = 0.4m;
    }
}
