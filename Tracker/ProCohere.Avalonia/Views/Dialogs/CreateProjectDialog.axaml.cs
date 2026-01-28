using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using ProCohere.Avalonia.Views.Controls;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating a new project.
/// Supports minimal creation (Name, Description, Due Date) plus optional work staging (Tasks/Goals).
/// All staging state is owned by the ViewModel - the view only binds to it.
/// </summary>
public partial class CreateProjectDialog : Window
{
    private readonly CreateProjectDialogViewModel _viewModel;
    
    /// <summary>
    /// Result of the dialog - the project data if created, null if cancelled.
    /// </summary>
    public CreateProjectResult? Result => _viewModel.Result;

    public CreateProjectDialog()
    {
        InitializeComponent();
        _viewModel = new CreateProjectDialogViewModel();
        DataContext = _viewModel;
        
        _viewModel.CloseRequested += () => Close();
        
        // Focus the name field when dialog opens
        NameTextBox.AttachedToVisualTree += (s, e) => NameTextBox.Focus();
    }
    
    /// <summary>
    /// Sets the callback for loading available tasks.
    /// </summary>
    public void SetTaskLoader(Func<Task<IEnumerable<LinkableItem>>> loader)
    {
        _viewModel.LoadAvailableTasksAsync = loader;
    }
    
    /// <summary>
    /// Sets the callback for loading available goals.
    /// </summary>
    public void SetGoalLoader(Func<Task<IEnumerable<LinkableItem>>> loader)
    {
        _viewModel.LoadAvailableGoalsAsync = loader;
    }
    
    /// <summary>
    /// Sets the callback for loading available team members.
    /// </summary>
    public void SetMemberLoader(Func<Task<IEnumerable<LinkableItem>>> loader)
    {
        _viewModel.LoadAvailableMembersAsync = loader;
    }
}
