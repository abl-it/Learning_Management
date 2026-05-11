namespace Training.Models.ViewModels
{
    public class UnitVM
    {
        public List<Group> Groups { get; set; } = new();
        public string SelectedGroup { get; set; }

        public List<Company> Companies { get; set; } = new();
        public string SelectedCompany { get; set; }
    }
}
