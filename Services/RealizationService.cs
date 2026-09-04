using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using Training.Models.DTO.Event.Realization;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Implements <see cref="IRealizationService"/> using Dapper against
    /// stored procedures, plus local disk storage for attachment files.
    /// </summary>
    public class RealizationService : IRealizationService
    {
        private const long MaxAttachmentSizeBytes = 20 * 1024 * 1024; // 20 MB

        private static readonly HashSet<string> AllowedAttachmentExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"
            };

        private readonly string _connectionString;
        private readonly string _storageRootPath;
        private readonly ILogger<RealizationService> _logger;

        public RealizationService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<RealizationService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;

            var configuredPath = configuration["FileStorage:RealizationAttachmentsPath"];

            _storageRootPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "Uploads", "RealizationAttachments")
                : Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(environment.ContentRootPath, configuredPath);

            Directory.CreateDirectory(_storageRootPath);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TrainingEventRealizationListItemDto>> GetListAsync(
            string role,
            string employeeCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                throw new ArgumentException(
                    "Current user could not be determined.",
                    nameof(employeeCode));
            }

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventRealization_GetList",
                new
                {
                    Role = role ?? string.Empty,
                    EmployeeCode = employeeCode
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            var result = await connection.QueryAsync<TrainingEventRealizationListItemDto>(command);

            return result.AsList();
        }

        /// <inheritdoc />
        public async Task<TrainingEventRealizationDetailDto?> GetDetailAsync(
            long trainingEventId,
            CancellationToken cancellationToken = default)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event identifier.",
                    nameof(trainingEventId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventRealization_GetDetail",
                new { TrainingEventId = trainingEventId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            using var multi = await connection.QueryMultipleAsync(command);

            var detail = await multi.ReadFirstOrDefaultAsync<TrainingEventRealizationDetailDto>();

            if (detail is null)
            {
                return null;
            }

            detail.Participants =
                (await multi.ReadAsync<TrainingEventRealizationParticipantDto>()).ToList();

            detail.Attachments =
                (await multi.ReadAsync<TrainingEventAttachmentDto>()).ToList();

            detail.CanEdit =
                (string.Equals(detail.EventStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(detail.EventStatus, "Realization Rejected", StringComparison.OrdinalIgnoreCase))
                && !detail.IsPosted;

            return detail;
        }

        /// <inheritdoc />
        public Task SaveDraftAsync(
            TrainingEventRealizationSaveDto request,
            string modifiedBy,
            CancellationToken cancellationToken = default)
            => SaveInternalAsync(request, modifiedBy, post: false, cancellationToken);

        /// <inheritdoc />
        public Task PostToHrdAsync(
            TrainingEventRealizationSaveDto request,
            string postedBy,
            CancellationToken cancellationToken = default)
            => SaveInternalAsync(request, postedBy, post: true, cancellationToken);

        private async Task SaveInternalAsync(
            TrainingEventRealizationSaveDto request,
            string by,
            bool post,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.TrainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event identifier.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(by))
            {
                throw new ArgumentException(
                    "Current user could not be determined.",
                    nameof(by));
            }

            var attendanceTable = BuildAttendanceTable(request.Participants);

            var parameters = new DynamicParameters();

            parameters.Add("@TrainingEventId", request.TrainingEventId, DbType.Int64);
            parameters.Add("@RealizationDate", request.RealizationDate.Date, DbType.Date);
            parameters.Add("@StartTime", request.StartTime, DbType.Time);
            parameters.Add("@EndTime", request.EndTime, DbType.Time);
            parameters.Add("@Notes", NullIfEmpty(request.Notes), DbType.String);
            parameters.Add("@By", by, DbType.String);
            parameters.Add("@Post", post, DbType.Boolean);
            parameters.Add("@Remarks", NullIfEmpty(request.Remarks), DbType.String);

            parameters.Add(
                "@Participants",
                attendanceTable.AsTableValuedParameter("dbo.TrainingEventAttendanceType"));

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventRealization_Save",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            try
            {
                await connection.ExecuteAsync(command);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save realization for TrainingEventId {TrainingEventId} (post={Post}).",
                    request.TrainingEventId,
                    post);

                throw;
            }
        }

        private static DataTable BuildAttendanceTable(
            IEnumerable<TrainingEventRealizationParticipantSaveDto> participants)
        {
            var table = new DataTable();

            table.Columns.Add("EmployeeCode", typeof(string));
            table.Columns.Add("PlannedParticipantId", typeof(long));
            table.Columns.Add("AttendanceStatus", typeof(string));
            table.Columns.Add("Remarks", typeof(string));

            foreach (var participant in participants)
            {
                if (string.IsNullOrWhiteSpace(participant.EmployeeCode))
                {
                    throw new ArgumentException("Participant employee code is required.");
                }

                table.Rows.Add(
                    participant.EmployeeCode.Trim(),
                    participant.PlannedParticipantId.HasValue
                        ? participant.PlannedParticipantId.Value
                        : (object)DBNull.Value,
                    NormalizeAttendanceStatus(participant.AttendanceStatus),
                    (object?)participant.Remarks?.Trim() ?? DBNull.Value);
            }

            return table;
        }

        private static string NormalizeAttendanceStatus(string? status)
        {
            var normalized = status?.Trim().ToUpperInvariant();

            return normalized is "PRESENT" or "PARTIAL" or "ABSENT"
                ? normalized
                : "ABSENT";
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <inheritdoc />
        public async Task RejectAsync(
            long trainingEventId,
            string rejectedBy,
            string remarks,
            CancellationToken cancellationToken = default)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event identifier.",
                    nameof(trainingEventId));
            }

            if (string.IsNullOrWhiteSpace(rejectedBy))
            {
                throw new ArgumentException(
                    "Current user could not be determined.",
                    nameof(rejectedBy));
            }

            if (string.IsNullOrWhiteSpace(remarks))
            {
                throw new ArgumentException(
                    "A reason is required when rejecting a realization.",
                    nameof(remarks));
            }

            var parameters = new DynamicParameters();

            parameters.Add("@TrainingEventId", trainingEventId, DbType.Int64);
            parameters.Add("@By", rejectedBy, DbType.String);
            parameters.Add("@Remarks", remarks.Trim(), DbType.String);

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventRealization_Reject",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            try
            {
                await connection.ExecuteAsync(command);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reject realization for TrainingEventId {TrainingEventId}.",
                    trainingEventId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task CompleteAsync(
            long trainingEventId,
            string completedBy,
            string? remarks,
            CancellationToken cancellationToken = default)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event identifier.",
                    nameof(trainingEventId));
            }

            if (string.IsNullOrWhiteSpace(completedBy))
            {
                throw new ArgumentException(
                    "Current user could not be determined.",
                    nameof(completedBy));
            }

            var parameters = new DynamicParameters();

            parameters.Add("@TrainingEventId", trainingEventId, DbType.Int64);
            parameters.Add("@By", completedBy, DbType.String);
            parameters.Add("@Remarks", NullIfEmpty(remarks), DbType.String);

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventRealization_Complete",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            try
            {
                await connection.ExecuteAsync(command);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to complete realization for TrainingEventId {TrainingEventId}.",
                    trainingEventId);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TrainingEventAttachmentDto> UploadAttachmentAsync(
            long trainingEventId,
            Stream fileStream,
            string originalFileName,
            string? contentType,
            long fileSize,
            string documentType,
            string uploadedBy,
            CancellationToken cancellationToken = default)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event identifier.",
                    nameof(trainingEventId));
            }

            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(originalFileName));
            }

            var extension = Path.GetExtension(originalFileName);

            if (string.IsNullOrWhiteSpace(extension)
                || !AllowedAttachmentExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    $"File type '{extension}' is not allowed. Allowed types: " +
                    string.Join(", ", AllowedAttachmentExtensions) + ".");
            }

            if (fileSize <= 0 || fileSize > MaxAttachmentSizeBytes)
            {
                throw new InvalidOperationException(
                    $"File size must be between 1 byte and {MaxAttachmentSizeBytes / 1024 / 1024} MB.");
            }

            var eventFolder = Path.Combine(_storageRootPath, trainingEventId.ToString());
            Directory.CreateDirectory(eventFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var relativePath = Path.Combine(trainingEventId.ToString(), storedFileName);
            var fullPath = Path.Combine(_storageRootPath, relativePath);

            await using (var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
            {
                await fileStream.CopyToAsync(destination, cancellationToken);
            }

            await using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();

            parameters.Add("@TrainingEventId", trainingEventId, DbType.Int64);
            parameters.Add("@FileName", storedFileName, DbType.String);
            parameters.Add("@OriginalFileName", originalFileName, DbType.String);
            parameters.Add("@ContentType", contentType, DbType.String);
            parameters.Add("@FileSize", fileSize, DbType.Int64);
            parameters.Add("@StoragePath", relativePath, DbType.String);
            parameters.Add("@DocumentType", documentType, DbType.String);
            parameters.Add("@CreatedBy", uploadedBy, DbType.String);

            var command = new CommandDefinition(
                "dbo.USP_TrainingEventAttachment_Insert",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            long newId;

            try
            {
                newId = await connection.ExecuteScalarAsync<long>(command);
            }
            catch (SqlException ex)
            {
                // Don't leave an orphaned file on disk if the database insert failed.
                TryDeleteFile(fullPath);

                _logger.LogError(
                    ex,
                    "Failed to record attachment for TrainingEventId {TrainingEventId}.",
                    trainingEventId);

                throw;
            }

            return new TrainingEventAttachmentDto
            {
                TrainingEventAttachmentId = newId,
                FileName = storedFileName,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                FileSize = fileSize,
                DocumentType = documentType,
                CreatedBy = uploadedBy,
                CreatedDate = DateTime.Now
            };
        }

        /// <inheritdoc />
        public async Task DeleteAttachmentAsync(
            long attachmentId,
            CancellationToken cancellationToken = default)
        {
            if (attachmentId <= 0)
            {
                throw new ArgumentException(
                    "Invalid attachment identifier.",
                    nameof(attachmentId));
            }

            await using var connection = new SqlConnection(_connectionString);

            var attachment = await connection.QueryFirstOrDefaultAsync<AttachmentFileRecord>(
                new CommandDefinition(
                    "dbo.USP_TrainingEventAttachment_GetById",
                    new { TrainingEventAttachmentId = attachmentId },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (attachment is null)
            {
                return;
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.USP_TrainingEventAttachment_Delete",
                    new { TrainingEventAttachmentId = attachmentId },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (!string.IsNullOrWhiteSpace(attachment.StoragePath))
            {
                TryDeleteFile(Path.Combine(_storageRootPath, attachment.StoragePath));
            }
        }

        /// <inheritdoc />
        public async Task<long?> GetAttachmentTrainingEventIdAsync(
            long attachmentId,
            CancellationToken cancellationToken = default)
        {
            if (attachmentId <= 0)
            {
                return null;
            }

            await using var connection = new SqlConnection(_connectionString);

            var attachment = await connection.QueryFirstOrDefaultAsync<AttachmentFileRecord>(
                new CommandDefinition(
                    "dbo.USP_TrainingEventAttachment_GetById",
                    new { TrainingEventAttachmentId = attachmentId },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return attachment?.TrainingEventId;
        }

        /// <inheritdoc />
        public async Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
            long attachmentId,
            CancellationToken cancellationToken = default)
        {
            if (attachmentId <= 0)
            {
                return null;
            }

            await using var connection = new SqlConnection(_connectionString);

            var attachment = await connection.QueryFirstOrDefaultAsync<AttachmentFileRecord>(
                new CommandDefinition(
                    "dbo.USP_TrainingEventAttachment_GetById",
                    new { TrainingEventAttachmentId = attachmentId },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (attachment is null || string.IsNullOrWhiteSpace(attachment.StoragePath))
            {
                return null;
            }

            var fullPath = Path.Combine(_storageRootPath, attachment.StoragePath);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return (stream, attachment.ContentType ?? "application/octet-stream", attachment.OriginalFileName);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup; a stray file on disk isn't worth failing the request for.
            }
        }

        /// <summary>Minimal projection used internally for delete/download lookups.</summary>
        private sealed class AttachmentFileRecord
        {
            public long TrainingEventId { get; set; }

            public string? StoragePath { get; set; }

            public string? ContentType { get; set; }

            public string OriginalFileName { get; set; } = string.Empty;
        }
    }
}
