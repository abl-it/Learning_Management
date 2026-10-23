namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Dampak perubahan isi sebuah Course Group terhadap matrix yang pernah memakainya
    /// (berdasarkan <c>SourceCourseGroupID</c>). Hanya Add: course yang ada di group tetapi belum ada
    /// di sel Sub Department × Layer yang dulu diisi dari group tersebut.
    /// </summary>
    public class GroupImpactDto
    {
        /// <summary>Identifier Course Group.</summary>
        public int CourseGroupId { get; set; }

        /// <summary>Nama group.</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Jumlah course aktif di group saat ini.</summary>
        public int ActiveCourseCount { get; set; }

        /// <summary>Jumlah matrix yang pernah memakai group ini.</summary>
        public int TotalMatrices { get; set; }

        /// <summary>Jumlah matrix yang sudah sinkron (tidak ada course yang kurang).</summary>
        public int UpToDateMatrices { get; set; }

        /// <summary>Matrix yang masih kurang course dari group.</summary>
        public List<GroupImpactItemDto> Items { get; set; } = new();
    }

    /// <summary>Satu matrix yang belum sinkron dengan Course Group.</summary>
    public class GroupImpactItemDto
    {
        /// <summary>Identifier matrix.</summary>
        public int MatrixId { get; set; }

        /// <summary>Kunci Department (ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>Status matrix (Draft, Rejected, Approved, ...).</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>DocStatus matrix (0 draft, 1 pending, 2 approved).</summary>
        public int DocStatus { get; set; }

        /// <summary>Jumlah sel (Sub Department × Layer) yang diisi dari group ini.</summary>
        public int CellCount { get; set; }

        /// <summary>Jumlah baris (sel × course) yang akan ditambahkan.</summary>
        public int MissingCount { get; set; }

        /// <summary>Course yang kurang (distinct).</summary>
        public List<GroupImpactCourseDto> MissingCourses { get; set; } = new();

        /// <summary>
        /// Cara penerapan: <c>Direct</c> (matrix Draft diubah langsung), <c>NewCr</c> (membuat Change Request),
        /// <c>AppendCr</c> (menambah ke Change Request Draft yang sudah ada), atau <c>Blocked</c>.
        /// </summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>Change Request aktif (bila ada).</summary>
        public int? ChangeRequestId { get; set; }

        /// <summary>Alasan tidak bisa diterapkan (Mode = Blocked).</summary>
        public string? BlockedReason { get; set; }
    }

    /// <summary>Course yang kurang.</summary>
    public class GroupImpactCourseDto
    {
        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;
    }

    /// <summary>Hasil penerapan Group Impact.</summary>
    public class GroupImpactApplyResultDto
    {
        /// <summary>Hasil per matrix.</summary>
        public List<GroupImpactApplyItemDto> Results { get; set; } = new();
    }

    /// <summary>Hasil penerapan pada satu matrix.</summary>
    public class GroupImpactApplyItemDto
    {
        /// <summary>Identifier matrix.</summary>
        public int MatrixId { get; set; }

        /// <summary>Kunci Department (ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>True bila berhasil diproses (termasuk "sudah sinkron").</summary>
        public bool Success { get; set; }

        /// <summary>Baris yang ditambahkan.</summary>
        public int Added { get; set; }

        /// <summary>Change Request yang dibuat / ditambah (bila ada).</summary>
        public int? ChangeRequestId { get; set; }

        /// <summary>Penjelasan hasil.</summary>
        public string Message { get; set; } = string.Empty;
    }
}
