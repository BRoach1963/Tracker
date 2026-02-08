using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Goals sub-tab within Pulse.
/// Implements narrative-first, discussion-driven goal management.
/// 
/// Philosophy: "Goals express intent, Metrics observe reality, Humans decide."
/// NO progress bars, percentages, or red/yellow/green status indicators.
/// </summary>
public partial class GoalsViewModel : ViewModelBase
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowGoalsList))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowGoalsList))]
    private string? _errorMessage;

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
    /// True when not loading, no error, and Goals collection is empty.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && string.IsNullOrEmpty(ErrorMessage) && Goals.Count == 0;
    
    /// <summary>
    /// True when not loading, no error, and Goals collection has items.
    /// </summary>
    public bool ShowGoalsList => !IsLoading && string.IsNullOrEmpty(ErrorMessage) && Goals.Count > 0;

    #endregion

    #region Scope Filter

    /// <summary>
    /// Goal scope filter: 0=My Goals, 1=Team Goals
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScopeMyGoals))]
    [NotifyPropertyChangedFor(nameof(IsScopeTeamGoals))]
    private int _selectedScope = 0;

    public bool IsScopeMyGoals => SelectedScope == 0;
    public bool IsScopeTeamGoals => SelectedScope == 1;

    [RelayCommand]
    private async Task SetScope(string scopeIndex)
    {
        if (int.TryParse(scopeIndex, out var index))
        {
            SelectedScope = index;
            await LoadGoalsAsync();
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// Active goals for display.
    /// </summary>
    public ObservableCollection<GoalDetail> Goals { get; } = new();

    #endregion

    #region Selection State

    [ObservableProperty]
    private GoalDetail? _selectedGoal;

    [ObservableProperty]
    private bool _isDetailFlyoutOpen;

    [ObservableProperty]
    private bool _isEditorFlyoutOpen;

    [ObservableProperty]
    private GoalDetail? _editingGoal;

    /// <summary>
    /// Detail tab: 0=Details, 1=Trajectory
    /// </summary>
    [ObservableProperty]
    private int _detailTab = 0;

    [RelayCommand]
    private void SetDetailTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            DetailTab = index;
        }
    }

    #endregion

    #region Trajectory State

    /// <summary>
    /// Trajectory prediction for selected goal.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrajectory))]
    [NotifyPropertyChangedFor(nameof(TrajectoryStatusDisplay))]
    [NotifyPropertyChangedFor(nameof(TrajectoryProbabilityDisplay))]
    private TrajectoryResult? _trajectory;

    [ObservableProperty]
    private bool _isLoadingTrajectory;

    /// <summary>
    /// Whether trajectory data is available.
    /// </summary>
    public bool HasTrajectory => Trajectory != null && Trajectory.Status != TrajectoryStatus.Unknown;

    /// <summary>
    /// Trajectory status for display.
    /// </summary>
    public string TrajectoryStatusDisplay => Trajectory?.StatusDisplay ?? "Unknown";

    /// <summary>
    /// Probability display.
    /// </summary>
    public string TrajectoryProbabilityDisplay => Trajectory?.ProbabilityDisplay ?? "--";

    #endregion

    #region Health/Lifecycle Dialog State

    [ObservableProperty]
    private bool _isHealthDialogOpen;

    [ObservableProperty]
    private bool _isLifecycleDialogOpen;

    [ObservableProperty]
    private GoalHealth _selectedHealth;

    [ObservableProperty]
    private GoalLifecycle _selectedLifecycle;

    [ObservableProperty]
    private string _healthReason = string.Empty;

    [ObservableProperty]
    private string _lifecycleReason = string.Empty;

    [ObservableProperty]
    private Guid? _supersededById;

    /// <summary>
    /// Available goals for "Superseded By" picker (excludes current goal).
    /// </summary>
    public ObservableCollection<GoalDetail> AvailableGoalsForSupersede { get; } = new();

    /// <summary>
    /// Health options for the picker.
    /// </summary>
    public static IReadOnlyList<GoalHealth> HealthOptions { get; } = new[]
    {
        GoalHealth.OnTrack,
        GoalHealth.NeedsAttention,
        GoalHealth.AtRisk,
        GoalHealth.ReframingNeeded
    };

    /// <summary>
    /// Lifecycle options for the picker.
    /// </summary>
    public static IReadOnlyList<GoalLifecycle> LifecycleOptions { get; } = new[]
    {
        GoalLifecycle.Active,
        GoalLifecycle.Evolving,
        GoalLifecycle.Paused,
        GoalLifecycle.Superseded,
        GoalLifecycle.Retired
    };

    /// <summary>
    /// Reflection prompt for current health selection.
    /// </summary>
    public string HealthReflectionPrompt => SelectedHealth.GetReflectionPrompt();

    /// <summary>
    /// Reflection prompt for current lifecycle selection.
    /// </summary>
    public string LifecycleReflectionPrompt => SelectedLifecycle.GetReflectionPrompt();

    /// <summary>
    /// Whether the superseded by picker should be visible.
    /// </summary>
    public bool ShowSupersededByPicker => SelectedLifecycle == GoalLifecycle.Superseded;

    partial void OnSelectedHealthChanged(GoalHealth value)
    {
        OnPropertyChanged(nameof(HealthReflectionPrompt));
    }

    partial void OnSelectedLifecycleChanged(GoalLifecycle value)
    {
        OnPropertyChanged(nameof(LifecycleReflectionPrompt));
        OnPropertyChanged(nameof(ShowSupersededByPicker));
    }

    #endregion

    #region Stats

    [ObservableProperty]
    private int _activeGoalsCount;

    [ObservableProperty]
    private int _needsAttentionCount;

    [ObservableProperty]
    private int _atRiskCount;

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
    /// Called when the Goals surface is activated (navigated to).
    /// This is the single entry point for refresh logic.
    /// Idempotent and safe to call repeatedly.
    /// </summary>
    public void OnSurfaceActivated()
    {
        System.Diagnostics.Debug.WriteLine("[GoalsViewModel] OnSurfaceActivated called");
        
        // If already loading, don't trigger another load
        if (IsLoading)
        {
            System.Diagnostics.Debug.WriteLine("[GoalsViewModel] OnSurfaceActivated: already loading, skipping");
            return;
        }
        
        // If data has never been loaded, trigger initial load
        if (_lastLoadTimestamp == DateTime.MinValue)
        {
            System.Diagnostics.Debug.WriteLine("[GoalsViewModel] OnSurfaceActivated: first activation, triggering initial load");
            _ = LoadDataAsync();
            return;
        }
        
        // Check for staleness
        var now = DateTime.UtcNow;
        var isStale = (now - _lastLoadTimestamp) > StalenessThreshold;
        
        if (isStale)
        {
            System.Diagnostics.Debug.WriteLine($"[GoalsViewModel] OnSurfaceActivated: data is stale, triggering background refresh");
            _ = LoadDataAsync();
            return;
        }
        
        // If marked dirty by external edits, trigger background refresh
        if (_isDirty)
        {
            System.Diagnostics.Debug.WriteLine("[GoalsViewModel] OnSurfaceActivated: dirty flag set, triggering background refresh");
            _isDirty = false;
            _ = LoadDataAsync();
            return;
        }
        
        // Data already loaded, fresh, and not dirty - render cached data immediately
        System.Diagnostics.Debug.WriteLine("[GoalsViewModel] OnSurfaceActivated: using cached data");
    }

    /// <summary>
    /// Marks the surface as dirty, requiring refresh on next activation.
    /// Called when goals are edited elsewhere (e.g., flyouts, other surfaces).
    /// </summary>
    public void MarkDirty()
    {
        System.Diagnostics.Debug.WriteLine("[GoalsViewModel] MarkDirty called");
        _isDirty = true;
    }

    /// <summary>
    /// Internal load method with RefreshStatus integration.
    /// </summary>
    private async Task LoadDataAsync()
    {
        // Cancel any pending update delay timer
        _updateDelayTokenSource?.Cancel();
        _updateDelayTokenSource = new CancellationTokenSource();
        
        // Start the 400ms delay timer for showing "Updating..." status
        _ = ShowUpdatingStatusAfterDelayAsync(_updateDelayTokenSource.Token);
        
        try
        {
            await LoadGoalsAsync();
            
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
            System.Diagnostics.Debug.WriteLine($"[GoalsViewModel] LoadDataAsync error: {ex.Message}");
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
            if (IsLoading)
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

    public GoalsViewModel()
    {
        // Don't load in constructor - let the View trigger load when visible
    }
    
    /// <summary>
    /// Public method to trigger data refresh. Called by View when it becomes visible.
    /// </summary>
    public Task RefreshAsync() => LoadGoalsAsync();

    #region Commands

    [RelayCommand]
    private async Task LoadGoalsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            List<GoalDetail> goals = SelectedScope switch
            {
                0 => await GoalsService.Instance.GetMyGoalsAsync(),
                1 => await GoalsService.Instance.GetTeamGoalsAsync(),
                _ => new List<GoalDetail>()
            };

            // Only show active lifecycle goals by default
            goals = goals.Where(g => g.Lifecycle.IsActionable()).ToList();

            Goals.Clear();
            foreach (var goal in goals)
            {
                Goals.Add(goal);
            }
            
            // Notify computed state properties
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowGoalsList));

            // Update stats
            ActiveGoalsCount = goals.Count(g => g.Health == GoalHealth.OnTrack);
            NeedsAttentionCount = goals.Count(g => g.Health == GoalHealth.NeedsAttention);
            AtRiskCount = goals.Count(g => g.Health == GoalHealth.AtRisk || g.Health == GoalHealth.ReframingNeeded);

            if (!string.IsNullOrEmpty(GoalsService.Instance.LastError))
            {
                ErrorMessage = GoalsService.Instance.LastError;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load goals: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectGoal(GoalDetail goal)
    {
        SelectedGoal = goal;
        DetailTab = 0;
        IsDetailFlyoutOpen = true;
        IsEditorFlyoutOpen = false;

        // Load trajectory in background
        await LoadTrajectoryAsync();
    }

    /// <summary>
    /// Selects a goal by its ID, opening the detail flyout.
    /// Used for cross-tab navigation.
    /// </summary>
    public async Task SelectGoalByIdAsync(Guid goalId)
    {
        var goal = Goals.FirstOrDefault(g => g.Id == goalId);
        if (goal != null)
        {
            await SelectGoal(goal);
        }
    }

    [RelayCommand]
    private void CloseDetailFlyout()
    {
        IsDetailFlyoutOpen = false;
        SelectedGoal = null;
        Trajectory = null;
    }

    [RelayCommand]
    private async Task LoadTrajectoryAsync()
    {
        if (SelectedGoal == null) return;

        IsLoadingTrajectory = true;
        try
        {
            var trajectory = await GoalsService.Instance.GetGoalTrajectoryAsync(SelectedGoal.Id);
            Trajectory = trajectory;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load trajectory: {ex.Message}";
            Trajectory = null;
        }
        finally
        {
            IsLoadingTrajectory = false;
        }
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
    /// Links the selected goal to a project.
    /// Called by the View when a project is selected in the popover.
    /// </summary>
    public async Task LinkGoalToProjectAsync(Guid projectId, string projectTitle)
    {
        if (SelectedGoal == null) return;
        
        try
        {
            IsLoading = true;
            
            // If already linked to a different project, remove old link first
            if (SelectedGoal.ProjectId.HasValue && SelectedGoal.ProjectId != projectId)
            {
                await ProjectService.Instance.RemoveProjectLinkAsync(
                    SelectedGoal.ProjectId.Value,
                    "goal",
                    SelectedGoal.Id);
            }
            
            // Add new link
            var link = await ProjectService.Instance.AddProjectLinkAsync(
                projectId,
                "goal",
                SelectedGoal.Id,
                SelectedGoal.Title);
            
            if (link != null)
            {
                // Update local state
                SelectedGoal.ProjectId = projectId;
                SelectedGoal.ProjectTitle = projectTitle;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedGoal));
                
                NotificationService.Instance.ShowSuccess(
                    "Goal Linked", 
                    $"'{SelectedGoal.Title}' linked to '{projectTitle}'");
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
    /// Unlinks the selected goal from its project.
    /// </summary>
    [RelayCommand]
    private async Task UnlinkGoalFromProject()
    {
        if (SelectedGoal?.ProjectId == null) return;
        
        try
        {
            IsLoading = true;
            
            var success = await ProjectService.Instance.RemoveProjectLinkAsync(
                SelectedGoal.ProjectId.Value,
                "goal",
                SelectedGoal.Id);
            
            if (success)
            {
                var projectTitle = SelectedGoal.ProjectTitle;
                
                // Update local state
                SelectedGoal.ProjectId = null;
                SelectedGoal.ProjectTitle = null;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedGoal));
                
                NotificationService.Instance.ShowInfo(
                    "Goal Unlinked", 
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

    #region Create/Edit Goal

    [RelayCommand]
    private void CreateNewGoal()
    {
        // Set visibility scope based on which tab is selected
        var visibilityScope = SelectedScope switch
        {
            1 => "team",    // Team Goals tab
            _ => "personal" // My Goals tab (default)
        };
        
        EditingGoal = new GoalDetail
        {
            Id = Guid.Empty,
            Title = string.Empty,
            Description = string.Empty,
            Health = GoalHealth.OnTrack,
            Lifecycle = GoalLifecycle.Active,
            VisibilityScope = visibilityScope
        };
        IsEditorFlyoutOpen = true;
        IsDetailFlyoutOpen = false;
    }

    [RelayCommand]
    private void EditGoal(GoalDetail? goal)
    {
        if (goal == null) return;
        EditingGoal = goal;
        IsEditorFlyoutOpen = true;
        IsDetailFlyoutOpen = false;
    }

    [RelayCommand]
    private void CloseEditorFlyout()
    {
        IsEditorFlyoutOpen = false;
        EditingGoal = null;
    }

    [RelayCommand]
    private async Task SaveGoalAsync()
    {
        if (EditingGoal == null) return;

        try
        {
            IsLoading = true;

            if (EditingGoal.Id == Guid.Empty)
            {
                var created = await GoalsService.Instance.CreateGoalAsync(EditingGoal);
                if (created != null)
                {
                    Goals.Insert(0, created);
                    UpdateStats();
                    NotificationService.Instance.ShowSuccess("Goal Created", $"'{created.Title}' has been added.");
                }
            }
            else
            {
                var updated = await GoalsService.Instance.UpdateGoalAsync(EditingGoal);
                if (updated != null)
                {
                    // Replace in collection
                    var index = Goals.IndexOf(Goals.FirstOrDefault(g => g.Id == updated.Id)!);
                    if (index >= 0)
                    {
                        Goals[index] = updated;
                    }
                    
                    // Update selected goal if it's the same
                    if (SelectedGoal?.Id == updated.Id)
                    {
                        SelectedGoal = updated;
                    }
                    
                    UpdateStats();
                    NotificationService.Instance.ShowSuccess("Goal Updated", $"'{updated.Title}' has been saved.");
                }
            }

            if (!string.IsNullOrEmpty(GoalsService.Instance.LastError))
            {
                ErrorMessage = GoalsService.Instance.LastError;
            }
            else
            {
                CloseEditorFlyout();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save goal: {ex.Message}";
            NotificationService.Instance.ShowError("Save Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteGoalAsync(GoalDetail? goal)
    {
        if (goal == null) return;

        // Show confirmation dialog for destructive action
        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Goal",
            $"Are you sure you want to delete '{goal.Title}'? This action cannot be undone.",
            "Delete Goal",
            "Cancel");
        
        if (!confirmed)
            return;

        try
        {
            var success = await GoalsService.Instance.DeleteGoalAsync(goal.Id);

            if (success)
            {
                Goals.Remove(goal);
                if (SelectedGoal?.Id == goal.Id)
                {
                    CloseDetailFlyout();
                }
                UpdateStats();
                NotificationService.Instance.ShowSuccess("Goal Deleted", $"'{goal.Title}' has been removed.");
            }
            else if (!string.IsNullOrEmpty(GoalsService.Instance.LastError))
            {
                ErrorMessage = GoalsService.Instance.LastError;
                NotificationService.Instance.ShowError("Delete Failed", GoalsService.Instance.LastError);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete goal: {ex.Message}";
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
        }
    }

    #endregion

    #region Health Change Commands

    [RelayCommand]
    private void OpenHealthDialog()
    {
        if (SelectedGoal == null) return;
        
        SelectedHealth = SelectedGoal.Health;
        HealthReason = string.Empty;
        IsHealthDialogOpen = true;
    }

    [RelayCommand]
    private void SelectHealth(GoalHealth health)
    {
        SelectedHealth = health;
    }

    [RelayCommand]
    private void CancelHealthDialog()
    {
        IsHealthDialogOpen = false;
        HealthReason = string.Empty;
    }

    [RelayCommand]
    private void CloseHealthDialog()
    {
        IsHealthDialogOpen = false;
        HealthReason = string.Empty;
    }

    [RelayCommand]
    private async Task SaveHealthChangeAsync()
    {
        if (SelectedGoal == null || string.IsNullOrWhiteSpace(HealthReason)) return;

        try
        {
            IsLoading = true;

            var updated = await GoalsService.Instance.UpdateHealthAsync(
                SelectedGoal.Id,
                SelectedHealth,
                HealthReason);

            if (updated != null)
            {
                // Update in collection
                var index = Goals.IndexOf(Goals.FirstOrDefault(g => g.Id == updated.Id)!);
                if (index >= 0)
                {
                    Goals[index] = updated;
                }
                
                SelectedGoal = updated;
                UpdateStats();
                CloseHealthDialog();
            }
            else if (!string.IsNullOrEmpty(GoalsService.Instance.LastError))
            {
                ErrorMessage = GoalsService.Instance.LastError;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update health: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Lifecycle Change Commands

    [RelayCommand]
    private async Task OpenLifecycleDialogAsync()
    {
        if (SelectedGoal == null) return;
        
        SelectedLifecycle = SelectedGoal.Lifecycle;
        LifecycleReason = string.Empty;
        SupersededById = null;
        
        // Load available goals for supersede picker
        AvailableGoalsForSupersede.Clear();
        var allGoals = await GoalsService.Instance.GetMyGoalsAsync();
        foreach (var goal in allGoals.Where(g => g.Id != SelectedGoal.Id && g.Lifecycle == GoalLifecycle.Active))
        {
            AvailableGoalsForSupersede.Add(goal);
        }
        
        IsLifecycleDialogOpen = true;
    }

    [RelayCommand]
    private void SelectLifecycle(GoalLifecycle lifecycle)
    {
        SelectedLifecycle = lifecycle;
    }

    [RelayCommand]
    private void CancelLifecycleDialog()
    {
        IsLifecycleDialogOpen = false;
        LifecycleReason = string.Empty;
        SupersededById = null;
    }

    [RelayCommand]
    private void CloseLifecycleDialog()
    {
        IsLifecycleDialogOpen = false;
        LifecycleReason = string.Empty;
        SupersededById = null;
    }

    [RelayCommand]
    private async Task SaveLifecycleChangeAsync()
    {
        if (SelectedGoal == null || string.IsNullOrWhiteSpace(LifecycleReason)) return;

        // Superseded requires a replacement goal ID
        if (SelectedLifecycle == GoalLifecycle.Superseded && SupersededById == null)
        {
            ErrorMessage = "Please select the replacement goal";
            return;
        }

        try
        {
            IsLoading = true;

            var updated = await GoalsService.Instance.UpdateLifecycleAsync(
                SelectedGoal.Id,
                SelectedLifecycle,
                LifecycleReason,
                SupersededById);

            if (updated != null)
            {
                // If goal is now terminal, remove from active list
                if (SelectedLifecycle.IsTerminal())
                {
                    Goals.Remove(Goals.FirstOrDefault(g => g.Id == updated.Id)!);
                    CloseDetailFlyout();
                }
                else
                {
                    // Update in collection
                    var index = Goals.IndexOf(Goals.FirstOrDefault(g => g.Id == updated.Id)!);
                    if (index >= 0)
                    {
                        Goals[index] = updated;
                    }
                    SelectedGoal = updated;
                }
                
                UpdateStats();
                CloseLifecycleDialog();
            }
            else if (!string.IsNullOrEmpty(GoalsService.Instance.LastError))
            {
                ErrorMessage = GoalsService.Instance.LastError;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update lifecycle: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Helpers

    private void UpdateStats()
    {
        ActiveGoalsCount = Goals.Count(g => g.Health == GoalHealth.OnTrack);
        NeedsAttentionCount = Goals.Count(g => g.Health == GoalHealth.NeedsAttention);
        AtRiskCount = Goals.Count(g => g.Health == GoalHealth.AtRisk || g.Health == GoalHealth.ReframingNeeded);
    }

    #endregion
}
