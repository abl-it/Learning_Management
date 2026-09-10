namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk attendance rate metrics
    /// </summary>
    public class AttendanceRateDto
    {
        /// <summary>
        /// Total participants yang attend (dari TrainingEventActualParticipant WHERE AttendanceStatus = 'Present')
        /// </summary>
        public int AttendeeCount { get; set; }

        /// <summary>
        /// Total participants yang planned + walk-in
        /// </summary>
        public int TotalParticipants { get; set; }

        /// <summary>
        /// Calculated attendance rate percentage
        /// </summary>
        public decimal AttendanceRate => TotalParticipants > 0 ? (decimal)AttendeeCount / TotalParticipants * 100 : 0;

        /// <summary>
        /// Absent count
        /// </summary>
        public int AbsentCount => TotalParticipants - AttendeeCount;
    }
}
