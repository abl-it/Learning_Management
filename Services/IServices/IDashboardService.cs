using Training.Models.Dtos;
using Training.Models.ViewModels;

namespace Training.Services.IServices
{
    /// <summary>
    /// Interface untuk Dashboard Analytics Service
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Mendapatkan semua metrics untuk dashboard
        /// </summary>
        Task<DashboardViewModel> GetDashboardMetricsAsync(
            string username,
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds);

        /// <summary>
        /// Mendapatkan daftar company untuk filter
        /// </summary>
        Task<List<FilterOptionDto>> GetCompanyOptionsAsync(string username = "");

        /// <summary>
        /// Mendapatkan daftar department untuk filter
        /// </summary>
        Task<List<FilterOptionDto>> GetDepartmentOptionsAsync(
            string username,
            string companyCodes,
            string currentSelection = "");

        /// <summary>
        /// Mendapatkan daftar category untuk filter
        /// </summary>
        Task<List<FilterOptionDto>> GetCategoryOptionsAsync(string currentSelection = "");

        // Metrics Methods
        Task<AttendanceRateDto> GetAttendanceRateAsync(
            string dateRange,
            string companyCodes,
            string departmentIds);

        Task<CompletionRateDto> GetCompletionRateAsync(
            string dateRange,
            string companyCodes,
            string departmentIds);

        Task<List<TrainingHoursByDepartmentDto>> GetTrainingHoursByDepartmentAsync(
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds);

        Task<List<PendingDocumentDto>> GetPendingDocumentsAsync(
            string username,
            string companyCodes,
            string departmentIds);

        // Chart Methods
        Task<ChartDataDto> GetAttendanceTrendChartAsync(
            string dateRange,
            string companyCodes,
            string departmentIds);

        Task<ChartDataDto> GetTrainingHoursByDeptChartAsync(
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds);

        Task<ChartDataDto> GetCompletionStatusChartAsync(
            string dateRange,
            string companyCodes,
            string departmentIds);

        Task<ChartDataDto> GetEventsByCategoryChartAsync(
            string dateRange,
            string companyCodes,
            string departmentIds);

        Task<ChartDataDto> GetDepartmentPerformanceChartAsync(
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds);
    }
}
