using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a specific review period (e.g., "Q4 2024 Performance Reviews").
/// Maps to Supabase review_cycles table.
/// </summary>
public class ReviewCycle
{
    /// <summary>
    /// Unique identifier for this cycle.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this cycle belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Template used for this cycle.
    /// </summary>
    [Required]
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Cycle name.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cycle description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the review period.
    /// </summary>
    [Required]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of the review period.
    /// </summary>
    [Required]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Due date for self-reviews.
    /// </summary>
    public DateOnly? SelfReviewDue { get; set; }

    /// <summary>
    /// Due date for manager reviews.
    /// </summary>
    public DateOnly? ManagerReviewDue { get; set; }

    /// <summary>
    /// Status of the cycle.
    /// </summary>
    [Required]
    public ReviewCycleStatus Status { get; set; } = ReviewCycleStatus.Draft;

    /// <summary>
    /// Whether to include all employees.
    /// </summary>
    public bool IncludeAllEmployees { get; set; } = true;

    /// <summary>
    /// Team IDs to include (if not all employees).
    /// Stored as JSON array.
    /// </summary>
    public string? TeamIds { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this cycle.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// When the cycle was launched.
    /// </summary>
    public DateTime? LaunchedAt { get; set; }

    /// <summary>
    /// When the cycle was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public virtual ReviewTemplate? Template { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    // Computed properties

    /// <summary>
    /// Whether the cycle is currently active.
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == ReviewCycleStatus.Active;

    /// <summary>
    /// Duration of the review period in days.
    /// </summary>
    [NotMapped]
    public int DurationDays => EndDate.DayNumber - StartDate.DayNumber;

    /// <summary>
    /// Whether self-review is overdue.
    /// </summary>
    [NotMapped]
    public bool IsSelfReviewOverdue => SelfReviewDue.HasValue && 
        SelfReviewDue.Value < DateOnly.FromDateTime(DateTime.UtcNow) && 
        Status == ReviewCycleStatus.Active;
}
