using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for recording an outcome (decision, feedback, or notes) for an agenda item.
/// </summary>
public partial class RecordOutcomeDialog : Window
{
    private MeetingAgendaItem? _agendaItem;
    private string _preselectedType = OutcomeType.DecisionRecorded;

    /// <summary>
    /// Result of the dialog - the outcome data if saved, null if cancelled.
    /// </summary>
    public RecordOutcomeResult? Result { get; private set; }

    public RecordOutcomeDialog()
    {
        InitializeComponent();
        
        // Wire up radio button changes to update the content label
        DecisionRadio.IsCheckedChanged += (s, e) => UpdateContentLabel();
        FeedbackRadio.IsCheckedChanged += (s, e) => UpdateContentLabel();
        NotesRadio.IsCheckedChanged += (s, e) => UpdateContentLabel();
        
        // Focus the content field
        ContentTextBox.AttachedToVisualTree += (s, e) => ContentTextBox.Focus();
    }

    /// <summary>
    /// Sets the agenda item context for this dialog.
    /// </summary>
    public void SetAgendaItem(MeetingAgendaItem item)
    {
        _agendaItem = item;
        AgendaItemTitleText.Text = item.Title;
    }

    /// <summary>
    /// Pre-selects the outcome type (e.g., when user clicks "Record Decision" button).
    /// </summary>
    public void SetOutcomeType(string outcomeType)
    {
        _preselectedType = outcomeType;
        
        switch (outcomeType)
        {
            case OutcomeType.DecisionRecorded:
                DecisionRadio.IsChecked = true;
                break;
            case OutcomeType.FeedbackCaptured:
                FeedbackRadio.IsChecked = true;
                break;
            case OutcomeType.NotesAdded:
                NotesRadio.IsChecked = true;
                break;
        }
        
        UpdateContentLabel();
    }

    private void UpdateContentLabel()
    {
        if (DecisionRadio.IsChecked == true)
        {
            ContentLabel.Text = "Decision";
            ContentTextBox.Watermark = "What was decided?";
        }
        else if (FeedbackRadio.IsChecked == true)
        {
            ContentLabel.Text = "Feedback";
            ContentTextBox.Watermark = "What feedback was shared?";
        }
        else if (NotesRadio.IsChecked == true)
        {
            ContentLabel.Text = "Notes";
            ContentTextBox.Watermark = "Capture the discussion...";
        }
    }

    private string GetSelectedOutcomeType()
    {
        if (DecisionRadio.IsChecked == true) return OutcomeType.DecisionRecorded;
        if (FeedbackRadio.IsChecked == true) return OutcomeType.FeedbackCaptured;
        if (NotesRadio.IsChecked == true) return OutcomeType.NotesAdded;
        return OutcomeType.NotesAdded;
    }

    private string GetSelectedVisibility()
    {
        var selectedItem = VisibilityComboBox.SelectedItem as ComboBoxItem;
        return selectedItem?.Tag?.ToString() ?? OutcomeVisibility.Attendees;
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var content = ContentTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(content))
        {
            ContentTextBox.Focus();
            return;
        }

        if (_agendaItem == null)
        {
            Close();
            return;
        }

        Result = new RecordOutcomeResult
        {
            AgendaItemId = _agendaItem.Id,
            OutcomeType = GetSelectedOutcomeType(),
            Content = content,
            Visibility = GetSelectedVisibility()
        };

        Close();
    }
}

/// <summary>
/// Result data from the RecordOutcomeDialog.
/// </summary>
public class RecordOutcomeResult
{
    public required Guid AgendaItemId { get; init; }
    public required string OutcomeType { get; init; }
    public required string Content { get; init; }
    public required string Visibility { get; init; }
}
