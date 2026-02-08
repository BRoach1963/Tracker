using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Tasks view.
/// Displays task list with filters and CRUD operations.
/// </summary>
public partial class TasksViewModel : ViewModelBase
{
    #region Navigation Events

    /// <summary>
    /// Event raised when user wants to navigate back to Pulse.
    /// </summary>
    public event EventHandler? NavigateBackRequested;

    [RelayCommand]
    private void NavigateBack() => NavigateBackRequested?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Loading State
    
    private bool _isLoadingData; // Tracks if we're currently fetching data

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowTaskList))]
    [NotifyPropertyChangedFor(nameof(ShowLoading))]
    private bool _isLoading; // Data is loaded at app startup before UI is shown

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowTaskList))]
    [NotifyPropertyChangedFor(nameof(ShowLoading))]
    private bool _hasError;

    /// <summary>
    /// Refresh status for non-blocking indicator (Idle/Updating/Updated).
    /// Shows "Updating..." only after 400ms delay to avoid flicker.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefreshStatusText))]
    [NotifyPropertyChangedFor(nameof(ShowRefreshStatus))]
    private RefreshStatus _refreshStatus = RefreshStatus.Idle;

    /// <summary>
    /// Display text for the refresh status chip.
    /// </summary>
    public string RefreshStatusText => RefreshStatus switch
    {
        RefreshStatus.Updating => "Updating…",
        RefreshStatus.Updated => "Updated",
        _ => string.Empty
    };

    /// <summary>
    /// Whether to show the refresh status chip (hide when Idle).
    /// </summary>
    public bool ShowRefreshStatus => RefreshStatus != RefreshStatus.Idle;

    /// <summary>
    /// Cancellation token for the 400ms delay timer.
    /// </summary>
    private CancellationTokenSource? _updateDelayTokenSource;
    
    /// <summary>
    /// Whether to show the loading spinner.
    /// </summary>
    public bool ShowLoading => IsLoading && !HasError;
    
    /// <summary>
    /// Whether to show the empty state (not loading, no error, no tasks).
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasError && FilteredTasks.Count == 0;
    
    /// <summary>
    /// Whether to show the task list (not loading, no error, has tasks).
    /// </summary>
    public bool ShowTaskList => !IsLoading && !HasError && FilteredTasks.Count > 0;

    #endregion

    #region Filter

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterToday))]
    [NotifyPropertyChangedFor(nameof(IsFilterUpcoming))]
    [NotifyPropertyChangedFor(nameof(IsFilterOverdue))]
    [NotifyPropertyChangedFor(nameof(IsFilterCompleted))]
    [NotifyPropertyChangedFor(nameof(FilteredTasks))]
    private TaskFilter _currentFilter = TaskFilter.All;

    public bool IsFilterAll => CurrentFilter == TaskFilter.All;
    public bool IsFilterToday => CurrentFilter == TaskFilter.Today;
    public bool IsFilterUpcoming => CurrentFilter == TaskFilter.Upcoming;
    public bool IsFilterOverdue => CurrentFilter == TaskFilter.Overdue;
    public bool IsFilterCompleted => CurrentFilter == TaskFilter.Completed;

    [RelayCommand]
    private async Task SetFilter(string filter)
    {
        CurrentFilter = filter switch
        {
            "1" or "Today" => TaskFilter.Today,
            "2" or "Upcoming" => TaskFilter.Upcoming,
            "3" or "Overdue" => TaskFilter.Overdue,
            "4" or "Completed" => TaskFilter.Completed,
            _ => TaskFilter.All
        };
        ApplyFilter();
    }

    #endregion

    #region Dialog Events

    /// <summary>
    /// Event raised when the add task dialog should be shown.
    /// </summary>
    public event EventHandler? AddTaskDialogRequested;

    /// <summary>
    /// Event raised when the edit task dialog should be shown.
    /// </summary>
    public event EventHandler<TaskDetail>? EditTaskDialogRequested;

    /// <summary>
    /// Requests the View to show the Add Task dialog.
    /// </summary>
    [RelayCommand]
    private void RequestAddTaskDialog()
    {
        AddTaskDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Collections

    /// <summary>
    /// All loaded tasks (before filtering).
    /// </summary>
    private ObservableCollection<TaskDetail> AllTasks { get; } = new();

    /// <summary>
    /// Filtered task list for display.
    /// </summary>
    public ObservableCollection<TaskDetail> FilteredTasks { get; } = new();

    /// <summary>
    /// Team members for assignment dropdown.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();

    #endregion

    #region Stats

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCountText))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TodayCountText))]
    private int _todayCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpcomingCountText))]
    private int _upcomingCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverdueCountText))]
    private int _overdueCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletedCountText))]
    private int _completedCount;

    public string TotalCountText => TotalCount.ToString();
    public string TodayCountText => TodayCount.ToString();
    public string UpcomingCountText => UpcomingCount.ToString();
    public string OverdueCountText => OverdueCount.ToString();
    public string CompletedCountText => CompletedCount.ToString();

    #endregion

    #region Selected Task (for detail flyout)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTask))]
    private TaskDetail? _selectedTask;

    [ObservableProperty]
    private bool _isTaskDetailOpen;

    [ObservableProperty]
    private TaskDetailTab _taskDetailTab = TaskDetailTab.Overview;

    public bool HasSelectedTask => SelectedTask != null;

    [RelayCommand]
    private void SelectTask(TaskDetail? task)
    {
        if (task == null)
        {
            SelectedTask = null;
            IsTaskDetailOpen = false;
            return;
        }

        if (SelectedTask?.Id == task.Id)
        {
            IsTaskDetailOpen = !IsTaskDetailOpen;
            if (!IsTaskDetailOpen)
                SelectedTask = null;
        }
        else
        {
            // Wire up IDetailEntity commands before setting the task
            task.CloseCommand = CloseTaskDetailCommand;
            task.EditCommand = new RelayCommand(() => EditTask(task));
            task.DeleteCommand = new AsyncRelayCommand(() => DeleteTaskAsync(task));
            
            SelectedTask = task;
            TaskDetailTab = TaskDetailTab.Overview;
            IsTaskDetailOpen = true;
        }
    }

    /// <summary>
    /// Selects a task by its ID, opening the detail flyout.
    /// Used for cross-tab navigation (e.g. from Briefing or notifications).
    /// </summary>
    public void SelectTaskById(Guid taskId)
    {
        var task = AllTasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            SelectTask(task);
        }
    }

    [RelayCommand]
    private void CloseTaskDetail()
    {
        IsTaskDetailOpen = false;
        SelectedTask = null;
    }

    [RelayCommand]
    private void SetTaskDetailTab(TaskDetailTab tab)
    {
        TaskDetailTab = tab;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedTask = null;
        IsTaskDetailOpen = false;
    }

    #endregion

    #region Project Linking
    
    /// <summary>
    /// Event raised when the project selector popover should be shown for linking.
    /// </summary>
    public event EventHandler? ProjectSelectorRequested;
    
    /// <summary>
    /// Whether the project selector popover is open.
    /// </summary>
    [ObservableProperty]
    private bool _isProjectSelectorOpen;
    
    /// <summary>
    /// Requests the View to show the project selector popover.
    /// </summary>
    [RelayCommand]
    private void ShowProjectSelector()
    {
        IsProjectSelectorOpen = true;
        ProjectSelectorRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Hides the project selector popover.
    /// </summary>
    public void HideProjectSelector()
    {
        IsProjectSelectorOpen = false;
    }
    
    /// <summary>
    /// Links the selected task to a project.
    /// Called by the View when a project is selected in the popover.
    /// </summary>
    public async Task LinkTaskToProjectAsync(Guid projectId, string projectTitle)
    {
        if (SelectedTask == null) return;
        
        try
        {
            IsLoading = true;
            
            // If already linked to a different project, remove old link first
            if (SelectedTask.ProjectId.HasValue && SelectedTask.ProjectId != projectId)
            {
                await ProjectService.Instance.RemoveProjectLinkAsync(
                    SelectedTask.ProjectId.Value,
                    "task",
                    SelectedTask.Id);
            }
            
            // Add new link
            var link = await ProjectService.Instance.AddProjectLinkAsync(
                projectId,
                "task",
                SelectedTask.Id,
                SelectedTask.Title);
            
            if (link != null)
            {
                // Update local state
                SelectedTask.ProjectId = projectId;
                SelectedTask.ProjectTitle = projectTitle;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedTask));
                
                NotificationService.Instance.ShowSuccess(
                    "Task Linked", 
                    $"'{SelectedTask.Title}' linked to '{projectTitle}'");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Link Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
            IsProjectSelectorOpen = false;
        }
    }
    
    /// <summary>
    /// Unlinks the selected task from its project.
    /// </summary>
    [RelayCommand]
    private async Task UnlinkTaskFromProject()
    {
        if (SelectedTask?.ProjectId == null) return;
        
        try
        {
            IsLoading = true;
            
            var success = await ProjectService.Instance.RemoveProjectLinkAsync(
                SelectedTask.ProjectId.Value,
                "task",
                SelectedTask.Id);
            
            if (success)
            {
                var projectTitle = SelectedTask.ProjectTitle;
                
                // Update local state
                SelectedTask.ProjectId = null;
                SelectedTask.ProjectTitle = null;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedTask));
                
                NotificationService.Instance.ShowInfo(
                    "Task Unlinked", 
                    $"Removed from '{projectTitle}'");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            NotificationService.Instance.ShowError("Unlink Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    #endregion

    #region New Task

    /// <summary>
    /// Creates a task from dialog result.
    /// Called from code-behind after dialog closes.
    /// </summary>
    public async Task<bool> CreateTaskFromDialogAsync(
        string title,
        string? description,
        string? priority,
        DateTime? dueDate,
        Guid? assigneeId)
    {
        try
        {
            var task = await TaskService.Instance.CreateTaskAsync(
                title: title,
                description: description,
                priority: priority,
                dueDate: dueDate,
                assignedTo: assigneeId
            );

            if (task != null)
            {
                // Add directly to collection - no full refresh needed
                AllTasks.Add(task);
                ApplyFilter(); // Update filtered view
                NotificationService.Instance.ShowSuccess("Task Created", $"'{task.Title}' has been added.");
                return true;
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError ?? "Failed to create task";
                HasError = true;
                NotificationService.Instance.ShowError("Create Failed", ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            NotificationService.Instance.ShowError("Create Failed", ex.Message);
            return false;
        }
    }

    #endregion

    #region Task Actions

    [RelayCommand]
    private async Task ToggleTaskCompleteAsync(TaskDetail task)
    {
        try
        {
            bool success;
            if (task.IsCompleted)
            {
                success = await TaskService.Instance.UncompleteTaskAsync(task.Id);
            }
            else
            {
                success = await TaskService.Instance.CompleteTaskAsync(task.Id);
            }

            if (success)
            {
                await LoadTasksAsync();
                var status = task.IsCompleted ? "marked incomplete" : "completed";
                NotificationService.Instance.ShowSuccess("Task Updated", $"'{task.Title}' has been {status}.");
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError;
                HasError = true;
                NotificationService.Instance.ShowError("Update Failed", ErrorMessage ?? "Failed to update task");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            NotificationService.Instance.ShowError("Update Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(TaskDetail task)
    {
        // Show confirmation dialog for destructive action
        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Task",
            $"Are you sure you want to delete '{task.Title}'? This action cannot be undone.",
            "Delete Task",
            "Cancel");
        
        if (!confirmed)
            return;

        try
        {
            var taskTitle = task.Title;
            var success = await TaskService.Instance.DeleteTaskAsync(task.Id);
            if (success)
            {
                if (SelectedTask?.Id == task.Id)
                {
                    SelectedTask = null;
                    IsTaskDetailOpen = false;
                }
                await LoadTasksAsync();
                NotificationService.Instance.ShowSuccess("Task Deleted", $"'{taskTitle}' has been removed.");
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError;
                HasError = true;
                NotificationService.Instance.ShowError("Delete Failed", ErrorMessage ?? "Failed to delete task");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
        }
    }

    [RelayCommand]
    private void EditTask(TaskDetail? task)
    {
        if (task == null) return;
        Log($"[TasksViewModel] Edit task requested: {task.Title}");
        EditTaskDialogRequested?.Invoke(this, task);
    }

    #endregion

    #region Task Callbacks

    /// <summary>
    /// Called by the View when a task is saved from the dialog.
    /// </summary>
    public void OnTaskSaved(TaskDetail task)
    {
        Log($"[TasksViewModel] Task saved: {task.Title}");
        
        var existing = AllTasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
        {
            AllTasks.Add(task);
        }
        else
        {
            var index = AllTasks.IndexOf(existing);
            AllTasks[index] = task;
        }
        
        ApplyFilter();
    }

    /// <summary>
    /// Called by the View when a task is deleted from the dialog.
    /// </summary>
    public void OnTaskDeleted(Guid taskId)
    {
        Log($"[TasksViewModel] Task deleted: {taskId}");
        
        var existing = AllTasks.FirstOrDefault(t => t.Id == taskId);
        if (existing != null)
        {
            AllTasks.Remove(existing);
        }
        
        if (SelectedTask?.Id == taskId)
        {
            SelectedTask = null;
            IsTaskDetailOpen = false;
        }
        
        ApplyFilter();
    }

    #endregion

    #region Surface Activation

    /// <summary>
    /// Timestamp of last data load to support staleness checks.
    /// </summary>
    private DateTime _lastLoadTimestamp = DateTime.MinValue;

    /// <summary>
    /// Whether the surface has been marked dirty by external edits.
    /// </summary>
    private bool _isDirty;

    /// <summary>
    /// Staleness threshold - if last refresh exceeds this, force refresh.
    /// Browse pages use 30 minutes (same as Me).
    /// </summary>
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Called when the Tasks surface is activated (navigated to).
    /// This is the single entry point for refresh logic.
    /// Idempotent and safe to call repeatedly.
    /// </summary>
    public void OnSurfaceActivated()
    {
        Log("[TasksViewModel] OnSurfaceActivated called");
        
        // If already loading, don't trigger another load
        if (IsLoading || _isLoadingData)
        {
            Log("[TasksViewModel] OnSurfaceActivated: already loading, skipping");
            return;
        }
        
        // If data has never been loaded, trigger initial load
        if (_lastLoadTimestamp == DateTime.MinValue)
        {
            Log("[TasksViewModel] OnSurfaceActivated: first activation, triggering initial load");
            _ = LoadDataWithStatusAsync();
            return;
        }
        
        // Check for staleness
        var now = DateTime.UtcNow;
        var isStale = (now - _lastLoadTimestamp) > StalenessThreshold;
        
        if (isStale)
        {
            Log($"[TasksViewModel] OnSurfaceActivated: data is stale, triggering background refresh");
            _ = LoadDataWithStatusAsync();
            return;
        }
        
        // If marked dirty by external edits, trigger background refresh
        if (_isDirty)
        {
            Log("[TasksViewModel] OnSurfaceActivated: dirty flag set, triggering background refresh");
            _isDirty = false;
            _ = LoadDataWithStatusAsync();
            return;
        }
        
        // Data already loaded, fresh, and not dirty - render cached data immediately
        Log("[TasksViewModel] OnSurfaceActivated: using cached data");
    }

    /// <summary>
    /// Marks the surface as dirty, requiring refresh on next activation.
    /// Called when tasks are edited elsewhere (e.g., flyouts, other surfaces).
    /// </summary>
    public void MarkDirty()
    {
        Log("[TasksViewModel] MarkDirty called");
        _isDirty = true;
    }

    /// <summary>
    /// Internal load method with RefreshStatus integration.
    /// </summary>
    private async Task LoadDataWithStatusAsync()
    {
        // Cancel any pending update delay timer
        _updateDelayTokenSource?.Cancel();
        _updateDelayTokenSource = new CancellationTokenSource();
        
        // Start the 400ms delay timer for showing "Updating..." status
        _ = ShowUpdatingStatusAfterDelayAsync(_updateDelayTokenSource.Token);
        
        try
        {
            await LoadTasksAsync();
            
            // Update timestamp on successful load
            _lastLoadTimestamp = DateTime.UtcNow;
            
            // Cancel the delay timer (if load completed quickly)
            _updateDelayTokenSource.Cancel();
            
            // Show "Updated" briefly, then fade to Idle
            RefreshStatus = RefreshStatus.Updated;
            _ = FadeRefreshStatusToIdleAsync();
        }
        catch (Exception ex)
        {
            Log($"[TasksViewModel] LoadDataWithStatusAsync error: {ex.Message}");
            _updateDelayTokenSource?.Cancel();
            RefreshStatus = RefreshStatus.Idle;
        }
    }

    /// <summary>
    /// Shows "Updating..." status after 400ms delay to avoid flicker on fast loads.
    /// </summary>
    private async Task ShowUpdatingStatusAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(400, cancellationToken);
            
            // Only show Updating if still loading (not already completed)
            if (IsLoading || _isLoadingData)
            {
                RefreshStatus = RefreshStatus.Updating;
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled (load completed quickly), ignore
        }
    }

    /// <summary>
    /// Fades the refresh status back to Idle after showing "Updated".
    /// </summary>
    private async Task FadeRefreshStatusToIdleAsync()
    {
        // Show "Updated" for 2 seconds, then fade to Idle
        await Task.Delay(2000);
        
        // Only transition to Idle if still showing Updated (not loading again)
        if (RefreshStatus == RefreshStatus.Updated)
        {
            RefreshStatus = RefreshStatus.Idle;
        }
    }

    #endregion

    public TasksViewModel()
    {
        Log("[TasksViewModel] Constructor called");
        
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Don't load in constructor - let the View trigger load when visible
    }

    private void OnProfileChanged(object? sender, UserProfile? profile)
    {
        Log($"[TasksViewModel] ProfileChanged: {profile?.Email ?? "NULL"}");
        if (profile != null)
        {
            _ = LoadTasksAsync();
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProCohere", "tasks_view.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch { }
        System.Diagnostics.Debug.WriteLine(message);
    }

    /// <summary>
    /// Loads tasks from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadTasksAsync()
    {
        // Prevent concurrent loads (but allow the initial load when _isLoadingData is false)
        if (_isLoadingData) return;
        _isLoadingData = true;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;
            Log("[TasksViewModel] Loading tasks...");

            // Load all tasks (including completed for stats)
            var tasks = await TaskService.Instance.GetTasksAsync(includeCompleted: true);
            Log($"[TasksViewModel] Loaded {tasks.Count} tasks");

            // Load team members for enrichment
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            var memberDict = dashboardData.TeamMembers.ToDictionary(m => m.Id);

            // Update team members collection
            TeamMembers.Clear();
            foreach (var member in dashboardData.TeamMembers)
            {
                TeamMembers.Add(member);
            }

            // Enrich tasks with owner names
            foreach (var task in tasks)
            {
                if (task.OwnerTeamMemberId.HasValue && 
                    memberDict.TryGetValue(task.OwnerTeamMemberId.Value, out var owner))
                {
                    task.OwnerName = owner.FullName;
                }
            }

            // Update stats
            var today = DateTime.UtcNow.Date;
            var incompleteTasks = tasks.Where(t => t.Status != "completed").ToList();
            
            TotalCount = incompleteTasks.Count;
            TodayCount = incompleteTasks.Count(t => t.DueDate?.Date == today);
            UpcomingCount = incompleteTasks.Count(t => !t.DueDate.HasValue || t.DueDate.Value.Date > today);
            OverdueCount = incompleteTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today);
            CompletedCount = tasks.Count(t => t.Status == "completed");

            // Store all tasks
            AllTasks.Clear();
            foreach (var task in tasks)
            {
                AllTasks.Add(task);
            }

            // Apply filter
            ApplyFilter();

            Log($"[TasksViewModel] Stats: {TotalCount} total, {TodayCount} today, {UpcomingCount} upcoming, {OverdueCount} overdue, {CompletedCount} completed");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Log($"[TasksViewModel] ERROR: {ex.Message}");
            Console.WriteLine($"[TasksViewModel] ERROR: {ex.Message}");
        }
        finally
        {
            _isLoadingData = false;
            IsLoading = false;
            
            Console.WriteLine($"[TasksViewModel] LoadTasksAsync complete: IsLoading={IsLoading}, ShowLoading={ShowLoading}, FilteredTasks.Count={FilteredTasks.Count}");
            
            // Re-notify computed properties
            OnPropertyChanged(nameof(ShowLoading));
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowTaskList));
        }
    }

    private void ApplyFilter()
    {
        FilteredTasks.Clear();
        var today = DateTime.UtcNow.Date;

        var filtered = CurrentFilter switch
        {
            TaskFilter.Today => AllTasks
                .Where(t => t.Status != "completed" && t.DueDate?.Date == today)
                .OrderBy(t => t.DueDate),
            TaskFilter.Upcoming => AllTasks
                .Where(t => t.Status != "completed" && (!t.DueDate.HasValue || t.DueDate.Value.Date > today))
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue),
            TaskFilter.Overdue => AllTasks
                .Where(t => t.Status != "completed" && t.DueDate.HasValue && t.DueDate.Value.Date < today)
                .OrderBy(t => t.DueDate),
            TaskFilter.Completed => AllTasks
                .Where(t => t.Status == "completed")
                .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt),
            _ => AllTasks // All (incomplete)
                .Where(t => t.Status != "completed")
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
        };

        foreach (var task in filtered)
        {
            FilteredTasks.Add(task);
        }

        Log($"[TasksViewModel] Filter applied: {CurrentFilter}, showing {FilteredTasks.Count} tasks");
        
        // Notify computed properties that depend on FilteredTasks.Count
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowTaskList));
    }

    /// <summary>
    /// Refreshes task list. Public for View to call when becoming visible.
    /// </summary>
    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadTasksAsync();
    }
}

/// <summary>
/// Tabs within the task detail flyout.
/// </summary>
public enum TaskDetailTab
{
    Overview,
    Activity
}

/// <summary>
/// Task filter options.
/// </summary>
public enum TaskFilter
{
    All,
    Today,
    Upcoming,
    Overdue,
    Completed
}
