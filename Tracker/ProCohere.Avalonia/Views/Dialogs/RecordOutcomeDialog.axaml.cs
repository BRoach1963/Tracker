using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for recording an outcome (decision, feedback, or notes) for an agenda item.
/// </summary>
public partial class RecordOutcomeDialog : Window
{
    private readonly RecordOutcomeDialogViewModel _viewModel;

    /// <summary>
    /// Result of the dialog - the outcome data if saved, null if cancelled.
    /// </summary>
    public RecordOutcomeResult? Result { get; private set; }

    public RecordOutcomeDialog()
    {
        InitializeComponent();
        
        _viewModel = new RecordOutcomeDialogViewModel();
        DataContext = _viewModel;
        
        _viewModel.CloseRequested += OnCloseRequested;
        
        // Focus the content field
        ContentTextBox.AttachedToVisualTree += (s, e) => ContentTextBox.Focus();
    }

    /// <summary>
    /// Sets the agenda item context for this dialog.
    /// </summary>
    public void SetAgendaItem(MeetingAgendaItem item)
    {
        _viewModel.Initialize(item);
    }

    /// <summary>
    /// Pre-selects the outcome type (e.g., when user clicks "Record Decision" button).
    /// </summary>
    public void SetOutcomeType(string outcomeType)
    {
        _viewModel.SetOutcomeType(outcomeType);
    }

    private void OnCloseRequested(object? sender, RecordOutcomeResult? result)
    {
        Result = result;
        Close();
    }
}
