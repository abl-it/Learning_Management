namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Payload simpan Course Group (create bila <see cref="CourseGroupId"/> = 0, selain itu update).
    /// </summary>
    public class CourseGroupSaveDto
    {
        /// <summary>Identifier Course Group; 0 untuk data baru.</summary>
        public int CourseGroupId { get; set; }

        /// <summary>Nama Course Group (wajib, unik, maks 100 karakter).</summary>
        public string? GroupName { get; set; }

        /// <summary>Deskripsi (opsional, maks 500 karakter).</summary>
        public string? Description { get; set; }

        /// <summary>Status aktif.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Daftar CourseId yang menjadi isi group (minimal satu).</summary>
        public List<int> CourseIds { get; set; } = new();
    }
}
