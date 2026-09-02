using Training.Models.DTO.Common;

namespace Training.Models.DTO
{
    public class EventFilterDto
    {
        public DataTableRequestDto DataTable { get; set; } = new();

        public string? CoCode { get; set; }

        public string? Abrv { get; set; }

        public string? Status { get; set; }

    }
}
