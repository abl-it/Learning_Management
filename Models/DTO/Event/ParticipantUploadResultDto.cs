namespace Training.Models.DTO.Event
{
    /// <summary>
    /// Result of parsing a participant Excel file on the Create Event page.
    /// Nothing is saved to the database - the caller adds <see cref="Participants"/>
    /// to the participant list in the form.
    /// </summary>
    public class ParticipantUploadResultDto
    {
        /// <summary>Number of non-empty data rows found in the file.</summary>
        public int TotalRows { get; set; }

        /// <summary>Valid, active employees (in file order) that can be added to the form.</summary>
        public List<ParticipantUploadRowDto> Participants { get; set; } = new();

        /// <summary>Rows that were skipped, with the reason.</summary>
        public List<ParticipantUploadErrorDto> Errors { get; set; } = new();
    }

    /// <summary>
    /// A participant resolved from <c>home.dbo.APP_Employees</c> / <c>APP_Departments</c>.
    /// </summary>
    public class ParticipantUploadRowDto
    {
        /// <summary>Employee code.</summary>
        public string EmployeeCode { get; set; } = string.Empty;

        /// <summary>Employee full name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Department abbreviation.</summary>
        public string? ABRV { get; set; }

        /// <summary>Department name.</summary>
        public string? DeptName { get; set; }
    }

    /// <summary>
    /// A skipped row in the participant Excel file.
    /// </summary>
    public class ParticipantUploadErrorDto
    {
        /// <summary>Excel row number (1-based, header is row 1).</summary>
        public int RowNumber { get; set; }

        /// <summary>Employee code as written in the file, if any.</summary>
        public string? EmployeeCode { get; set; }

        /// <summary>Why the row was skipped.</summary>
        public string Message { get; set; } = string.Empty;
    }
}
