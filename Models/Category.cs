using System.Drawing;

namespace Training.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Color { get; set; }
        public int? IsActive { get; set; } = 1;

    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string CategoryCode { get; set; } = "";
        public string? Description { get; set; }
        public int IsActive { get; set; } = 1;

        public string CreatedBy { get; set; }

    }

    //public class ApiResponse
    //{
    //    public bool Success { get; set; }
    //    public string Message { get; set; } = "";
    //    public object? Data { get; set; }
    //}

    public class CategorySequance
    {
        public int CategoryId { get; set; }
        public int? Sequance { get; set; }
    }

}
