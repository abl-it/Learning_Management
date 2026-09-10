namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk import Master Course dari Excel
    /// </summary>
    public class CourseImportDto
    {
        /// <summary>
        /// Category Code (TMG, TSK, TIS, TSF, TWI, dll)
        /// </summary>
        public string CategoryCode { get; set; }

        /// <summary>
        /// Nama kursus
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// Deskripsi kursus
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Durasi training dalam jam
        /// </summary>
        public decimal Duration { get; set; }

        /// <summary>
        /// Tipe trainer/provider (Internal, External, dll)
        /// </summary>
        public string TrainerType { get; set; }

        /// <summary>
        /// Status aktif (Yes/No)
        /// </summary>
        public string IsActive { get; set; }
    }

    /// <summary>
    /// Response untuk hasil import Master Course
    /// </summary>
    public class CourseImportResultDto
    {
        public int SuccessCount { get; set; }
        public List<CourseImportErrorDto> Errors { get; set; } = new();
        public string Message { get; set; }

        /// <summary>
        /// Cek apakah ada error
        /// </summary>
        public bool HasErrors => Errors?.Count > 0;
    }

    /// <summary>
    /// Detail error per row saat import
    /// </summary>
    public class CourseImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; }
        public string CategoryCode { get; set; }
        public string CourseName { get; set; }
    }
}
