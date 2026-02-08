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
/// ViewModel for the Metrics sub-tab within Pulse.
/// Implements signals-not-targets philosophy for metric observation.
/// 
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// - Display DIRECTIONAL TRENDS (↗ → ↘), not numeric values by default
/// - NO progress bars, percentages, or red/yellow/green status indicators
/// - Metrics inform but never determine goal health
/// </summary>
public partial class MetricsViewModel : ViewModelBase
{
    #region Navigation Events

    /// <summary>
    /// Event raised when user wants to navigate back to Pulse.
    /// </summary>
    public event EventHandler? NavigateBackRequested;

    [RelayCommand]
    private void NavigateBack() => NavigateBackRequested?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Dialog Events
    
    /// <summary>
    /// Raised when the create metric dialog should be shown.
    /// </summary>
    public event EventHandler? CreateMetricDialogRequested;
    
    /// <summary>
    /// Raised when the edit metric dialog should be shown.
    /// </summary>
    public event EventHandler<MetricDetail>? EditMetricDialogRequested;
    
    /// <summary>
    /// Raised when the update value dialog should be shown.
    /// </summary>
    public event EventHandler<MetricDetail>? UpdateValueDialogRequested;
    
    #endregion
    
    #region Loading State

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowMetricsList))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    [NotifyPropertyChangedFor(nameof(ShowMetricsList))]
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
    /// True when not loading, no error, and Metrics collection is empty.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && string.IsNullOrEmpty(ErrorMessage) && Metrics.Count == 0;
    
    /// <summary>
    /// True when not loading, no error, and Metrics collection has items.
    /// </summary>
    public bool ShowMetricsList => !IsLoading && string.IsNullOrEmpty(ErrorMessage) && Metrics.Count > 0;

    #endregion

    #region Scope Filter

    /// <summary>
    /// Metric scope filter: 0=Individual, 1=Team, 2=Organization, 3=All
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScopeIndividual))]
    [NotifyPropertyChangedFor(nameof(IsScopeTeam))]
    [NotifyPropertyChangedFor(nameof(IsScopeOrganization))]
    [NotifyPropertyChangedFor(nameof(IsScopeAll))]
    private int _selectedScope = 3; // Default to All

    public bool IsScopeIndividual => SelectedScope == 0;
    public bool IsScopeTeam => SelectedScope == 1;
    public bool IsScopeOrganization => SelectedScope == 2;
    public bool IsScopeAll => SelectedScope == 3;

    [RelayCommand]
    private async Task SetScope(string scopeIndex)
    {
        if (int.TryParse(scopeIndex, out var index))
        {
            SelectedScope = index;
            await LoadMetricsAsync();
        }
    }

    #endregion

    #region Lifecycle Filter

    /// <summary>
    /// Lifecycle filter: null=All, otherwise specific lifecycle
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLifecycleAll))]
    [NotifyPropertyChangedFor(nameof(IsLifecycleActive))]
    [NotifyPropertyChangedFor(nameof(IsLifecycleDormant))]
    [NotifyPropertyChangedFor(nameof(IsLifecycleRetired))]
    private MetricLifecycle? _lifecycleFilter = MetricLifecycle.Active; // Default to Active

    public bool IsLifecycleAll => LifecycleFilter == null;
    public bool IsLifecycleActive => LifecycleFilter == MetricLifecycle.Active;
    public bool IsLifecycleDormant => LifecycleFilter == MetricLifecycle.Dormant;
    public bool IsLifecycleRetired => LifecycleFilter == MetricLifecycle.Retired;

    [RelayCommand]
    private async Task SetLifecycleFilter(string? lifecycle)
    {
        LifecycleFilter = lifecycle switch
        {
            "active" => MetricLifecycle.Active,
            "dormant" => MetricLifecycle.Dormant,
            "retired" => MetricLifecycle.Retired,
            _ => null
        };
        await LoadMetricsAsync();
    }

    #endregion

    #region Source Filter

    /// <summary>
    /// Source filter: null=All, otherwise specific source
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSourceAll))]
    [NotifyPropertyChangedFor(nameof(IsSourceSystem))]
    [NotifyPropertyChangedFor(nameof(IsSourceSurvey))]
    [NotifyPropertyChangedFor(nameof(IsSourceManual))]
    private MetricSource? _sourceFilter;

    public bool IsSourceAll => SourceFilter == null;
    public bool IsSourceSystem => SourceFilter == MetricSource.System;
    public bool IsSourceSurvey => SourceFilter == MetricSource.Survey;
    public bool IsSourceManual => SourceFilter == MetricSource.Manual;

    [RelayCommand]
    private async Task SetSourceFilter(string? source)
    {
        SourceFilter = source switch
        {
            "system" => MetricSource.System,
            "survey" => MetricSource.Survey,
            "manual" => MetricSource.Manual,
            _ => null
        };
        await LoadMetricsAsync();
    }

    #endregion

    #region Trend Filter

    /// <summary>
    /// Trend filter: null=All, otherwise specific trend direction
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTrendAll))]
    [NotifyPropertyChangedFor(nameof(IsTrendUp))]
    [NotifyPropertyChangedFor(nameof(IsTrendDown))]
    private MetricTrend? _trendFilter;

    public bool IsTrendAll => TrendFilter == null;
    public bool IsTrendUp => TrendFilter == MetricTrend.TrendingUp;
    public bool IsTrendDown => TrendFilter == MetricTrend.TrendingDown;

    [RelayCommand]
    private async Task SetTrendFilter(string? trend)
    {
        TrendFilter = trend switch
        {
            "up" => MetricTrend.TrendingUp,
            "down" => MetricTrend.TrendingDown,
            _ => null
        };
        await LoadMetricsAsync();
    }

    #endregion

    #region Search

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        // Debounce could be added here for performance
        _ = LoadMetricsAsync();
    }

    #endregion

    #region Collections

    /// <summary>
    /// Metrics for display (filtered by current scope/lifecycle/source).
    /// </summary>
    public ObservableCollection<MetricDetail> Metrics { get; } = new();

    /// <summary>
    /// History entries for the selected metric.
    /// </summary>
    public ObservableCollection<MetricHistoryEntry> MetricHistory { get; } = new();

    #endregion

    #region Selection State

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMetricTrendDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedMetricSourceDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedMetricScopeDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedMetricLifecycleDisplay))]
    [NotifyPropertyChangedFor(nameof(IsSelectedMetricManual))]
    [NotifyPropertyChangedFor(nameof(HasTrendAnalysis))]
    private MetricDetail? _selectedMetric;

    [ObservableProperty]
    private bool _isDetailFlyoutOpen;

    [ObservableProperty]
    private bool _isEditorFlyoutOpen;

    [ObservableProperty]
    private MetricDetail? _editingMetric;

    /// <summary>
    /// Detail tab: 0=Details, 1=History, 2=Trend Analysis
    /// </summary>
    [ObservableProperty]
    private int _detailTab = 0;

    /// <summary>
    /// Detailed trend analysis for selected metric using linear regression.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrendAnalysis))]
    [NotifyPropertyChangedFor(nameof(TrendConfidenceDescription))]
    [NotifyPropertyChangedFor(nameof(TrendProjectionDescription))]
    private TrendResult? _trendAnalysis;

    [ObservableProperty]
    private bool _isLoadingTrendAnalysis;

    /// <summary>
    /// Whether trend analysis data is available.
    /// </summary>
    public bool HasTrendAnalysis => TrendAnalysis != null && TrendAnalysis.Direction != MetricTrend.Unknown;

    /// <summary>
    /// Human-readable confidence description.
    /// </summary>
    public string TrendConfidenceDescription => TrendAnalysis != null 
        ? $"{TrendAnalysis.ConfidenceLevel} confidence (R²={TrendAnalysis.RSquared:F3})" 
        : "No trend analysis available";

    /// <summary>
    /// Description of trend projection.
    /// </summary>
    public string TrendProjectionDescription
    {
        get
        {
            if (TrendAnalysis == null || TrendAnalysis.Direction == MetricTrend.Unknown)
                return "Not enough data for projection";

            var daysToProject = 30;
            var targetDate = DateTime.UtcNow.AddDays(daysToProject);
            var analyzer = new TrendAnalyzer();
            var projected = analyzer.ProjectValue(TrendAnalysis, targetDate);

            return $"In {daysToProject} days (by {targetDate:MMM d}): {projected:F2}";
        }
    }

    [RelayCommand]
    private void SetDetailTab(string tabIndex)
    {
        if (int.TryParse(tabIndex, out var index))
        {
            DetailTab = index;
        }
    }

    public string SelectedMetricTrendDescription => SelectedMetric?.Trend.GetDescription() ?? "No trend data available";
    public string SelectedMetricSourceDisplay => SelectedMetric?.SourceEnum.ToDisplayName() ?? "Unknown";
    public string SelectedMetricScopeDisplay => SelectedMetric?.ScopeEnum.ToDisplayName() ?? "Unknown";
    public string SelectedMetricLifecycleDisplay => SelectedMetric?.LifecycleEnum.ToDisplayName() ?? "Unknown";
    public bool IsSelectedMetricManual => SelectedMetric?.SourceEnum == MetricSource.Manual;

    #endregion

    #region Value Update Dialog

    [ObservableProperty]
    private bool _isValueUpdateDialogOpen;

    [ObservableProperty]
    private string _newValueText = string.Empty;

    [ObservableProperty]
    private string _whatChangedNote = string.Empty;

    #endregion

    #region Lifecycle Dialog

    [ObservableProperty]
    private bool _isLifecycleDialogOpen;

    [ObservableProperty]
    private MetricLifecycle _newLifecycle;

    /// <summary>
    /// Lifecycle options for the picker.
    /// </summary>
    public static IReadOnlyList<MetricLifecycle> LifecycleOptions { get; } = new[]
    {
        MetricLifecycle.Active,
        MetricLifecycle.Dormant,
        MetricLifecycle.Retired
    };

    /// <summary>
    /// Reflection prompt for current lifecycle selection.
    /// </summary>
    public string LifecycleReflectionPrompt => NewLifecycle.GetReflectionPrompt();

    partial void OnNewLifecycleChanged(MetricLifecycle value)
    {
        OnPropertyChanged(nameof(LifecycleReflectionPrompt));
    }

    #endregion

    #region Stats

    [ObservableProperty]
    private int _totalMetricsCount;

    [ObservableProperty]
    private int _activeMetricsCount;

    [ObservableProperty]
    private int _trendingUpCount;

    [ObservableProperty]
    private int _trendingDownCount;

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
    /// Called when the Metrics surface is activated (navigated to).
    /// This is the single entry point for refresh logic.
    /// Idempotent and safe to call repeatedly.
    /// </summary>
    public void OnSurfaceActivated()
    {
        System.Diagnostics.Debug.WriteLine("[MetricsViewModel] OnSurfaceActivated called");
        
        // If already loading, don't trigger another load
        if (IsLoading)
        {
            System.Diagnostics.Debug.WriteLine("[MetricsViewModel] OnSurfaceActivated: already loading, skipping");
            return;
        }
        
        // If data has never been loaded, trigger initial load
        if (_lastLoadTimestamp == DateTime.MinValue)
        {
            System.Diagnostics.Debug.WriteLine("[MetricsViewModel] OnSurfaceActivated: first activation, triggering initial load");
            _ = LoadDataAsync();
            return;
        }
        
        // Check for staleness
        var now = DateTime.UtcNow;
        var isStale = (now - _lastLoadTimestamp) > StalenessThreshold;
        
        if (isStale)
        {
            System.Diagnostics.Debug.WriteLine($"[MetricsViewModel] OnSurfaceActivated: data is stale, triggering background refresh");
            _ = LoadDataAsync();
            return;
        }
        
        // If marked dirty by external edits, trigger background refresh
        if (_isDirty)
        {
            System.Diagnostics.Debug.WriteLine("[MetricsViewModel] OnSurfaceActivated: dirty flag set, triggering background refresh");
            _isDirty = false;
            _ = LoadDataAsync();
            return;
        }
        
        // Data already loaded, fresh, and not dirty - render cached data immediately
        System.Diagnostics.Debug.WriteLine("[MetricsViewModel] OnSurfaceActivated: using cached data");
    }

    /// <summary>
    /// Marks the surface as dirty, requiring refresh on next activation.
    /// Called when metrics are edited elsewhere (e.g., flyouts, other surfaces).
    /// </summary>
    public void MarkDirty()
    {
        System.Diagnostics.Debug.WriteLine("[MetricsViewModel] MarkDirty called");
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
            await LoadMetricsAsync();
            
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
            System.Diagnostics.Debug.WriteLine($"[MetricsViewModel] LoadDataAsync error: {ex.Message}");
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

    public MetricsViewModel()
    {
        // Don't load in constructor - let the View trigger load when visible
    }
    
    /// <summary>
    /// Public method to trigger data refresh. Called by View when it becomes visible.
    /// </summary>
    public Task RefreshAsync() => LoadMetricsAsync();

    #region Load Commands

    [RelayCommand]
    private async Task LoadMetricsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            List<MetricDetail> metrics;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                // Search takes precedence
                metrics = await MetricsService.Instance.SearchMetricsAsync(SearchQuery);
            }
            else if (LifecycleFilter.HasValue)
            {
                metrics = await MetricsService.Instance.GetMetricsByLifecycleAsync(LifecycleFilter.Value);
            }
            else
            {
                metrics = await MetricsService.Instance.GetAllMetricsAsync();
            }

            // Apply scope filter client-side (if not "All")
            if (SelectedScope != 3) // Not "All"
            {
                var scopeFilter = SelectedScope switch
                {
                    0 => MetricScope.Individual,
                    1 => MetricScope.Team,
                    2 => MetricScope.Organization,
                    _ => (MetricScope?)null
                };

                if (scopeFilter.HasValue)
                {
                    metrics = metrics.Where(m => m.ScopeEnum == scopeFilter.Value).ToList();
                }
            }

            // Apply source filter client-side
            if (SourceFilter.HasValue)
            {
                metrics = metrics.Where(m => m.SourceEnum == SourceFilter.Value).ToList();
            }

            // Apply trend filter client-side
            if (TrendFilter.HasValue)
            {
                metrics = metrics.Where(m => m.Trend == TrendFilter.Value).ToList();
            }

            // Update collection
            Metrics.Clear();
            foreach (var metric in metrics)
            {
                Metrics.Add(metric);
            }
            
            // Notify computed state properties
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowMetricsList));

            // Update stats
            TotalMetricsCount = metrics.Count;
            ActiveMetricsCount = metrics.Count(m => m.LifecycleEnum == MetricLifecycle.Active);
            TrendingUpCount = metrics.Count(m => m.Trend == MetricTrend.TrendingUp);
            TrendingDownCount = metrics.Count(m => m.Trend == MetricTrend.TrendingDown);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load metrics: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMetricHistoryAsync()
    {
        if (SelectedMetric == null) return;

        try
        {
            var history = await MetricsService.Instance.GetHistoryAsync(SelectedMetric.Id);
            
            MetricHistory.Clear();
            foreach (var entry in history)
            {
                MetricHistory.Add(entry);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load history: {ex.Message}";
        }
    }

    #endregion

    #region Selection Commands

    [RelayCommand]
    private async Task SelectMetric(MetricDetail metric)
    {
        SelectedMetric = metric;
        DetailTab = 0;
        IsDetailFlyoutOpen = true;
        IsEditorFlyoutOpen = false;

        // Load history and trend analysis in parallel
        var historyTask = LoadMetricHistoryAsync();
        var trendTask = LoadTrendAnalysisAsync();
        await Task.WhenAll(historyTask, trendTask);
    }

    /// <summary>
    /// Selects a metric by its ID, opening the detail flyout.
    /// Used for cross-tab navigation.
    /// </summary>
    public async Task SelectMetricByIdAsync(Guid metricId)
    {
        var metric = Metrics.FirstOrDefault(m => m.Id == metricId);
        if (metric != null)
        {
            await SelectMetric(metric);
        }
    }

    [RelayCommand]
    private void CloseDetailFlyout()
    {
        IsDetailFlyoutOpen = false;
        SelectedMetric = null;
        MetricHistory.Clear();
        TrendAnalysis = null;
    }

    [RelayCommand]
    private async Task LoadTrendAnalysisAsync()
    {
        if (SelectedMetric == null) return;

        IsLoadingTrendAnalysis = true;
        try
        {
            var analysis = await MetricsService.Instance.GetTrendAnalysisAsync(SelectedMetric.Id, lookbackDays: 30);
            TrendAnalysis = analysis;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load trend analysis: {ex.Message}";
            TrendAnalysis = null;
        }
        finally
        {
            IsLoadingTrendAnalysis = false;
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
    [RelayCommand]
    private void HideProjectSelector()
    {
        IsProjectSelectorOpen = false;
    }
    
    /// <summary>
    /// Links the selected metric to a project.
    /// Called by the View when a project is selected in the popover.
    /// </summary>
    public async Task LinkMetricToProjectAsync(Guid projectId, string projectTitle)
    {
        if (SelectedMetric == null) return;
        
        try
        {
            IsLoading = true;
            
            // If already linked to a different project, remove old link first
            if (SelectedMetric.ProjectId.HasValue && SelectedMetric.ProjectId != projectId)
            {
                await ProjectService.Instance.RemoveProjectLinkAsync(
                    SelectedMetric.ProjectId.Value,
                    "metric",
                    SelectedMetric.Id);
            }
            
            // Add new link
            var link = await ProjectService.Instance.AddProjectLinkAsync(
                projectId,
                "metric",
                SelectedMetric.Id,
                SelectedMetric.Name);
            
            if (link != null)
            {
                // Update local state
                SelectedMetric.ProjectId = projectId;
                SelectedMetric.ProjectTitle = projectTitle;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedMetric));
                
                NotificationService.Instance.ShowSuccess(
                    "Metric Linked", 
                    $"'{SelectedMetric.Name}' linked to '{projectTitle}'");
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
    /// Unlinks the selected metric from its project.
    /// </summary>
    [RelayCommand]
    private async Task UnlinkMetricFromProject()
    {
        if (SelectedMetric?.ProjectId == null) return;
        
        try
        {
            IsLoading = true;
            
            var success = await ProjectService.Instance.RemoveProjectLinkAsync(
                SelectedMetric.ProjectId.Value,
                "metric",
                SelectedMetric.Id);
            
            if (success)
            {
                var projectTitle = SelectedMetric.ProjectTitle;
                
                // Update local state
                SelectedMetric.ProjectId = null;
                SelectedMetric.ProjectTitle = null;
                
                // Notify UI
                OnPropertyChanged(nameof(SelectedMetric));
                
                NotificationService.Instance.ShowInfo(
                    "Metric Unlinked", 
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

    #region CRUD Commands

    [RelayCommand]
    private void CreateNewMetric()
    {
        // Fire event to show dialog (View handles via AppDialogService)
        CreateMetricDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void EditMetric(MetricDetail? metric)
    {
        if (metric == null) return;
        
        // Fire event to show dialog (View handles via AppDialogService)
        EditMetricDialogRequested?.Invoke(this, metric);
    }

    [RelayCommand]
    private void CloseEditorFlyout()
    {
        IsEditorFlyoutOpen = false;
        EditingMetric = null;
    }

    [RelayCommand]
    private async Task SaveMetricAsync()
    {
        if (EditingMetric == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            if (EditingMetric.Id == Guid.Empty)
            {
                var created = await MetricsService.Instance.CreateMetricAsync(EditingMetric);
                if (created != null)
                {
                    Metrics.Insert(0, created);
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowMetricsList));
                }
            }
            else
            {
                var updated = await MetricsService.Instance.UpdateMetricAsync(EditingMetric);
                if (updated != null)
                {
                    var index = Metrics.IndexOf(Metrics.FirstOrDefault(m => m.Id == updated.Id)!);
                    if (index >= 0)
                    {
                        Metrics[index] = updated;
                    }
                }
            }

            CloseEditorFlyout();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save metric: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMetricAsync(MetricDetail? metric)
    {
        if (metric == null) return;

        // Show confirmation dialog for destructive action
        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Metric",
            $"Are you sure you want to delete '{metric.Name}'? All historical values will be lost. This action cannot be undone.",
            "Delete Metric",
            "Cancel");
        
        if (!confirmed)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var metricName = metric.Name;

            await MetricsService.Instance.DeleteMetricAsync(metric.Id);

            Metrics.Remove(metric);
            if (SelectedMetric?.Id == metric.Id)
            {
                CloseDetailFlyout();
            }
            NotificationService.Instance.ShowSuccess("Metric Deleted", $"'{metricName}' has been removed.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete metric: {ex.Message}";
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Value Update Commands

    [RelayCommand]
    private void OpenValueUpdateDialog()
    {
        if (SelectedMetric == null) return;

        // Fire event to show dialog (View handles via AppDialogService)
        UpdateValueDialogRequested?.Invoke(this, SelectedMetric);
    }

    [RelayCommand]
    private void CancelValueUpdate()
    {
        IsValueUpdateDialogOpen = false;
        NewValueText = string.Empty;
        WhatChangedNote = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmValueUpdateAsync()
    {
        if (SelectedMetric == null) return;

        if (!decimal.TryParse(NewValueText, out var newValue))
        {
            ErrorMessage = "Please enter a valid number";
            return;
        }

        // Manual metrics require a "what changed" note
        if (SelectedMetric.SourceEnum == MetricSource.Manual && string.IsNullOrWhiteSpace(WhatChangedNote))
        {
            ErrorMessage = "Please describe what changed (required for manual metrics)";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await MetricsService.Instance.UpdateValueAsync(
                SelectedMetric.Id, 
                newValue, 
                WhatChangedNote);

            if (updated != null)
            {
                // Update selected metric with new data
                SelectedMetric = updated;
                
                // Refresh the list
                await LoadMetricsAsync();
                
                // Reload history
                await LoadMetricHistoryAsync();
            }

            IsValueUpdateDialogOpen = false;
            NewValueText = string.Empty;
            WhatChangedNote = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update value: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Lifecycle Commands

    [RelayCommand]
    private void OpenLifecycleDialog()
    {
        if (SelectedMetric == null) return;

        NewLifecycle = SelectedMetric.LifecycleEnum;
        IsLifecycleDialogOpen = true;
    }

    [RelayCommand]
    private void CancelLifecycleChange()
    {
        IsLifecycleDialogOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmLifecycleChangeAsync()
    {
        if (SelectedMetric == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await MetricsService.Instance.UpdateLifecycleAsync(
                SelectedMetric.Id, 
                NewLifecycle);

            if (updated != null)
            {
                SelectedMetric = updated;
                await LoadMetricsAsync();
            }

            IsLifecycleDialogOpen = false;
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

    /// <summary>
    /// Quick lifecycle change from button (no dialog).
    /// </summary>
    [RelayCommand]
    private async Task SetLifecycle(string lifecycle)
    {
        if (SelectedMetric == null) return;

        var newLifecycle = MetricLifecycleExtensions.ParseMetricLifecycle(lifecycle);
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await MetricsService.Instance.UpdateLifecycleAsync(
                SelectedMetric.Id, 
                newLifecycle);

            if (updated != null)
            {
                SelectedMetric = updated;
                await LoadMetricsAsync();
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

    #region Public Methods for Dialog Pattern

    /// <summary>
    /// Updates a metric value from dialog result.
    /// Called by the View when the UpdateMetricValueDialog returns.
    /// </summary>
    public async Task UpdateMetricValueAsync(Guid metricId, decimal newValue, string? whatChanged)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var updated = await MetricsService.Instance.UpdateValueAsync(
                metricId, 
                newValue, 
                whatChanged);

            if (updated != null)
            {
                // Update selected metric with new data
                SelectedMetric = updated;
                
                // Refresh the list
                await LoadMetricsAsync();
                
                // Reload history
                await LoadMetricHistoryAsync();
                
                NotificationService.Instance.ShowSuccess("Value Updated", $"'{updated.Name}' recorded new value: {newValue}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update value: {ex.Message}";
            NotificationService.Instance.ShowError("Update Failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Called by View after a metric is created or edited via dialog.
    /// Refreshes the metrics list.
    /// </summary>
    public async Task OnMetricSavedAsync(MetricDetail? metric)
    {
        if (metric == null) return;
        
        await LoadMetricsAsync();
        
        // If it was the selected metric being edited, update it
        if (SelectedMetric?.Id == metric.Id)
        {
            SelectedMetric = metric;
        }
    }

    #endregion
}
