namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Dokumen matrix lengkap: header, Sub Department, baris course, dan aksi workflow
    /// yang boleh dilakukan pengguna saat ini.
    /// </summary>
    public class MatrixDetailDto
    {
        /// <summary>Identifier dokumen matrix.</summary>
        public int MatrixId { get; set; }

        /// <summary>Kunci Department (ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>Status workflow.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>DocStatus (0 draft, 1 pending, 2 approved).</summary>
        public int DocStatus { get; set; }

        /// <summary>Catatan approval terakhir.</summary>
        public string? Notes { get; set; }

        /// <summary>Approver terakhir.</summary>
        public string? ApprovedBy { get; set; }

        /// <summary>Tanggal approve.</summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>True bila pengguna ini boleh mengubah isi (role tc dan DocStatus = 0).</summary>
        public bool IsEditable { get; set; }

        /// <summary>Change Request aktif (bila ada).</summary>
        public int? ActiveChangeRequestId { get; set; }

        /// <summary>True bila pengguna (role tc) boleh membuat Change Request: matrix Approved dan belum ada CR aktif.</summary>
        public bool CanRequestChange { get; set; }

        /// <summary>Aksi workflow yang tersedia untuk role pengguna pada status saat ini.</summary>
        public List<string> Actions { get; set; } = new();

        /// <summary>Sub Department di bawah Department ini.</summary>
        public List<MatrixSubDepartmentDto> SubDepartments { get; set; } = new();

        /// <summary>Baris detail matrix.</summary>
        public List<MatrixRowDto> Rows { get; set; } = new();
    }

    /// <summary>Sub Department pada dokumen matrix.</summary>
    public class MatrixSubDepartmentDto
    {
        /// <summary>Identifier Sub Department.</summary>
        public int SubDepartmentId { get; set; }

        /// <summary>Kode Sub Department.</summary>
        public string SubDepartmentCode { get; set; } = string.Empty;

        /// <summary>Nama Sub Department.</summary>
        public string SubDepartmentName { get; set; } = string.Empty;

        /// <summary>Kode Department induk (mis. C300 atau D300).</summary>
        public string DepartmentCode { get; set; } = string.Empty;
    }

    /// <summary>Satu baris matrix: Sub Dept x Layer x Course.</summary>
    public class MatrixRowDto
    {
        /// <summary>Identifier baris.</summary>
        public int MatrixDetailId { get; set; }

        /// <summary>Identifier Sub Department.</summary>
        public int SubDepartmentId { get; set; }

        /// <summary>Layer (1-3).</summary>
        public int Layer { get; set; }

        /// <summary>Identifier course.</summary>
        public int CourseId { get; set; }

        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>Durasi (jam).</summary>
        public decimal DurationHours { get; set; }

        /// <summary>Mandatory atau Recommended.</summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>Course Group asal saat di-apply (informasi saja).</summary>
        public int? SourceCourseGroupId { get; set; }

        /// <summary>Nama Course Group asal; null bila group sudah dihapus.</summary>
        public string? SourceCourseGroupName { get; set; }
    }
}
