namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Payload "Apply": menambahkan course (dari Course Group dan/atau pilihan manual)
    /// ke satu atau beberapa kombinasi Sub Department x Layer. Dikirim sebagai form post.
    /// </summary>
    public class MatrixApplyDto
    {
        /// <summary>Identifier dokumen matrix.</summary>
        public int MatrixId { get; set; }

        /// <summary>Course Group yang di-expand (opsional).</summary>
        public int? CourseGroupId { get; set; }

        /// <summary>Course tambahan yang dipilih manual (opsional).</summary>
        public List<int> CourseIds { get; set; } = new();

        /// <summary>Mandatory atau Recommended.</summary>
        public string? Requirement { get; set; }

        /// <summary>Target, masing-masing berformat "SubDepartmentId:Layer", mis. "12:1".</summary>
        public List<string> Targets { get; set; } = new();
    }

    /// <summary>Hasil Apply.</summary>
    public class MatrixApplyResultDto
    {
        /// <summary>Baris baru yang ditambahkan.</summary>
        public int Added { get; set; }

        /// <summary>Baris yang dilewati karena sudah ada.</summary>
        public int Skipped { get; set; }
    }
}
