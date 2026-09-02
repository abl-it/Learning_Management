namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventParticipantDto
    {
        public string EmployeeCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ABRV { get; set; }

        public string? DeptName { get; set; }
    
    }

}
