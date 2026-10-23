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

        private const string ReasonNoSubDept = "Sub Department belum diisi";
        private const string ReasonUnknownSubDept = "Sub Department tidak dikenal";
        private const string ReasonNoPosition = "PositionID belum diisi";
        private const string ReasonNoGrade = "JobGrade belum diisi";
        private const string ReasonGradeOutOfRange = "JobGrade di luar Layer 1-3 (Manager Operasional = Layer 3)";

        private readonly string _connectionString;
        private readonly string? _hrmsConnectionString;
        private readonly ILogger<TrainingMatrixReportService> _logger;

        /// <summary>
        /// Membuat instance baru <see cref="TrainingMatrixReportService"/>.
        /// </summary>
        public TrainingMatrixReportService(
            IConfiguration configuration,
            ILogger<TrainingMatrixReportService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _hrmsConnectionString = configuration.GetConnectionString("HRMS");
            _logger = logger;
        }

        // =====================================================================
        // PUBLIC
        // =====================================================================

        /// <inheritdoc />
        public async Task<ComplianceReportDto> GetSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var model = await BuildAsync(cancellationToken);

            var included = model.Employees.Where(e => e.Status == StatusIncluded).ToList();

            var report = new ComplianceReportDto
            {
                GeneratedAt = DateTime.Now,
                TotalEmployees = model.Employees.Count,
                IncludedEmployees = included.Count,
                UnmappedEmployees = model.Employees.Count(e => e.Status == StatusUnmapped),
                NoMatrixEmployees = model.Employees.Count(e => e.Status == StatusNoMatrix),
                MandatoryRequired = included.Sum(e => e.MandatoryRequired),
                MandatoryFulfilled = included.Sum(e => e.MandatoryFulfilled),
                EmployeesWithGap = included.Count(e => e.MandatoryFulfilled < e.MandatoryRequired)
            };

            report.CompliancePercent = Pct(report.MandatoryFulfilled, report.MandatoryRequired);
            report.UnmappedPercent = Pct(report.UnmappedEmployees, report.TotalEmployees);

            report.UnmappedReasons = model.Employees
                .Where(e => e.Status == StatusUnmapped)
                .GroupBy(e => e.Reason ?? string.Empty)
                .Select(g => new UnmappedReasonDto { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToList();

            report.Departments = model.DepartmentAbrvs
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .Select(abrv =>
                {
                    var inDept = model.Employees
                        .Where(e => string.Equals(e.Abrv, abrv, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var inc = inDept.Where(e => e.Status == StatusIncluded).ToList();

                    model.MatrixStatus.TryGetValue(abrv, out var status);
                    model.MatrixIds.TryGetValue(abrv, out var matrixId);

                    var dto = new DepartmentComplianceDto
                    {
                        DepartmentAbrv = abrv,
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
            CancellationToken cancellationToken = default)
        {
            var model = await BuildAsync(cancellationToken);

            return model.Employees
                .Where(e => e.Status == StatusUnmapped)
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

                SELECT DISTINCT LTRIM(RTRIM(ABRV)) AS Abrv
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

            var abrvs = (await multi.ReadAsync<string>()).ToList();

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

                        if (!fulfilled)
                        {
                            courses.TryGetValue(r.CourseId, out var course);

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

            return new ReportModel
            {
                Employees = employees,
                DepartmentAbrvs = abrvs,
                MatrixStatus = matrixStatus,
                MatrixIds = matrixIds
            };
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

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
            public List<string> DepartmentAbrvs { get; set; } = new();
            public Dictionary<string, string> MatrixStatus { get; set; } = new();
            public Dictionary<string, int> MatrixIds { get; set; } = new();
        }

        private sealed class EmployeeResult
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Abrv { get; set; }
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
