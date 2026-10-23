namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Change Request atas matrix yang sudah Approved: header, matrix saat ini, dan perubahan yang diajukan.
    /// </summary>
    public class ChangeRequestDetailDto
    {
        /// <summary>Identifier Change Request.</summary>
        public int ChangeRequestId { get; set; }

        /// <summary>Identifier matrix yang diubah.</summary>
        public int MatrixId { get; set; }

        /// <summary>Kunci Department (ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>Status workflow Change Request.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>DocStatus (0 draft, 1 pending, 2 approved/applied).</summary>
        public int DocStatus { get; set; }

        /// <summary>Alasan perubahan.</summary>
        public string? Reason { get; set; }

        /// <summary>Catatan approval terakhir.</summary>
        public string? Notes { get; set; }

        /// <summary>Approver terakhir.</summary>
        public string? ApprovedBy { get; set; }

        /// <summary>Tanggal approve.</summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>Pembuat.</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Tanggal dibuat.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>True bila pengguna boleh mengubah isi (role tc dan DocStatus = 0).</summary>
        public bool IsEditable { get; set; }

        /// <summary>Aksi workflow yang tersedia untuk role pengguna.</summary>
        public List<string> Actions { get; set; } = new();

        /// <summary>Sub Department milik Department matrix.</summary>
        public List<MatrixSubDepartmentDto> SubDepartments { get; set; } = new();

        /// <summary>Isi matrix saat ini (yang sudah Approved).</summary>
        public List<MatrixRowDto> MatrixRows { get; set; } = new();

        /// <summary>Perubahan yang diajukan (Add / Remove).</summary>
        public List<ChangeRequestRowDto> Changes { get; set; } = new();
    }

    /// <summary>Satu perubahan pada Change Request.</summary>
    public class ChangeRequestRowDto
    {
        /// <summary>Identifier baris perubahan.</summary>
        public int ChangeRequestDetailId { get; set; }

        /// <summary>Add atau Remove.</summary>
        public string ActionType { get; set; } = string.Empty;

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

        /// <summary>Requirement (hanya untuk Add).</summary>
        public string? Requirement { get; set; }

        /// <summary>Course Group asal (hanya untuk Add).</summary>
        public int? SourceCourseGroupId { get; set; }

        /// <summary>Nama Course Group asal.</summary>
        public string? SourceCourseGroupName { get; set; }
    }

    /// <summary>Payload menambahkan course ke Change Request (form post).</summary>
    public class ChangeRequestApplyDto
    {
        /// <summary>Identifier Change Request.</summary>
        public int ChangeRequestId { get; set; }

        /// <summary>Course Group yang di-expand (opsional).</summary>
        public int? CourseGroupId { get; set; }

        /// <summary>Course tambahan manual (opsional).</summary>
        public List<int> CourseIds { get; set; } = new();

        /// <summary>Mandatory atau Recommended.</summary>
        public string? Requirement { get; set; }

        /// <summary>Target "SubDepartmentId:Layer".</summary>
        public List<string> Targets { get; set; } = new();
    }
}
