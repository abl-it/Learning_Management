namespace Training.Models.DataTables
{
    public class DataTableContracts
    {

    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? SearchValue { get; set; }
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableOrder> Order { get; set; } = new();

        // Custom filters
        public string? CompanyFilter { get; set; }
        public string? DepartmentFilter { get; set; }
        public DateTime? HireDateFrom { get; set; }
        public DateTime? HireDateTo { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }

        //for Course
        public string? CategoryFilter { get; set; }
        public string? StatusFilter { get; set; }
        public int CategoryId { get; set; }

        //for needs
        public string? YearFilter { get; set; }
        public string? UnitFilter { get; set; }
        public string? GroupFilter { get; set; }

        //for plansDetail
        

    }
    public class DtPlansDetailRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? SearchValue { get; set; }
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableToggleOrder> Order { get; set; } = new();

        // Custom filters
        public string? PlanId { get; set; }
        //for plansDetail


    }
    public class DataTableRequestMaster
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? SearchValue { get; set; }
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableOrder> Order { get; set; } = new();

        // Custom filters
        public string? DepartmentFilter { get; set; }
        public string? CategoryFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? GroupFilter { get; set; }
        public string? CompanyFilter { get; set; }
        
    }

    public class DataTableColumn
    {
        public string Data { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch? Search { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "desc";
    }
    public class DataTableToggleOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = new();
    }

}
