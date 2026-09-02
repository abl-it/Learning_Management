using System.ComponentModel.DataAnnotations;

namespace Training.Models.ViewModels;

/// <summary>
/// Represents the input required to create a training event.
/// </summary>
public sealed class TrainingEventCreateViewModel
{
    /// <summary>
    /// Gets or sets the course identifier.
    /// </summary>
    [Required]
    public int? CourseId { get; set; }

    /// <summary>
    /// Gets or sets the training title.
    /// </summary>
    [Required]
    [StringLength(250)]
    public string TrainingTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the company code.
    /// </summary>
    public string? CoCode { get; set; }

    /// <summary>
    /// Gets or sets the department abbreviation.
    /// </summary>
    public string? ABRV { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// I = Internal, E = External.
    /// </summary>
    [Required]
    [StringLength(1)]
    public string EventType { get; set; } = "I";

    /// <summary>
    /// Gets or sets the training category identifier.
    /// </summary>
    public int? TrainingCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the trainer identifier.
    /// </summary>
    public string? TrainerId { get; set; }

    /// <summary>
    /// Gets or sets the trainer name.
    /// </summary>
    [StringLength(200)]
    public string? TrainerName { get; set; }

    /// <summary>
    /// Gets or sets the participant quota.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int ParticipantQuota { get; set; }

    /// <summary>
    /// Gets or sets the event budget.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? Budget { get; set; }

    /// <summary>
    /// Gets or sets the venue.
    /// </summary>
    [StringLength(500)]
    public string? Venue { get; set; }

    /// <summary>
    /// Gets or sets the event start date.
    /// </summary>
    [Required]
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Gets or sets the event end date.
    /// </summary>
    [Required]
    public DateTime EventEndDate { get; set; }

    /// <summary>
    /// Gets or sets the training description.
    /// </summary>
    public string? TrainingDescription { get; set; }

    /// <summary>
    /// Gets or sets the training objective.
    /// </summary>
    public string? TrainingObjective { get; set; }

    /// <summary>
    /// Gets or sets the return URL.
    /// </summary>
    public string? ReturnUrl { get; set; }
}