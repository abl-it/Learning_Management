using Training.Models;
using Training.Models.DataTables;

namespace Training.Services.IServices
{
    public interface ICategoryService
    {
        Task<DataTableResponse<Category>> GetCategoriesAsync(DataTableRequest request);

        Task<Category?> GetCategoryByIdAsync(int id);
        Task<ApiResponse> CreateCategoryAsync(CategoryDto categoryDto);
        Task<ApiResponse> UpdateCategoryAsync(CategoryDto categoryDto);
        Task<ApiResponse> DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(string categoryName, string categoryCode, int? excludeId = null);


        Task<List<Category>> GetAllCategoriesAsync();

    }
}
