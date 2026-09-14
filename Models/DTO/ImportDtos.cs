namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk hasil import data
    /// </summary>
    public class ImportResultDto
    {
        /// <summary>
        /// Jumlah record yang berhasil di-import
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// List error details jika ada
        /// </summary>
        public List<ImportErrorDto> Errors { get; set; } = new List<ImportErrorDto>();

        /// <summary>
        /// Message hasil import
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// DTO untuk detail error per row
    /// </summary>
    public class ImportErrorDto
    {
        /// <summary>
        /// Nomor row di Excel (1-based)
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Field/identifier yang error
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Error message detail
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
