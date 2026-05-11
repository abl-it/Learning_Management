namespace Training.Models.ViewModels
{
    public class PlanVM
    {
        public List<Department> Departments { get; set; } = new();
        public string SelectedDepartment { get; set; }
        
        public List<Company> Companies { get; set; } = new();
        public string SelectedCompany { get; set; }

        public int SelectedYear { get; set; }
        public string SelectedStatus { get; set; }

        // New property added here
        public List<string> StatusList { get; set; } = new() { "All", "Draft", "Pending", "Approved", "Canceled" };
        // New property added here
        //public List<string> YearList1 { get; set; } = new() { "2026", "2027", "2028", "2029" };
        public List<FiscalYears> YearList { get; set; }
    }

    public class PlanDisp
    {
        public string CoCode { get; set; }
        public string ABRV { get; set; }
        public string DeptName { get; set; }
        public int PlanId { get; set; }
        public string PlanCode { get; set; }
        public int Year { get; set; }
        public string PlanStatus { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        public int PlansNbr { get; set; }

    }

}
