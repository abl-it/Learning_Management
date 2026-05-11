namespace Training.Models.DTO
{
    public class PlanDTO
    {
    }
    public class AddCourseRequest
    {
        public int PlanId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Session { get; set; }
        public int Participant { get; set; }
        public decimal Duration { get; set; }
        public string Provider { get; set; }
        public decimal EstCost { get; set; }
        public string TargetParticipant { get; set; }
        public int IsNewCourse { get; set; }
    }

   
}
