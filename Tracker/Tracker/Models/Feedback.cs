using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents feedback given to/from team members.
/// Maps to Supabase feedback table.
/// </summary>
public class Feedback
{
    /// <summary>
    /// Unique identifier for this feedback.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this feedback belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who gave the feedback.
    /// </summary>
    [Required]
    public Guid FromTeamMemberId { get; set; }

    /// <summary>
    /// Team member who received the feedback.
    /// </summary>
    [Required]
    public Guid ToTeamMemberId { get; set; }

    /// <summary>
    /// Type of feedback.
    /// </summary>
    [Required]
    public FeedbackType FeedbackType { get; set; } = FeedbackType.General;

    /// <summary>
    /// Sentiment/tone of the feedback.
    /// </summary>
    [Required]
    public FeedbackSentiment Sentiment { get; set; } = FeedbackSentiment.Neutral;

    /// <summary>
    /// Feedback content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Context type (project, meeting, task, general).
    /// </summary>
    [MaxLength(50)]
    public string? ContextType { get; set; }

    /// <summary>
    /// ID of the related entity (if any).
    /// </summary>
    public Guid? ContextId { get; set; }

    /// <summary>
    /// Whether this feedback is private (only visible to giver/receiver).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Whether this feedback was requested.
    /// </summary>
    public bool IsRequested { get; set; }

    /// <summary>
    /// If this was in response to a request, the request ID.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// AI-generated summary of the feedback.
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// AI-generated tags (stored as JSON).
    /// </summary>
    public string? AiTags { get; set; }

    /// <summary>
    /// Whether the recipient has acknowledged this feedback.
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// When the feedback was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this feedback is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this feedback was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the feedback.
    /// </summary>
    public Guid? DeletedBy { get; set; }

    // Sync metadata
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int SyncVersion { get; set; } = 1;
    public DateTime SyncModifiedAt { get; set; } = DateTime.UtcNow;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(FromTeamMemberId))]
    public virtual TeamMember? FromTeamMember { get; set; }

    [ForeignKey(nameof(ToTeamMemberId))]
    public virtual TeamMember? ToTeamMember { get; set; }

    [ForeignKey(nameof(RequestId))]
    public virtual FeedbackRequest? Request { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this is positive feedback.
    /// </summary>
    [NotMapped]
    public bool IsPositive => Sentiment == FeedbackSentiment.Positive || FeedbackType == FeedbackType.Praise;

    /// <summary>
    /// Whether this feedback has AI analysis.
    /// </summary>
    [NotMapped]
    public bool HasAiAnalysis => !string.IsNullOrWhiteSpace(AiSummary);

    /// <summary>
    /// A truncated preview of the content.
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100 ? Content[..97] + "..." : Content;
}
