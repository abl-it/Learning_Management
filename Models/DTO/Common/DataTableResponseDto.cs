namespace Training.Models.DTO.Common
{
    public sealed class DataTableResponseDto<T>
    {
        public int Draw { get; set; }

        public int RecordsTotal { get; set; }

        public int RecordsFiltered { get; set; }

        public List<T> Data { get; set; } = [];
    
    }
}
