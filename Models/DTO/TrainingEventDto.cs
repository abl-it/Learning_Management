namespace Training.DTOs.Training;
public sealed class TrainingEventDto
{
    public long TrainingEventId { get; set; }

    public string EventCode { get; set; } = string.Empty;

    public string TrainingTitle { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public string? CoCode { get; set; }

    public string? Abrv { get; set; }

    public string? DeptName { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? TrainerName { get; set; }

    public int ParticipantQuota { get; set; }

    public decimal? Budget { get; set; }

    public string? Venue { get; set; }

    public DateTime EventStartDate { get; set; }

    public DateTime EventEndDate { get; set; }

    public string? Workflow { get; set; }

    public int? DocStatus { get; set; }
    
    public string? Status { get; set; }

    public int? SessionCount { get; set; }
    
    public decimal? DurationHours { get; set; }

    public string? CategoryName { get; set; }



    public int RegisteredParticipantCount { get; set; }

    public int ActualParticipantCount { get; set; }

    public int AttendedParticipantCount { get; set; }

    public int AdditionalParticipantCount { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public string EventTypeText { get; set; } = string.Empty;

    public string EventDateText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the current user can view the event.
    /// </summary>
    public bool CanShow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can edit the event.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user can perform workflow actions.
    /// </summary>
    public bool CanAction { get; set; }
}