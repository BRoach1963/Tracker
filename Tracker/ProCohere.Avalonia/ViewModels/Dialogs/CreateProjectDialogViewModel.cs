using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Views.Controls;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the CreateProjectDialog.
/// Supports minimal creation (Name, Description, Due Date) plus optional work staging (Tasks/Goals).
/// </summary>
public partial class CreateProjectDialogViewModel : ObservableObject
{
    /// <summary>
    /// Result of the dialog - the project data if created, null if cancelled.
    /// </summary>
    public CreateProjectResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    /// <summary>
    /// Callback to load available tasks for linking.
    /// Injected by the caller to avoid service dependencies.
    /// </summary>
    public Func<Task<IEnumerable<LinkableItem>>>? LoadAvailableTasksAsync { get; set; }
    
    /// <summary>
    /// Callback to load available goals for linking.
    /// Injected by the caller to avoid service dependencies.
    /// </summary>
    public Func<Task<IEnumerable<LinkableItem>>>? LoadAvailableGoalsAsync { get; set; }
    
    /// <summary>
    /// Callback to load available team members for adding to project.
    /// Injected by the caller to avoid service dependencies.
    /// </summary>
    public Func<Task<IEnumerable<LinkableItem>>>? LoadAvailableMembersAsync { get; set; }
    
    #region Observable Properties - Basic Info
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _description = string.Empty;
    
    [ObservableProperty]
    private DateTimeOffset? _dueDate;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    #endregion
    
    #region Observable Properties - Work Staging
    
    /// <summary>
    /// Whether the work staging section is expanded.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkSectionButtonText))]
    private bool _isWorkSectionExpanded;
    
    /// <summary>
    /// Selected tab index (0 = Tasks, 1 = Goals).
    /// </summary>
    [ObservableProperty]
    private int _selectedWorkTabIndex;
    
    /// <summary>
    /// Whether available items are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingAvailableItems;
    
    /// <summary>
    /// New task titles to create (title-only bootstrapping).
    /// </summary>
    public ObservableCollection<string> NewTaskTitles { get; } = new();
    
    /// <summary>
    /// New goal titles to create (title-only bootstrapping).
    /// </summary>
    public ObservableCollection<string> NewGoalTitles { get; } = new();
    
    /// <summary>
    /// Available tasks for linking. Selection state stored in LinkableItem.IsSelected.
    /// </summary>
    public ObservableCollection<LinkableItem> AvailableTasks { get; } = new();
    
    /// <summary>
    /// Available goals for linking. Selection state stored in LinkableItem.IsSelected.
    /// </summary>
    public ObservableCollection<LinkableItem> AvailableGoals { get; } = new();
    
    /// <summary>
    /// Available team members for adding to project. Selection state stored in LinkableItem.IsSelected.
    /// </summary>
    public ObservableCollection<LinkableItem> AvailableMembers { get; } = new();
    
    /// <summary>
    /// Summary of staged work for display.
    /// </summary>
    public string StagedWorkSummary
    {
        get
        {
            var parts = new List<string>();
            var taskCount = NewTaskTitles.Count + AvailableTasks.Count(t => t.IsSelected);
            var goalCount = NewGoalTitles.Count + AvailableGoals.Count(g => g.IsSelected);
            var memberCount = AvailableMembers.Count(m => m.IsSelected);
            
            if (taskCount > 0)
                parts.Add($"{taskCount} task{(taskCount == 1 ? "" : "s")}");
            if (goalCount > 0)
                parts.Add($"{goalCount} goal{(goalCount == 1 ? "" : "s")}");
            if (memberCount > 0)
                parts.Add($"{memberCount} member{(memberCount == 1 ? "" : "s")}");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "No work added";
        }
    }
    
    #endregion
    
    #region Computed Properties
    
    /// <summary>
    /// Whether the create button can be enabled.
    /// </summary>
    public bool CanCreate => !string.IsNullOrWhiteSpace(Name);
    
    /// <summary>
    /// Text for the work section expand/collapse button.
    /// </summary>
    public string WorkSectionButtonText => IsWorkSectionExpanded ? "Hide work" : "Add work now (optional)";
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        var nameText = Name?.Trim();
        
        if (string.IsNullOrWhiteSpace(nameText))
        {
            ErrorMessage = "Project name is required";
            return;
        }
        
        ErrorMessage = null;
        
        // Gather selected existing IDs from the LinkableItem collections
        var selectedTaskIds = AvailableTasks.Where(t => t.IsSelected).Select(t => t.Id).ToList();
        var selectedGoalIds = AvailableGoals.Where(g => g.IsSelected).Select(g => g.Id).ToList();
        var selectedMemberIds = AvailableMembers.Where(m => m.IsSelected).Select(m => m.Id).ToList();
        
        Result = new CreateProjectResult
        {
            Name = nameText,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            DueDate = DueDate?.DateTime,
            NewTaskTitles = new List<string>(NewTaskTitles),
            ExistingTaskIds = selectedTaskIds,
            NewGoalTitles = new List<string>(NewGoalTitles),
            ExistingGoalIds = selectedGoalIds,
            MemberIds = selectedMemberIds
        };
        
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private async Task ToggleWorkSectionAsync()
    {
        IsWorkSectionExpanded = !IsWorkSectionExpanded;
        
        if (IsWorkSectionExpanded)
        {
            await LoadAvailableItemsAsync();
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private async Task LoadAvailableItemsAsync()
    {
        if (IsLoadingAvailableItems) return;
        
        IsLoadingAvailableItems = true;
        
        try
        {
            // Load tasks
            if (LoadAvailableTasksAsync != null)
            {
                var tasks = await LoadAvailableTasksAsync();
                AvailableTasks.Clear();
                foreach (var task in tasks)
                {
                    AvailableTasks.Add(task);
                }
            }
            
            // Load goals
            if (LoadAvailableGoalsAsync != null)
            {
                var goals = await LoadAvailableGoalsAsync();
                AvailableGoals.Clear();
                foreach (var goal in goals)
                {
                    AvailableGoals.Add(goal);
                }
            }
            
            // Load team members
            if (LoadAvailableMembersAsync != null)
            {
                var members = await LoadAvailableMembersAsync();
                AvailableMembers.Clear();
                foreach (var member in members)
                {
                    AvailableMembers.Add(member);
                }
            }
        }
        finally
        {
            IsLoadingAvailableItems = false;
        }
    }
    
    #endregion
}
