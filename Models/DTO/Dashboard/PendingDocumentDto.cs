namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk pending documents (EntryRealization dengan CanAction = 1)
    /// </summary>
    public class PendingDocumentDto
    {
        /// <summary>
        /// Document ID
        /// </summary>
        public int DocumentId { get; set; }

        /// <summary>
        /// Document type (e.g., "Entry Realization", "Training Request", etc)
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Document number/reference
        /// </summary>
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Department that submitted
        /// </summary>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// When document was submitted
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Days pending
        /// </summary>
        public int DaysPending => (int)(DateTime.Now - CreatedDate).TotalDays;
    }
}
