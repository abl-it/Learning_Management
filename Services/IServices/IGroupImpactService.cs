using Training.Models.DTO.TrainingMatrix;

namespace Training.Services.IServices
{
    /// <summary>
    /// Menyebarkan perubahan isi Course Group ke matrix yang pernah memakainya, tanpa link hidup:
    /// matrix Draft diubah langsung, matrix Approved lewat Change Request (Add saja).
    /// </summary>
    public interface IGroupImpactService
    {
        /// <summary>
        /// Matrix yang masih kurang course dari group.
        /// </summary>
        /// <param name="courseGroupId">Identifier Course Group.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Group tidak ditemukan.</exception>
        Task<GroupImpactDto> GetImpactAsync(
            int courseGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Menerapkan penambahan course dari group ke matrix terpilih. Kekurangan dihitung ulang di server
        /// (bukan dari client). Tiap matrix diproses dalam transaksi sendiri; kegagalan satu matrix tidak
        /// membatalkan yang lain.
        /// </summary>
        /// <param name="courseGroupId">Identifier Course Group.</param>
        /// <param name="matrixIds">Matrix yang dipilih.</param>
        /// <param name="reason">Alasan (wajib bila ada matrix Approved yang membutuhkan Change Request baru).</param>
        /// <param name="by">Employee code pelaksana.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GroupImpactApplyResultDto> ApplyAsync(
            int courseGroupId,
            IReadOnlyCollection<int> matrixIds,
            string? reason,
            string by,
            CancellationToken cancellationToken = default);
    }
}
