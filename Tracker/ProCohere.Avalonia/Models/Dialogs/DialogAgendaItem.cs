using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Agenda item model for the dialog that supports optional linking to entities.
/// Enhanced to be a "conversation container" with context, talking points, and outcomes.
/// </summary>
public partial class DialogAgendaItem : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveTitle))]
    private string _title = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveTitle))]
    private string? _displayTitle;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContext))]
    private string? _sharedContext;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContext))]
    private string? _privateContext;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPersonalAgenda))]
    [NotifyPropertyChangedFor(nameof(VisibilityIcon))]
    [NotifyPropertyChangedFor(nameof(VisibilityTooltip))]
    private string _visibilityScope = "meeting";
    
    // Linked entity (optional - for discussing existing tasks/goals/metrics)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkedEntity))]
    [NotifyPropertyChangedFor(nameof(LinkedEntityTypeDisplay))]
    [NotifyPropertyChangedFor(nameof(TypeIcon))]
    [NotifyPropertyChangedFor(nameof(TypeColor))]
    private Guid? _linkedEntityId;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkedEntity))]
    [NotifyPropertyChangedFor(nameof(LinkedEntityTypeDisplay))]
    [NotifyPropertyChangedFor(nameof(TypeIcon))]
    [NotifyPropertyChangedFor(nameof(TypeColor))]
    private string? _linkedEntityType;
    
    [ObservableProperty]
    private string? _linkedEntityTitle;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveTitle))]
    private string? _linkedEntityTitleSnapshot;
    
    // Outcome tracking (captured during/after meeting)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutcome))]
    [NotifyPropertyChangedFor(nameof(OutcomeTypeDisplay))]
    [NotifyPropertyChangedFor(nameof(OutcomeBadgeColor))]
    private string? _outcomeType;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutcome))]
    private string? _outcomeSummary;
    
    // Talking points (JSON stored but edited as list)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTalkingPoints))]
    [NotifyPropertyChangedFor(nameof(TalkingPointsCount))]
    private List<TalkingPoint> _talkingPoints = new();
    
    #region Computed Properties
    
    public bool HasLinkedEntity => LinkedEntityId.HasValue && !string.IsNullOrEmpty(LinkedEntityType);
    public bool IsPersonalAgenda => VisibilityScope == "personal";
    public bool HasContext => !string.IsNullOrWhiteSpace(SharedContext) || !string.IsNullOrWhiteSpace(PrivateContext);
    public bool HasTalkingPoints => TalkingPoints.Count > 0;
    public int TalkingPointsCount => TalkingPoints.Count;
    public bool HasOutcome => !string.IsNullOrWhiteSpace(OutcomeType);
    
    /// <summary>
    /// Effective title for display - prefers DisplayTitle, falls back to LinkedEntityTitleSnapshot or Title.
    /// </summary>
    public string EffectiveTitle => !string.IsNullOrWhiteSpace(DisplayTitle)
        ? DisplayTitle
        : !string.IsNullOrWhiteSpace(LinkedEntityTitleSnapshot)
            ? LinkedEntityTitleSnapshot
            : Title;
    
    public string VisibilityIcon => IsPersonalAgenda
        ? "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z"  // Lock
        : "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z"; // People
    
    public string VisibilityTooltip => IsPersonalAgenda
        ? "Personal reminder - only you can see this"
        : "Shared with meeting attendees";
    
    /// <summary>
    /// Display text for outcome type - matches DB constraint values.
    /// </summary>
    public string OutcomeTypeDisplay => OutcomeType?.ToLower() switch
    {
        "discussed" => "Discussed",
        "decision" => "Decision",
        "deferred" => "Deferred",
        "blocked" => "Blocked",
        _ => ""
    };
    
    /// <summary>
    /// Badge color based on outcome type.
    /// </summary>
    public global::Avalonia.Media.IBrush OutcomeBadgeColor => OutcomeType?.ToLower() switch
    {
        "discussed" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#27AE60")), // Green
        "decision" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#3498DB")),  // Blue
        "deferred" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#F39C12")),  // Orange
        "blocked" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#E74C3C")),   // Red
        _ => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#7F8C8D"))
    };
    
    public string LinkedEntityTypeDisplay => LinkedEntityType?.ToLower() switch
    {
        "task" => "Task",
        "goal" => "Goal",
        "metric" => "Metric",
        "project" => "Project",
        _ => ""
    };
    
    public string TypeIcon => LinkedEntityType?.ToLower() switch
    {
        "task" => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z",
        "goal" => "M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19C19,19.55 18.55,20 18,20H6C5.45,20 5,19.55 5,19V18H19V19Z",
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z",
        "project" => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z",
        _ => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
    };
    
    public global::Avalonia.Media.IBrush TypeColor => LinkedEntityType?.ToLower() switch
    {
        "task" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#3498DB")),
        "goal" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#27AE60")),
        "metric" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#9B59B6")),
        "project" => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#E67E22")),
        _ => new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.Parse("#7F8C8D"))
    };
    
    #endregion
    
    #region Factory Methods
    
    /// <summary>
    /// Creates a DialogAgendaItem from a database MeetingAgendaItem.
    /// </summary>
    public static DialogAgendaItem FromModel(MeetingAgendaItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        DisplayTitle = item.DisplayTitle,
        SharedContext = item.SharedContext,
        PrivateContext = item.PrivateContext,
        VisibilityScope = item.VisibilityScope ?? "meeting",
        LinkedEntityId = item.LinkedEntityId,
        LinkedEntityType = item.LinkedEntityType,
        LinkedEntityTitle = item.LinkedEntityTitle,
        LinkedEntityTitleSnapshot = item.LinkedEntityTitleSnapshot,
        OutcomeType = item.OutcomeType,
        OutcomeSummary = item.OutcomeSummary,
        TalkingPoints = item.TalkingPoints
    };
    
    /// <summary>
    /// Creates a new DialogAgendaItem for a linked entity.
    /// </summary>
    public static DialogAgendaItem ForLinkedEntity(string entityType, Guid entityId, string entityTitle) => new()
    {
        Title = $"Discuss {entityTitle}",
        LinkedEntityId = entityId,
        LinkedEntityType = entityType,
        LinkedEntityTitle = entityTitle,
        LinkedEntityTitleSnapshot = entityTitle
    };
    
    #endregion
}
