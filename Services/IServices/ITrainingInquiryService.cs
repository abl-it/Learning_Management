using Training.Models.DTO;
using Training.Models.DTO.Report;

namespace Training.Services.IServices
{
    /// <summary>
    /// Provides the "Training Inquiry" report: realized training hours and
    /// attendance, aggregated by department or by person, with company/
    /// department/date-range filtering.
    /// </summary>
    public interface ITrainingInquiryService
    {
        /// <summary>Company options for the report's filter dropdown.</summary>
        Task<IReadOnlyList<SelectOptionDto>> GetCompanyOptionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Department options for the report's filter dropdown, optionally
        /// narrowed to a single company.
        /// </summary>
        Task<IReadOnlyList<SelectOptionDto>> GetDepartmentOptionsAsync(
            string? coCode,
            CancellationToken cancellationToken = default);

        /// <summary>Realized training hours/attendance aggregated per department.</summary>
        Task<IReadOnlyList<TrainingInquiryDepartmentDto>> GetByDepartmentAsync(
            TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken = default);

        /// <summary>Realized training hours/attendance aggregated per person.</summary>
        Task<IReadOnlyList<TrainingInquiryPersonDto>> GetByPersonAsync(
            TrainingInquiryFilterDto filter,
            CancellationToken cancellationToken = default);
    }
}
