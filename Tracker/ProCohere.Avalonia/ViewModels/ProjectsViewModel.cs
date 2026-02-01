using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// Tab options for the project detail flyout.
/// </summary>
public enum ProjectDetailTab
{
    Overview,
    Goals,
    Tasks,
    Meetings
}

/// <summary>
/// ViewModel for the Projects view.
/// Manages project listing, filtering, and CRUD operations.
/// </summary>
public partial class ProjectsViewModel : ViewModelBase
{
    #region Events
    
    /// <summary>
    /// Raised when the create project dialog should be shown.
    /// View subscribes to this event and shows the modal dialog.
    /// </summary>
    public event EventHandler? CreateProjectDialogRequested;
    
    /// <summary>
    /// Raised when the edit project dialog should be shown.
    /// View subscribes to this event and shows the modal dialog.
    /// </summary>
    public event EventHandler<Project>? EditProjectDialogRequested;
    
    #endregion

    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Status Filter

    /// <summary>
    /// Status filter: 0=All, 1=Active, 2=Paused, 3=Completed
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatusAll))]
    [NotifyPropertyChangedFor(nameof(IsStatusActive))]
    [NotifyPropertyChangedFor(nameof(IsStatusPaused))]
    [NotifyPropertyChangedFor(nameof(IsStatusCompleted))]
    private int _selectedStatusFilter;

    public bool IsStatusAll => SelectedStatusFilter == 0;
    public bool IsStatusActive => SelectedStatusFilter == 1;
    public bool IsStatusPaused => SelectedStatusFilter == 2;
    public bool IsStatusCompleted => SelectedStatusFilter == 3;

    /// <summary>
    /// Status filter options for UI.
    /// </summary>
    public static IReadOnlyList<string> StatusFilterOptions { get; } = new[]
    {
        "All Projects",
        "Active",
        "Paused",
        "Completed"
    };

    [RelayCommand]
    private async Task SetStatusFilter(string filterIndex)
    {
        if (int.TryParse(filterIndex, out var index))
        {
            SelectedStatusFilter = index;
            await LoadProjectsAsync();
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// All loaded projects.
    /// </summary>
    private List<Project> _allProjects = new();

    /// <summary>
    /// Filtered projects for display.
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();
    
    /// <summary>
    /// True if user has no projects at all.
    /// </summary>
    public bool HasNoProjects => _allProjects.Count == 0;
    
    /// <summary>
    /// True if user has projects but none match current filter.
    /// </summary>
    public bool HasNoFilteredResults => _allProjects.Count > 0 && Projects.Count == 0;
    
    /// <summary>
    /// True if project list should be shown.
    /// </summary>
    public bool HasProjects => Projects.Count > 0;

    #endregion

    #region Selection State

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProject))]
    [NotifyPropertyChangedFor(nameof(IsCurrentUserOwner))]
    [NotifyCanExecuteChangedFor(nameof(EditProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectCommand))]
    private Project? _selectedProject;

    public bool HasSelectedProject => SelectedProject != null;
    
    /// <summary>
    /// True if the current user is the owner of the selected project.
    /// Only owners can change project status.
    /// </summary>
    public bool IsCurrentUserOwner
    {
        get
        {
            if (SelectedProject == null) return false;
            var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;
            return currentUserId.HasValue && SelectedProject.OwnerTeamMemberId == currentUserId.Value;
        }
    }

    [ObservableProperty]
    private bool _isDetailFlyoutOpen;

    [ObservableProperty]
    private bool _isEditorFlyoutOpen;

    [ObservableProperty]
    private bool _isNewProject;

    /// <summary>
    /// Current tab selection in the detail flyout.
    /// </summary>
    [ObservableProperty]
    private ProjectDetailTab _projectDetailTab = ProjectDetailTab.Overview;

    /// <summary>
    /// Sets the detail flyout tab.
    /// </summary>
    [RelayCommand]
    private void SetProjectDetailTab(ProjectDetailTab tab)
    {
        ProjectDetailTab = tab;
    }

    #endregion

    #region Linked Items by Type
    
    /// <summary>
    /// Goals linked to the selected project.
    /// </summary>
    public ObservableCollection<ProjectLink> LinkedGoals { get; } = new();
    
    /// <summary>
    /// Tasks linked to the selected project.
    /// </summary>
    public ObservableCollection<ProjectLink> LinkedTasks { get; } = new();
    
    /// <summary>
    /// Meetings linked to the selected project.
    /// </summary>
    public ObservableCollection<ProjectLink> LinkedMeetings { get; } = new();
    
    /// <summary>
    /// Metrics linked to the selected project.
    /// </summary>
    public ObservableCollection<ProjectLink> LinkedMetrics { get; } = new();
    
    /// <summary>
    /// Whether the selected project has any linked goals.
    /// </summary>
    public bool HasLinkedGoals => LinkedGoals.Count > 0;
    
    /// <summary>
    /// Whether the selected project has any linked tasks.
    /// </summary>
    public bool HasLinkedTasks => LinkedTasks.Count > 0;
    
    /// <summary>
    /// Whether the selected project has any linked meetings.
    /// </summary>
    public bool HasLinkedMeetings => LinkedMeetings.Count > 0;
    
    /// <summary>
    /// Whether the selected project has any linked metrics.
    /// </summary>
    public bool HasLinkedMetrics => LinkedMetrics.Count > 0;
    
    /// <summary>
    /// Populates the linked item collections from the selected project's Links.
    /// </summary>
    private void PopulateLinkedItemCollections()
    {
        LinkedGoals.Clear();
        LinkedTasks.Clear();
        LinkedMeetings.Clear();
        LinkedMetrics.Clear();
        
        if (SelectedProject?.Links == null) return;
        
        foreach (var link in SelectedProject.Links.Where(l => !l.IsDeleted))
        {
            switch (link.EntityType)
            {
                case ProjectLinkEntityType.Goal:
                    LinkedGoals.Add(link);
                    break;
                case ProjectLinkEntityType.Task:
                    LinkedTasks.Add(link);
                    break;
                case ProjectLinkEntityType.Meeting:
                    LinkedMeetings.Add(link);
                    break;
                case ProjectLinkEntityType.Metric:
                    LinkedMetrics.Add(link);
                    break;
            }
        }
        
        OnPropertyChanged(nameof(HasLinkedGoals));
        OnPropertyChanged(nameof(HasLinkedTasks));
        OnPropertyChanged(nameof(HasLinkedMeetings));
        OnPropertyChanged(nameof(HasLinkedMetrics));
    }

    #endregion

    #region Owner Transfer State
    
    /// <summary>
    /// Event raised when the team member selector should be shown for ownership transfer.
    /// </summary>
    public event EventHandler? OwnerSelectorRequested;
    
    /// <summary>
    /// Whether the owner selector popover is open.
    /// </summary>
    [ObservableProperty]
    private bool _isOwnerSelectorOpen;
    
    /// <summary>
    /// True if the current user is an admin and can reclaim orphaned projects.
    /// </summary>
    public bool IsAdmin => AuthService.Instance.CurrentRole?.Name?.ToLowerInvariant() == "admin";
    
    /// <summary>
    /// Whether the current user can change ownership (owner or admin for orphaned).
    /// </summary>
    public bool CanTransferOwnership => IsCurrentUserOwner || (IsAdmin && SelectedProject?.IsOrphaned == true);
    
    /// <summary>
    /// Requests the View to show the owner selector popover.
    /// </summary>
    [RelayCommand]
    private void ShowOwnerSelector()
    {
        IsOwnerSelectorOpen = true;
        OwnerSelectorRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Hides the owner selector popover.
    /// </summary>
    public void HideOwnerSelector()
    {
        IsOwnerSelectorOpen = false;
    }
    
    /// <summary>
    /// Transfers ownership to a new team member.
    /// Called by the View when a team member is selected in the popover.
    /// </summary>
    public async Task TransferOwnershipAsync(Guid newOwnerId, string newOwnerName)
    {
        if (SelectedProject == null) return;
        
        try
        {
            IsLoading = true;
            
            var success = await ProjectService.Instance.TransferOwnershipAsync(
                SelectedProject.Id,
                newOwnerId);
            
            if (success)
            {
                // Reload project to get updated owner info
                var updatedProject = await ProjectService.Instance.GetProjectByIdAsync(SelectedProject.Id);
                if (updatedProject != null)
                {
                    // Update in local collections
                    var index = _allProjects.FindIndex(p => p.Id == updatedProject.Id);
                    if (index >= 0)
                    {
                        _allProjects[index] = updatedProject;
                    }
                    
                    SelectedProject = updatedProject;
                    ApplyFilters();
                    
                    // Notify ownership-related properties changed
                    OnPropertyChanged(nameof(IsCurrentUserOwner));
                    OnPropertyChanged(nameof(CanTransferOwnership));
                }
                
                NotificationService.Instance.ShowSuccess(
                    "Ownership Transferred",
                    $"'{SelectedProject.Name}' is now owned by {newOwnerName}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "Transfer Failed",
                    ProjectService.Instance.LastError ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Transfer Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
            IsOwnerSelectorOpen = false;
        }
    }
    
    #endregion

    #region Editor State

    [ObservableProperty]
    private string _editorName = string.Empty;

    [ObservableProperty]
    private string _editorDescription = string.Empty;

    [ObservableProperty]
    private StatusOption? _editorStatusOption;

    [ObservableProperty]
    private DateTime? _editorDueDate;

    /// <summary>
    /// Status options for the editor.
    /// </summary>
    public static IReadOnlyList<StatusOption> StatusOptions { get; } = new[]
    {
        new StatusOption(ProjectStatus.Active, "Active"),
        new StatusOption(ProjectStatus.Paused, "Paused"),
        new StatusOption(ProjectStatus.Completed, "Completed")
    };

    #endregion

    #region Linked Chronicle Notes
    
    /// <summary>
    /// Chronicle notes linked to the selected project.
    /// Only titles are displayed; clicking opens the note in Chronicle.
    /// </summary>
    public ObservableCollection<Note> LinkedNotes { get; } = new();
    
    /// <summary>
    /// Whether the selected project has any linked Chronicle notes.
    /// </summary>
    public bool HasLinkedNotes => LinkedNotes.Count > 0;
    
    /// <summary>
    /// Opens a Chronicle note (navigates to Chronicle tab with note selected).
    /// For now, shows a notification directing user to Chronicle tab.
    /// TODO: Implement cross-tab navigation when NavigationService is available.
    /// </summary>
    [RelayCommand]
    private void OpenNote(Note? note)
    {
        if (note == null) return;
        
        // For now, just show a notification directing to Chronicle
        // A full navigation service would require MainWindowViewModel changes
        NotificationService.Instance.ShowInfo(
            "View in Chronicle",
            $"Open the Chronicle tab to view '{note.DisplayTitle}'");
    }
    
    /// <summary>
    /// Loads Chronicle notes linked to the specified project.
    /// </summary>
    private async Task LoadLinkedNotesAsync(Guid projectId)
    {
        try
        {
            LinkedNotes.Clear();
            
            // Get notes that have a link to this project via note_links table
            var notes = await NotesService.Instance.GetNotesForEntityViaLinksAsync("project", projectId);
            foreach (var note in notes)
            {
                LinkedNotes.Add(note);
            }
            
            OnPropertyChanged(nameof(HasLinkedNotes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProjectsViewModel] Failed to load linked notes: {ex.Message}");
        }
    }
    
    #endregion

    #region Stats

    [ObservableProperty]
    private int _totalProjectsCount;

    [ObservableProperty]
    private int _activeProjectsCount;

    [ObservableProperty]
    private int _overdueProjectsCount;

    #endregion

    public ProjectsViewModel()
    {
        // Load projects on initialization
        _ = LoadProjectsAsync();
    }

    #region Load Commands

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _allProjects = await ProjectService.Instance.GetAllProjectsAsync();

            if (ProjectService.Instance.LastError != null)
            {
                ErrorMessage = ProjectService.Instance.LastError;
                return;
            }

            ApplyFilters();
            UpdateStats();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        Projects.Clear();

        var filtered = SelectedStatusFilter switch
        {
            1 => _allProjects.Where(p => p.Status == ProjectStatus.Active),
            2 => _allProjects.Where(p => p.Status == ProjectStatus.Paused),
            3 => _allProjects.Where(p => p.Status == ProjectStatus.Completed),
            _ => _allProjects
        };

        foreach (var project in filtered.OrderByDescending(p => p.CreatedAt))
        {
            Projects.Add(project);
        }
        
        // Notify empty state properties
        OnPropertyChanged(nameof(HasNoProjects));
        OnPropertyChanged(nameof(HasNoFilteredResults));
        OnPropertyChanged(nameof(HasProjects));
    }

    private void UpdateStats()
    {
        TotalProjectsCount = _allProjects.Count;
        ActiveProjectsCount = _allProjects.Count(p => p.Status == ProjectStatus.Active);
        OverdueProjectsCount = _allProjects.Count(p => p.IsOverdue);
    }

    #endregion

    #region Selection Commands

    [RelayCommand]
    private async Task SelectProject(Project? project)
    {
        if (project == null)
        {
            SelectedProject = null;
            IsDetailFlyoutOpen = false;
            LinkedNotes.Clear();
            LinkedGoals.Clear();
            LinkedTasks.Clear();
            LinkedMeetings.Clear();
            LinkedMetrics.Clear();
            OnPropertyChanged(nameof(HasLinkedNotes));
            OnPropertyChanged(nameof(HasLinkedGoals));
            OnPropertyChanged(nameof(HasLinkedTasks));
            OnPropertyChanged(nameof(HasLinkedMeetings));
            OnPropertyChanged(nameof(HasLinkedMetrics));
            return;
        }

        // Load full details with members and links
        var fullProject = await ProjectService.Instance.GetProjectByIdAsync(project.Id);
        if (fullProject != null)
        {
            SelectedProject = fullProject;
            IsDetailFlyoutOpen = true;
            
            // Reset tab to Overview when selecting new project
            ProjectDetailTab = ProjectDetailTab.Overview;
            
            // Populate linked item collections by type
            PopulateLinkedItemCollections();
            
            // Load linked Chronicle notes
            await LoadLinkedNotesAsync(fullProject.Id);
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailFlyoutOpen = false;
    }

    #endregion

    #region Create Commands

    [RelayCommand]
    private void ShowCreateDialog()
    {
        // Raise event for view to show modal dialog
        CreateProjectDialogRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Creates a project from the dialog result.
    /// Called by the view after the modal dialog closes with a result.
    /// Handles staged work (new tasks/goals to create, existing ones to link).
    /// </summary>
    public async Task CreateProjectFromDialogAsync(CreateProjectResult result)
    {
        if (result == null || string.IsNullOrWhiteSpace(result.Name))
        {
            ErrorMessage = "Project name is required";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // 1. Create the project
            var project = await ProjectService.Instance.CreateProjectAsync(
                result.Name,
                result.Description,
                ProjectStatus.Active,
                result.DueDate);

            if (project == null)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to create project";
                return;
            }

            // 2. Create new tasks (title-only bootstrapping)
            foreach (var title in result.NewTaskTitles)
            {
                var task = await TaskService.Instance.CreateMinimalTaskAsync(title, project.Id);
                if (task == null)
                {
                    // Log error but continue - don't fail entire operation
                    System.Diagnostics.Debug.WriteLine($"Failed to create task: {title}");
                }
            }

            // 3. Link existing tasks to the project
            foreach (var taskId in result.ExistingTaskIds)
            {
                var success = await TaskService.Instance.LinkTaskToProjectAsync(taskId, project.Id);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to link task: {taskId}");
                }
            }

            // 4. Create new goals (title-only bootstrapping)
            foreach (var title in result.NewGoalTitles)
            {
                var goal = await GoalsService.Instance.CreateMinimalGoalAsync(title, project.Id);
                if (goal == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create goal: {title}");
                }
            }

            // 5. Link existing goals to the project
            foreach (var goalId in result.ExistingGoalIds)
            {
                var success = await GoalsService.Instance.LinkGoalToProjectAsync(goalId, project.Id);
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to link goal: {goalId}");
                }
            }

            // 6. Add team members to the project
            foreach (var memberId in result.MemberIds)
            {
                var added = await ProjectService.Instance.AddProjectMemberAsync(project.Id, memberId);
                if (added == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add member: {memberId}");
                }
            }

            // Build success message
            var workCount = result.NewTaskTitles.Count + result.ExistingTaskIds.Count +
                           result.NewGoalTitles.Count + result.ExistingGoalIds.Count;
            var memberCount = result.MemberIds.Count;
            
            var parts = new List<string>();
            if (workCount > 0)
                parts.Add($"{workCount} work item{(workCount == 1 ? "" : "s")}");
            if (memberCount > 0)
                parts.Add($"{memberCount} member{(memberCount == 1 ? "" : "s")}");
            
            var message = parts.Count > 0
                ? $"'{project.Name}' created with {string.Join(" and ", parts)}."
                : $"'{project.Name}' has been created.";

            NotificationService.Instance.ShowSuccess("Project Created", message);
            
            await LoadProjectsAsync();
            
            // Per spec: immediately open the Project Detail flyout for the newly created project
            await SelectProject(project);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(EditorName))
        {
            ErrorMessage = "Project name is required";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var project = await ProjectService.Instance.CreateProjectAsync(
                EditorName,
                string.IsNullOrWhiteSpace(EditorDescription) ? null : EditorDescription,
                EditorStatusOption?.Value ?? ProjectStatus.Active,
                EditorDueDate);

            if (project == null)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to create project";
                return;
            }

            IsEditorFlyoutOpen = false;
            NotificationService.Instance.ShowSuccess("Project Created", $"'{project.Name}' has been created.");
            
            await LoadProjectsAsync();
            await SelectProject(project);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Edit Commands

    [RelayCommand(CanExecute = nameof(HasSelectedProject))]
    private void EditProject()
    {
        if (SelectedProject == null) return;

        // Raise event to show the edit dialog
        EditProjectDialogRequested?.Invoke(this, SelectedProject);
    }
    
    /// <summary>
    /// Updates a project from the dialog result.
    /// Called by the View after the edit dialog is closed.
    /// </summary>
    public async Task UpdateProjectFromDialogAsync(Project updatedData)
    {
        if (SelectedProject == null) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            // Map status string to enum
            var status = updatedData.Status?.ToLowerInvariant() switch
            {
                "paused" => ProjectStatus.Paused,
                "completed" => ProjectStatus.Completed,
                _ => ProjectStatus.Active
            };

            var updated = await ProjectService.Instance.UpdateProjectAsync(
                SelectedProject.Id,
                updatedData.Name ?? SelectedProject.Name,
                updatedData.Description,
                status,
                updatedData.DueDate);

            if (updated == null)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to update project";
                NotificationService.Instance.ShowError("Update Failed", ErrorMessage);
                return;
            }

            NotificationService.Instance.ShowSuccess("Project Updated", $"'{updated.Name}' has been updated.");
            
            await LoadProjectsAsync();
            
            // Refresh the selected project with full details
            var refreshed = await ProjectService.Instance.GetProjectByIdAsync(updated.Id);
            if (refreshed != null)
            {
                SelectedProject = refreshed;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Update Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (IsNewProject)
        {
            await CreateProjectAsync();
            return;
        }

        if (SelectedProject == null || string.IsNullOrWhiteSpace(EditorName))
        {
            ErrorMessage = "Project name is required";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await ProjectService.Instance.UpdateProjectAsync(
                SelectedProject.Id,
                EditorName,
                string.IsNullOrWhiteSpace(EditorDescription) ? null : EditorDescription,
                EditorStatusOption?.Value ?? ProjectStatus.Active,
                EditorDueDate);

            if (updated == null)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to update project";
                return;
            }

            IsEditorFlyoutOpen = false;
            NotificationService.Instance.ShowSuccess("Project Updated", $"'{updated.Name}' has been updated.");
            
            await LoadProjectsAsync();
            
            // Refresh the selected project with full details
            var refreshed = await ProjectService.Instance.GetProjectByIdAsync(updated.Id);
            if (refreshed != null)
            {
                SelectedProject = refreshed;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditorFlyoutOpen = false;
    }

    #endregion

    #region Delete Commands

    [RelayCommand(CanExecute = nameof(HasSelectedProject))]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject == null) return;

        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Project?",
            $"Are you sure you want to delete '{SelectedProject.Name}'?\n\nThis will also remove all members and links. This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var projectName = SelectedProject.Name;
            var success = await ProjectService.Instance.DeleteProjectAsync(SelectedProject.Id);

            if (!success)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to delete project";
                return;
            }

            SelectedProject = null;
            IsDetailFlyoutOpen = false;
            NotificationService.Instance.ShowSuccess("Project Deleted", $"'{projectName}' has been deleted.");
            
            await LoadProjectsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Status Commands

    [RelayCommand]
    private async Task SetProjectStatus(string status)
    {
        if (SelectedProject == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await ProjectService.Instance.UpdateProjectStatusAsync(SelectedProject.Id, status);

            if (updated == null)
            {
                ErrorMessage = ProjectService.Instance.LastError ?? "Failed to update status";
                return;
            }

            NotificationService.Instance.ShowSuccess("Status Updated", $"Project status changed to {updated.StatusDisplay}.");
            
            await LoadProjectsAsync();
            
            // Refresh the selected project
            var refreshed = await ProjectService.Instance.GetProjectByIdAsync(updated.Id);
            if (refreshed != null)
            {
                SelectedProject = refreshed;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Refresh

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadProjectsAsync();
        
        // Also refresh selected project if any
        if (SelectedProject != null)
        {
            var refreshed = await ProjectService.Instance.GetProjectByIdAsync(SelectedProject.Id);
            if (refreshed != null)
            {
                SelectedProject = refreshed;
            }
        }
    }

    #endregion
}

/// <summary>
/// Status option for combo boxes.
/// </summary>
public record StatusOption(string Value, string Display);
