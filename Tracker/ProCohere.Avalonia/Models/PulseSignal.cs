using System;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Type of entity that generated the signal.
/// </summary>
public enum PulseSourceType
{
    Goal,
    Metric,
    Meeting,
    Task
}

/// <summary>
/// What triggered this signal to be generated.
/// </summary>
public enum PulseTriggerReason
{
    /// <summary>Metric crossed its defined threshold.</summary>
    ThresholdCrossed,
    
    /// <summary>Trend direction changed (up→down, etc.).</summary>
    TrendReversal,
    
    /// <summary>Trend has stalled (no meaningful change).</summary>
    TrendStalled,
    
    /// <summary>Goal or metric status changed (OnTrack→AtRisk, etc.).</summary>
    StatusChange,
    
    /// <summary>Deadline or checkpoint is approaching.</summary>
    DeadlineApproaching,
    
    /// <summary>Metric has not been updated recently.</summary>
    StaleMetric,
    
    /// <summary>Goal health has degraded repeatedly.</summary>
    RepeatedDegradation,
    
    /// <summary>Task was completed that was linked to a goal/meeting.</summary>
    TaskCompleted,
    
    /// <summary>Meeting had discussion items linked to goals/metrics.</summary>
    MeetingDiscussion,
    
    /// <summary>Review or update is due.</summary>
    ReviewDue
}

/// <summary>
/// Severity level of the signal.
/// </summary>
public enum PulseSignalSeverity
{
    /// <summary>Informational - no action required.</summary>
    Info,
    
    /// <summary>Warning - attention may be needed soon.</summary>
    Warning,
    
    /// <summary>Critical - immediate attention required.</summary>
    Critical
}

/// <summary>
/// Which section of Pulse this signal belongs in.
/// </summary>
public enum PulseSection
{
    /// <summary>Immediate intervention signals only.</summary>
    AttentionRequired,
    
    /// <summary>Awareness without alarm.</summary>
    WhatChanged,
    
    /// <summary>Narrative continuity from meetings.</summary>
    RecentDiscussions,
    
    /// <summary>Completed actions that reinforce follow-through.</summary>
    ActionsTaken
}

/// <summary>
/// Represents a signal in the Pulse synthesis feed (v4).
/// 
/// Design principles:
/// - Immutable after creation (init-only properties)
/// - Deterministic IDs for deduplication (content-based hash)
/// - Typed navigation targets (no runtime derivation in ViewModel)
/// - UTC timestamps with DateTimeOffset for timezone awareness
/// 
/// Per spec:
/// - Pulse is derived, never manually edited
/// - Signals are time-scoped and role-aware
/// - Each signal implies meaning, not just data
/// </summary>
public partial class PulseSignal : ObservableObject
{
    #region Identity
    
    /// <summary>
    /// Unique identifier for this signal instance.
    /// Deterministic: derived from SourceId + TriggerReason + date for deduplication.
    /// </summary>
    public Guid SignalId { get; init; }
    
    /// <summary>
    /// Creates a deterministic signal ID based on content.
    /// Same source + trigger + day = same ID (prevents duplicates).
    /// </summary>
    public static Guid CreateDeterministicId(Guid sourceId, PulseTriggerReason trigger, DateTimeOffset timestamp)
    {
        var input = $"{sourceId}|{trigger}|{timestamp:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Use first 16 bytes of hash as GUID
        return new Guid(hash.AsSpan(0, 16));
    }
    
    #endregion
    
    #region Source Information
    
    /// <summary>
    /// Type of entity that generated this signal.
    /// </summary>
    public PulseSourceType SourceType { get; init; }
    
    /// <summary>
    /// ID of the source entity (goal, metric, meeting, or task).
    /// </summary>
    public Guid SourceId { get; init; }
    
    /// <summary>
    /// Name/title of the source entity for display.
    /// </summary>
    public string SourceName { get; init; } = string.Empty;
    
    #endregion
    
    #region Classification
    
    /// <summary>
    /// User this signal is relevant to.
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// What triggered this signal.
    /// </summary>
    public PulseTriggerReason TriggerReason { get; init; }
    
    /// <summary>
    /// Severity level (info, warning, critical).
    /// </summary>
    public PulseSignalSeverity Severity { get; init; }
    
    /// <summary>
    /// Which Pulse section this signal belongs in.
    /// </summary>
    public PulseSection Section { get; init; }
    
    #endregion
    
    #region Navigation Target (v4)
    
    /// <summary>
    /// Navigation target page for this signal.
    /// Stored directly instead of derived at runtime (MVVM-clean).
    /// </summary>
    public NavigationItem NavigationTarget { get; init; }
    
    /// <summary>
    /// Derives the navigation target from source type.
    /// Used during signal creation.
    /// </summary>
    public static NavigationItem DeriveNavigationTarget(PulseSourceType sourceType)
    {
        return sourceType switch
        {
            PulseSourceType.Goal => NavigationItem.Goals,
            PulseSourceType.Metric => NavigationItem.Metrics,
            PulseSourceType.Task => NavigationItem.Tasks,
            PulseSourceType.Meeting => NavigationItem.Me, // Meetings in personal hub
            _ => NavigationItem.Pulse
        };
    }
    
    #endregion
    
    #region Content
    
    /// <summary>
    /// Human-readable summary of the signal.
    /// Should be one line, interpreted, not raw data.
    /// Example: "Customer satisfaction dropped below target"
    /// </summary>
    public string Summary { get; init; } = string.Empty;
    
    /// <summary>
    /// Recommended action (optional).
    /// Example: "Review feedback before Friday"
    /// </summary>
    public string? RecommendedAction { get; init; }
    
    #endregion
    
    #region Timestamps (v4 - DateTimeOffset for timezone awareness)
    
    /// <summary>
    /// When the signal was detected/generated (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// When this signal should no longer be shown (optional).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
    
    #endregion
    
    #region Links
    
    /// <summary>
    /// Optional linked task ID (for action-sourced signals).
    /// </summary>
    public Guid? LinkedTaskId { get; init; }
    
    /// <summary>
    /// Optional linked meeting ID (for discussion-sourced signals).
    /// </summary>
    public Guid? LinkedMeetingId { get; init; }
    
    #endregion
    
    #region Sorting
    
    /// <summary>
    /// Priority for sorting (higher = more urgent).
    /// </summary>
    public int Priority { get; init; }
    
    #endregion
    
    #region UI Helpers
    
    /// <summary>
    /// Icon path data based on source type.
    /// </summary>
    public string SourceIcon => SourceType switch
    {
        PulseSourceType.Goal => "M5,21L7.5,13L1,9H8.5L11,1L13.5,9H21L14.5,13L17,21L11,16L5,21Z", // Star
        PulseSourceType.Metric => "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z", // Chart
        PulseSourceType.Meeting => "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1", // Calendar
        PulseSourceType.Task => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M9,13V19H7V13H9M15,15V19H13V15H15M11,11V19H9V11H11M13,13V19H11V13H13", // Document
        _ => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z" // Circle
    };
    
    /// <summary>
    /// Color class based on severity.
    /// </summary>
    public string SeverityColorClass => Severity switch
    {
        PulseSignalSeverity.Critical => "severity-critical",
        PulseSignalSeverity.Warning => "severity-warning",
        PulseSignalSeverity.Info => "severity-info",
        _ => "severity-info"
    };
    
    /// <summary>
    /// Badge text for source type.
    /// </summary>
    public string SourceBadge => SourceType switch
    {
        PulseSourceType.Goal => "Goal",
        PulseSourceType.Metric => "Metric",
        PulseSourceType.Meeting => "Meeting",
        PulseSourceType.Task => "Task",
        _ => "Item"
    };
    
    /// <summary>
    /// Human-readable trigger description.
    /// </summary>
    public string TriggerDescription => TriggerReason switch
    {
        PulseTriggerReason.ThresholdCrossed => "crossed threshold",
        PulseTriggerReason.TrendReversal => "trend changed",
        PulseTriggerReason.TrendStalled => "trend stalled",
        PulseTriggerReason.StatusChange => "status changed",
        PulseTriggerReason.DeadlineApproaching => "deadline approaching",
        PulseTriggerReason.StaleMetric => "needs update",
        PulseTriggerReason.RepeatedDegradation => "repeated issues",
        PulseTriggerReason.TaskCompleted => "completed",
        PulseTriggerReason.MeetingDiscussion => "discussed",
        PulseTriggerReason.ReviewDue => "review due",
        _ => ""
    };
    
    #endregion
}
