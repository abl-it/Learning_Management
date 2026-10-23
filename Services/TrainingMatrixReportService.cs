using Dapper;
using Microsoft.Data.SqlClient;
using Training.Models.DTO.TrainingMatrix;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implementasi <see cref="ITrainingMatrixReportService"/>.
    /// Karyawan: HRMS (connection string "HRMS", read-only). Matrix, event, dan master: DefaultConnection.
    /// </summary>
    public class TrainingMatrixReportService : ITrainingMatrixReportService
    {
        // Aturan Layer: JobGradeID 1-3 = Layer 1-3; JobGradeID 4 (Manager Operasional) = Layer 3.
        private const int ManagerGrade = 4;
        private const int ManagerLayer = 3;

        private const string StatusIncluded = "Included";
        private const string StatusUnmapped = "Unmapped";
        private const string StatusNoMatrix = "No Matrix";

        private static readonly string[] Companies = { "ABL", "NKM" };

        private const string ReasonNoSubDept = "Sub Department belum diisi";
        private const string ReasonUnknownSubDept = "Sub Department tidak dikenal";
        private const string ReasonNoPosition = "PositionID belum diisi";
        private const string ReasonNoGrade = "JobGrade belum diisi";
        private const string ReasonGradeOutOfRange = "JobGrade di luar Layer 1-3 (Manager Operasional = Layer 3)";

        private readonly string _connectionString;
        private readonly string? _hrmsConnectionString;
        private readonly IMatrixAccessService _access;
        private readonly ILogger<TrainingMatrixReportService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="TrainingMatrixReportService"/>.
        /// </summary>
        public TrainingMatrixReportService(
            IConfiguration configuration,
            IMatrixAccessService access,
            ILogger<TrainingMatrixReportService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _hrmsConnectionString = configuration.GetConnectionString("HRMS");
            _access = access;
            _logger = logger;
        }

        // =====================================================================
        // PUBLIC
        // =====================================================================

        /// <inheritdoc />
        public async Task<ComplianceReportDto> GetSummaryAsync(
            string company,
            CancellationToken cancellationToken = default)
        {
            company = NormalizeCompany(company);

            var model = await BuildAsync(cancellationToken);

            var scoped = model.Employees
                .Where(e => string.Equals(e.Company, company, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var included = scoped.Where(e => e.Status == StatusIncluded).ToList();

            var report = new ComplianceReportDto
            {
                GeneratedAt = DateTime.Now,
                TotalEmployees = scoped.Count,
                IncludedEmployees = included.Count,
                UnmappedEmployees = scoped.Count(e => e.Status == StatusUnmapped),
                NoMatrixEmployees = scoped.Count(e => e.Status == StatusNoMatrix),
                NoCompanyEmployees = model.Employees.Count(e => e.Company is null),
                AvailableCompanies = Companies
                    .Where(c => model.Departments.Any(d => string.Equals(d.Company, c, StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                MandatoryRequired = included.Sum(e => e.MandatoryRequired),
                MandatoryFulfilled = included.Sum(e => e.MandatoryFulfilled),
                EmployeesWithGap = included.Count(e => e.MandatoryFulfilled < e.MandatoryRequired)
            };

            report.CompliancePercent = Pct(report.MandatoryFulfilled, report.MandatoryRequired);
            report.UnmappedPercent = Pct(report.UnmappedEmployees, report.TotalEmployees);

            report.UnmappedReasons = scoped
                .Where(e => e.Status == StatusUnmapped)
                .GroupBy(e => e.Reason ?? string.Empty)
                .Select(g => new UnmappedReasonDto { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList();

            report.Departments = model.Departments
                .Where(d => string.Equals(d.Company, company, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.DeptName, StringComparer.OrdinalIgnoreCase)
                .Select(info =>
                {
                    var abrv = info.Abrv;

                    var inDept = scoped
                        .Where(e => string.Equals(e.Abrv, abrv, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var inc = inDept.Where(e => e.Status == StatusIncluded).ToList();

                    model.MatrixStatus.TryGetValue(abrv, out var status);
                    model.MatrixIds.TryGetValue(abrv, out var matrixId);

                    var dto = new DepartmentComplianceDto
                    {
                        DepartmentAbrv = abrv,
                        DepartmentName = info.DeptName,
                        MatrixStatus = status ?? "No matrix",
                        MatrixId = matrixId == 0 ? null : matrixId,
                        EmployeeCount = inDept.Count,
                        IncludedCount = inc.Count,
                        UnmappedCount = inDept.Count(e => e.Status == StatusUnmapped),
                        MandatoryRequired = inc.Sum(e => e.MandatoryRequired),
                        MandatoryFulfilled = inc.Sum(e => e.MandatoryFulfilled),
                        EmployeesWithGap = inc.Count(e => e.MandatoryFulfilled < e.MandatoryRequired),
                        RecommendedRequired = inc.Sum(e => e.RecommendedRequired),
                        RecommendedFulfilled = inc.Sum(e => e.RecommendedFulfilled)
                    };

                    dto.CompliancePercent = Pct(dto.MandatoryFulfilled, dto.MandatoryRequired);
                    dto.RecommendedPercent = Pct(dto.RecommendedFulfilled, dto.RecommendedRequired);

                    return dto;
                })
                .ToList();

            return report;
        }

        /// <inheritdoc />
        public async Task<List<EmployeeComplianceDto>> GetDepartmentEmployeesAsync(
            string departmentAbrv,
            CancellationToken cancellationToken = default)
        {
            var abrv = departmentAbrv?.Trim();

            if (string.IsNullOrEmpty(abrv))
            {
                throw new ArgumentException("Department is required.");
            }

            var model = await BuildAsync(cancellationToken);

            return model.Employees
                .Where(e => string.Equals(e.Abrv, abrv, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.MandatoryRequired - e.MandatoryFulfilled)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(e => new EmployeeComplianceDto
                {
                    EmployeeCode = e.Code,
                    Name = e.Name,
                    SubDepartmentId = e.SubDepartmentId,
                    SubDepartmentCode = e.SubDepartmentCode,
                    SubDepartmentName = e.SubDepartmentName,
                    Layer = e.Layer,
                    Status = e.Status,
                    Reason = e.Reason,
                    MandatoryRequired = e.MandatoryRequired,
                    MandatoryFulfilled = e.MandatoryFulfilled,
                    RecommendedRequired = e.RecommendedRequired,
                    RecommendedFulfilled = e.RecommendedFulfilled,
                    Gaps = e.Gaps
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<List<UnmappedEmployeeDto>> GetUnmappedAsync(
            string company,
            CancellationToken cancellationToken = default)
        {
            company = NormalizeCompany(company);

            var model = await BuildAsync(cancellationToken);

            return model.Employees
                .Where(e => e.Status == StatusUnmapped
                         && string.Equals(e.Company, company, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Reason, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Abrv, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Code, StringComparer.OrdinalIgnoreCase)
                .Select(e => new UnmappedEmployeeDto
                {
                    EmployeeCode = e.Code,
                    Name = e.Name,
                    DepartmentAbrv = e.Abrv,
                    DepartmentName = e.DeptName,
                    SubDepartmentName = e.SubDepartmentName,
                    SubDepartmentId = e.SubDepartmentId,
                    PositionId = e.PositionId,
                    JobGradeId = e.JobGradeId,
                    Reason = e.Reason ?? string.Empty
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<MatrixFilterDto> GetFiltersAsync(
            CancellationToken cancellationToken = default)
        {
            var model = await BuildAsync(cancellationToken);

            var departments = model.Departments
                .Where(d => Companies.Contains(d.Company, StringComparer.OrdinalIgnoreCase))
                .OrderBy(d => d.DeptName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var abrvs = departments.Select(d => d.Abrv).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new MatrixFilterDto
            {
                // Hanya perusahaan yang punya Department yang boleh dilihat pengguna.
                Companies = Companies
                    .Where(c => departments.Any(d => string.Equals(d.Company, c, StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                Departments = departments
                    .Select(d => new FilterDepartmentDto
                    {
                        Abrv = d.Abrv,
                        Name = d.DeptName,
                        Company = d.Company.ToUpperInvariant()
                    })
                    .ToList(),
                SubDepartments = model.SubDepartments
                    .Where(s => abrvs.Contains(s.Abrv))
                    .OrderBy(s => s.SubDepartmentCode, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new FilterSubDepartmentDto
                    {
                        Id = s.SubDepartmentID,
                        Code = s.SubDepartmentCode,
                        Name = s.SubDepartmentName,
                        Abrv = s.Abrv
                    })
                    .ToList()
            };
        }

        /// <inheritdoc />
        public async Task<List<EmployeeMatrixRowDto>> GetEmployeesAsync(
            string company,
            string? departmentAbrv,
            int? subDepartmentId,
            CancellationToken cancellationToken = default)
        {
            company = NormalizeCompany(company);

            var abrv = string.IsNullOrWhiteSpace(departmentAbrv) ? null : departmentAbrv.Trim();

            var model = await BuildAsync(cancellationToken);

            var deptNames = model.Departments.ToDictionary(
                d => d.Abrv, d => d.DeptName, StringComparer.OrdinalIgnoreCase);

            return model.Employees
                .Where(e => string.Equals(e.Company, company, StringComparison.OrdinalIgnoreCase))
                .Where(e => abrv is null
                            || string.Equals(e.Abrv, abrv, StringComparison.OrdinalIgnoreCase))
                .Where(e => subDepartmentId is null || e.SubDepartmentId == subDepartmentId)
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Select(e => new EmployeeMatrixRowDto
                {
                    EmployeeCode = e.Code,
                    Name = e.Name,
                    DepartmentAbrv = e.Abrv,
                    DepartmentName = e.Abrv is not null && deptNames.TryGetValue(e.Abrv, out var dn) ? dn : null,
                    SubDepartmentId = e.SubDepartmentId,
                    SubDepartmentCode = e.SubDepartmentCode,
                    SubDepartmentName = e.SubDepartmentName,
                    Layer = e.Layer,
                    Status = e.Status,
                    Reason = e.Reason,
                    MandatoryRequired = e.MandatoryRequired,
                    MandatoryFulfilled = e.MandatoryFulfilled,
                    RecommendedRequired = e.RecommendedRequired,
                    RecommendedFulfilled = e.RecommendedFulfilled
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<MatrixExportFileDto> ExportMatrixAsync(
            string company,
            string? departmentAbrv,
            int? subDepartmentId,
            CancellationToken cancellationToken = default)
        {
            company = NormalizeCompany(company);

            var abrv = string.IsNullOrWhiteSpace(departmentAbrv) ? null : departmentAbrv.Trim();

            var model = await BuildAsync(cancellationToken);

            var departments = model.Departments
                .Where(d => string.Equals(d.Company, company, StringComparison.OrdinalIgnoreCase))
                .Where(d => abrv is null || string.Equals(d.Abrv, abrv, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.DeptName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (departments.Count == 0)
            {
                throw new ArgumentException("Department tidak ditemukan pada company ini.");
            }

            // Aktual training: semua kehadiran pada course yang ada di matrix (dikelompokkan per karyawan + course)
            var scopeAbrvs = departments
                .Where(d => model.ApprovedAbrvs.Contains(d.Abrv))
                .Select(d => d.Abrv)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var courseIds = model.Details
                .Where(d => scopeAbrvs.Contains(d.DepartmentAbrv.Trim()))
                .Select(d => d.CourseId)
                .Distinct()
                .ToList();

            var attendance = new Dictionary<string, List<AttendRow>>(StringComparer.OrdinalIgnoreCase);

            if (courseIds.Count > 0)
            {
                await using var attendConnection = new SqlConnection(_connectionString);

                var attendRows = await attendConnection.QueryAsync<AttendRow>(
                    new CommandDefinition(
                        """
                        SELECT LTRIM(RTRIM(a.EmployeeCode)) AS EmployeeCode, e.CourseId, e.EventCode,
                               e.EventStartDate, e.Status AS EventStatus, a.AttendanceStatus
                        FROM dbo.TrainingEventActualParticipant a
                        INNER JOIN dbo.TrainingEvent e ON e.TrainingEventId = a.TrainingEventId
                        WHERE e.CourseId IN @CourseIds;
                        """,
                        new { CourseIds = courseIds },
                        cancellationToken: cancellationToken));

                foreach (var g in attendRows.GroupBy(r => DoneKey(r.EmployeeCode, r.CourseId)))
                {
                    attendance[g.Key] = g.OrderByDescending(r => r.EventStartDate).ToList();
                }
            }

            var export = new MatrixExportDto
            {
                GeneratedAt = DateTime.Now,
                Title = BuildExportTitle(company, departments, abrv, subDepartmentId, model)
            };

            foreach (var dept in departments)
            {
                model.MatrixStatus.TryGetValue(dept.Abrv, out var status);

                var deptDto = new MatrixExportDepartmentDto
                {
                    Abrv = dept.Abrv,
                    Name = dept.DeptName,
                    MatrixStatus = status ?? "No matrix",
                    IsApproved = model.ApprovedAbrvs.Contains(dept.Abrv)
                };

                var subs = model.SubDepartments
                    .Where(s => string.Equals(s.Abrv, dept.Abrv, StringComparison.OrdinalIgnoreCase))
                    .Where(s => subDepartmentId is null || s.SubDepartmentID == subDepartmentId)
                    .OrderBy(s => s.SubDepartmentCode, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var sub in subs)
                {
                    var people = model.Employees.Where(e => e.SubDepartmentId == sub.SubDepartmentID).ToList();

                    var subDto = new MatrixExportSubDepartmentDto
                    {
                        Code = sub.SubDepartmentCode,
                        Name = sub.SubDepartmentName,
                        Layer1Employees = people.Count(e => e.Layer == 1),
                        Layer2Employees = people.Count(e => e.Layer == 2),
                        Layer3Employees = people.Count(e => e.Layer == 3),
                        UnmappedEmployees = people.Count(e => e.Status == StatusUnmapped)
                    };

                    subDto.Unmapped = people
                        .Where(e => e.Status == StatusUnmapped)
                        .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(e => new MatrixExportUnmappedDto
                        {
                            EmployeeCode = e.Code,
                            Name = e.Name,
                            Reason = e.Reason ?? string.Empty
                        })
                        .ToList();

                    var included = people
                        .Where(e => e.Status == StatusIncluded && e.Layer is not null)
                        .OrderBy(e => e.Layer)
                        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    subDto.People = included
                        .Select(e => new MatrixExportPersonDto
                        {
                            EmployeeCode = e.Code,
                            Name = e.Name,
                            Layer = e.Layer!.Value
                        })
                        .ToList();

                    if (deptDto.IsApproved)
                    {
                        var rows = model.Details
                            .Where(d => d.SubDepartmentID == sub.SubDepartmentID
                                     && string.Equals(d.DepartmentAbrv.Trim(), dept.Abrv, StringComparison.OrdinalIgnoreCase))
                            .GroupBy(d => d.CourseId);

                        foreach (var g in rows)
                        {
                            model.Courses.TryGetValue(g.Key, out var course);

                            var courseDto = new MatrixExportCourseDto
                            {
                                CourseCode = course?.CourseCode ?? g.Key.ToString(),
                                CourseName = course?.CourseName ?? string.Empty,
                                Layer1 = g.FirstOrDefault(x => x.Layer == 1)?.Requirement,
                                Layer2 = g.FirstOrDefault(x => x.Layer == 2)?.Requirement,
                                Layer3 = g.FirstOrDefault(x => x.Layer == 3)?.Requirement
                            };

                            foreach (var person in included)
                            {
                                var requirement = person.Layer switch
                                {
                                    1 => courseDto.Layer1,
                                    2 => courseDto.Layer2,
                                    3 => courseDto.Layer3,
                                    _ => null
                                };

                                if (string.IsNullOrEmpty(requirement))
                                {
                                    continue;
                                }

                                attendance.TryGetValue(DoneKey(person.Code, g.Key), out var records);

                                // Utamakan event yang memenuhi (PRESENT + Complete); bila tidak ada, event terbaru
                                var pick = records?.FirstOrDefault(IsFulfilling) ?? records?.FirstOrDefault();

                                courseDto.Employees.Add(new MatrixExportEmployeeDto
                                {
                                    EmployeeCode = person.Code,
                                    Name = person.Name,
                                    Layer = person.Layer!.Value,
                                    Requirement = requirement,
                                    Fulfilled = records?.Any(IsFulfilling) == true,
                                    EventCode = pick?.EventCode,
                                    EventDate = pick?.EventStartDate,
                                    EventStatus = pick?.EventStatus,
                                    Attendance = pick?.AttendanceStatus
                                });
                            }

                            subDto.Courses.Add(courseDto);
                        }

                        subDto.Courses = subDto.Courses
                            .OrderBy(c => c.CourseCode, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }

                    deptDto.SubDepartments.Add(subDto);
                }

                export.Departments.Add(deptDto);
            }

            var scope = abrv is null ? company : $"{company}_{SafeFileName(abrv)}";

            return new MatrixExportFileDto
            {
                FileName = $"TrainingMatrix_{scope}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                Content = MatrixExcelWriter.Build(export)
            };
        }

        /// <inheritdoc />
        public async Task<EmployeeMatrixDetailDto> GetEmployeeDetailAsync(
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            var code = employeeCode?.Trim();

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Employee code is required.");
            }

            var model = await BuildAsync(cancellationToken);

            var emp = model.Employees.FirstOrDefault(
                e => string.Equals(e.Code, code, StringComparison.OrdinalIgnoreCase));

            if (emp is null)
            {
                throw new ArgumentException("Karyawan tidak ditemukan atau sudah tidak aktif.");
            }

            const string sql = """
                SELECT e.TrainingEventId, e.EventCode, e.CourseId, c.CourseCode, e.TrainingTitle,
                       e.EventStartDate, e.EventEndDate, e.Status AS EventStatus,
                       a.AttendanceStatus, e.TrainerName, e.Venue, r.RealizationDate
                FROM dbo.TrainingEventActualParticipant a
                INNER JOIN dbo.TrainingEvent e ON e.TrainingEventId = a.TrainingEventId
                LEFT JOIN dbo.Master_Courses c ON c.CourseId = e.CourseId
                LEFT JOIN dbo.TrainingEventRealization r ON r.TrainingEventId = e.TrainingEventId
                WHERE LTRIM(RTRIM(a.EmployeeCode)) = @Code
                ORDER BY e.EventStartDate DESC, e.TrainingEventId DESC;
                """;

            await using var connection = new SqlConnection(_connectionString);

            var rows = (await connection.QueryAsync<HistoryRow>(
                new CommandDefinition(sql, new { Code = code }, cancellationToken: cancellationToken)))
                .ToList();

            var history = rows
                .Select(h => new TrainingHistoryDto
                {
                    TrainingEventId = h.TrainingEventId,
                    EventCode = h.EventCode,
                    CourseCode = h.CourseCode,
                    TrainingTitle = h.TrainingTitle,
                    EventStartDate = h.EventStartDate,
                    EventEndDate = h.EventEndDate,
                    RealizationDate = h.RealizationDate,
                    EventStatus = h.EventStatus,
                    AttendanceStatus = h.AttendanceStatus,
                    TrainerName = h.TrainerName,
                    Venue = h.Venue,
                    CountsForCompliance = IsFulfilling(h)
                })
                .ToList();

            // Course menurut matrix: tambahkan event yang memenuhi (terbaru)
            var required = emp.Required
                .Select(r =>
                {
                    var hit = rows.FirstOrDefault(h => h.CourseId == r.CourseId && IsFulfilling(h));

                    return new RequiredCourseDto
                    {
                        CourseId = r.CourseId,
                        CourseCode = r.CourseCode,
                        CourseName = r.CourseName,
                        Requirement = r.Requirement,
                        Fulfilled = r.Fulfilled,
                        FulfilledEventCode = hit?.EventCode,
                        FulfilledDate = hit?.EventStartDate
                    };
                })
                .OrderBy(r => r.Requirement == "Mandatory" ? 0 : 1)
                .ThenBy(r => r.Fulfilled ? 1 : 0)
                .ThenBy(r => r.CourseCode, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var deptName = model.Departments
                .FirstOrDefault(d => string.Equals(d.Abrv, emp.Abrv, StringComparison.OrdinalIgnoreCase))
                ?.DeptName;

            string? matrixStatus = null;
            int? matrixId = null;

            if (emp.Abrv is not null)
            {
                if (model.MatrixStatus.TryGetValue(emp.Abrv, out var ms)) matrixStatus = ms;
                if (model.MatrixIds.TryGetValue(emp.Abrv, out var mid) && mid != 0) matrixId = mid;
            }

            return new EmployeeMatrixDetailDto
            {
                EmployeeCode = emp.Code,
                Name = emp.Name,
                DepartmentAbrv = emp.Abrv,
                DepartmentName = deptName,
                SubDepartmentCode = emp.SubDepartmentCode,
                SubDepartmentName = emp.SubDepartmentName,
                Layer = emp.Layer,
                Status = emp.Status,
                Reason = emp.Reason,
                MatrixStatus = matrixStatus,
                MatrixId = matrixId,
                MandatoryRequired = emp.MandatoryRequired,
                MandatoryFulfilled = emp.MandatoryFulfilled,
                RecommendedRequired = emp.RecommendedRequired,
                RecommendedFulfilled = emp.RecommendedFulfilled,
                Required = required,
                History = history
            };
        }

        // =====================================================================
        // CORE
        // =====================================================================

        private async Task<ReportModel> BuildAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_hrmsConnectionString))
            {
                throw new ArgumentException(
                    "Koneksi HRMS belum dikonfigurasi (ConnectionStrings:HRMS pada appsettings).");
            }

            // ---- 1. Karyawan aktif dari HRMS (read-only) ------------------------------
            List<HrmsRow> hrms;

            try
            {
                await using var hrmsConnection = new SqlConnection(_hrmsConnectionString);

                hrms = (await hrmsConnection.QueryAsync<HrmsRow>(
                    new CommandDefinition(
                        """
                        SELECT e.EmployeeCode, e.SubDepartmentID, e.PositionID, p.JobGradeID
                        FROM dbo.EmployeeOrgAssignment e
                        LEFT JOIN dbo.Position p ON p.PositionID = e.PositionID
                        WHERE e.IsActive = 1
                        """,
                        cancellationToken: cancellationToken))).AsList();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Failed to read employees from HRMS.");
                throw;
            }

            // ---- 2. Data aplikasi (satu koneksi, satu round trip) -----------------------
            const string sql = """
                SELECT s.SubDepartmentID, s.SubDepartmentCode, s.SubDepartmentName,
                       LTRIM(RTRIM(d.ABRV)) AS Abrv, d.DeptName
                FROM home.dbo.APP_SubDepartments s
                INNER JOIN home.dbo.APP_Departments d ON d.DepartmentID = s.DepartmentID
                WHERE d.Disabled = 0 AND s.Disabled = 0 AND d.ABRV IS NOT NULL;

                SELECT LTRIM(RTRIM(ABRV)) AS Abrv, LTRIM(RTRIM(CoCode)) AS Company, DeptName
                FROM home.dbo.APP_Departments
                WHERE Disabled = 0 AND ABRV IS NOT NULL AND LTRIM(RTRIM(ABRV)) <> '';

                SELECT EmployeeCode, FullName
                FROM home.dbo.APP_Employees
                WHERE ResignDate IS NULL;

                SELECT MatrixID, DepartmentAbrv, Status, DocStatus
                FROM dbo.TM_Matrix
                WHERE IsDeleted = 0;

                SELECT m.DepartmentAbrv, d.SubDepartmentID, d.Layer, d.CourseId, d.Requirement
                FROM dbo.TM_Matrix m
                INNER JOIN dbo.TM_MatrixDetail d ON d.MatrixID = m.MatrixID
                WHERE m.IsDeleted = 0 AND m.DocStatus = 2;

                SELECT CourseId, CourseCode, CourseName
                FROM dbo.Master_Courses;

                SELECT DISTINCT a.EmployeeCode, e.CourseId
                FROM dbo.TrainingEventActualParticipant a
                INNER JOIN dbo.TrainingEvent e ON e.TrainingEventId = a.TrainingEventId
                WHERE a.AttendanceStatus = 'PRESENT'
                  AND e.Status = 'Complete';
                """;

            await using var connection = new SqlConnection(_connectionString);

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, cancellationToken: cancellationToken));

            var subDepts = (await multi.ReadAsync<SubDeptRow>())
                .GroupBy(s => s.SubDepartmentID)
                .ToDictionary(g => g.Key, g => g.First());

            var deptInfos = (await multi.ReadAsync<DeptRow>())
                .GroupBy(d => d.Abrv, StringComparer.OrdinalIgnoreCase)
                .Select(g => new DepartmentInfo
                {
                    Abrv = g.Key,
                    Company = g.Select(x => x.Company).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty,
                    DeptName = string.Join(" / ", g
                        .Select(x => (x.DeptName ?? string.Empty).Trim())
                        .Where(n => n.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                })
                .ToList();

            foreach (var d in deptInfos.Where(d => d.DeptName.Length == 0))
            {
                d.DeptName = d.Abrv;
            }

            var companyByAbrv = deptInfos.ToDictionary(
                d => d.Abrv, d => d.Company, StringComparer.OrdinalIgnoreCase);

            var names = (await multi.ReadAsync<NameRow>())
                .GroupBy(n => n.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().FullName, StringComparer.OrdinalIgnoreCase);

            var matrices = (await multi.ReadAsync<MatrixRow>()).ToList();
            var details = (await multi.ReadAsync<DetailRow>()).ToList();

            var courses = (await multi.ReadAsync<CourseRow>())
                .ToDictionary(c => c.CourseId);

            var done = (await multi.ReadAsync<DoneRow>())
                .Select(d => DoneKey(d.EmployeeCode, d.CourseId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ---- 3. Indeks matrix --------------------------------------------------------
            var approvedAbrvs = matrices
                .Where(m => m.DocStatus == 2)
                .Select(m => m.DepartmentAbrv.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var matrixStatus = matrices.ToDictionary(
                m => m.DepartmentAbrv.Trim(), m => m.Status, StringComparer.OrdinalIgnoreCase);

            var matrixIds = matrices.ToDictionary(
                m => m.DepartmentAbrv.Trim(), m => m.MatrixID, StringComparer.OrdinalIgnoreCase);

            var requirements = details
                .GroupBy(d => ReqKey(d.DepartmentAbrv.Trim(), d.SubDepartmentID, d.Layer),
                         StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // ---- 4. Hitung per karyawan ----------------------------------------------------
            var employees = new List<EmployeeResult>(hrms.Count);

            foreach (var h in hrms)
            {
                var code = h.EmployeeCode.Trim();

                var emp = new EmployeeResult
                {
                    Code = code,
                    Name = names.TryGetValue(code, out var fullName) ? fullName : code,
                    SubDepartmentId = h.SubDepartmentID,
                    PositionId = h.PositionID,
                    JobGradeId = h.JobGradeID
                };

                // Sub Department -> Department (ABRV)
                if (h.SubDepartmentID is null)
                {
                    emp.Status = StatusUnmapped;
                    emp.Reason = ReasonNoSubDept;
                    employees.Add(emp);
                    continue;
                }

                if (!subDepts.TryGetValue(h.SubDepartmentID.Value, out var sub))
                {
                    emp.Status = StatusUnmapped;
                    emp.Reason = ReasonUnknownSubDept;
                    employees.Add(emp);
                    continue;
                }

                emp.Abrv = sub.Abrv;
                emp.Company = companyByAbrv.TryGetValue(sub.Abrv, out var co) ? co : null;
                emp.SubDepartmentCode = sub.SubDepartmentCode;
                emp.SubDepartmentName = sub.SubDepartmentName;
                emp.DeptName = sub.DeptName;

                // Layer dari Position.JobGradeID
                if (h.PositionID is null)
                {
                    emp.Status = StatusUnmapped;
                    emp.Reason = ReasonNoPosition;
                    employees.Add(emp);
                    continue;
                }

                if (h.JobGradeID is null)
                {
                    emp.Status = StatusUnmapped;
                    emp.Reason = ReasonNoGrade;
                    employees.Add(emp);
                    continue;
                }

                emp.Layer = h.JobGradeID switch
                {
                    1 or 2 or 3 => h.JobGradeID,
                    ManagerGrade => ManagerLayer,
                    _ => null
                };

                if (emp.Layer is null)
                {
                    emp.Status = StatusUnmapped;
                    emp.Reason = ReasonGradeOutOfRange;
                    employees.Add(emp);
                    continue;
                }

                // Department belum punya matrix Approved
                if (!approvedAbrvs.Contains(sub.Abrv))
                {
                    emp.Status = StatusNoMatrix;
                    employees.Add(emp);
                    continue;
                }

                emp.Status = StatusIncluded;

                if (requirements.TryGetValue(
                        ReqKey(sub.Abrv, sub.SubDepartmentID, emp.Layer.Value), out var reqs))
                {
                    foreach (var r in reqs)
                    {
                        var fulfilled = done.Contains(DoneKey(code, r.CourseId));
                        var isMandatory = string.Equals(
                            r.Requirement, "Mandatory", StringComparison.OrdinalIgnoreCase);

                        if (isMandatory)
                        {
                            emp.MandatoryRequired++;
                            if (fulfilled) emp.MandatoryFulfilled++;
                        }
                        else
                        {
                            emp.RecommendedRequired++;
                            if (fulfilled) emp.RecommendedFulfilled++;
                        }

                        courses.TryGetValue(r.CourseId, out var course);

                        emp.Required.Add(new RequiredCourseDto
                        {
                            CourseId = r.CourseId,
                            CourseCode = course?.CourseCode ?? r.CourseId.ToString(),
                            CourseName = course?.CourseName ?? string.Empty,
                            Requirement = isMandatory ? "Mandatory" : "Recommended",
                            Fulfilled = fulfilled
                        });

                        if (!fulfilled)
                        {
                            emp.Gaps.Add(new GapCourseDto
                            {
                                CourseId = r.CourseId,
                                CourseCode = course?.CourseCode ?? r.CourseId.ToString(),
                                CourseName = course?.CourseName ?? string.Empty,
                                Requirement = isMandatory ? "Mandatory" : "Recommended"
                            });
                        }
                    }

                    emp.Gaps = emp.Gaps
                        .OrderBy(g => g.Requirement == "Mandatory" ? 0 : 1)
                        .ThenBy(g => g.CourseCode, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                employees.Add(emp);
            }

            // ---- 3. Pembatasan akses per Department (tc / hrhead = semua) --------------
            // Dipasang di satu titik ini sehingga KPI, daftar, detail karyawan, dan export ikut terbatas.
            var allowed = await _access.GetAllowedAbrvsAsync(cancellationToken);
            var subDeptList = subDepts.Values.ToList();

            if (allowed is not null)
            {
                employees = employees
                    .Where(e => e.Abrv is not null && allowed.Contains(e.Abrv))
                    .ToList();

                deptInfos = deptInfos
                    .Where(d => allowed.Contains(d.Abrv))
                    .ToList();

                subDeptList = subDeptList
                    .Where(s => allowed.Contains(s.Abrv))
                    .ToList();
            }

            return new ReportModel
            {
                Employees = employees,
                Departments = deptInfos,
                SubDepartments = subDeptList,
                Details = details,
                Courses = courses,
                ApprovedAbrvs = approvedAbrvs,
                MatrixStatus = matrixStatus,
                MatrixIds = matrixIds
            };
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static string BuildExportTitle(
            string company,
            List<DepartmentInfo> departments,
            string? abrv,
            int? subDepartmentId,
            ReportModel model)
        {
            var title = $"Training Matrix - {company}";

            if (abrv is not null)
            {
                title += $" - {departments[0].DeptName}";
            }

            if (subDepartmentId is not null)
            {
                var sub = model.SubDepartments.FirstOrDefault(s => s.SubDepartmentID == subDepartmentId);

                if (sub is not null)
                {
                    title += $" - {sub.SubDepartmentName}";
                }
            }

            return title;
        }

        private static string SafeFileName(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "[^A-Za-z0-9_-]+", "_").Trim('_');

        private static bool IsFulfilling(HistoryRow h) =>
            IsFulfilling(h.AttendanceStatus, h.EventStatus);

        private static bool IsFulfilling(AttendRow r) =>
            IsFulfilling(r.AttendanceStatus, r.EventStatus);

        private static bool IsFulfilling(string? attendanceStatus, string? eventStatus) =>
            string.Equals(attendanceStatus, "PRESENT", StringComparison.OrdinalIgnoreCase)
            && string.Equals(eventStatus, "Complete", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeCompany(string? company)
        {
            var value = company?.Trim().ToUpperInvariant();

            if (value is null || !Companies.Contains(value))
            {
                throw new ArgumentException("Company harus ABL atau NKM.");
            }

            return value;
        }

        private static double? Pct(int part, int whole) =>
            whole == 0 ? null : Math.Round(part * 100.0 / whole, 1);

        private static string ReqKey(string abrv, int subDepartmentId, int layer) =>
            $"{abrv}|{subDepartmentId}|{layer}";

        private static string DoneKey(string employeeCode, int courseId) =>
            $"{employeeCode.Trim()}|{courseId}";

        // ---- model internal ----

        private sealed class ReportModel
        {
            public List<EmployeeResult> Employees { get; set; } = new();
            public List<DepartmentInfo> Departments { get; set; } = new();
            public List<SubDeptRow> SubDepartments { get; set; } = new();
            public List<DetailRow> Details { get; set; } = new();
            public HashSet<string> ApprovedAbrvs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, CourseRow> Courses { get; set; } = new();
            public Dictionary<string, string> MatrixStatus { get; set; } = new();
            public Dictionary<string, int> MatrixIds { get; set; } = new();
        }

        private sealed class AttendRow
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public string EventCode { get; set; } = string.Empty;
            public DateTime? EventStartDate { get; set; }
            public string? EventStatus { get; set; }
            public string? AttendanceStatus { get; set; }
        }

        private sealed class HistoryRow
        {
            public long TrainingEventId { get; set; }
            public string EventCode { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public string? CourseCode { get; set; }
            public string? TrainingTitle { get; set; }
            public DateTime? EventStartDate { get; set; }
            public DateTime? EventEndDate { get; set; }
            public string? EventStatus { get; set; }
            public string? AttendanceStatus { get; set; }
            public string? TrainerName { get; set; }
            public string? Venue { get; set; }
            public DateTime? RealizationDate { get; set; }
        }

        private sealed class DepartmentInfo
        {
            public string Abrv { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
            public string DeptName { get; set; } = string.Empty;
        }

        private sealed class DeptRow
        {
            public string Abrv { get; set; } = string.Empty;
            public string? Company { get; set; }
            public string? DeptName { get; set; }
        }

        private sealed class EmployeeResult
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Abrv { get; set; }
            public string? Company { get; set; }
            public int? SubDepartmentId { get; set; }
            public string? SubDepartmentCode { get; set; }
            public string? SubDepartmentName { get; set; }
            public string? DeptName { get; set; }
            public int? PositionId { get; set; }
            public int? JobGradeId { get; set; }
            public int? Layer { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Reason { get; set; }
            public int MandatoryRequired { get; set; }
            public int MandatoryFulfilled { get; set; }
            public int RecommendedRequired { get; set; }
            public int RecommendedFulfilled { get; set; }
            public List<GapCourseDto> Gaps { get; set; } = new();
            public List<RequiredCourseDto> Required { get; set; } = new();
        }

        // ---- baris pemetaan Dapper ----

        private sealed class HrmsRow
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public int? SubDepartmentID { get; set; }
            public int? PositionID { get; set; }
            public int? JobGradeID { get; set; }
        }

        private sealed class SubDeptRow
        {
            public int SubDepartmentID { get; set; }
            public string SubDepartmentCode { get; set; } = string.Empty;
            public string SubDepartmentName { get; set; } = string.Empty;
            public string Abrv { get; set; } = string.Empty;
            public string? DeptName { get; set; }
        }

        private sealed class NameRow
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
        }

        private sealed class MatrixRow
        {
            public int MatrixID { get; set; }
            public string DepartmentAbrv { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int DocStatus { get; set; }
        }

        private sealed class DetailRow
        {
            public string DepartmentAbrv { get; set; } = string.Empty;
            public int SubDepartmentID { get; set; }
            public int Layer { get; set; }
            public int CourseId { get; set; }
            public string Requirement { get; set; } = string.Empty;
        }

        private sealed class CourseRow
        {
            public int CourseId { get; set; }
            public string CourseCode { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
        }

        private sealed class DoneRow
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public int CourseId { get; set; }
        }
    }
}
