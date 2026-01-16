using System;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a feedback item.
/// </summary>
public class FeedbackDetail
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The team member who received the feedback.
    /// </summary>
    public Guid? TeamMemberId { get; set; }
    
    /// <summary>
    /// Name of the recipient.
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of feedback (praise, constructive, coaching, etc.)
    /// </summary>
    public string FeedbackType { get; set; } = "praise";
    
    /// <summary>
    /// The feedback content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// When the feedback was given.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional: context (e.g., project, meeting, etc.)
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// Whether this is private (manager only) or shared with the recipient.
    /// </summary>
    public bool IsPrivate { get; set; } = false;

    #region Computed Display Properties

    /// <summary>
    /// Display text for the feedback type.
    /// </summary>
    public string TypeDisplay => FeedbackType?.ToLower() switch
    {
        "praise" => "🌟 Praise",
        "constructive" => "💡 Constructive",
        "coaching" => "📚 Coaching",
        "recognition" => "🏆 Recognition",
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

    #endregion
}
