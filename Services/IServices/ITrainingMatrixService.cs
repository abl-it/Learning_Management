using Training.Models.DTO.TrainingMatrix;

namespace Training.Services.IServices
{
    /// <summary>
    /// Mengelola dokumen Training Matrix (satu dokumen per Department, dikunci menurut ABRV).
    /// Otorisasi per aksi (role tc untuk edit, role approver untuk approve) ditegakkan di sini
    /// sebagai lapis kedua; controller tetap memeriksa role tc untuk operasi tulis.
    /// </summary>
    public interface ITrainingMatrixService
    {
        /// <summary>
        /// Mengambil SEMUA role pengguna dari home.dbo.Users.Role (dipisah ';').
        /// Berbeda dari ITrainingService.GetRoleAsync yang hanya mengembalikan role tertinggi menurut
        /// HierarchyLevel, sehingga role seperti tc atau hrhead bisa "tertutup" role lain.
        /// </summary>
        /// <param name="username">Username pengguna.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Daftar role (kosong bila tidak ada).</returns>
        Task<IReadOnlyList<string>> GetRolesAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengambil seluruh Department aktif (per ABRV) beserta status matrix-nya.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<MatrixListItemDto>> GetListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Membuat dokumen matrix Draft untuk satu Department.
        /// </summary>
        /// <param name="departmentAbrv">ABRV Department.</param>
        /// <param name="createdBy">Employee code pembuat.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>MatrixId baru.</returns>
        /// <exception cref="ArgumentException">ABRV tidak dikenal atau matrix sudah ada.</exception>
        Task<int> CreateAsync(
            string departmentAbrv,
            string createdBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengambil dokumen matrix lengkap.
        /// </summary>
        /// <param name="matrixId">Identifier matrix.</param>
        /// <param name="roles">Seluruh role pengguna (menentukan IsEditable dan Actions).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dokumen, atau null bila tidak ditemukan.</returns>
        Task<MatrixDetailDto?> GetDetailAsync(
            int matrixId,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menambahkan course (snapshot dari Course Group dan/atau pilihan manual) ke
        /// kombinasi Sub Department x Layer. Hanya saat DocStatus = 0.
        /// </summary>
        /// <param name="request">Data apply.</param>
        /// <param name="by">Employee code pengguna.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Data tidak valid atau dokumen terkunci.</exception>
        Task<MatrixApplyResultDto> ApplyAsync(
            MatrixApplyDto request,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menghapus baris detail. Hanya saat DocStatus = 0.
        /// </summary>
        /// <returns>Jumlah baris terhapus.</returns>
        Task<int> RemoveRowsAsync(
            int matrixId,
            IReadOnlyCollection<int> detailIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mengubah Requirement (Mandatory/Recommended) baris terpilih. Hanya saat DocStatus = 0.
        /// </summary>
        /// <returns>Jumlah baris berubah.</returns>
        Task<int> SetRequirementAsync(
            int matrixId,
            IReadOnlyCollection<int> detailIds,
            string requirement,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menghapus dokumen (soft delete). Hanya saat DocStatus = 0 (Draft atau Rejected).
        /// </summary>
        Task DeleteAsync(
            int matrixId,
            string by,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menjalankan aksi workflow (Request for Approve, Approve, Reject) lewat USP_Approve,
        /// setelah memeriksa kecocokan role dengan dbo.Workflow.Allowed.
        /// </summary>
        /// <param name="matrixId">Identifier matrix.</param>
        /// <param name="action">Request for Approve, Approve, atau Reject.</param>
        /// <param name="remarks">Catatan (wajib untuk Reject, maks 250 karakter).</param>
        /// <param name="by">Employee code pengguna.</param>
        /// <param name="roles">Seluruh role pengguna.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Status baru dokumen.</returns>
        /// <exception cref="ArgumentException">Aksi/isian tidak valid.</exception>
        /// <exception cref="UnauthorizedAccessException">Role tidak berhak atas aksi ini.</exception>
        Task<string> TransitionAsync(
            int matrixId,
            string action,
            string? remarks,
            string by,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default);
    }
}
