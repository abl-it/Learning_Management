namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>File hasil export (isi + nama file).</summary>
    public class MatrixExportFileDto
    {
        /// <summary>Nama file .xlsx.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Isi file.</summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>Content type.</summary>
        public string ContentType { get; set; } =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    /// <summary>Data untuk sheet export Training Matrix.</summary>
    public class MatrixExportDto
    {
        /// <summary>Judul sheet.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Waktu export (lokal).</summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>Department yang diexport.</summary>
        public List<MatrixExportDepartmentDto> Departments { get; set; } = new();
    }

    /// <summary>Department pada export.</summary>
    public class MatrixExportDepartmentDto
    {
        /// <summary>ABRV.</summary>
        public string Abrv { get; set; } = string.Empty;

        /// <summary>DeptName.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Status dokumen matrix ("Approved", "Draft", "No matrix", ...).</summary>
        public string MatrixStatus { get; set; } = string.Empty;

        /// <summary>True bila matrix Approved (isi course ditampilkan).</summary>
        public bool IsApproved { get; set; }

        /// <summary>Sub Department.</summary>
        public List<MatrixExportSubDepartmentDto> SubDepartments { get; set; } = new();
    }

    /// <summary>Sub Department pada export.</summary>
    public class MatrixExportSubDepartmentDto
    {
        /// <summary>Kode.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Jumlah karyawan Layer 1.</summary>
        public int Layer1Employees { get; set; }

        /// <summary>Jumlah karyawan Layer 2.</summary>
        public int Layer2Employees { get; set; }

        /// <summary>Jumlah karyawan Layer 3.</summary>
        public int Layer3Employees { get; set; }

        /// <summary>Jumlah karyawan Unmapped.</summary>
        public int UnmappedEmployees { get; set; }

        /// <summary>Course pada matrix.</summary>
        public List<MatrixExportCourseDto> Courses { get; set; } = new();

        /// <summary>Karyawan yang belum dapat dinilai (Unmapped) pada Sub Department ini.</summary>
        public List<MatrixExportUnmappedDto> Unmapped { get; set; } = new();

        /// <summary>Karyawan yang dapat dinilai (Included) pada Sub Department ini.</summary>
        public List<MatrixExportPersonDto> People { get; set; } = new();
    }

    /// <summary>Karyawan Included pada export.</summary>
    public class MatrixExportPersonDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Layer 1-3.</summary>
        public int Layer { get; set; }
    }

    /// <summary>Karyawan Unmapped pada export.</summary>
    public class MatrixExportUnmappedDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Alasan Unmapped.</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>Satu course dan requirement-nya per Layer.</summary>
    public class MatrixExportCourseDto
    {
        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>Requirement pada Layer 1 (Mandatory/Recommended) atau null.</summary>
        public string? Layer1 { get; set; }

        /// <summary>Requirement pada Layer 2 atau null.</summary>
        public string? Layer2 { get; set; }

        /// <summary>Requirement pada Layer 3 atau null.</summary>
        public string? Layer3 { get; set; }

        /// <summary>Karyawan yang terkena course ini (Layer-nya punya requirement) beserta aktualnya.</summary>
        public List<MatrixExportEmployeeDto> Employees { get; set; } = new();
    }

    /// <summary>Karyawan pada satu course beserta training aktualnya.</summary>
    public class MatrixExportEmployeeDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Nama.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Layer karyawan.</summary>
        public int Layer { get; set; }

        /// <summary>Requirement untuk Layer karyawan (Mandatory/Recommended).</summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>True bila PRESENT pada event Complete.</summary>
        public bool Fulfilled { get; set; }

        /// <summary>EventCode (yang memenuhi, atau event terbaru bila belum memenuhi).</summary>
        public string? EventCode { get; set; }

        /// <summary>Tanggal mulai event tersebut.</summary>
        public DateTime? EventDate { get; set; }

        /// <summary>Status event tersebut.</summary>
        public string? EventStatus { get; set; }

        /// <summary>Kehadiran pada event tersebut.</summary>
        public string? Attendance { get; set; }
    }
}
