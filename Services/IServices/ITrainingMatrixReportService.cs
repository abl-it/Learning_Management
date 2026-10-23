using Training.Models.DTO.TrainingMatrix;

namespace Training.Services.IServices
{
    /// <summary>
    /// Laporan Compliance Training Matrix. Data karyawan dibaca dari HRMS (server berbeda, read-only)
    /// dan digabung di memori dengan matrix Approved dan realisasi training (event Complete, PRESENT).
    /// </summary>
    public interface ITrainingMatrixReportService
    {
        /// <summary>
        /// Ringkasan Compliance seluruh perusahaan dan per Department.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Connection string HRMS belum dikonfigurasi.</exception>
        Task<ComplianceReportDto> GetSummaryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Compliance per karyawan pada satu Department beserta daftar gap.
        /// </summary>
        /// <param name="departmentAbrv">ABRV Department.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<EmployeeComplianceDto>> GetDepartmentEmployeesAsync(
            string departmentAbrv,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Karyawan Unmapped beserta alasannya (Data Quality).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<UnmappedEmployeeDto>> GetUnmappedAsync(CancellationToken cancellationToken = default);
    }
}
