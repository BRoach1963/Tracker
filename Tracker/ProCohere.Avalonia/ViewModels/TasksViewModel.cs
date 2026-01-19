using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Filter

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterToday))]
    [NotifyPropertyChangedFor(nameof(IsFilterOverdue))]
    [NotifyPropertyChangedFor(nameof(IsFilterCompleted))]
    [NotifyPropertyChangedFor(nameof(FilteredTasks))]
    private TaskFilter _currentFilter = TaskFilter.All;

    public bool IsFilterAll => CurrentFilter == TaskFilter.All;
    public bool IsFilterToday => CurrentFilter == TaskFilter.Today;
    public bool IsFilterOverdue => CurrentFilter == TaskFilter.Overdue;
    public bool IsFilterCompleted => CurrentFilter == TaskFilter.Completed;

    [RelayCommand]
    private async Task SetFilter(string filter)
    {
        CurrentFilter = filter switch
        {
            "Today" => TaskFilter.Today,
            "Overdue" => TaskFilter.Overdue,
            "Completed" => TaskFilter.Completed,
            _ => TaskFilter.All
        };
        await LoadTasksAsync();
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
    [NotifyPropertyChangedFor(nameof(OverdueCountText))]
    private int _overdueCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletedCountText))]
    private int _completedCount;

    public string TotalCountText => TotalCount.ToString();
    public string TodayCountText => TodayCount.ToString();
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
            SelectedTask = task;
            TaskDetailTab = TaskDetailTab.Overview;
            IsTaskDetailOpen = true;
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

    #region New Task Fields

    [ObservableProperty]
    private bool _isAddingTask;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private string? _newTaskDescription;

    [ObservableProperty]
    private string? _newTaskPriority;

    [ObservableProperty]
    private DateTime? _newTaskDueDate;

    [ObservableProperty]
    private Guid? _newTaskAssignee;

    [RelayCommand]
    private void StartAddTask()
    {
        IsAddingTask = true;
        NewTaskTitle = string.Empty;
        NewTaskDescription = null;
        NewTaskPriority = null;
        NewTaskDueDate = DateTime.Now.AddDays(1); // Default to tomorrow
        NewTaskAssignee = null;
    }

    [RelayCommand]
    private void CancelAddTask()
    {
        IsAddingTask = false;
        NewTaskTitle = string.Empty;
    }

    [RelayCommand]
    private async Task SaveNewTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle))
            return;

        try
        {
            var task = await TaskService.Instance.CreateTaskAsync(
                title: NewTaskTitle.Trim(),
                description: NewTaskDescription,
                priority: NewTaskPriority,
                dueDate: NewTaskDueDate,
                assignedTo: NewTaskAssignee
            );

            if (task != null)
            {
                IsAddingTask = false;
                await LoadTasksAsync();
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError ?? "Failed to create task";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
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
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(TaskDetail task)
    {
        try
        {
            var success = await TaskService.Instance.DeleteTaskAsync(task.Id);
            if (success)
            {
                if (SelectedTask?.Id == task.Id)
                {
                    SelectedTask = null;
                    IsTaskDetailOpen = false;
                }
                await LoadTasksAsync();
            }
            else
            {
                ErrorMessage = TaskService.Instance.LastError;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
    }

    [RelayCommand]
    private void EditTask(TaskDetail? task)
    {
        // TODO: Implement edit task dialog
        // For now, just log the action
        Log($"[TasksViewModel] Edit task requested: {task?.Title}");
    }

    #endregion

    public TasksViewModel()
    {
        Log("[TasksViewModel] Constructor called");
        
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Only load data if profile is already available
        if (AuthService.Instance.CurrentProfile != null)
        {
            Log("[TasksViewModel] Profile available, loading tasks");
            _ = LoadTasksAsync();
        }
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
        if (IsLoading) return;

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

            Log($"[TasksViewModel] Stats: {TotalCount} total, {TodayCount} today, {OverdueCount} overdue, {CompletedCount} completed");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Log($"[TasksViewModel] ERROR: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
    }

    /// <summary>
    /// Refreshes task list.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
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
    Overdue,
    Completed
}
