using Training.Models.DTO.TrainingMatrix;

namespace Training.Services.IServices
{
    /// <summary>
    /// Mengelola Course Group: paket course yang dipakai ulang saat menyusun Training Matrix.
    /// </summary>
    public interface ICourseGroupService
    {
        /// <summary>
        /// Mengambil seluruh Course Group beserta jumlah course-nya.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Daftar Course Group, diurutkan menurut nama.</returns>
        Task<List<CourseGroupListItemDto>> GetListAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengambil satu Course Group beserta course di dalamnya.
        /// </summary>
        /// <param name="courseGroupId">Identifier Course Group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Detail group, atau null bila tidak ditemukan.</returns>
        Task<CourseGroupDetailDto?> GetDetailAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengambil course aktif dari master course untuk picker.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Course aktif, diurutkan menurut kode.</returns>
        Task<List<CourseGroupItemDto>> GetCourseOptionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Membuat atau memperbarui Course Group beserta isinya dalam satu transaksi.
        /// </summary>
        /// <param name="request">Data yang disimpan.</param>
        /// <param name="savedBy">Employee code pengguna yang menyimpan.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Identifier Course Group yang disimpan.</returns>
        /// <exception cref="ArgumentException">Data tidak valid, nama duplikat, atau course tidak ada di master.</exception>
        Task<int> SaveAsync(
            CourseGroupSaveDto request,
            string savedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menghapus Course Group beserta isinya.
        /// </summary>
        /// <param name="courseGroupId">Identifier Course Group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Course Group tidak ditemukan.</exception>
        Task DeleteAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default);
    }
}
