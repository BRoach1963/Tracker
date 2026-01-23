using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a feedback item - maps to the procohere.feedback table.
/// </summary>
[Table("feedback")]
public class FeedbackDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// The team member who gave the feedback.
    /// </summary>
    [Column("from_member_id")]
    public Guid FromMemberId { get; set; }
    
    /// <summary>
    /// The team member who received the feedback.
    /// </summary>
    [Column("to_member_id")]
    public Guid TeamMemberId { get; set; }
    
    /// <summary>
    /// Type of feedback (praise, constructive, coaching, etc.)
    /// </summary>
    [Column("feedback_type")]
    public string FeedbackType { get; set; } = "general";
    
    /// <summary>
    /// Optional title for the feedback.
    /// </summary>
    [Column("title")]
    public string? Title { get; set; }
    
    /// <summary>
    /// The feedback content.
    /// </summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Visibility (private, shared, etc.)
    /// </summary>
    [Column("visibility")]
    public string Visibility { get; set; } = "private";
    
    /// <summary>
    /// Whether feedback is anonymous.
    /// </summary>
    [Column("is_anonymous")]
    public bool IsAnonymous { get; set; }
    
    /// <summary>
    /// Optional rating (1-5).
    /// </summary>
    [Column("rating")]
    public int? Rating { get; set; }
    
    /// <summary>
    /// Optional meeting this feedback is associated with.
    /// </summary>
    [Column("meeting_id")]
    public Guid? MeetingId { get; set; }
    
    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// When the feedback was given.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
    
    #region Non-DB Properties (set by service)
    
    /// <summary>
    /// Name of the recipient (set by service join).
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;
    
    /// <summary>
    /// Avatar URL of the recipient (set by service join).
    /// </summary>
    public string? RecipientAvatarUrl { get; set; }
    
    /// <summary>
    /// Name of the sender (set by service join).
    /// </summary>
    public string SenderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Avatar URL of the sender (set by service join).
    /// </summary>
    public string? SenderAvatarUrl { get; set; }
    
    /// <summary>
    /// Optional: context (e.g., project, meeting, etc.)
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// Whether this is private (manager only) or shared with the recipient.
    /// </summary>
    public bool IsPrivate => Visibility == "private";

    #endregion

    #region Computed Display Properties

    /// <summary>
    /// Display text for the feedback type.
    /// </summary>
    public string TypeDisplay => FeedbackType?.ToLower() switch
    {
        "praise" => "Praise",
        "constructive" => "Constructive",
        "coaching" => "Coaching",
        "recognition" => "Recognition",
        _ => FeedbackType ?? "Feedback"
    };

    /// <summary>
    /// Display text for when the feedback was given.
    /// </summary>
    public string DateDisplay
    {
        get
        {
            var now = DateTime.Now;
            var local = CreatedAt.ToLocalTime();
            var diff = now - local;
            
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            
            return local.ToString("MMM d");
        }
    }

    /// <summary>
    /// Truncated content for card display.
    /// </summary>
    public string ContentPreview => Content.Length > 100 ? Content[..97] + "..." : Content;

    /// <summary>
    /// Alias for ContentPreview for XAML binding compatibility.
    /// </summary>
    public string Preview => ContentPreview;

    /// <summary>
    /// Alias for SenderName (the person who gave the feedback).
    /// </summary>
    public string AuthorName => SenderName;

    /// <summary>
    /// Initials of the feedback author (sender).
    /// </summary>
    public string AuthorInitials => GetInitials(SenderName);

    /// <summary>
    /// Initials of the feedback recipient.
    /// </summary>
    public string RecipientInitials => GetInitials(RecipientName);

    /// <summary>
    /// Helper to extract initials from a name.
    /// </summary>
    private static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpper(),
            _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpper()
        };
    }

    #endregion
}
