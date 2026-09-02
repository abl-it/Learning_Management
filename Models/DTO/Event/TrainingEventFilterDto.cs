namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventFilterDto
    {
        public string? SearchTerm { get; set; }

        public string? CoCode { get; set; }

        public string? Abrv { get; set; }

        public string? Status { get; set; }

        public int Start { get; set; }

        public int Length { get; set; }

        public string? OrderColumn { get; set; }

        public string? OrderDirection { get; set; }
    }
}
