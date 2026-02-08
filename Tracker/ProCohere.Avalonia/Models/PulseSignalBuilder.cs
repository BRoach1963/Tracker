using System;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Builder for creating PulseSignal instances with proper defaults and validation.
/// 
/// Why a builder?
/// - PulseSignal has many properties, most with sensible defaults
/// - Deterministic IDs need to be computed from other properties
/// - Navigation target can be auto-derived from source type
/// - Builder enforces required properties before Build()
/// 
/// Usage:
///   var signal = new PulseSignalBuilder()
///       .ForGoal(goalId, goalTitle)
///       .WithTrigger(PulseTriggerReason.StatusChange)
///       .WithSeverity(PulseSignalSeverity.Warning)
///       .InSection(PulseSection.AttentionRequired)
///       .WithSummary("Goal health degraded")
///       .ForUser(userId)
///       .Build();
/// </summary>
public class PulseSignalBuilder
{
    private PulseSourceType _sourceType;
    private Guid _sourceId;
    private string _sourceName = string.Empty;
    private Guid _userId;
    private PulseTriggerReason _triggerReason;
    private PulseSignalSeverity _severity = PulseSignalSeverity.Info;
    private PulseSection _section;
    private NavigationItem? _navigationTarget;
    private string _summary = string.Empty;
    private string? _detail;
    private string? _recommendedAction;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _expiresAt;
    private Guid? _linkedTaskId;
    private Guid? _linkedMeetingId;
    private int _priority;
    
    #region Source Methods
    
    /// <summary>
    /// Set source as a Goal.
    /// </summary>
    public PulseSignalBuilder ForGoal(Guid goalId, string goalTitle)
    {
        _sourceType = PulseSourceType.Goal;
        _sourceId = goalId;
        _sourceName = goalTitle;
        return this;
    }
    
    /// <summary>
    /// Set source as a Metric.
    /// </summary>
    public PulseSignalBuilder ForMetric(Guid metricId, string metricName)
    {
        _sourceType = PulseSourceType.Metric;
        _sourceId = metricId;
        _sourceName = metricName;
        return this;
    }
    
    /// <summary>
    /// Set source as a Task.
    /// </summary>
    public PulseSignalBuilder ForTask(Guid taskId, string taskTitle)
    {
        _sourceType = PulseSourceType.Task;
        _sourceId = taskId;
        _sourceName = taskTitle;
        return this;
    }
    
    /// <summary>
    /// Set source as a Meeting.
    /// </summary>
    public PulseSignalBuilder ForMeeting(Guid meetingId, string meetingTitle)
    {
        _sourceType = PulseSourceType.Meeting;
        _sourceId = meetingId;
        _sourceName = meetingTitle;
        return this;
    }
    
    #endregion
    
    #region Classification Methods
    
    /// <summary>
    /// Set the trigger reason.
    /// </summary>
    public PulseSignalBuilder WithTrigger(PulseTriggerReason trigger)
    {
        _triggerReason = trigger;
        return this;
    }
    
    /// <summary>
    /// Set the severity level.
    /// </summary>
    public PulseSignalBuilder WithSeverity(PulseSignalSeverity severity)
    {
        _severity = severity;
        return this;
    }
    
    /// <summary>
    /// Set the Pulse section.
    /// </summary>
    public PulseSignalBuilder InSection(PulseSection section)
    {
        _section = section;
        return this;
    }
    
    /// <summary>
    /// Set the user this signal is for.
    /// </summary>
    public PulseSignalBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }
    
    #endregion
    
    #region Content Methods
    
    /// <summary>
    /// Set the summary text.
    /// </summary>
    public PulseSignalBuilder WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }
    
    /// <summary>
    /// Set the detail/subtitle text (optional).
    /// Used for narrative grouping to show recent topics.
    /// </summary>
    public PulseSignalBuilder WithDetail(string? detail)
    {
        _detail = detail;
        return this;
    }
    
    /// <summary>
    /// Set the recommended action (optional).
    /// </summary>
    public PulseSignalBuilder WithRecommendedAction(string action)
    {
        _recommendedAction = action;
        return this;
    }
    
    #endregion
    
    #region Navigation Methods
    
    /// <summary>
    /// Override the auto-derived navigation target.
    /// </summary>
    public PulseSignalBuilder NavigateTo(NavigationItem target)
    {
        _navigationTarget = target;
        return this;
    }
    
    #endregion
    
    #region Timing Methods
    
    /// <summary>
    /// Set the creation timestamp (defaults to now).
    /// </summary>
    public PulseSignalBuilder CreatedAt(DateTimeOffset timestamp)
    {
        _createdAt = timestamp;
        return this;
    }
    
    /// <summary>
    /// Set the creation timestamp from a DateTime (assumes UTC).
    /// </summary>
    public PulseSignalBuilder CreatedOn(DateTime timestamp)
    {
        _createdAt = new DateTimeOffset(timestamp, TimeSpan.Zero);
        return this;
    }
    
    /// <summary>
    /// Set expiration time.
    /// </summary>
    public PulseSignalBuilder ExpiresAt(DateTimeOffset expiry)
    {
        _expiresAt = expiry;
        return this;
    }
    
    /// <summary>
    /// Set expiration relative to creation.
    /// </summary>
    public PulseSignalBuilder ExpiresIn(TimeSpan duration)
    {
        _expiresAt = _createdAt.Add(duration);
        return this;
    }
    
    #endregion
    
    #region Link Methods
    
    /// <summary>
    /// Link to a task.
    /// </summary>
    public PulseSignalBuilder LinkedToTask(Guid taskId)
    {
        _linkedTaskId = taskId;
        return this;
    }
    
    /// <summary>
    /// Link to a meeting.
    /// </summary>
    public PulseSignalBuilder LinkedToMeeting(Guid meetingId)
    {
        _linkedMeetingId = meetingId;
        return this;
    }
    
    /// <summary>
    /// Link to a meeting (nullable overload - no-op if null).
    /// </summary>
    public PulseSignalBuilder LinkedToMeeting(Guid? meetingId)
    {
        if (meetingId.HasValue)
        {
            _linkedMeetingId = meetingId.Value;
        }
        return this;
    }
    
    #endregion
    
    #region Priority Methods
    
    /// <summary>
    /// Set the priority (higher = more urgent).
    /// </summary>
    public PulseSignalBuilder WithPriority(int priority)
    {
        _priority = priority;
        return this;
    }
    
    /// <summary>
    /// Set priority based on severity (convenience method).
    /// Critical=100, Warning=50, Info=10
    /// </summary>
    public PulseSignalBuilder WithSeverityBasedPriority()
    {
        _priority = _severity switch
        {
            PulseSignalSeverity.Critical => 100,
            PulseSignalSeverity.Warning => 50,
            PulseSignalSeverity.Info => 10,
            _ => 0
        };
        return this;
    }
    
    #endregion
    
    #region Build
    
    /// <summary>
    /// Build the PulseSignal instance.
    /// Computes deterministic ID and auto-derives navigation target if not specified.
    /// </summary>
    public PulseSignal Build()
    {
        // Validate required fields
        if (_sourceId == Guid.Empty)
            throw new InvalidOperationException("Source ID is required. Use ForGoal, ForMetric, ForTask, or ForMeeting.");
        
        if (string.IsNullOrWhiteSpace(_summary))
            throw new InvalidOperationException("Summary is required. Use WithSummary.");
        
        // Compute deterministic ID
        var signalId = PulseSignal.CreateDeterministicId(_sourceId, _triggerReason, _createdAt);
        
        // Auto-derive navigation target if not specified
        var navTarget = _navigationTarget ?? PulseSignal.DeriveNavigationTarget(_sourceType);
        
        return new PulseSignal
        {
            SignalId = signalId,
            SourceType = _sourceType,
            SourceId = _sourceId,
            SourceName = _sourceName,
            UserId = _userId,
            TriggerReason = _triggerReason,
            Severity = _severity,
            Section = _section,
            NavigationTarget = navTarget,
            Summary = _summary,
            Detail = _detail,
            RecommendedAction = _recommendedAction,
            CreatedAt = _createdAt,
            ExpiresAt = _expiresAt,
            LinkedTaskId = _linkedTaskId,
            LinkedMeetingId = _linkedMeetingId,
            Priority = _priority
        };
    }
    
    #endregion
}
