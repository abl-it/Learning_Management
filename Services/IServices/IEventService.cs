using Training.DTOs.Training;
using Training.Models;
using Training.Models.DTO;
using Training.Models.DTO.Common;
using Training.Models.DTO.Event;

namespace Training.Services.IServices
{
    public interface IEventService
    {
        Task<List<TrainingEventDto>> GetTrainingEventsAsync();

        //Task<List<TrainingEventDto>> GetTrainingEventsAsync(EventFilterDto filter);

        Task<DataTableResponseDto<TrainingEventDto>> GetTrainingEventsAsync(EventFilterDto filter, string username);

        Task<TrainingEventListResultDto> GetTrainingEventsAsync(TrainingEventFilterDto filter, string username);

        //Create Event

        Task<TrainingEventCreateResultDto> CreateAsync(
            TrainingEventCreateDto request,
            string createdBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Plan (tahun berjalan, approved, masih ada sisa session) yang bisa dipilih saat Create Event.
        /// </summary>
        Task<List<TrainingEventPlanOptionDto>> GetPlanOptionsAsync(
            string coCode,
            string abrv,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses an uploaded participant Excel file (.xlsx, column "EmployeeCode") for the
        /// Create Event page. Employees are resolved from the employee master (active only);
        /// nothing is saved.
        /// </summary>
        /// <param name="excelStream">Stream of the uploaded .xlsx file.</param>
        /// <param name="existingEmployeeCodes">Employee codes already in the participant list; these rows are skipped.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Valid participants and the rows that were skipped.</returns>
        /// <exception cref="ArgumentException">The file is not a usable participant workbook.</exception>
        Task<ParticipantUploadResultDto> ParseParticipantsAsync(
            Stream excelStream,
            IEnumerable<string>? existingEmployeeCodes,
            CancellationToken cancellationToken = default);

        Task<TrainingEventDetailDto?> GetDetailAsync(
            long trainingEventId,
            string username,
            CancellationToken cancellationToken);

        Task<TrainingEventDetailDto?> GetByIdAsync(
            long trainingEventId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a training event.
        /// </summary>
        /// <param name="trainingEventId">Training event identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(
            long trainingEventId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets workflow history for a training event.
        /// </summary>
        /// <param name="trainingEventId">Training event identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Workflow history entries.</returns>
        Task<List<History>> GetHistoryAsync(
            long trainingEventId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing training event and its participants.
        /// </summary>
        /// <param name="request">Training event update request.</param>
        /// <param name="modifiedBy">Employee code of the user modifying the event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated training event identifier.</returns>
        Task<long> UpdateAsync(
            TrainingEventEditDto request,
            string modifiedBy,
            CancellationToken cancellationToken);



    }
}
