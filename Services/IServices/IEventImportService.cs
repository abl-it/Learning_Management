using Training.Models.DTO.Event.Import;

namespace Training.Services.IServices
{
    /// <summary>
    /// Service untuk import Training Event beserta Participant-nya dari satu file Excel.
    /// </summary>
    public interface IEventImportService
    {
        /// <summary>
        /// Import Training Event + Participant dari file Excel (sheet "Events" dan "Participants").
        /// </summary>
        /// <param name="filePath">Path file Excel yang sudah di-upload (temp file).</param>
        /// <param name="createdBy">Employee code user yang melakukan import.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result dengan detail success/error per EventNo.</returns>
        Task<EventImportResultDto> ImportEventsFromExcelAsync(
            string filePath,
            string createdBy,
            CancellationToken cancellationToken = default);
    }
}
