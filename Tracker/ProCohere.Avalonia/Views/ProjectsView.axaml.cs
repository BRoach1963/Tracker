using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Controls;
using ProCohere.Avalonia.Views.Dialogs;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Projects view - displays projects in a list with detail panel.
/// </summary>
public partial class ProjectsView : UserControl
{
    private ProjectsViewModel? _viewModel;
    private Popup? _ownerSelectorPopup;
    private TeamMemberSelectorPopover? _ownerSelectorPopover;

    public ProjectsView()
    {
        InitializeComponent();
        
        _viewModel = new ProjectsViewModel();
        DataContext = _viewModel;
        
        // Create the owner selector popup
        _ownerSelectorPopover = new TeamMemberSelectorPopover();
        _ownerSelectorPopover.MemberSelected += OnOwnerSelected;
        
        _ownerSelectorPopup = new Popup
        {
            Child = _ownerSelectorPopover,
            Placement = PlacementMode.Pointer,
            IsLightDismissEnabled = true
        };
        
        // Subscribe to dialog request events
        _viewModel.CreateProjectDialogRequested += OnCreateProjectDialogRequested;
        _viewModel.EditProjectDialogRequested += OnEditProjectDialogRequested;
        _viewModel.OwnerSelectorRequested += OnOwnerSelectorRequested;
    }
    
    /// <summary>
    /// Shows the owner selector popover.
    /// </summary>
    private void OnOwnerSelectorRequested(object? sender, EventArgs e)
    {
        if (_ownerSelectorPopup != null && _viewModel?.SelectedProject != null)
        {
            // Exclude current owner from the list
            _ownerSelectorPopover?.SetExcludeMember(_viewModel.SelectedProject.OwnerTeamMemberId);
            
            _ownerSelectorPopup.PlacementTarget = this;
            _ownerSelectorPopup.IsOpen = true;
        }
    }
    
    /// <summary>
    /// Handles selection of a new owner from the popover.
    /// </summary>
    private async void OnOwnerSelected(object? sender, TeamMemberDetail member)
    {
        _ownerSelectorPopup?.Close();
        _viewModel?.HideOwnerSelector();
        
        if (_viewModel != null)
        {
            await _viewModel.TransferOwnershipAsync(member.Id, member.FullName ?? member.Email ?? "Unknown");
        }
    }

    /// <summary>
    /// Shows the Create Project modal dialog.
    /// </summary>
    private async void OnCreateProjectDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null)
            return;

        var dialog = new CreateProjectDialog();
        
        // Set up loaders for available tasks, goals, and members
        dialog.SetTaskLoader(LoadAvailableTasksAsync);
        dialog.SetGoalLoader(LoadAvailableGoalsAsync);
        dialog.SetMemberLoader(LoadAvailableMembersAsync);
        
        await dialog.ShowDialog(window);
        
        var result = dialog.Result;
        if (result != null)
        {
            // Create the project with staged work
            await _viewModel.CreateProjectFromDialogAsync(result);
        }
    }
    
    /// <summary>
    /// Shows the Edit Project modal dialog.
    /// </summary>
    private async void OnEditProjectDialogRequested(object? sender, Project project)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null || _viewModel == null)
            return;

        var dialog = new EditProjectDialog();
        dialog.InitForEdit(project);
        
        await dialog.ShowDialog(window);
        
        var result = dialog.Result;
        if (result != null)
        {
            // Update the project
            await _viewModel.UpdateProjectFromDialogAsync(result);
        }
    }
    
    /// <summary>
    /// Loads tasks available for linking (unlinked, incomplete).
    /// </summary>
    private async Task<IEnumerable<LinkableItem>> LoadAvailableTasksAsync()
    {
        var tasks = await TaskService.Instance.GetLinkableTasksAsync();
        return tasks.Select(t => new LinkableItem
        {
            Id = t.Id,
            Title = t.Title ?? "Untitled Task",
            Subtitle = t.DueDate?.ToString("MMM d") ?? null
        });
    }
    
    /// <summary>
    /// Loads goals available for linking (unlinked, active).
    /// </summary>
    private async Task<IEnumerable<LinkableItem>> LoadAvailableGoalsAsync()
    {
        var goals = await GoalsService.Instance.GetLinkableGoalsAsync();
        return goals.Select(g => new LinkableItem
        {
            Id = g.Id,
            Title = g.Title ?? "Untitled Goal",
            Subtitle = g.GoalType.ToString()
        });
    }
    
    /// <summary>
    /// Loads team members available for adding to project.
    /// </summary>
    private async Task<IEnumerable<LinkableItem>> LoadAvailableMembersAsync()
    {
        var members = await TeamService.Instance.GetVisibleTeamMembersAsync();
        var currentUser = AuthService.Instance.CurrentTeamMember;
        
        // Exclude current user (they're the owner) and show all others
        return members
            .Where(m => currentUser == null || m.Id != currentUser.Id)
            .Select(m => new LinkableItem
            {
                Id = m.Id,
                Title = m.DisplayName ?? m.Email ?? "Unknown",
                Subtitle = m.JobTitle
            });
    }

    /// <summary>
    /// Handles clicking on a project card to show details.
    /// </summary>
    private async void OnProjectCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Project project && _viewModel != null)
        {
            await _viewModel.SelectProjectCommand.ExecuteAsync(project);
        }
    }
}
