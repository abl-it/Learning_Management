namespace Training.Models.DTO.Common
{
    public class DataTableRequestDto
    {
        public int Draw { get; set; }

        public int Start { get; set; }

        public int Length { get; set; }

        public string? SearchValue { get; set; }

        public int OrderColumn { get; set; }

        public string? OrderDirection { get; set; }
    
    }
}
