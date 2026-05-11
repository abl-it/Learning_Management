using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection.Emit;
using static Azure.Core.HttpHeader;

namespace Training.Models
{
    public class Plans
    {
        public string CoCode { get; set; }
        public string  ABRV { get; set; }
        public string DeptName { get; set; }
        public int PlanId { get; set; }
        public string PlanCode { get; set; }
        public int Year { get; set; }
        public string PlanStatus { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }


        public int PlansNbr { get; set; }
        public int DocStatus { get; set; }
        public string Workflow { get; set; }

    }
}
