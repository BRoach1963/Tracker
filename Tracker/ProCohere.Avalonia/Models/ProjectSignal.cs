using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Type of project signal displayed in the Briefing.
/// These are passive awareness indicators, not action triggers.
/// </summary>
public enum ProjectSignalType
{
    /// <summary>
    /// Project is approaching or past its due date.
    /// </summary>
    DueSoon,
    
    /// <summary>
    /// Project has tasks that are overdue.
    /// </summary>
    OverdueTasks,
    
    /// <summary>
    /// Project has goals that need attention.
    /// </summary>
    GoalsNeedAttention,
    
    /// <summary>
    /// Project has been idle (no activity) for a while.
    /// </summary>
    Stale,
    
    /// <summary>
    /// Project has a high percentage of completed work.
    /// </summary>
    NearingCompletion
}

/// <summary>
/// Represents a signal about a project's state for the Briefing view.
/// 
/// Design Philosophy:
/// - These are passive awareness signals, not action triggers
/// - Clicking navigates to Projects tab (no flyout, no inline fixes)
/// - One project can have multiple signals
/// - Signals inform, they don't prescribe action
/// </summary>
public partial class ProjectSignal : ObservableObject
{
    /// <summary>
    /// ID of the project this signal is about.
    /// </summary>
    public Guid ProjectId { get; init; }
    
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of signal (determines icon and styling).
    /// </summary>
    public ProjectSignalType SignalType { get; init; }
    
    /// <summary>
    /// Human-readable summary of the signal.
    /// Example: "2 overdue tasks", "Due in 3 days", "3 goals need attention"
    /// </summary>
    public string Summary { get; init; } = string.Empty;
    
    /// <summary>
    /// Priority of this signal (higher = more urgent).
    /// Used for sorting signals in the UI.
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// When the signal was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the icon glyph for this signal type.
    /// </summary>
    public string IconGlyph => SignalType switch
    {
        ProjectSignalType.DueSoon => "\uE823", // Calendar
        ProjectSignalType.OverdueTasks => "\uE7BA", // Warning
        ProjectSignalType.GoalsNeedAttention => "\uE945", // Target/Focus
        ProjectSignalType.Stale => "\uE916", // Sleep/Pause
        ProjectSignalType.NearingCompletion => "\uE73E", // Checkmark
        _ => "\uE946" // Info
    };
    
    /// <summary>
    /// Gets the color key for this signal type.
    /// </summary>
    public string ColorKey => SignalType switch
    {
        ProjectSignalType.DueSoon => "Warning",
        ProjectSignalType.OverdueTasks => "Danger",
        ProjectSignalType.GoalsNeedAttention => "Warning",
        ProjectSignalType.Stale => "Muted",
        ProjectSignalType.NearingCompletion => "Success",
        _ => "Primary"
    };
}
