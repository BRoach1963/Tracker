using System.Collections.Generic;
using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing a task.
/// </summary>
public partial class AddTaskDialog : Window
{
    private readonly AddTaskDialogViewModel _viewModel;
    private bool _forceClose;
    
    /// <summary>
    /// Result of the dialog - the task data if saved, null if cancelled.
    /// </summary>
    public AddTaskResult? Result => _viewModel.Result;

    public AddTaskDialog()
    {
        InitializeComponent();
        _viewModel = new AddTaskDialogViewModel();
        DataContext = _viewModel;
        SetupViewModel();
        
        // Focus the title field
        TitleTextBox.AttachedToVisualTree += (s, e) => TitleTextBox.Focus();
    }
    
    private void SetupViewModel()
    {
        // Provide dialog service for confirmations
        _viewModel.SetDialogService(new DialogService(this));
        
        // Close window when ViewModel requests it (approved close path)
        _viewModel.CloseRequested += () =>
        {
            _forceClose = true;
            Close();
        };
    }
    
    /// <summary>
    /// Handle window closing to show confirmation if there are unsaved changes.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }
        
        if (_viewModel.HasUnsavedChanges)
        {
            e.Cancel = true;
            
            if (_viewModel.CancelCommand.CanExecute(null))
            {
                await _viewModel.CancelCommand.ExecuteAsync(null);
            }
        }
        else
        {
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Sets the list of team members for the assignee dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> members)
    {
        _viewModel.SetTeamMembers(members);
    }
    
    /// <summary>
    /// Load an existing task for editing.
    /// </summary>
    public void LoadTask(TaskDetail task)
    {
        _viewModel.LoadTask(task);
    }
}
