using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Pulse synthesis view.
/// 
/// Pulse answers three questions:
/// 1. What needs attention? (Attention Required)
/// 2. What changed? (What Changed)
/// 3. What story is emerging? (Recent Discussions + Actions Taken)
/// 
/// Per spec:
/// - Pulse is synthesis, not a browse surface
/// - Quick access strip at top for navigating to Goals/Metrics/Tasks
/// - Single-column card-based feed below
/// - Role-aware time windows (IC=7d, Manager=14d, MoM=30d)
/// </summary>
public partial class PulseViewModel : ViewModelBase
{
    #region Quick Access Navigation
    
    /// <summary>
    /// Event raised when user wants to navigate to Goals browse page.
    /// </summary>
    public event EventHandler? NavigateToGoalsRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to Metrics browse page.
    /// </summary>
    public event EventHandler? NavigateToMetricsRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to Tasks browse page.
    /// </summary>
    public event EventHandler? NavigateToTasksRequested;
    
    /// <summary>
    /// Event raised when user wants to create a new survey.
    /// </summary>
    public event EventHandler? CreateSurveyRequested;
    
    /// <summary>
    /// Event raised when user wants to distribute a survey.
    /// </summary>
    public event EventHandler<Guid>? DistributeSurveyRequested;
    
    /// <summary>
    /// Event raised when user wants to close a survey.
    /// </summary>
    public event EventHandler<Guid>? CloseSurveyRequested;
    
    /// <summary>
    /// Event raised when user wants to view survey analytics.
    /// </summary>
    public event EventHandler<Guid>? ViewAnalyticsRequested;
    
    [RelayCommand]
    private void NavigateToGoals() => NavigateToGoalsRequested?.Invoke(this, EventArgs.Empty);
    
    [RelayCommand]
    private void NavigateToMetrics() => NavigateToMetricsRequested?.Invoke(this, EventArgs.Empty);
    
    [RelayCommand]
    private void NavigateToTasks() => NavigateToTasksRequested?.Invoke(this, EventArgs.Empty);
    
    [RelayCommand]
    private void CreateSurvey() => CreateSurveyRequested?.Invoke(this, EventArgs.Empty);
    
    #endregion
    
    #region Survey Collections
    
    /// <summary>
    /// Draft surveys (not yet distributed).
    /// </summary>
    public ObservableCollection<SurveyCardViewModel> DraftSurveys { get; } = new();
    
    /// <summary>
    /// Active surveys (currently accepting responses).
    /// </summary>
    public ObservableCollection<SurveyCardViewModel> ActiveSurveys { get; } = new();
    
    /// <summary>
    /// Closed surveys (no longer accepting responses).
    /// </summary>
    public ObservableCollection<SurveyCardViewModel> ClosedSurveys { get; } = new();
    
    /// <summary>
    /// Selected survey tab (0=Draft, 1=Active, 2=Closed).
    /// </summary>
    [ObservableProperty]
    private int _selectedSurveyTab;
    
    #endregion
    
    #region Signal Collections
    
    /// <summary>
    /// Signals requiring immediate attention (max 5 per spec).
    /// </summary>
    public ObservableCollection<PulseSignal> AttentionRequired { get; } = new();
    
    /// <summary>
    /// Signals about what changed (awareness without alarm).
    /// </summary>
    public ObservableCollection<PulseSignal> WhatChanged { get; } = new();
    
    /// <summary>
    /// Signals from recent meeting discussions.
    /// </summary>
    public ObservableCollection<PulseSignal> RecentDiscussions { get; } = new();
    
    /// <summary>
    /// Completed actions that reinforce follow-through.
    /// </summary>
    public ObservableCollection<PulseSignal> ActionsTaken { get; } = new();
    
    #endregion
    
    #region UI State
    
    /// <summary>
    /// Global loading state - true when any section is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;
    
    /// <summary>
    /// Loading state for Attention Required section.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingAttention;
    
    /// <summary>
    /// Loading state for What Changed section.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingChanges;
    
    /// <summary>
    /// Loading state for Recent Discussions section.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingDiscussions;
    
    /// <summary>
    /// Loading state for Actions Taken section.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingActions;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    [ObservableProperty]
    private DateTime _lastRefreshed;
    
    [ObservableProperty]
    private int _timeWindowDays = 7;
    
    /// <summary>
    /// Whether there are any attention signals.
    /// </summary>
    public bool HasAttentionItems => AttentionRequired.Count > 0;
    
    /// <summary>
    /// Whether Attention Required section should show empty state.
    /// </summary>
    public bool ShowAttentionEmpty => !IsLoadingAttention && !HasAttentionItems;
    
    /// <summary>
    /// Whether there are any change signals.
    /// </summary>
    public bool HasChangeItems => WhatChanged.Count > 0;
    
    /// <summary>
    /// Whether What Changed section should show empty state.
    /// </summary>
    public bool ShowChangesEmpty => !IsLoadingChanges && !HasChangeItems;
    
    /// <summary>
    /// Whether there are any discussion signals.
    /// </summary>
    public bool HasDiscussionItems => RecentDiscussions.Count > 0;
    
    /// <summary>
    /// Whether Recent Discussions section should show empty state.
    /// </summary>
    public bool ShowDiscussionsEmpty => !IsLoadingDiscussions && !HasDiscussionItems;
    
    /// <summary>
    /// Whether there are any action signals.
    /// </summary>
    public bool HasActionItems => ActionsTaken.Count > 0;
    
    /// <summary>
    /// Whether Actions Taken section should show empty state.
    /// </summary>
    public bool ShowActionsEmpty => !IsLoadingActions && !HasActionItems;
    
    /// <summary>
    /// Whether there are any signals at all.
    /// </summary>
    public bool HasAnySignals => HasAttentionItems || HasChangeItems || HasDiscussionItems || HasActionItems;
    
    /// <summary>
    /// Whether to show the global empty state (all sections empty after loading).
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasAnySignals;
    
    #endregion
    
    #region Signal Detail Flyouts
    
    [ObservableProperty]
    private PulseSignal? _selectedSignal;
    
    /// <summary>
    /// Selected goal for detail flyout.
    /// </summary>
    [ObservableProperty]
    private GoalDetail? _selectedGoal;
    
    /// <summary>
    /// Selected metric for detail flyout.
    /// </summary>
    [ObservableProperty]
    private MetricDetail? _selectedMetric;
    
    /// <summary>
    /// Selected task for detail flyout.
    /// </summary>
    [ObservableProperty]
    private TaskDetail? _selectedTask;
    
    /// <summary>
    /// Whether goal detail flyout is open.
    /// </summary>
    [ObservableProperty]
    private bool _isGoalFlyoutOpen;
    
    /// <summary>
    /// Whether metric detail flyout is open.
    /// </summary>
    [ObservableProperty]
    private bool _isMetricFlyoutOpen;
    
    /// <summary>
    /// Whether task detail flyout is open.
    /// </summary>
    [ObservableProperty]
    private bool _isTaskFlyoutOpen;
    
    [RelayCommand]
    private void CloseFlyout()
    {
        IsGoalFlyoutOpen = false;
        IsMetricFlyoutOpen = false;
        IsTaskFlyoutOpen = false;
        SelectedGoal = null;
        SelectedMetric = null;
        SelectedTask = null;
    }
    
    [RelayCommand]
    private async Task SelectSignalAsync(PulseSignal signal)
    {
        SelectedSignal = signal;
        
        // Close any open flyout first
        CloseFlyout();
        
        // Load and open the appropriate flyout based on source type
        switch (signal.SourceType)
        {
            case PulseSourceType.Goal:
                var goal = await GoalsService.Instance.GetGoalByIdAsync(signal.SourceId);
                if (goal != null)
                {
                    SelectedGoal = goal;
                    IsGoalFlyoutOpen = true;
                }
                break;
                
            case PulseSourceType.Metric:
                var metric = await MetricsService.Instance.GetMetricByIdAsync(signal.SourceId);
                if (metric != null)
                {
                    SelectedMetric = metric;
                    IsMetricFlyoutOpen = true;
                }
                break;
                
            case PulseSourceType.Task:
                var task = await TaskService.Instance.GetTaskAsync(signal.SourceId);
                if (task != null)
                {
                    SelectedTask = task;
                    IsTaskFlyoutOpen = true;
                }
                break;
        }
    }
    
    #endregion
    
    public PulseViewModel()
    {
        // Determine time window based on user role
        // TODO: Get actual user role from auth service
        TimeWindowDays = PulseSignalService.GetTimeWindowDays(isManager: false, isManagerOfManagers: false);
    }
    
    /// <summary>
    /// Loads all Pulse signals with per-section loading states.
    /// </summary>
    [RelayCommand]
    private async Task LoadPulseDataAsync(CancellationToken ct = default)
    {
        try
        {
            // Set all loading states
            IsLoading = true;
            IsLoadingAttention = true;
            IsLoadingChanges = true;
            IsLoadingDiscussions = true;
            IsLoadingActions = true;
            ErrorMessage = null;
            
            System.Diagnostics.Debug.WriteLine("[PulseViewModel] LoadPulseDataAsync starting...");
            
            // Get current user ID from team member
            var userId = AuthService.Instance.CurrentTeamMember?.Id ?? Guid.Empty;
            System.Diagnostics.Debug.WriteLine($"[PulseViewModel] UserId: {userId}, TimeWindow: {TimeWindowDays} days");
            
            // Generate signals
            var pulseData = await PulseSignalService.Instance.GenerateAllSignalsAsync(
                userId, 
                TimeWindowDays, 
                ct);
            
            System.Diagnostics.Debug.WriteLine($"[PulseViewModel] Signals received - Attention: {pulseData.AttentionRequired.Count}, Changed: {pulseData.WhatChanged.Count}, Discussions: {pulseData.RecentDiscussions.Count}, Actions: {pulseData.ActionsTaken.Count}");
            
            // Update Attention Required section
            AttentionRequired.Clear();
            foreach (var signal in pulseData.AttentionRequired)
                AttentionRequired.Add(signal);
            IsLoadingAttention = false;
            OnPropertyChanged(nameof(HasAttentionItems));
            OnPropertyChanged(nameof(ShowAttentionEmpty));
            
            // Update What Changed section
            WhatChanged.Clear();
            foreach (var signal in pulseData.WhatChanged)
                WhatChanged.Add(signal);
            IsLoadingChanges = false;
            OnPropertyChanged(nameof(HasChangeItems));
            OnPropertyChanged(nameof(ShowChangesEmpty));
            
            // Update Recent Discussions section
            RecentDiscussions.Clear();
            foreach (var signal in pulseData.RecentDiscussions)
                RecentDiscussions.Add(signal);
            IsLoadingDiscussions = false;
            OnPropertyChanged(nameof(HasDiscussionItems));
            OnPropertyChanged(nameof(ShowDiscussionsEmpty));
            
            // Update Actions Taken section
            ActionsTaken.Clear();
            foreach (var signal in pulseData.ActionsTaken)
                ActionsTaken.Add(signal);
            IsLoadingActions = false;
            OnPropertyChanged(nameof(HasActionItems));
            OnPropertyChanged(nameof(ShowActionsEmpty));
            
            // Load surveys
            await LoadSurveysAsync(ct);
            
            LastRefreshed = DateTime.Now;
            IsLoading = false;
            
            System.Diagnostics.Debug.WriteLine("[PulseViewModel] LoadPulseDataAsync complete");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load Pulse data: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[PulseViewModel] ERROR: {ex}");
            IsLoading = false;
            IsLoadingAttention = false;
            IsLoadingChanges = false;
            IsLoadingDiscussions = false;
            IsLoadingActions = false;
        }
    }
    
    /// <summary>
    /// Loads surveys and their response statistics.
    /// </summary>
    private async Task LoadSurveysAsync(CancellationToken ct = default)
    {
        try
        {
            var allSurveys = await SurveyService.Instance.GetOrganizationSurveysAsync(ct);
            
            DraftSurveys.Clear();
            ActiveSurveys.Clear();
            ClosedSurveys.Clear();
            
            foreach (var survey in allSurveys)
            {
                // Get response stats
                var (total, completed) = await SurveyService.Instance.GetSurveyStatsAsync(survey.Id, ct);
                var card = new SurveyCardViewModel(survey, total, completed);
                
                if (survey.Status == "draft")
                    DraftSurveys.Add(card);
                else if (survey.Status == "active")
                    ActiveSurveys.Add(card);
                else if (survey.Status == "closed")
                    ClosedSurveys.Add(card);
            }
            
            System.Diagnostics.Debug.WriteLine($"[PulseViewModel] Surveys loaded - Draft: {DraftSurveys.Count}, Active: {ActiveSurveys.Count}, Closed: {ClosedSurveys.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PulseViewModel] ERROR loading surveys: {ex}");
        }
    }
    
    /// <summary>
    /// Distributes a survey to target team members.
    /// </summary>
    [RelayCommand]
    private async Task DistributeSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        DistributeSurveyRequested?.Invoke(this, surveyId);
    }
    
    /// <summary>
    /// Closes a survey (stops accepting responses).
    /// </summary>
    [RelayCommand]
    private async Task CloseSurveyAsync(Guid surveyId, CancellationToken ct = default)
    {
        CloseSurveyRequested?.Invoke(this, surveyId);
    }
    
    /// <summary>
    /// Opens the analytics dialog for a survey.
    /// </summary>
    [RelayCommand]
    private void ViewAnalytics(Guid surveyId)
    {
        ViewAnalyticsRequested?.Invoke(this, surveyId);
    }
    
    /// <summary>
    /// Called after a survey is distributed to refresh the list.
    /// </summary>
    public async Task OnSurveyDistributedAsync()
    {
        await LoadSurveysAsync();
    }
    
    /// <summary>
    /// Called after a survey is closed to refresh the list.
    /// </summary>
    public async Task OnSurveyClosedAsync()
    {
        await LoadSurveysAsync();
    }
    
    /// <summary>
    /// Called after a new survey is created to refresh the list.
    /// </summary>
    public async Task OnSurveyCreatedAsync()
    {
        await LoadSurveysAsync();
    }
}
