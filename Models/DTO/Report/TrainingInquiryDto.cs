namespace Training.Models.DTO.Report
{
    /// <summary>Filter criteria for the Training Inquiry report.</summary>
    public sealed class TrainingInquiryFilterDto
    {
        /// <summary>Company code (home.dbo.APP_Departments.CoCode). Null/empty = all companies.</summary>
        public string? CoCode { get; set; }

        /// <summary>Department abbreviation. Null/empty = all departments.</summary>
        public string? ABRV { get; set; }

        /// <summary>Inclusive start of the realization date range.</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Inclusive end of the realization date range.</summary>
        public DateTime? EndDate { get; set; }
    }

    /// <summary>One row of the "Training per Department" view.</summary>
    public sealed class TrainingInquiryDepartmentDto
    {
        public string? CoCode { get; set; }

        public string? ABRV { get; set; }

        public string? DeptName { get; set; }

        public int TotalEvents { get; set; }

        public int TotalParticipants { get; set; }

        /// <summary>Total person-hours of realized training delivered to this department.</summary>
        public decimal TotalHours { get; set; }
    }

    /// <summary>One row of the "Training per Person" view.</summary>
    public sealed class TrainingInquiryPersonDto
    {
        public string EmployeeCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ABRV { get; set; }

        public string? DeptName { get; set; }

        /// <summary>"Participant" or "Instructor" - a person can appear as both, in separate rows.</summary>
        public string Role { get; set; } = "Participant";

        public int TotalTrainings { get; set; }

        /// <summary>Total hours of realized training this person actually attended or delivered.</summary>
        public decimal TotalHours { get; set; }
    }
}
