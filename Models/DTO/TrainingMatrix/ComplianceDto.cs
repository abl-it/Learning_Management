namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Ringkasan Compliance Training Matrix seluruh perusahaan.
    /// Course dianggap terpenuhi bila karyawan PRESENT pada event berstatus Complete untuk course tersebut.
    /// Karyawan Unmapped dan Department tanpa matrix Approved tidak masuk penyebut.
    /// </summary>
    public class ComplianceReportDto
    {
        /// <summary>Waktu laporan dihitung.</summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>Total karyawan aktif di HRMS.</summary>
        public int TotalEmployees { get; set; }

        /// <summary>Karyawan yang masuk perhitungan (Layer dikenal dan matrix Approved).</summary>
        public int IncludedEmployees { get; set; }

        /// <summary>Karyawan Unmapped (Sub Dept/Position/JobGrade belum lengkap atau tidak dikenal).</summary>
        public int UnmappedEmployees { get; set; }

        /// <summary>Karyawan yang Department-nya belum punya matrix Approved.</summary>
        public int NoMatrixEmployees { get; set; }

        /// <summary>Karyawan yang perusahaannya tidak bisa ditentukan (Sub Department kosong/tidak dikenal); tidak ikut filter ABL/NKM.</summary>
        public int NoCompanyEmployees { get; set; }

        /// <summary>Perusahaan (ABL/NKM) yang punya Department yang boleh dilihat pengguna; dipakai mengisi filter Company.</summary>
        public List<string> AvailableCompanies { get; set; } = new();

        /// <summary>Total course Mandatory yang wajib dipenuhi.</summary>
        public int MandatoryRequired { get; set; }

        /// <summary>Total course Mandatory yang terpenuhi.</summary>
        public int MandatoryFulfilled { get; set; }

        /// <summary>% Compliance Mandatory; null bila belum ada kewajiban.</summary>
        public double? CompliancePercent { get; set; }

        /// <summary>Karyawan yang masih punya gap Mandatory.</summary>
        public int EmployeesWithGap { get; set; }

        /// <summary>% Unmapped terhadap total karyawan.</summary>
        public double? UnmappedPercent { get; set; }

        /// <summary>Ringkasan per Department (ABRV).</summary>
        public List<DepartmentComplianceDto> Departments { get; set; } = new();

        /// <summary>Alasan Unmapped beserta jumlahnya (Data Quality).</summary>
        public List<UnmappedReasonDto> UnmappedReasons { get; set; } = new();
    }

    /// <summary>Compliance satu Department (ABRV).</summary>
    public class DepartmentComplianceDto
    {
        /// <summary>Kunci Department (ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>Nama Department (DeptName); bila satu ABRV menggabungkan beberapa Department, dipisah " / ".</summary>
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>Status matrix: Approved, Draft, Pending for HRGA Head, Rejected, atau No matrix.</summary>
        public string MatrixStatus { get; set; } = string.Empty;

        /// <summary>Identifier matrix (bila ada).</summary>
        public int? MatrixId { get; set; }

        /// <summary>Jumlah karyawan pada Department ini.</summary>
        public int EmployeeCount { get; set; }

        /// <summary>Karyawan yang masuk perhitungan.</summary>
        public int IncludedCount { get; set; }

        /// <summary>Karyawan Unmapped (Layer tidak bisa ditentukan).</summary>
        public int UnmappedCount { get; set; }

        /// <summary>Total Mandatory wajib.</summary>
        public int MandatoryRequired { get; set; }

        /// <summary>Total Mandatory terpenuhi.</summary>
        public int MandatoryFulfilled { get; set; }

        /// <summary>% Compliance Mandatory.</summary>
        public double? CompliancePercent { get; set; }

        /// <summary>Karyawan dengan gap Mandatory.</summary>
        public int EmployeesWithGap { get; set; }

        /// <summary>Total Recommended.</summary>
        public int RecommendedRequired { get; set; }

        /// <summary>Total Recommended terpenuhi.</summary>
        public int RecommendedFulfilled { get; set; }

        /// <summary>% Recommended (dilaporkan terpisah).</summary>
        public double? RecommendedPercent { get; set; }
    }

    /// <summary>Alasan Unmapped dan jumlahnya.</summary>
    public class UnmappedReasonDto
    {
        /// <summary>Alasan.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Jumlah karyawan.</summary>
        public int Count { get; set; }
    }

    /// <summary>Compliance satu karyawan beserta daftar gap.</summary>
    public class EmployeeComplianceDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Identifier Sub Department.</summary>
        public int? SubDepartmentId { get; set; }

        /// <summary>Kode Sub Department.</summary>
        public string? SubDepartmentCode { get; set; }

        /// <summary>Nama Sub Department.</summary>
        public string? SubDepartmentName { get; set; }

        /// <summary>Layer 1-3; null bila Unmapped.</summary>
        public int? Layer { get; set; }

        /// <summary>Included, Unmapped, atau No Matrix.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Alasan Unmapped.</summary>
        public string? Reason { get; set; }

        /// <summary>Mandatory wajib.</summary>
        public int MandatoryRequired { get; set; }

        /// <summary>Mandatory terpenuhi.</summary>
        public int MandatoryFulfilled { get; set; }

        /// <summary>Recommended wajib.</summary>
        public int RecommendedRequired { get; set; }

        /// <summary>Recommended terpenuhi.</summary>
        public int RecommendedFulfilled { get; set; }

        /// <summary>Course yang belum terpenuhi.</summary>
        public List<GapCourseDto> Gaps { get; set; } = new();
    }

    /// <summary>Course yang belum dipenuhi karyawan.</summary>
    public class GapCourseDto
    {
        /// <summary>Identifier course.</summary>
        public int CourseId { get; set; }

        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>Mandatory atau Recommended.</summary>
        public string Requirement { get; set; } = string.Empty;
    }

    /// <summary>Karyawan Unmapped (Data Quality).</summary>
    public class UnmappedEmployeeDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Department (ABRV) bila Sub Dept dikenal.</summary>
        public string? DepartmentAbrv { get; set; }

        /// <summary>Nama Department (APP_Departments.DeptName) bila Sub Dept dikenal.</summary>
        public string? DepartmentName { get; set; }

        /// <summary>Nama Sub Department bila dikenal.</summary>
        public string? SubDepartmentName { get; set; }

        /// <summary>Identifier Sub Department di HRMS.</summary>
        public int? SubDepartmentId { get; set; }

        /// <summary>PositionID di HRMS.</summary>
        public int? PositionId { get; set; }

        /// <summary>JobGradeID dari Position.</summary>
        public int? JobGradeId { get; set; }

        /// <summary>Alasan Unmapped.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
