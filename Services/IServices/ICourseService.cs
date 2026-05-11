using Training.Models;
using Training.Models.DataTables;

namespace Training.Services.IServices
{
    public interface ICourseService
    {
        Task<DataTableResponse<Course>> GetCoursesAsync(DataTableRequest request, CancellationToken ct = default);

        Task<Category?> GetCourseByIdAsync(int id);
        Task<ApiResponse> CreateCourseAsync(CourseDto courseDto, CancellationToken ct = default);
        Task<ApiResponse> UpdateCourseAsync(CourseDto courseDto, CancellationToken ct = default);
        Task<ApiResponse> DeleteCourseAsync(int id, CancellationToken ct = default);
        Task<bool> CourseExistsAsync(string courseCode, int? excludeId = null);

        Task<List<Course>> GetAllCourseAsync();
    }
}
