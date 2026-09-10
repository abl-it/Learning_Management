namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk training hours by department
    /// </summary>
    public class TrainingHoursByDepartmentDto
    {
        /// <summary>
        /// Department code/abbreviation (e.g., "MO", "HR")
        /// </summary>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// Full department name
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Total training hours completed
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// Number of training events
        /// </summary>
        public int EventCount { get; set; }

        /// <summary>
        /// Number of participants
        /// </summary>
        public int ParticipantCount { get; set; }

        /// <summary>
        /// Average hours per event
        /// </summary>
        public decimal AvgHoursPerEvent => EventCount > 0 ? TotalHours / EventCount : 0;
    }
}
