using Training.DTOs.Training;

namespace Training.Models.DTO.Event
{
    public sealed class TrainingEventListResultDto
    {
        public long TotalRecords { get; set; }

        public long FilteredRecords { get; set; }

        public List<TrainingEventDto> Data { get; set; } = new();
    }
}
