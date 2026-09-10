namespace Training.Services.Interfaces
{
    using Training.Models.Dtos;

    /// <summary>
    /// Service untuk manajemen Training Course
    /// </summary>
    public interface ITrainingCourseService
    {
        /// <summary>
        /// Import Master Course dari Excel file
        /// </summary>
        /// <param name="filePath">Path ke file Excel yang di-upload</param>
        /// <param name="createdBy">Username yang melakukan import</param>
        /// <returns>Result dengan detail success/error</returns>
        Task<CourseImportResultDto> ImportCoursesFromExcelAsync(string filePath, string createdBy);

        /// <summary>
        /// Validasi single course data sebelum insert
        /// </summary>
        Task<(bool IsValid, string ErrorMessage)> ValidateCourseDataAsync(CourseImportDto course);

        /// <summary>
        /// Get category ID berdasarkan category code
        /// </summary>
        Task<int?> GetCategoryIdByCodeAsync(string categoryCode);

        /// <summary>
        /// Cek apakah course dengan nama yang sama sudah ada di kategori yang sama (case-insensitive)
        /// </summary>
        Task<bool> IsDuplicateCourseNameAsync(int categoryId, string courseName);

        /// <summary>
        /// Insert single course dengan auto-generated course code
        /// </summary>
        /// <remarks>
        /// Procedure: USP_Master_CreateCourse
        /// - Auto-increment sequence di Master_CategorySequence
        /// - Generate course code: {CategoryCode}-{Sequence}
        /// </remarks>
        Task<(bool Success, int CourseId, string CourseCode, string ErrorMessage)>
            CreateCourseAsync(int categoryId, string courseName, string description,
                decimal duration, string provider, bool isActive, string createdBy);
    }
}
