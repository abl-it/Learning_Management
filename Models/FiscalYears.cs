using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Training.Models
{
    public class FiscalYears
    {
        public int FiscalYearID { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
    }
}
