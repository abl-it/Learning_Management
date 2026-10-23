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
        /// <param name="company">Kode perusahaan: "ABL" atau "NKM".</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="ArgumentException">Company tidak valid atau connection string HRMS belum dikonfigurasi.</exception>
        Task<ComplianceReportDto> GetSummaryAsync(
            string company,
            CancellationToken cancellationToken = default);

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
        /// <param name="company">Kode perusahaan: "ABL" atau "NKM".</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<UnmappedEmployeeDto>> GetUnmappedAsync(
            string company,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pilihan filter Company / Department / Sub Department untuk Employee Matrix.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<MatrixFilterDto> GetFiltersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Daftar karyawan satu perusahaan, opsional dipersempit per Department dan Sub Department.
        /// </summary>
        /// <param name="company">"ABL" atau "NKM".</param>
        /// <param name="departmentAbrv">ABRV Department (opsional).</param>
        /// <param name="subDepartmentId">SubDepartmentID (opsional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<EmployeeMatrixRowDto>> GetEmployeesAsync(
            string company,
            string? departmentAbrv,
            int? subDepartmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Export Excel (satu sheet): Training Matrix per Sub Department menurut matrix Approved,
        /// dipersempit per Company, Department, dan Sub Department.
        /// </summary>
        /// <param name="company">"ABL" atau "NKM".</param>
        /// <param name="departmentAbrv">ABRV Department (opsional).</param>
        /// <param name="subDepartmentId">SubDepartmentID (opsional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<MatrixExportFileDto> ExportMatrixAsync(
            string company,
            string? departmentAbrv,
            int? subDepartmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detail satu karyawan: course menurut matrix beserta statusnya, dan riwayat training aktual.
        /// </summary>
        /// <param name="employeeCode">Employee code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EmployeeMatrixDetailDto> GetEmployeeDetailAsync(
            string employeeCode,
            CancellationToken cancellationToken = default);
    }
}
