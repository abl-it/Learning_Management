namespace Training.Models.DTO.Event.Realization
{
    /// <summary>
    /// A file attached to a training event (e.g. scanned attendance sheet,
    /// supporting documents) via the Realization page.
    /// </summary>
    public sealed class TrainingEventAttachmentDto
    {
        public long TrainingEventAttachmentId { get; set; }

        /// <summary>Randomly generated name the file is stored under on disk.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>The name the file had when the user uploaded it.</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public long? FileSize { get; set; }

        /// <summary>E.g. "Attendance" (scanned daftar hadir) or "Other".</summary>
        public string? DocumentType { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
    }
}
