using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a request for feedback from another team member.
/// Maps to Supabase feedback_requests table.
/// </summary>
public class FeedbackRequest
{
    /// <summary>
    /// Unique identifier for this request.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this request belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who made the request.
    /// </summary>
    [Required]
    public Guid RequesterTeamMemberId { get; set; }

    /// <summary>
    /// Team member who is being asked for feedback.
    /// </summary>
    [Required]
    public Guid RequestedFromTeamMemberId { get; set; }

    /// <summary>
    /// Team member the feedback is about (could be self or another person).
    /// </summary>
    [Required]
    public Guid AboutTeamMemberId { get; set; }

    /// <summary>
    /// Optional message explaining what feedback is wanted.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Context type (project, skill, general).
    /// </summary>
    [MaxLength(50)]
    public string? ContextType { get; set; }

    /// <summary>
    /// ID of the related context entity.
    /// </summary>
    public Guid? ContextId { get; set; }

    /// <summary>
    /// Due date for the feedback.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Status of the request.
    /// </summary>
    [Required]
    public FeedbackRequestStatus Status { get; set; } = FeedbackRequestStatus.Pending;

    /// <summary>
    /// When the request was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the request was declined.
    /// </summary>
    public DateTime? DeclinedAt { get; set; }

    /// <summary>
    /// Reason for declining (if declined).
    /// </summary>
    public string? DeclineReason { get; set; }

    /// <summary>
    /// ID of the feedback given in response to this request.
    /// </summary>
    public Guid? ResponseFeedbackId { get; set; }

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

    [ForeignKey(nameof(RequesterTeamMemberId))]
    public virtual TeamMember? Requester { get; set; }

    [ForeignKey(nameof(RequestedFromTeamMemberId))]
    public virtual TeamMember? RequestedFrom { get; set; }

    [ForeignKey(nameof(AboutTeamMemberId))]
    public virtual TeamMember? About { get; set; }

    [ForeignKey(nameof(ResponseFeedbackId))]
    public virtual Feedback? ResponseFeedback { get; set; }

    // Computed properties

    /// <summary>
    /// Whether the request is still pending.
    /// </summary>
    [NotMapped]
    public bool IsPending => Status == FeedbackRequestStatus.Pending;

    /// <summary>
    /// Whether the request is overdue.
    /// </summary>
    [NotMapped]
    public bool IsOverdue => IsPending && DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Days remaining until due date (negative if overdue).
    /// </summary>
    [NotMapped]
    public int? DaysRemaining => DueDate.HasValue
        ? (DueDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days
        : null;

    /// <summary>
    /// Whether this is a self-feedback request.
    /// </summary>
    [NotMapped]
    public bool IsSelfFeedback => RequesterTeamMemberId == AboutTeamMemberId;
}
