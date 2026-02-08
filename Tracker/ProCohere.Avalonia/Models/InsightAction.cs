using System;
using System.Text.Json;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a user's action on an insight (viewed, dismissed, snoozed, acted).
/// Maps to procohere.ai_insight_actions table.
/// </summary>
public class InsightAction
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Organization this action belongs to.</summary>
    public Guid OrganizationId { get; set; }
    
    /// <summary>Team member who took the action.</summary>
    public Guid TeamMemberId { get; set; }
    
    /// <summary>Optional reference to the specific insight (for audit trail).</summary>
    public Guid? InsightId { get; set; }
    
    /// <summary>Signature hash that identifies the insight pattern (64 hex chars).</summary>
    public string SignatureHash { get; set; } = string.Empty;
    
    /// <summary>Type of action: viewed, dismissed, snoozed, acted.</summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>Optional reason for the action (e.g., "Already handled").</summary>
    public string? ActionReason { get; set; }
    
    /// <summary>Additional metadata as JSON.</summary>
    public JsonDocument? ActionMetadata { get; set; }
    
    /// <summary>When this action expires (for snooze/dismiss with time bounds).</summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>When the action was created.</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the action was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>Whether this action is deleted (soft delete).</summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>When deleted, if applicable.</summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>Who deleted the action, if applicable.</summary>
    public Guid? DeletedBy { get; set; }
    
    // Computed properties
    
    /// <summary>True if this action is still active (not expired, not deleted).</summary>
    public bool IsActive => !IsDeleted && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    
    /// <summary>True if this is a dismiss action.</summary>
    public bool IsDismiss => ActionType == InsightActionType.Dismissed;
    
    /// <summary>True if this is a snooze action.</summary>
    public bool IsSnooze => ActionType == InsightActionType.Snoozed;
    
    /// <summary>True if this is an acted action.</summary>
    public bool IsActed => ActionType == InsightActionType.Acted;
    
    /// <summary>True if this is a viewed action.</summary>
    public bool IsViewed => ActionType == InsightActionType.Viewed;
}

/// <summary>
/// Constants for insight action types.
/// </summary>
public static class InsightActionType
{
    public const string Viewed = "viewed";
    public const string Dismissed = "dismissed";
    public const string Snoozed = "snoozed";
    public const string Acted = "acted";
}
