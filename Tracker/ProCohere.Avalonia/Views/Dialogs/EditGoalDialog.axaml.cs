using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing goals.
/// </summary>
public partial class EditGoalDialog : Window
{
    private readonly EditGoalDialogViewModel _viewModel;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditGoalResult? Result => _viewModel.Result;
    
    public EditGoalDialog()
    {
        InitializeComponent();
        _viewModel = new EditGoalDialogViewModel();
        DataContext = _viewModel;
        _viewModel.CloseRequested += () => Close();
    }
    
    /// <summary>
    /// Load an existing goal for editing.
    /// </summary>
    public void LoadGoal(GoalDetail goal)
    {
        _viewModel.LoadGoal(goal);
    }
    
    /// <summary>
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _viewModel.SetTeamMembers(teamMembers);
    }
}
