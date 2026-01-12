using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels;

/// <summary>
/// Represents notes from a meeting.
/// Maps to Supabase meeting_notes table.
/// </summary>
public class MeetingNote
{
    /// <summary>
    /// Unique identifier for this note.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The meeting this note belongs to.
    /// </summary>
    [Required]
    public Guid MeetingId { get; set; }

    /// <summary>
    /// Team member who wrote this note.
    /// </summary>
    public Guid? AuthorTeamMemberId { get; set; }

    /// <summary>
    /// Content of the note.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether this note is private (only visible to author).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// AI-generated summary of the note.
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Sync metadata
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int SyncVersion { get; set; } = 1;
    public DateTime SyncModifiedAt { get; set; } = DateTime.UtcNow;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;

    // Navigation properties
    [ForeignKey(nameof(MeetingId))]
    public virtual Meeting? Meeting { get; set; }

    [ForeignKey(nameof(AuthorTeamMemberId))]
    public virtual TeamMember? Author { get; set; }

    // Computed properties

    /// <summary>
    /// Whether this note has an AI summary.
    /// </summary>
    [NotMapped]
    public bool HasAiSummary => !string.IsNullOrWhiteSpace(AiSummary);

    /// <summary>
    /// A truncated preview of the content.
    /// </summary>
    [NotMapped]
    public string ContentPreview => Content.Length > 100 ? Content[..97] + "..." : Content;
}
