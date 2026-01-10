using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a periodic performance review record.
/// Maps to Supabase performance_reviews table.
/// </summary>
public class PerformanceReview
{
    /// <summary>
    /// Unique identifier for this review.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this review belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member being reviewed.
    /// </summary>
    [Required]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Reviewer (usually manager).
    /// </summary>
    [Required]
    public Guid ReviewerTeamMemberId { get; set; }

    /// <summary>
    /// Start of the review period.
    /// </summary>
    [Required]
    public DateOnly ReviewPeriodStart { get; set; }

    /// <summary>
    /// End of the review period.
    /// </summary>
    [Required]
    public DateOnly ReviewPeriodEnd { get; set; }

    /// <summary>
    /// Type of review.
    /// </summary>
    [Required]
    public PerformanceReviewType ReviewType { get; set; } = PerformanceReviewType.Annual;

    /// <summary>
    /// Status of the review.
    /// </summary>
    [Required]
    public PerformanceReviewStatus Status { get; set; } = PerformanceReviewStatus.Draft;

    /// <summary>
    /// Self-review content (stored as JSON).
    /// </summary>
    public string? SelfReviewContent { get; set; }

    /// <summary>
    /// When self-review was submitted.
    /// </summary>
    public DateTime? SelfReviewSubmittedAt { get; set; }

    /// <summary>
    /// Manager review content (stored as JSON).
    /// </summary>
    public string? ManagerReviewContent { get; set; }

    /// <summary>
    /// When manager review was submitted.
    /// </summary>
    public DateTime? ManagerReviewSubmittedAt { get; set; }

    /// <summary>
    /// Overall rating (1-5 scale).
    /// </summary>
    public int? OverallRating { get; set; }

    /// <summary>
    /// Rating label (Exceeds, Meets, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? RatingLabel { get; set; }

    /// <summary>
    /// Summary of strengths.
    /// </summary>
    public string? Strengths { get; set; }

    /// <summary>
    /// Areas for improvement.
    /// </summary>
    public string? AreasForImprovement { get; set; }

    /// <summary>
    /// Goals for the next period.
    /// </summary>
    public string? GoalsForNextPeriod { get; set; }

    /// <summary>
    /// Whether the employee has acknowledged the review.
    /// </summary>
    public bool EmployeeAcknowledged { get; set; }

    /// <summary>
    /// When the employee acknowledged the review.
    /// </summary>
    public DateTime? EmployeeAcknowledgedAt { get; set; }

    /// <summary>
    /// Employee comments on the review.
    /// </summary>
    public string? EmployeeComments { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this review is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this review was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(TeamMemberId))]
    public virtual TeamMember? TeamMember { get; set; }

    [ForeignKey(nameof(ReviewerTeamMemberId))]
    public virtual TeamMember? Reviewer { get; set; }

    // Computed properties

    /// <summary>
    /// Whether the review is complete.
    /// </summary>
    [NotMapped]
    public bool IsComplete => Status == PerformanceReviewStatus.Complete;

    /// <summary>
    /// Whether self-review has been submitted.
    /// </summary>
    [NotMapped]
    public bool HasSelfReview => SelfReviewSubmittedAt.HasValue;

    /// <summary>
    /// Whether manager review has been submitted.
    /// </summary>
    [NotMapped]
    public bool HasManagerReview => ManagerReviewSubmittedAt.HasValue;

    /// <summary>
    /// Duration of the review period in days.
    /// </summary>
    [NotMapped]
    public int ReviewPeriodDays => ReviewPeriodEnd.DayNumber - ReviewPeriodStart.DayNumber;
}
