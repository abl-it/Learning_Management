using Training.Models.DTO.TrainingMatrix;

namespace Training.Services.IServices
{
    /// <summary>
    /// Change Request (Add/Remove course) atas Training Matrix yang sudah Approved.
    /// Maksimal satu Change Request aktif per matrix. Saat Change Request di-approve,
    /// USP_Approve (workflow MATRIXCR) menerapkan perubahan ke matrix dalam satu transaksi.
    /// </summary>
    public interface IMatrixChangeRequestService
    {
        /// <summary>
        /// Membuat Change Request Draft untuk matrix yang sudah Approved.
        /// </summary>
        /// <param name="matrixId">Identifier matrix.</param>
        /// <param name="reason">Alasan perubahan (wajib, maks 500 karakter).</param>
        /// <param name="by">Employee code pembuat.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ChangeRequestId baru.</returns>
        /// <exception cref="ArgumentException">Matrix belum Approved, sudah punya CR aktif, atau data tidak valid.</exception>
        Task<int> CreateAsync(
            int matrixId,
            string? reason,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengambil Change Request lengkap beserta isi matrix saat ini.
        /// </summary>
        /// <param name="changeRequestId">Identifier Change Request.</param>
        /// <param name="roles">Seluruh role pengguna.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ChangeRequestDetailDto?> GetDetailAsync(
            int changeRequestId,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menambahkan perubahan "Add" (dari Course Group dan/atau course manual) ke Sub Dept x Layer.
        /// Course yang sudah ada di matrix atau sudah ada di Change Request dilewati.
        /// </summary>
        Task<MatrixApplyResultDto> AddCoursesAsync(
            ChangeRequestApplyDto request,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menandai baris matrix saat ini untuk dihapus (perubahan "Remove").
        /// </summary>
        /// <returns>Jumlah perubahan Remove yang ditambahkan.</returns>
        Task<int> MarkRemoveAsync(
            int changeRequestId,
            IReadOnlyCollection<int> matrixDetailIds,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Membatalkan perubahan (menghapus baris perubahan dari Change Request).
        /// </summary>
        /// <returns>Jumlah baris dibatalkan.</returns>
        Task<int> UndoChangesAsync(
            int changeRequestId,
            IReadOnlyCollection<int> changeDetailIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menghapus Change Request (soft delete). Hanya saat DocStatus = 0.
        /// </summary>
        Task DeleteAsync(
            int changeRequestId,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menjalankan aksi workflow MATRIXCR lewat USP_Approve setelah memeriksa role.
        /// </summary>
        /// <returns>Status baru Change Request.</returns>
        /// <exception cref="ArgumentException">Aksi/isian tidak valid.</exception>
        /// <exception cref="UnauthorizedAccessException">Role tidak berhak.</exception>
        Task<string> TransitionAsync(
            int changeRequestId,
            string action,
            string? remarks,
            string by,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default);
    }
}
