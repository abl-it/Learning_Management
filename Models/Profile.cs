namespace Training.Models
{
    public class Profile
    {
        public string Username { get; set; }
        public int EmployeeID { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public string? JobTitle { get; set; }
        public string? Dept { get; set; }
        public string? SubDept { get; set; }
        public int DeptId { get; set; }
        public int SubDeptId { get; set; }
    }
}
