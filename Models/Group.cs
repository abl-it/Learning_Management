namespace Training.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        public string GroupCode { get; set; }
        public string Description { get; set; }
        public string CoCode { get; set; }
        public int IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

    }
}
