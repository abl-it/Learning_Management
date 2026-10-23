namespace Training.Services.IServices
{
    /// <summary>
    /// Pembatasan akses Training Matrix per Department (ABRV) untuk pengguna saat ini.
    /// Role <c>tc</c>, <c>hrhead</c>, <c>gm</c>, dan <c>director</c> melihat semua Department. Pengguna lain hanya melihat Department yang
    /// dikembalikan <c>dbo.fc_GetDepartmentsByAccess(@employeeCode, 'MATRIX')</c>: Department miliknya sendiri
    /// ditambah baris <c>dbo.Access</c> dengan DocumentType 'MATRIX' (mendukung ALL, ALL-ABL, ALL-NKM, atau daftar ABRV).
    /// </summary>
    public interface IMatrixAccessService
    {
        /// <summary>
        /// ABRV yang boleh dilihat; <c>null</c> berarti tanpa batas (tc / hrhead).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlySet<string>?> GetAllowedAbrvsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Memastikan pengguna boleh melihat Department tertentu.
        /// </summary>
        /// <param name="abrv">ABRV Department.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="UnauthorizedAccessException">Tidak punya akses.</exception>
        Task EnsureAbrvAsync(string? abrv, CancellationToken cancellationToken = default);

        /// <summary>
        /// Memastikan pengguna boleh melihat Department milik sebuah matrix.
        /// </summary>
        /// <param name="matrixId">Identifier matrix.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Matrix tidak ditemukan.</exception>
        /// <exception cref="UnauthorizedAccessException">Tidak punya akses.</exception>
        Task EnsureMatrixAsync(int matrixId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Memastikan pengguna boleh melihat Department milik sebuah Change Request.
        /// </summary>
        /// <param name="changeRequestId">Identifier Change Request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Change Request tidak ditemukan.</exception>
        /// <exception cref="UnauthorizedAccessException">Tidak punya akses.</exception>
        Task EnsureChangeRequestAsync(int changeRequestId, CancellationToken cancellationToken = default);
    }
}
