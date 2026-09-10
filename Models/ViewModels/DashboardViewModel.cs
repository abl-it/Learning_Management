using Training.Models.Dtos;

namespace Training.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk dashboard analytics
    /// </summary>
    public class DashboardViewModel
    {
        // User & Session
        public string CurrentUsername { get; set; } = "";
        public string CurrentUserRole { get; set; } = "";

        // Period Display
        public string PeriodDisplay { get; set; } = "";
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Filter Options
        public List<FilterOptionDto> DateRangeOptions { get; set; } = new();
        public List<FilterOptionDto> CompanyOptions { get; set; } = new();
        public List<FilterOptionDto> DepartmentOptions { get; set; } = new();
        public List<FilterOptionDto> CategoryOptions { get; set; } = new();

        // Selected Filter Values
        public string SelectedDateRange { get; set; } = "";
        public string SelectedCompanies { get; set; } = "";
        public string SelectedDepartments { get; set; } = "";
        public string SelectedCategories { get; set; } = "";
        public decimal PendingApprovalsCount { get; set; } = 0;

        // KPI Metrics
        public AttendanceRateDto AttendanceMetrics { get; set; } = new();
        public CompletionRateDto CompletionMetrics { get; set; } = new();
        public List<TrainingHoursByDepartmentDto> TrainingHoursByDepartment { get; set; } = new();
        public List<PendingDocumentDto> PendingDocuments { get; set; } = new();

        // KPI Cards
        public MetricsCardDto AttendanceRateCard { get; set; } = new();
        public MetricsCardDto CompletionRateCard { get; set; } = new();
        public MetricsCardDto TrainingHoursCard { get; set; } = new();
        public MetricsCardDto PendingApprovalsCard { get; set; } = new();
        public MetricsCardDto PendingDocumentsCard { get; set; } = new();
        public MetricsCardDto DepartmentPerformanceCard { get; set; } = new();

        // Charts
        public ChartDataDto AttendanceTrendChart { get; set; } = new();
        public ChartDataDto TrainingHoursByDeptChart { get; set; } = new();
        public ChartDataDto CompletionStatusChart { get; set; } = new();
        public ChartDataDto EventsByCategoryChart { get; set; } = new();
        public ChartDataDto DepartmentPerformanceChart { get; set; } = new();
    }
}
