namespace Training.Models.DTO
{
    public class NeedDTO
    {
        public int Id { get; set; }
        public string CoCode { get; set; }
        public string NeedId { get; set; }
        public int Year { get; set; }
        public int GroupId { get; set; }
        public int UnitId { get; set; }
        public string CourseId { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        public int IsActive { get; set; }
    }

    public class SaveTrainingNeedsRequest
    {
        public List<NeedDTO> TrainingNeeds { get; set; }
    }
}
