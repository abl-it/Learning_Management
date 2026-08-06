namespace Training.Models
{
    public class History
    {
            public int DocId { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
            public string? DocType { get; set; }
            public DateTime Date { get; set; }
            public string? Strategy { get; set; }
            public string? Remark { get; set; }
            public string? State { get; set; }
            public string? NextState { get; set; }
            public string? Action { get; set; }
            public string? ChangeBy { get; set; }
            public string? ChangeByName { get; set; }
        
    }
}
