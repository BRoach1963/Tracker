using System.Collections.Generic;
using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing a task.
/// </summary>
public partial class AddTaskDialog : Window
{
    private readonly AddTaskDialogViewModel _viewModel;
    
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
        _viewModel.CloseRequested += () => Close();
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
