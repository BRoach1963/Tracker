using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents an individual review instance for a team member in a cycle.
/// Maps to Supabase reviews table.
/// </summary>
public class Review
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
    /// Review cycle this review belongs to.
    /// </summary>
    [Required]
    public Guid CycleId { get; set; }

    /// <summary>
    /// Team member being reviewed.
    /// </summary>
    [Required]
    public Guid RevieweeTeamMemberId { get; set; }

    /// <summary>
    /// Team member reviewing (manager).
    /// </summary>
    public Guid? ReviewerTeamMemberId { get; set; }

    /// <summary>
    /// Overall status of the review.
    /// </summary>
    [Required]
    public ReviewStatus Status { get; set; } = ReviewStatus.NotStarted;

    /// <summary>
    /// Status of the self-review portion.
    /// </summary>
    [Required]
    public ReviewStatus SelfReviewStatus { get; set; } = ReviewStatus.NotStarted;

    /// <summary>
    /// When self-review was submitted.
    /// </summary>
    public DateTime? SelfReviewSubmittedAt { get; set; }

    /// <summary>
    /// Status of the manager review portion.
    /// </summary>
    [Required]
    public ReviewStatus ManagerReviewStatus { get; set; } = ReviewStatus.NotStarted;

    /// <summary>
    /// When manager review was submitted.
    /// </summary>
    public DateTime? ManagerReviewSubmittedAt { get; set; }

    /// <summary>
    /// Overall rating (calculated or manual).
    /// </summary>
    public decimal? OverallRating { get; set; }

    /// <summary>
    /// Overall comments.
    /// </summary>
    public string? OverallComments { get; set; }

    /// <summary>
    /// Manager's summary of strengths.
    /// </summary>
    public string? Strengths { get; set; }

    /// <summary>
    /// Manager's summary of areas for improvement.
    /// </summary>
    public string? AreasForImprovement { get; set; }

    /// <summary>
    /// Goals for the next period.
    /// </summary>
    public string? GoalsForNextPeriod { get; set; }

    /// <summary>
    /// When the review was acknowledged by the employee.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Employee's comments on acknowledgment.
    /// </summary>
    public string? AcknowledgmentComments { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(CycleId))]
    public virtual ReviewCycle? Cycle { get; set; }

    [ForeignKey(nameof(RevieweeTeamMemberId))]
    public virtual TeamMember? Reviewee { get; set; }

    [ForeignKey(nameof(ReviewerTeamMemberId))]
    public virtual TeamMember? Reviewer { get; set; }

    public virtual ICollection<ReviewResponse> Responses { get; set; } = new List<ReviewResponse>();

    // Computed properties

    /// <summary>
    /// Whether the review is complete.
    /// </summary>
    [NotMapped]
    public bool IsComplete => Status == ReviewStatus.Completed;

    /// <summary>
    /// Whether the employee has acknowledged the review.
    /// </summary>
    [NotMapped]
    public bool IsAcknowledged => AcknowledgedAt.HasValue;

    /// <summary>
    /// Whether self-review is complete.
    /// </summary>
    [NotMapped]
    public bool IsSelfReviewComplete => SelfReviewStatus == ReviewStatus.Submitted || 
                                         SelfReviewStatus == ReviewStatus.Completed;

    /// <summary>
    /// Whether manager review is complete.
    /// </summary>
    [NotMapped]
    public bool IsManagerReviewComplete => ManagerReviewStatus == ReviewStatus.Submitted || 
                                            ManagerReviewStatus == ReviewStatus.Completed;
}

/// <summary>
/// Represents an answer to a review question.
/// Maps to Supabase review_responses table.
/// </summary>
public class ReviewResponse
{
    /// <summary>
    /// Unique identifier for this response.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Review this response belongs to.
    /// </summary>
    [Required]
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Question being answered.
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Type of responder (self, manager, peer).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ResponderType { get; set; } = "self";

    /// <summary>
    /// Team member who responded.
    /// </summary>
    public Guid? ResponderTeamMemberId { get; set; }

    /// <summary>
    /// Rating value (for rating questions).
    /// </summary>
    public int? RatingValue { get; set; }

    /// <summary>
    /// Text value (for text questions).
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Selected option (for multiple choice).
    /// </summary>
    [MaxLength(200)]
    public string? SelectedOption { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ReviewId))]
    public virtual Review? Review { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public virtual ReviewTemplateQuestion? Question { get; set; }

    [ForeignKey(nameof(ResponderTeamMemberId))]
    public virtual TeamMember? Responder { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this is a self-review response.
    /// </summary>
    [NotMapped]
    public bool IsSelfReview => ResponderType == "self";

    /// <summary>
    /// Whether this is a manager review response.
    /// </summary>
    [NotMapped]
    public bool IsManagerReview => ResponderType == "manager";
}
