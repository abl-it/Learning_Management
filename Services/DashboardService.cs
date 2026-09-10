using Dapper;
using Microsoft.Data.SqlClient;
using Training.Models.Dtos;
using Training.Models.ViewModels;
using Training.Services.Interfaces;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Service untuk dashboard analytics
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly string _connectionString;
        private readonly ILogger<DashboardService> _logger;
        private readonly ITrainingService _trainingService;

        public DashboardService(
            IConfiguration configuration,
            ILogger<DashboardService> logger,
            ITrainingService trainingService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
            _trainingService = trainingService;
        }

        #region Main Dashboard Method

        public async Task<DashboardViewModel> GetDashboardMetricsAsync(
            string username,
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds)
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    CurrentUsername = username,
                    CurrentUserRole = await _trainingService.GetRoleAsync(username),
                    SelectedDateRange = dateRange,
                    SelectedCompanies = companyCodes,
                    SelectedDepartments = departmentIds,
                    SelectedCategories = categoryIds,
                    PeriodDisplay = GetPeriodDisplay(dateRange)
                };

                // Get company options
                viewModel.CompanyOptions = await GetCompanyOptionsAsync(username);
                if (string.IsNullOrEmpty(companyCodes))
                {
                    // Default: ABL only (tidak menggabungkan semua company)
                    companyCodes = viewModel.CompanyOptions.Any(c => c.Value == "ABL")
                        ? "ABL"
                        : viewModel.CompanyOptions.Select(c => c.Value).FirstOrDefault() ?? "";
                    viewModel.SelectedCompanies = companyCodes;
                }

                // Get department access for this user
                var accessibleDepts = await GetAccessibleDepartmentsAsync(username, companyCodes);
                if (string.IsNullOrEmpty(departmentIds))
                {
                    departmentIds = string.Join(",", accessibleDepts.Select(d => d.Value));
                }

                // Get filter options
                viewModel.DateRangeOptions = GetDateRangeOptions(dateRange);
                viewModel.DepartmentOptions = await GetDepartmentOptionsAsync(username, companyCodes, departmentIds);
                viewModel.CategoryOptions = await GetCategoryOptionsAsync(categoryIds);

                // Get all metrics in parallel for better performance
                var attendanceTask = GetAttendanceRateAsync(dateRange, companyCodes, departmentIds);
                var completionTask = GetCompletionRateAsync(dateRange, companyCodes, departmentIds);
                var hoursTask = GetTrainingHoursByDepartmentAsync(dateRange, companyCodes, departmentIds, categoryIds);
                var pendingTask = GetPendingDocumentsAsync(username, companyCodes, departmentIds);
                var attendanceTrendTask = GetAttendanceTrendChartAsync(dateRange, companyCodes, departmentIds);
                var hoursChartTask = GetTrainingHoursByDeptChartAsync(dateRange, companyCodes, departmentIds, categoryIds);
                var completionChartTask = GetCompletionStatusChartAsync(dateRange, companyCodes, departmentIds);
                var categoryChartTask = GetEventsByCategoryChartAsync(dateRange, companyCodes, departmentIds);
                var deptPerfChartTask = GetDepartmentPerformanceChartAsync(dateRange, companyCodes, departmentIds, categoryIds);

                await Task.WhenAll(
                    attendanceTask, completionTask, hoursTask, pendingTask,
                    attendanceTrendTask, hoursChartTask, completionChartTask,
                    categoryChartTask, deptPerfChartTask);

                // Set metrics data
                viewModel.AttendanceMetrics = await attendanceTask;
                viewModel.CompletionMetrics = await completionTask;
                viewModel.TrainingHoursByDepartment = await hoursTask;
                viewModel.PendingDocuments = await pendingTask;

                // Build KPI cards
                viewModel.AttendanceRateCard = new MetricsCardDto
                {
                    Title = "Attendance Rate",
                    Value = viewModel.AttendanceMetrics.AttendanceRate,
                    Unit = "%",
                    IconClass = "bi-person-check",
                    BadgeStyle = viewModel.AttendanceMetrics.AttendanceRate >= 80 ? "success" : "warning",
                    Description = $"{viewModel.AttendanceMetrics.AttendeeCount} / {viewModel.AttendanceMetrics.TotalParticipants} attended"
                };

                viewModel.CompletionRateCard = new MetricsCardDto
                {
                    Title = "Completion Rate",
                    Value = viewModel.CompletionMetrics.CompletionRate,
                    Unit = "%",
                    IconClass = "bi-check-circle",
                    BadgeStyle = viewModel.CompletionMetrics.CompletionRate >= 90 ? "success" : "warning",
                    Description = $"{viewModel.CompletionMetrics.CompletedCount} / {viewModel.CompletionMetrics.PlannedCount} completed"
                };

                var totalHours = viewModel.TrainingHoursByDepartment.Sum(d => d.TotalHours);
                viewModel.TrainingHoursCard = new MetricsCardDto
                {
                    Title = "Total Training Hours",
                    Value = totalHours,
                    Unit = " Hours",
                    IconClass = "bi-clock",
                    BadgeStyle = "info",
                    Description = $"{viewModel.TrainingHoursByDepartment.Count} departments"
                };

                viewModel.PendingDocumentsCard = new MetricsCardDto
                {
                    Title = "Pending Approvals",
                    Value = viewModel.PendingDocuments.Count,
                    Unit = "",
                    IconClass = "bi-inbox",
                    BadgeStyle = viewModel.PendingDocuments.Count > 0 ? "danger" : "success",
                    Description = $"Awaiting action"
                };

                // Set chart data
                viewModel.AttendanceTrendChart = await attendanceTrendTask;
                viewModel.TrainingHoursByDeptChart = await hoursChartTask;
                viewModel.CompletionStatusChart = await completionChartTask;
                viewModel.EventsByCategoryChart = await categoryChartTask;
                viewModel.DepartmentPerformanceChart = await deptPerfChartTask;

                // Avg Department Performance = rata-rata Attendance Rate per department
                // dari data yang sama dipakai DepartmentPerformanceChart (bar chart per dept)
                var perfValues = viewModel.DepartmentPerformanceChart?.Datasets?.FirstOrDefault()?.Data;
                var avgDeptPerformance = (perfValues != null && perfValues.Count > 0)
                    ? perfValues.Average()
                    : 0m;
                var deptCountInChart = viewModel.DepartmentPerformanceChart?.Labels?.Count ?? 0;

                viewModel.DepartmentPerformanceCard = new MetricsCardDto
                {
                    Title = "Avg Department Performance",
                    Value = avgDeptPerformance,
                    Unit = "%",
                    IconClass = "bi-building",
                    BadgeStyle = avgDeptPerformance >= 80 ? "success" : "warning",
                    Description = $"Across {deptCountInChart} department(s)"
                };

                viewModel.LastUpdated = DateTime.Now;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard metrics for {Username}: {Message}", username, ex.Message);
                throw;
            }
        }

        #endregion

        #region Attendance & Completion Metrics

        public async Task<AttendanceRateDto> GetAttendanceRateAsync(string dateRange, string companyCodes, string departmentIds)
        {
            try
            {
                var (startDate, endDate) = GetDateRangeValues(dateRange);

                var query = @"
                    SELECT 
                        COUNT(CASE WHEN tap.AttendanceStatus = 'Present' THEN 1 END) AS AttendeeCount,
                        COUNT(*) AS TotalParticipants
                    FROM dbo.TrainingEventActualParticipant tap
                    INNER JOIN dbo.TrainingEventRealization ter ON tap.TrainingEventId = ter.TrainingEventId
                    INNER JOIN home.dbo.APP_Employees emp ON tap.EmployeeCode = emp.EmployeeCode
                    INNER JOIN home.dbo.APP_Departments d ON emp.DepartmentID = d.DepartmentID
                    WHERE ter.RealizationDate >= @StartDate
                      AND ter.RealizationDate < @EndDate
                      AND d.CoCode IN ({0})
                      AND d.ABRV IN ({1})
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}");
                }
                else
                {
                    query = query.Replace("AND d.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND d.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startDate);
                    parameters.Add("@EndDate", endDate);

                    var result = await connection.QueryFirstOrDefaultAsync<AttendanceRateDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text);

                    return result ?? new AttendanceRateDto { AttendeeCount = 0, TotalParticipants = 0 };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance rate: {Message}", ex.Message);
                return new AttendanceRateDto { AttendeeCount = 0, TotalParticipants = 0 };
            }
        }

        public async Task<CompletionRateDto> GetCompletionRateAsync(string dateRange, string companyCodes, string departmentIds)
        {
            try
            {
                var (startDate, endDate) = GetDateRangeValues(dateRange);

                var query = @"
                    SELECT 
                        COUNT(CASE WHEN ter.Status = 'Posted' THEN 1 END) AS CompletedCount,
                        COUNT(DISTINCT ter.TrainingEventRealizationId) AS PlannedCount
                    FROM dbo.TrainingEventRealization ter
                    INNER JOIN dbo.TrainingEvent te ON ter.TrainingEventId = te.TrainingEventId
                    WHERE ter.RealizationDate >= @StartDate
                      AND ter.RealizationDate < @EndDate
                      AND te.CoCode IN ({0})
                      AND te.ABRV IN ({1})
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}");
                }
                else
                {
                    query = query.Replace("AND te.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND te.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startDate);
                    parameters.Add("@EndDate", endDate);

                    var result = await connection.QueryFirstOrDefaultAsync<CompletionRateDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text);

                    return result ?? new CompletionRateDto { CompletedCount = 0, PlannedCount = 0 };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion rate: {Message}", ex.Message);
                return new CompletionRateDto { CompletedCount = 0, PlannedCount = 0 };
            }
        }

        #endregion

        #region Training Hours & Department Performance

        public async Task<List<TrainingHoursByDepartmentDto>> GetTrainingHoursByDepartmentAsync(
            string dateRange,
            string companyCodes,
            string departmentIds,
            string categoryIds)
        {
            try
            {
                var (startDate, endDate) = GetDateRangeValues(dateRange);

                var query = @"
                    SELECT 
                        d.ABRV AS DepartmentCode,
                        d.DeptName AS DepartmentName,
                        SUM(CAST(te.DurationHours AS DECIMAL(10,2))) AS TotalHours,
                        COUNT(DISTINCT ter.TrainingEventRealizationId) AS EventCount,
                        COUNT(DISTINCT tap.EmployeeCode) AS ParticipantCount
                    FROM dbo.TrainingEventRealization ter
                    INNER JOIN dbo.TrainingEvent te ON ter.TrainingEventId = te.TrainingEventId
                    INNER JOIN dbo.TrainingEventActualParticipant tap ON ter.TrainingEventId = te.TrainingEventId
                    INNER JOIN home.dbo.APP_Departments d ON tap.EmployeeCode IN (
                        SELECT EmployeeCode FROM home.dbo.APP_Employees WHERE DepartmentID = d.DepartmentID
                    )
                    WHERE ter.RealizationDate >= @StartDate
                      AND ter.RealizationDate < @EndDate
                      AND ter.Status = 'Posted'
                      AND d.CoCode IN ({0})
                      AND d.ABRV IN ({1})
                      AND te.TrainingCategoryId IN ({2})
                    GROUP BY d.ABRV, d.DeptName
                    ORDER BY TotalHours DESC
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}", "{2}");
                }
                else
                {
                    query = query.Replace("AND d.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}", "{2}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND d.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                // Build category filter
                if (!string.IsNullOrEmpty(categoryIds))
                {
                    var cats = categoryIds.Split(',').Select(c => c.Trim()).ToList();
                    query = query.Replace("{2}", string.Join(",", cats));
                }
                else
                {
                    query = query.Replace("AND te.TrainingCategoryId IN ({2})", "");
                    query = query.Replace("{2}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startDate);
                    parameters.Add("@EndDate", endDate);

                    var results = (await connection.QueryAsync<TrainingHoursByDepartmentDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training hours by department: {Message}", ex.Message);
                return new List<TrainingHoursByDepartmentDto>();
            }
        }

        #endregion

        #region Pending Documents

        public async Task<List<PendingDocumentDto>> GetPendingDocumentsAsync(string username, string companyCodes, string departmentIds)
        {
            try
            {
                var query = @"
                    ;WITH Pending AS (
                        SELECT
                            te.TrainingEventId AS DocumentId,
                            'Training Event' AS DocumentType,
                            te.EventCode AS DocumentNumber,
                            te.ABRV AS DepartmentCode,
                            te.Status AS Status,
                            te.CreatedDate AS CreatedDate
                        FROM dbo.TrainingEvent te
                        OUTER APPLY dbo.fc_GetDocumentPermission(te.TrainingEventId, @Username, 'EVENT') perm
                        WHERE perm.CanAction = 1
                          AND te.CoCode IN ({0})
                          AND te.ABRV IN ({1})

                        UNION ALL

                        SELECT
                            typ.PlanId AS DocumentId,
                            'Training Plan' AS DocumentType,
                            typ.PlanCode AS DocumentNumber,
                            typ.ABRV AS DepartmentCode,
                            typ.PlanStatus AS Status,
                            typ.CreatedDate AS CreatedDate
                        FROM dbo.Training_YearlyPlans typ
                        OUTER APPLY dbo.fc_GetDocumentPermission(typ.PlanId, @Username, 'PLAN') perm
                        WHERE perm.CanAction = 1
                          AND typ.CoCode IN ({2})
                          AND typ.ABRV IN ({3})
                    )
                    SELECT TOP 20 *
                    FROM Pending
                    ORDER BY CreatedDate DESC
                ";

                // Build company filter (applies to both te.CoCode and typ.CoCode placeholders)
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    var coList = string.Join(",", cos);
                    query = query.Replace("{0}", coList).Replace("{2}", coList);
                }
                else
                {
                    query = query.Replace("AND te.CoCode IN ({0})", "");
                    query = query.Replace("AND typ.CoCode IN ({2})", "");
                }

                // Build department filter (applies to both te.ABRV and typ.ABRV placeholders)
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    var deptList = string.Join(",", depts);
                    query = query.Replace("{1}", deptList).Replace("{3}", deptList);
                }
                else
                {
                    query = query.Replace("AND te.ABRV IN ({1})", "");
                    query = query.Replace("AND typ.ABRV IN ({3})", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Username", username);

                    var results = (await connection.QueryAsync<PendingDocumentDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending documents: {Message}", ex.Message);
                return new List<PendingDocumentDto>();
            }
        }

        #endregion

        #region Chart Data Methods

        public async Task<ChartDataDto> GetAttendanceTrendChartAsync(string dateRange, string companyCodes, string departmentIds)
        {
            var chart = new ChartDataDto { ChartType = "line", Title = "Attendance Trend" };
            try
            {
                var (startDate, endDate) = GetDateRangeValues(dateRange);

                var query = @"
                    SELECT 
                        FORMAT(ter.RealizationDate, 'MMM yyyy') AS Month,
                        COUNT(CASE WHEN tap.AttendanceStatus = 'Present' THEN 1 END) * 100.0 / COUNT(*) AS AttendanceRate
                    FROM dbo.TrainingEventActualParticipant tap
                    INNER JOIN dbo.TrainingEventRealization ter ON tap.TrainingEventId = ter.TrainingEventId
                    INNER JOIN home.dbo.APP_Employees emp ON tap.EmployeeCode = emp.EmployeeCode
                    INNER JOIN home.dbo.APP_Departments d ON emp.DepartmentID = d.DepartmentID
                    WHERE ter.RealizationDate >= @StartDate
                      AND ter.RealizationDate < @EndDate
                      AND d.CoCode IN ({0})
                      AND d.ABRV IN ({1})
                    GROUP BY FORMAT(ter.RealizationDate, 'MMM yyyy'), ter.RealizationDate
                    ORDER BY ter.RealizationDate
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}");
                }
                else
                {
                    query = query.Replace("AND d.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND d.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startDate);
                    parameters.Add("@EndDate", endDate);

                    var results = (await connection.QueryAsync<dynamic>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    chart.Labels = results.Select(r => (string)r.Month).ToList();
                    var dataset = new ChartDatasetDto
                    {
                        Label = "Attendance Rate (%)",
                        Data = results.Select(r => (decimal)r.AttendanceRate).ToList(),
                        BorderColor = "#0d6efd",
                        BackgroundColor = "rgba(13, 110, 253, 0.1)",
                        Fill = true,
                        Tension = 0.4m
                    };
                    chart.Datasets.Add(dataset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building attendance trend chart: {Message}", ex.Message);
            }

            return chart;
        }

        public async Task<ChartDataDto> GetTrainingHoursByDeptChartAsync(string dateRange, string companyCodes, string departmentIds, string categoryIds)
        {
            var chart = new ChartDataDto { ChartType = "bar", Title = "Training Hours by Department" };
            try
            {
                var hours = await GetTrainingHoursByDepartmentAsync(dateRange, companyCodes, departmentIds, categoryIds);
                chart.Labels = hours.Select(h => h.DepartmentCode).ToList();

                var dataset = new ChartDatasetDto
                {
                    Label = "Hours",
                    Data = hours.Select(h => h.TotalHours).ToList(),
                    BackgroundColor = "rgba(75, 192, 192, 0.6)",
                    BorderColor = "#20c997"
                };
                chart.Datasets.Add(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building training hours chart: {Message}", ex.Message);
            }

            return chart;
        }

        public async Task<ChartDataDto> GetCompletionStatusChartAsync(string dateRange, string companyCodes, string departmentIds)
        {
            var chart = new ChartDataDto { ChartType = "doughnut", Title = "Completion Status" };
            try
            {
                var completion = await GetCompletionRateAsync(dateRange, companyCodes, departmentIds);
                chart.Labels = new List<string> { "Completed", "Pending" };

                var dataset = new ChartDatasetDto
                {
                    Label = "Training Events",
                    Data = new List<decimal> { completion.CompletedCount, completion.PendingCount },
                    BackgroundColor = "rgba(75, 192, 75, 0.6),rgba(255, 193, 7, 0.6)"
                };
                chart.Datasets.Add(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building completion status chart: {Message}", ex.Message);
            }

            return chart;
        }

        public async Task<ChartDataDto> GetEventsByCategoryChartAsync(string dateRange, string companyCodes, string departmentIds)
        {
            var chart = new ChartDataDto { ChartType = "bar", Title = "Training Events by Category" };
            try
            {
                var query = @"
                    SELECT 
                        mc.CategoryName,
                        COUNT(DISTINCT ter.TrainingEventRealizationId) AS EventCount
                    FROM dbo.TrainingEventRealization ter
                    INNER JOIN dbo.TrainingEvent te ON ter.TrainingEventId = te.TrainingEventId
                    INNER JOIN dbo.Master_TrainingCategories mc ON te.TrainingCategoryId = mc.CategoryId
                    WHERE ter.Status = 'Posted'
                      AND te.CoCode IN ({0})
                      AND te.ABRV IN ({1})
                    GROUP BY mc.CategoryName
                    ORDER BY EventCount DESC
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}");
                }
                else
                {
                    query = query.Replace("AND te.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND te.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var results = (await connection.QueryAsync<dynamic>(
                        query,
                        null,
                        commandType: System.Data.CommandType.Text)).ToList();

                    chart.Labels = results.Select(r => (string)r.CategoryName).ToList();
                    var dataset = new ChartDatasetDto
                    {
                        Label = "Events",
                        Data = results.Select(r => (decimal)(int)r.EventCount).ToList(),
                        BackgroundColor = "rgba(153, 102, 255, 0.6)",
                        BorderColor = "#9966ff"
                    };
                    chart.Datasets.Add(dataset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building events by category chart: {Message}", ex.Message);
            }

            return chart;
        }

        public async Task<ChartDataDto> GetDepartmentPerformanceChartAsync(string dateRange, string companyCodes, string departmentIds, string categoryIds)
        {
            var chart = new ChartDataDto { ChartType = "bar", Title = "Department Performance (Attendance Rate)" };
            try
            {
                var (startDate, endDate) = GetDateRangeValues(dateRange);

                var query = @"
                    SELECT 
                        d.ABRV AS DepartmentCode,
                        COUNT(CASE WHEN tap.AttendanceStatus = 'Present' THEN 1 END) * 100.0 / COUNT(*) AS AttendanceRate
                    FROM dbo.TrainingEventActualParticipant tap
                    INNER JOIN dbo.TrainingEventRealization ter ON tap.TrainingEventId = ter.TrainingEventId
                    INNER JOIN dbo.TrainingEvent te ON ter.TrainingEventId = te.TrainingEventId
                    INNER JOIN home.dbo.APP_Employees emp ON tap.EmployeeCode = emp.EmployeeCode
                    INNER JOIN home.dbo.APP_Departments d ON emp.DepartmentID = d.DepartmentID
                    WHERE ter.RealizationDate >= @StartDate
                      AND ter.RealizationDate < @EndDate
                      AND d.CoCode IN ({0})
                      AND d.ABRV IN ({1})
                      AND te.TrainingCategoryId IN ({2})
                    GROUP BY d.ABRV
                    ORDER BY AttendanceRate DESC
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos), "{1}", "{2}");
                }
                else
                {
                    query = query.Replace("AND d.CoCode IN ({0})", "");
                    query = string.Format(query, "", "{1}", "{2}");
                }

                // Build department filter
                if (!string.IsNullOrEmpty(departmentIds))
                {
                    var depts = departmentIds.Split(',').Select(d => $"'{d.Trim()}'").ToList();
                    query = query.Replace("{1}", string.Join(",", depts));
                }
                else
                {
                    query = query.Replace("AND d.ABRV IN ({1})", "");
                    query = query.Replace("{1}", "");
                }

                // Build category filter
                if (!string.IsNullOrEmpty(categoryIds))
                {
                    var cats = categoryIds.Split(',').Select(c => c.Trim()).ToList();
                    query = query.Replace("{2}", string.Join(",", cats));
                }
                else
                {
                    query = query.Replace("AND te.TrainingCategoryId IN ({2})", "");
                    query = query.Replace("{2}", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", startDate);
                    parameters.Add("@EndDate", endDate);

                    var results = (await connection.QueryAsync<dynamic>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    chart.Labels = results.Select(r => (string)r.DepartmentCode).ToList();
                    var dataset = new ChartDatasetDto
                    {
                        Label = "Attendance Rate (%)",
                        Data = results.Select(r => (decimal)r.AttendanceRate).ToList(),
                        BackgroundColor = "rgba(255, 159, 64, 0.6)",
                        BorderColor = "#ff9f40"
                    };
                    chart.Datasets.Add(dataset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building department performance chart: {Message}", ex.Message);
            }

            return chart;
        }

        #endregion

        #region Filter Options

        public List<FilterOptionDto> GetDateRangeOptions(string currentSelection = "ThisMonth")
        {
            return new List<FilterOptionDto>
            {
                new FilterOptionDto { Value = "ThisMonth", Text = "This Month", Selected = currentSelection == "ThisMonth" },
                new FilterOptionDto { Value = "YTD", Text = "Year to Date (YTD)", Selected = currentSelection == "YTD" },
                new FilterOptionDto { Value = "Rolling3Months", Text = "Last 3 Months", Selected = currentSelection == "Rolling3Months" }
            };
        }

        public async Task<List<FilterOptionDto>> GetCompanyOptionsAsync(string username = "")
        {
            try
            {
                // Derive accessible companies from the same access function used for departments,
                // so Company dropdown only shows companies the user actually has department access to.
                var query = @"
                    DECLARE @EmployeeCode NVARCHAR(50) = (SELECT EmployeeCode FROM home.dbo.Users WHERE Username = @Username);

                    SELECT DISTINCT
                        depts.CoCode AS Value,
                        ac.CompanyName AS Text
                    FROM dbo.fc_GetDepartmentsByAccess(@EmployeeCode, 'EVENT') AS depts
                    INNER JOIN home.dbo.APP_Company ac ON ac.CompanyCode = depts.CoCode
                    WHERE ac.IsActive = 1
                    ORDER BY ac.CompanyName
                ";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Username", username);

                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    if (results.Any())
                        return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting accessible companies, falling back to all active companies: {Message}", ex.Message);
            }

            // Fallback: all active companies (safety net if access function returns nothing)
            try
            {
                var fallbackQuery = @"
                    SELECT DISTINCT
                        ac.CompanyCode AS Value,
                        ac.CompanyName AS Text
                    FROM home.dbo.APP_Company ac
                    WHERE ac.IsActive = 1
                    ORDER BY ac.CompanyName
                ";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        fallbackQuery,
                        null,
                        commandType: System.Data.CommandType.Text)).ToList();

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company options (both methods failed): {Message}", ex.Message);
                return new List<FilterOptionDto>();
            }
        }

        public async Task<List<FilterOptionDto>> GetDepartmentOptionsAsync(string username, string companyCodes, string currentSelection = "")
        {
            try
            {
                var depts = await GetAccessibleDepartmentsAsync(username, companyCodes);
                return depts.Select(d => new FilterOptionDto
                {
                    Value = d.Value,
                    Text = d.Text,
                    Selected = currentSelection.Contains(d.Value)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department options: {Message}", ex.Message);
                return new List<FilterOptionDto>();
            }
        }

        public async Task<List<FilterOptionDto>> GetCategoryOptionsAsync(string currentSelection = "")
        {
            try
            {
                // Try to get from Master_TrainingCategories
                var query = "SELECT CategoryId AS Value, CategoryName AS Text FROM dbo.Master_TrainingCategories ORDER BY CategoryName";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        query,
                        null,
                        commandType: System.Data.CommandType.Text)).ToList();

                    if (results.Any())
                    {
                        foreach (var item in results)
                        {
                            item.Selected = currentSelection.Contains(item.Value);
                        }
                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting categories from Master_TrainingCategories: {Message}", ex.Message);
            }

            // Fallback: Get categories from TrainingEvent table
            try
            {
                var fallbackQuery = @"
                    SELECT 
                        CAST(tc.CategoryId AS VARCHAR) AS Value,
                        tc.CategoryName AS Text
                    FROM dbo.Master_TrainingCategories tc
                    ORDER BY tc.CategoryName
                ";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        fallbackQuery,
                        null,
                        commandType: System.Data.CommandType.Text)).ToList();

                    if (results.Any())
                    {
                        foreach (var item in results)
                        {
                            item.Selected = currentSelection.Contains(item.Value);
                        }
                        return results;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting categories from fallback: {Message}", ex.Message);
            }

            _logger.LogWarning("No categories found - returning empty list");
            return new List<FilterOptionDto>();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get accessible departments for a user via stored procedure
        /// </summary>
        private async Task<List<FilterOptionDto>> GetAccessibleDepartmentsAsync(string username, string companyCodes = "")
        {
            try
            {
                // fc_GetDepartmentsByAccess requires (@employeeCode, @docType) - resolve EmployeeCode from Username first.
                // DocType 'EVENT' is used since it covers the broadest access rule (including 'tc' full-access branch)
                // and the dashboard is primarily Training Event based.
                var query = @"
                    DECLARE @EmployeeCode NVARCHAR(50) = (SELECT EmployeeCode FROM home.dbo.Users WHERE Username = @Username);

                    SELECT DISTINCT
                        depts.ABRV AS Value,
                        depts.DeptName AS Text
                    FROM dbo.fc_GetDepartmentsByAccess(@EmployeeCode, 'EVENT') AS depts
                    WHERE depts.CoCode IN ({0})
                    ORDER BY depts.DeptName
                ";

                // Build company filter
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    query = string.Format(query, string.Join(",", cos));
                }
                else
                {
                    query = query.Replace("WHERE depts.CoCode IN ({0})", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Username", username);

                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        query,
                        parameters,
                        commandType: System.Data.CommandType.Text)).ToList();

                    // If results found, return them
                    if (results.Any())
                        return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error using permission function, falling back to all departments: {Message}", ex.Message);
            }

            // Fallback: Return all departments if permission function fails or returns nothing
            try
            {
                var fallbackQuery = @"
                    SELECT DISTINCT
                        ABRV AS Value,
                        DeptName AS Text
                    FROM home.dbo.APP_Departments
                    WHERE CoCode IN ({0})
                    ORDER BY DeptName
                ";

                // Build company filter for fallback
                if (!string.IsNullOrEmpty(companyCodes))
                {
                    var cos = companyCodes.Split(',').Select(c => $"'{c.Trim()}'").ToList();
                    fallbackQuery = string.Format(fallbackQuery, string.Join(",", cos));
                }
                else
                {
                    fallbackQuery = fallbackQuery.Replace("WHERE CoCode IN ({0})", "");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var results = (await connection.QueryAsync<FilterOptionDto>(
                        fallbackQuery,
                        commandType: System.Data.CommandType.Text)).ToList();

                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments (both methods failed): {Message}", ex.Message);
                return new List<FilterOptionDto>();
            }
        }

        /// <summary>
        /// Get start and end date based on date range
        /// </summary>
        private (DateTime startDate, DateTime endDate) GetDateRangeValues(string dateRange)
        {
            var today = DateTime.Now;
            return dateRange switch
            {
                "ThisMonth" => (
                    new DateTime(today.Year, today.Month, 1),
                    today.AddDays(1)
                ),
                "YTD" => (
                    new DateTime(today.Year, 1, 1),
                    today.AddDays(1)
                ),
                "Rolling3Months" => (
                    today.AddDays(-90),
                    today.AddDays(1)
                ),
                _ => (
                    new DateTime(today.Year, today.Month, 1),
                    today.AddDays(1)
                )
            };
        }

        /// <summary>
        /// Get display text for period
        /// </summary>
        private string GetPeriodDisplay(string dateRange)
        {
            var today = DateTime.Now;
            return dateRange switch
            {
                "ThisMonth" => today.ToString("MMMM yyyy"),
                "YTD" => $"Jan - {today:MMM yyyy}",
                "Rolling3Months" => $"Last 90 days (to {today:dd MMM yyyy})",
                _ => today.ToString("MMMM yyyy")
            };
        }

        #endregion
    }
}
