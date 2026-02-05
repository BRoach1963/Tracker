using System;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents an AI-generated insight about user data.
/// Maps to existing ai_insights table schema (procohere.ai_insights).
/// </summary>
public class Insight
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Organization this insight belongs to.</summary>
    public Guid OrganizationId { get; set; }
    
    /// <summary>Team member this insight was generated for (RLS key).</summary>
    public Guid GeneratedFor { get; set; }
    
    /// <summary>Optional team member this insight is about.</summary>
    public Guid? TeamMemberId { get; set; }
    
    // Insight Metadata
    
    /// <summary>Type of insight (task_overdue, goal_off_track, etc.).</summary>
    public InsightType Type { get; set; }
    
    /// <summary>Short, actionable title.</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Detailed content/message.</summary>
    public string Content { get; set; } = string.Empty;
    
    // Related Entity (source)
    
    /// <summary>Optional entity type this insight relates to (goal, task, meeting, etc.).</summary>
    public string? EntityType { get; set; }
    
    /// <summary>Optional entity ID this insight relates to.</summary>
    public Guid? EntityId { get; set; }
    
    // Status Tracking
    
    /// <summary>Whether this insight has been dismissed.</summary>
    public bool IsDismissed { get; set; }
    
    /// <summary>When the insight was dismissed, if applicable.</summary>
    public DateTime? DismissedAt { get; set; }
    
    // Analyzer Metadata
    
    /// <summary>Relevance score (0.0-1.0) from the analyzer.</summary>
    public decimal? RelevanceScore { get; set; }
    
    // Timestamps
    
    /// <summary>When the insight was created.</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the insight was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    
    // Soft Delete
    
    /// <summary>Whether this insight is deleted.</summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>When deleted, if applicable.</summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>Who deleted the insight, if applicable.</summary>
    public Guid? DeletedBy { get; set; }
    
    // Computed Properties for UI Binding
    
    /// <summary>True if the insight is currently active.</summary>
    public bool IsActive => !IsDismissed && !IsDeleted;
    
    /// <summary>Severity based on relevance score (for UI display).</summary>
    public InsightSeverity Severity => (RelevanceScore ?? 0.5m) switch
    {
        >= 0.9m => InsightSeverity.Critical,
        >= 0.7m => InsightSeverity.High,
        >= 0.4m => InsightSeverity.Medium,
        _ => InsightSeverity.Low
    };
    
    /// <summary>True if this is a critical severity insight.</summary>
    public bool IsCritical => Severity == InsightSeverity.Critical;
    
    /// <summary>True if this is a positive insight (reinforcement).</summary>
    public bool IsPositive => Type == InsightType.GoalOnTrack || 
                              Type == InsightType.SentimentImproving;
    
    /// <summary>Color for severity indicator.</summary>
    public string SeverityColor => Severity switch
    {
        InsightSeverity.Critical => "#DC2626", // Red 600
        InsightSeverity.High => "#F59E0B",     // Amber 500
        InsightSeverity.Medium => "#3B82F6",   // Blue 500
        InsightSeverity.Low => "#10B981",      // Green 500
        _ => "#6B7280"                          // Gray 500
    };
    
    /// <summary>Icon path data for severity indicator.</summary>
    public string SeverityIcon => Severity switch
    {
        InsightSeverity.Critical => "M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z", // Alert triangle
        InsightSeverity.High => "M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z", // Exclamation circle
        InsightSeverity.Medium => "M11.25 11.25l.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z", // Info circle
        InsightSeverity.Low => "M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z", // Check circle
        _ => "M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
    };
    
    /// <summary>User-friendly type name for display.</summary>
    public string TypeDisplayName => Type switch
    {
        InsightType.TaskOverdue => "Task Overdue",
        InsightType.StaleActionItem => "Stale Task",
        InsightType.GoalOffTrack => "Goal Off Track",
        InsightType.GoalOnTrack => "Goal On Track",
        InsightType.MeetingOverdue => "Meeting Overdue",
        InsightType.MeetingUpcoming => "Meeting Upcoming",
        InsightType.MetricMissing => "Missing Metric",
        InsightType.MetricDeclining => "Metric Declining",
        InsightType.PersonalDate => "Personal Date",
        InsightType.SentimentDeclining => "Sentiment Declining",
        InsightType.SentimentImproving => "Sentiment Improving",
        _ => Type.ToString()
    };
    
    /// <summary>Relative time display (e.g., "2 hours ago").</summary>
    public string RelativeTime
    {
        get
        {
            var timeSpan = DateTime.UtcNow - CreatedAt;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            
            return CreatedAt.ToString("MMM d");
        }
    }
}
