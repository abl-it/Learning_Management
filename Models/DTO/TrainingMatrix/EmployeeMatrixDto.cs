namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>Data pilihan filter Company / Department / Sub Department.</summary>
    public class MatrixFilterDto
    {
        /// <summary>Perusahaan yang tersedia (ABL, NKM).</summary>
        public List<string> Companies { get; set; } = new();

        /// <summary>Department (satu baris per ABRV).</summary>
        public List<FilterDepartmentDto> Departments { get; set; } = new();

        /// <summary>Sub Department.</summary>
        public List<FilterSubDepartmentDto> SubDepartments { get; set; } = new();
    }

    /// <summary>Department pada filter.</summary>
    public class FilterDepartmentDto
    {
        /// <summary>Kunci Department (ABRV).</summary>
        public string Abrv { get; set; } = string.Empty;

        /// <summary>Nama Department (DeptName).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Perusahaan (CoCode).</summary>
        public string Company { get; set; } = string.Empty;
    }

    /// <summary>Sub Department pada filter.</summary>
    public class FilterSubDepartmentDto
    {
        /// <summary>SubDepartmentID.</summary>
        public int Id { get; set; }

        /// <summary>Kode Sub Department.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Nama Sub Department.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ABRV Department induk.</summary>
        public string Abrv { get; set; } = string.Empty;
    }

    /// <summary>Satu baris karyawan pada daftar Employee Matrix.</summary>
    public class EmployeeMatrixRowDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ABRV Department.</summary>
        public string? DepartmentAbrv { get; set; }

        /// <summary>Nama Department (DeptName).</summary>
        public string? DepartmentName { get; set; }

        /// <summary>SubDepartmentID.</summary>
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

        /// <summary>Course Mandatory yang dibutuhkan.</summary>
        public int MandatoryRequired { get; set; }

        /// <summary>Course Mandatory yang terpenuhi.</summary>
        public int MandatoryFulfilled { get; set; }

        /// <summary>Course Recommended yang dibutuhkan.</summary>
        public int RecommendedRequired { get; set; }

        /// <summary>Course Recommended yang terpenuhi.</summary>
        public int RecommendedFulfilled { get; set; }
    }

    /// <summary>Course yang dibutuhkan karyawan menurut matrix Approved.</summary>
    public class RequiredCourseDto
    {
        /// <summary>CourseId.</summary>
        public int CourseId { get; set; }

        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>Mandatory atau Recommended.</summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>True bila PRESENT pada event Complete.</summary>
        public bool Fulfilled { get; set; }

        /// <summary>EventCode event yang memenuhi (terbaru).</summary>
        public string? FulfilledEventCode { get; set; }

        /// <summary>Tanggal mulai event yang memenuhi.</summary>
        public DateTime? FulfilledDate { get; set; }
    }

    /// <summary>Riwayat training aktual karyawan (dari kehadiran event).</summary>
    public class TrainingHistoryDto
    {
        /// <summary>TrainingEventId.</summary>
        public long TrainingEventId { get; set; }

        /// <summary>EventCode.</summary>
        public string EventCode { get; set; } = string.Empty;

        /// <summary>Kode course.</summary>
        public string? CourseCode { get; set; }

        /// <summary>Judul training.</summary>
        public string? TrainingTitle { get; set; }

        /// <summary>Tanggal mulai event.</summary>
        public DateTime? EventStartDate { get; set; }

        /// <summary>Tanggal selesai event.</summary>
        public DateTime? EventEndDate { get; set; }

        /// <summary>Tanggal realisasi (bila sudah direalisasi).</summary>
        public DateTime? RealizationDate { get; set; }

        /// <summary>Status event.</summary>
        public string? EventStatus { get; set; }

        /// <summary>Status kehadiran (AttendanceStatus).</summary>
        public string? AttendanceStatus { get; set; }

        /// <summary>Trainer.</summary>
        public string? TrainerName { get; set; }

        /// <summary>Tempat.</summary>
        public string? Venue { get; set; }

        /// <summary>True bila PRESENT pada event Complete (dihitung untuk compliance).</summary>
        public bool CountsForCompliance { get; set; }
    }

    /// <summary>Detail satu karyawan: course yang harus dilakukan (matrix) dan training aktual.</summary>
    public class EmployeeMatrixDetailDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ABRV Department.</summary>
        public string? DepartmentAbrv { get; set; }

        /// <summary>Nama Department (DeptName).</summary>
        public string? DepartmentName { get; set; }

        /// <summary>Nama Sub Department.</summary>
        public string? SubDepartmentName { get; set; }

        /// <summary>Kode Sub Department.</summary>
        public string? SubDepartmentCode { get; set; }

        /// <summary>Layer 1-3; null bila Unmapped.</summary>
        public int? Layer { get; set; }

        /// <summary>Included, Unmapped, atau No Matrix.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Alasan Unmapped.</summary>
        public string? Reason { get; set; }

        /// <summary>Status dokumen matrix Department (Approved, Draft, ...).</summary>
        public string? MatrixStatus { get; set; }

        /// <summary>MatrixID dokumen Department.</summary>
        public int? MatrixId { get; set; }

        /// <summary>Course Mandatory yang dibutuhkan.</summary>
        public int MandatoryRequired { get; set; }

        /// <summary>Course Mandatory yang terpenuhi.</summary>
        public int MandatoryFulfilled { get; set; }

        /// <summary>Course Recommended yang dibutuhkan.</summary>
        public int RecommendedRequired { get; set; }

        /// <summary>Course Recommended yang terpenuhi.</summary>
        public int RecommendedFulfilled { get; set; }

        /// <summary>Course menurut matrix (kosong bila bukan Included).</summary>
        public List<RequiredCourseDto> Required { get; set; } = new();

        /// <summary>Riwayat training aktual.</summary>
        public List<TrainingHistoryDto> History { get; set; } = new();
    }
}
