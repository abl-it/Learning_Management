namespace Training.Models
{
    public class Employees
    {
        public string CoCode { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }

        public int DepartmentID { get; set; }
        public DateTime JoinDate { get; set; }
        public string JobTitle { get; set; }
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
        public int SubDepartmentID { get; set; }
        public string SubDepartmentCode { get; set; }
        public string SubDepartmentName { get; set; }
        public int WorkGroupID { get; set; }
        public string WorkGroupName { get; set; }
        public DateTime? ResignDate { get; set; }

        public DateTime BirthDate { get; set; }

        public string Username { get; set; }
        public string Roles { get; set; }
        public string Role { get; set; }

        public string ABRV { get; set; }
        public string  DeptName { get; set; }

    }
}
