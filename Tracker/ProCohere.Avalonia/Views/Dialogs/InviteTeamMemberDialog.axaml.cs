using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for inviting a new team member.
/// Minimal code-behind - all logic in ViewModel.
/// </summary>
public partial class InviteTeamMemberDialog : Window
{
    private readonly InviteTeamMemberDialogViewModel _viewModel;

    public InviteTeamMemberDialog()
    {
        InitializeComponent();

        _viewModel = new InviteTeamMemberDialogViewModel();
        DataContext = _viewModel;

        _viewModel.CloseRequested += (_, _) => Close();
    }

    /// <summary>
    /// Gets the dialog result after closing.
    /// </summary>
    public InviteTeamMemberResult? Result => _viewModel.Result;

    /// <summary>
    /// Initialize with available managers for assignment.
    /// </summary>
    public void SetManagers(IEnumerable<TeamMemberDetail> managers)
    {
        _viewModel.Initialize(managers);
    }
}
