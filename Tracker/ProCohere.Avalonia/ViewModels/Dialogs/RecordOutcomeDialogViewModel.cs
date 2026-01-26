using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the RecordOutcomeDialog.
/// </summary>
public partial class RecordOutcomeDialogViewModel : ObservableObject
{
    private MeetingAgendaItem? _agendaItem;

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<RecordOutcomeResult?>? CloseRequested;

    /// <summary>
    /// Gets the agenda item title for display.
    /// </summary>
    [ObservableProperty]
    private string _agendaItemTitle = string.Empty;

    /// <summary>
    /// Gets or sets whether Decision type is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentLabel))]
    [NotifyPropertyChangedFor(nameof(ContentWatermark))]
    private bool _isDecisionSelected = true;

    /// <summary>
    /// Gets or sets whether Feedback type is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentLabel))]
    [NotifyPropertyChangedFor(nameof(ContentWatermark))]
    private bool _isFeedbackSelected;

    /// <summary>
    /// Gets or sets whether Notes type is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContentLabel))]
    [NotifyPropertyChangedFor(nameof(ContentWatermark))]
    private bool _isNotesSelected;

    /// <summary>
    /// Gets or sets the outcome content.
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the selected visibility index.
    /// </summary>
    [ObservableProperty]
    private int _selectedVisibilityIndex = 1; // Default to "Meeting Attendees"

    /// <summary>
    /// Gets the label for the content field based on selected type.
    /// </summary>
    public string ContentLabel => IsDecisionSelected ? "Decision" 
        : IsFeedbackSelected ? "Feedback" 
        : "Notes";

    /// <summary>
    /// Gets the watermark text for the content field based on selected type.
    /// </summary>
    public string ContentWatermark => IsDecisionSelected ? "What was decided?" 
        : IsFeedbackSelected ? "What feedback was shared?" 
        : "Capture the discussion...";

    /// <summary>
    /// Initializes the dialog with an agenda item.
    /// </summary>
    public void Initialize(MeetingAgendaItem item)
    {
        _agendaItem = item;
        AgendaItemTitle = item.Title;
    }

    /// <summary>
    /// Pre-selects the outcome type.
    /// </summary>
    public void SetOutcomeType(string outcomeType)
    {
        IsDecisionSelected = outcomeType == OutcomeType.DecisionRecorded;
        IsFeedbackSelected = outcomeType == OutcomeType.FeedbackCaptured;
        IsNotesSelected = outcomeType == OutcomeType.NotesAdded;
    }

    private string GetSelectedOutcomeType()
    {
        if (IsDecisionSelected) return OutcomeType.DecisionRecorded;
        if (IsFeedbackSelected) return OutcomeType.FeedbackCaptured;
        return OutcomeType.NotesAdded;
    }

    private string GetSelectedVisibility()
    {
        return SelectedVisibilityIndex switch
        {
            0 => OutcomeVisibility.Private,
            1 => OutcomeVisibility.Attendees,
            2 => OutcomeVisibility.Team,
            3 => OutcomeVisibility.Organization,
            _ => OutcomeVisibility.Attendees
        };
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void Save()
    {
        var content = Content?.Trim();
        
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (_agendaItem == null)
        {
            CloseRequested?.Invoke(this, null);
            return;
        }

        var result = new RecordOutcomeResult
        {
            AgendaItemId = _agendaItem.Id,
            OutcomeType = GetSelectedOutcomeType(),
            Content = content,
            Visibility = GetSelectedVisibility()
        };

        CloseRequested?.Invoke(this, result);
    }
}
