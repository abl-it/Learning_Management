using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;

namespace Training.Models
{
    public class Unit
    {
        public  int UnitId { get; set; }
        public string? UnitCode { get; set; }
        public string? UnitName { get; set; }
        public int? GroupId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string GroupCode { get; set; }
        public string GroupName { get; set; }

        public string CoCode { get; set; }

    }
}
