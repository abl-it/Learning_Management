using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;

namespace Training.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public decimal DurationHours { get; set; }
        public int? ValidityMonths { get; set; }
        public string? TrainingProvider { get; set; }
        public decimal? Cost { get; set; }
        public int? MaxParticipants { get; set; }
        public string? PrerequisiteCourseId { get; set; }
        public int? IsActive { get; set; } = 1;
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public string Color { get; set; }

        public string? CategoryName { get; set; }
    }

    public class CourseDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; } = "";
        public decimal DurationHours { get; set; }
        public int? ValidityMonths { get; set; }
        public string? TrainingProvider { get; set; }
        public decimal? Cost { get; set; }
        public int? MaxParticipants { get; set; }
        public string? PrerequisiteCourseId { get; set; }
        public int IsActive { get; set; } = 1;
        public string CreatedBy { get; set; } = "";
    }

}
