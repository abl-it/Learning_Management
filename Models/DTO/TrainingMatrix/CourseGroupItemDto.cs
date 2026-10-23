namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Satu course di dalam Course Group, atau satu opsi pada picker course.
    /// </summary>
    public class CourseGroupItemDto
    {
        /// <summary>Identifier course (dbo.Master_Courses.CourseId).</summary>
        public int CourseId { get; set; }

        /// <summary>Kode course.</summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>Nama course.</summary>
        public string CourseName { get; set; } = string.Empty;

        /// <summary>Durasi dalam jam.</summary>
        public decimal DurationHours { get; set; }

        /// <summary>Status aktif course pada master.</summary>
        public bool IsActive { get; set; }
    }
}
