using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a free-form note that can be linked to various entities.
/// Maps to Supabase notes table.
/// </summary>
public class Note
{
    /// <summary>
    /// Unique identifier for this note.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this note belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Author of the note.
    /// </summary>
    [Required]
    public Guid AuthorTeamMemberId { get; set; }

    /// <summary>
    /// Note title (optional).
    /// </summary>
    [MaxLength(300)]
    public string? Title { get; set; }

    /// <summary>
    /// Note content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content format (plain, markdown, html).
    /// </summary>
    [Required]
    public ContentFormat ContentFormat { get; set; } = ContentFormat.Plain;

    /// <summary>
    /// Linked team member (if about a person).
    /// </summary>
    public Guid? LinkedTeamMemberId { get; set; }

    /// <summary>
    /// Linked meeting (if from a meeting).
    /// </summary>
    public Guid? LinkedMeetingId { get; set; }

    /// <summary>
    /// Linked project.
    /// </summary>
    public Guid? LinkedProjectId { get; set; }

    /// <summary>
    /// Linked goal.
    /// </summary>
    public Guid? LinkedGoalId { get; set; }

    /// <summary>
    /// Linked task.
    /// </summary>
    public Guid? LinkedTaskId { get; set; }

    /// <summary>
    /// Note category.
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Tags (stored as JSON array).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Whether this note is private (only author can see).
    /// </summary>
    public bool IsPrivate { get; set; } = true;

    /// <summary>
    /// Whether this note is pinned/favorite.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// When the note was pinned.
    /// </summary>
    public DateTime? PinnedAt { get; set; }

    /// <summary>
    /// AI-generated summary.
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// AI-suggested actions (stored as JSON).
    /// </summary>
    public string? AiSuggestedActions { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this note is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this note was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the note.
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

    [ForeignKey(nameof(AuthorTeamMemberId))]
    public virtual TeamMember? Author { get; set; }

    [ForeignKey(nameof(LinkedTeamMemberId))]
    public virtual TeamMember? LinkedTeamMember { get; set; }

    [ForeignKey(nameof(LinkedMeetingId))]
    public virtual Meeting? LinkedMeeting { get; set; }

    [ForeignKey(nameof(LinkedProjectId))]
    public virtual Project? LinkedProject { get; set; }

    [ForeignKey(nameof(LinkedGoalId))]
    public virtual Goal? LinkedGoal { get; set; }

    [ForeignKey(nameof(LinkedTaskId))]
    public virtual TrackerTask? LinkedTask { get; set; }

    // Computed properties

    /// <summary>
    /// A truncated preview of the content.
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 200 ? Content[..197] + "..." : Content;

    /// <summary>
    /// Whether this note has any links.
    /// </summary>
    [NotMapped]
    public bool HasLinks => LinkedTeamMemberId.HasValue || LinkedMeetingId.HasValue ||
                            LinkedProjectId.HasValue || LinkedGoalId.HasValue || LinkedTaskId.HasValue;

    /// <summary>
    /// Whether this note has AI analysis.
    /// </summary>
    [NotMapped]
    public bool HasAiAnalysis => !string.IsNullOrWhiteSpace(AiSummary);
}
