using Training.Models.DTO.Event.Realization;

namespace Training.Services.IServices
{
    /// <summary>
    /// Handles training event realization: entering the actual realization
    /// date/time, recording attendance (registered participants plus
    /// walk-ins), managing supporting attachments, and posting the
    /// finished realization to HRD.
    /// </summary>
    public interface IRealizationService
    {
        /// <summary>
        /// Gets the "Event Realization" landing page list - events the
        /// current user can act on or review, scoped by their role. The
        /// "tc" role sees every event awaiting their review; everyone else
        /// sees their own events somewhere in the realization lifecycle.
        /// </summary>
        Task<IReadOnlyList<TrainingEventRealizationListItemDto>> GetListAsync(
            string role,
            string employeeCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the realization page data for a training event: header,
        /// attendance list, and attachments. Returns <see langword="null"/>
        /// only when the training event itself doesn't exist.
        /// </summary>
        Task<TrainingEventRealizationDetailDto?> GetDetailAsync(
            long trainingEventId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves realization data as a draft. Can be called repeatedly while
        /// the parent event is Approved and the realization hasn't been
        /// posted to HRD yet.
        /// </summary>
        Task SaveDraftAsync(
            TrainingEventRealizationSaveDto request,
            string modifiedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves realization data and, in the same transaction, advances the
        /// parent training event's workflow to "Realization Submitted" and
        /// locks the realization from further edits.
        /// </summary>
        Task PostToHrdAsync(
            TrainingEventRealizationSaveDto request,
            string postedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The "tc" (HRD) confirmation step: advances a posted realization's
        /// parent event from "Realization Submitted" to "Complete".
        /// </summary>
        Task CompleteAsync(
            long trainingEventId,
            string completedBy,
            string? remarks,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The "tc" (HRD) rejection step: sends a submitted realization back
        /// to the creator (event moves to "Realization Rejected") and
        /// unlocks the form for editing again. A reason is required.
        /// </summary>
        Task RejectAsync(
            long trainingEventId,
            string rejectedBy,
            string remarks,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an uploaded file on disk and records it against the
        /// training event.
        /// </summary>
        /// <param name="trainingEventId">Owning training event.</param>
        /// <param name="fileStream">Readable stream of the uploaded file's content.</param>
        /// <param name="originalFileName">File name as uploaded by the user.</param>
        /// <param name="contentType">MIME type reported by the browser.</param>
        /// <param name="fileSize">Size, in bytes, of the uploaded file.</param>
        /// <param name="documentType">Logical category, e.g. "Attendance" or "Other".</param>
        /// <param name="uploadedBy">Employee code of the uploader.</param>
        Task<TrainingEventAttachmentDto> UploadAttachmentAsync(
            long trainingEventId,
            Stream fileStream,
            string originalFileName,
            string? contentType,
            long fileSize,
            string documentType,
            string uploadedBy,
            CancellationToken cancellationToken = default);

        /// <summary>Permanently deletes an attachment's database record and physical file.</summary>
        Task DeleteAttachmentAsync(
            long attachmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks up which training event an attachment belongs to, so the
        /// caller can check the user is allowed to view that event before
        /// opening the file.
        /// </summary>
        Task<long?> GetAttachmentTrainingEventIdAsync(
            long attachmentId,
            CancellationToken cancellationToken = default);

        /// <summary>Opens an attachment's content for download.</summary>
        /// <returns>
        /// The file stream, content type, and original file name, or
        /// <see langword="null"/> when the attachment (or its physical file)
        /// no longer exists.
        /// </returns>
        Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
            long attachmentId,
            CancellationToken cancellationToken = default);
    }
}
