using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for viewing team member details and editing organization-specific fields.
/// Personal info is view-only as it's owned by the team member.
/// </summary>
public partial class TeamMemberDetailsDialog : Window
{
    private TeamMemberDetailsDialogViewModel? _viewModel;

    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public TeamMemberDetailsResult? Result { get; private set; }

    public TeamMemberDetailsDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialize the dialog with a ViewModel.
    /// </summary>
    public void Initialize(TeamMemberDetailsDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
        DataContext = _viewModel;

        // Bind HireDate manually since DatePicker doesn't support two-way binding well
        if (_viewModel.HireDate.HasValue)
        {
            HireDatePicker.SelectedDate = _viewModel.HireDate.Value;
        }
        HireDatePicker.SelectedDateChanged += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.HireDate = HireDatePicker.SelectedDate;
            }
        };
    }

    private void OnCloseRequested(object? sender, TeamMemberDetailsResult? result)
    {
        Result = result;
        Close(Result);
    }

    /// <summary>
    /// Handle Add Note button click - shows the AddNoteDialog.
    /// </summary>
    private async void AddNoteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        var dialog = new AddNoteDialog();
        var result = await dialog.ShowDialog<AddNoteResult?>(this);

        if (result != null && !string.IsNullOrWhiteSpace(result.Content))
        {
            await _viewModel.AddNoteFromDialogAsync(result.Title, result.Content);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }
        base.OnClosed(e);
    }
}
