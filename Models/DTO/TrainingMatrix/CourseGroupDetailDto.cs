namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Header Course Group beserta daftar course di dalamnya.
    /// </summary>
    public class CourseGroupDetailDto
    {
        /// <summary>Identifier Course Group.</summary>
        public int CourseGroupId { get; set; }

        /// <summary>Nama Course Group.</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Deskripsi (opsional).</summary>
        public string? Description { get; set; }

        /// <summary>Status aktif.</summary>
        public bool IsActive { get; set; }

        /// <summary>Course di dalam group.</summary>
        public List<CourseGroupItemDto> Courses { get; set; } = new();
    }
}
