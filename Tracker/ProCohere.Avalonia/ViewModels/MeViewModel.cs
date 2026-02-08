using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Services.Insights;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// Tabs available on the Me screen - mirrors Circle structure for single user
/// </summary>
public enum MeTab
{
    Tasks,
    Goals,
    Feedback,
    Meetings,
    Development,
    Insights
}

/// <summary>
/// Types of flyouts available on the Me screen
/// </summary>
public enum MeFlyoutType
{
    None,
    Task,
    Meeting,
    Goal,
    Feedback,
    DevelopmentPlan
}

/// <summary>
/// Tabs for the meeting flyout in Me view.
/// Different from CircleView's MeetingDetailTab - personal prep focus.
/// Working the meeting: Prep → Follow-ups → People → Notes
/// </summary>
public enum MeMeetingTab
{
    /// <summary>My prep items for this meeting - personal readiness.</summary>
    Prep,
    /// <summary>Meeting-scoped follow-up tasks (source_type=meeting or agenda_item for this meeting).</summary>
    FollowUps,
    /// <summary>Meeting attendees with roles.</summary>
    People,
    /// <summary>My notes - private by default, with shared notes toggle.</summary>
    MyNotes
}

/// <summary>
/// ViewModel for the ME screen - the personal operating hub.
/// Shows only the current user's tasks, goals, meetings, and feedback.
/// Design principle: Personal-first, actionable, no comparison.
/// </summary>
public partial class MeViewModel : ViewModelBase
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "me_view.log");

    #region Observable Properties

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Tab selection
    [ObservableProperty]
    private MeTab _selectedTab = MeTab.Tasks;

    // My Tasks - tasks where I am the owner
    [ObservableProperty]
    private ObservableCollection<TaskDetail> _myTasks = new();

    // My Goals - goals where I am the owner
    [ObservableProperty]
    private ObservableCollection<GoalDetail> _myGoals = new();

    // My Meetings - meetings I'm participating in
    [ObservableProperty]
    private ObservableCollection<MeetingDetail> _myMeetings = new();

    // My Feedback - received
    [ObservableProperty]
    private ObservableCollection<FeedbackDetail> _receivedFeedback = new();

    // My Feedback - given
    [ObservableProperty]
    private ObservableCollection<FeedbackDetail> _givenFeedback = new();

    // My AI Insights - grouped by type for organized display
    [ObservableProperty]
    private ObservableCollection<Insight> _myInsights = new();

    // My Development Plans - career growth tracking
    [ObservableProperty]
    private ObservableCollection<DevelopmentPlan> _myDevelopmentPlans = new();

    // Feedback tab selection
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceivedFeedbackTab))]
    [NotifyPropertyChangedFor(nameof(IsGivenFeedbackTab))]
    private int _selectedFeedbackTab = 0;

    // User info
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _userGreeting = string.Empty;

    // ==================== Flyout State ====================
    
    /// <summary>
    /// What type of flyout is currently open
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFlyoutOpen))]
    [NotifyPropertyChangedFor(nameof(IsTaskFlyoutOpen))]
    [NotifyPropertyChangedFor(nameof(IsMeetingFlyoutOpen))]
    [NotifyPropertyChangedFor(nameof(IsGoalFlyoutOpen))]
    [NotifyPropertyChangedFor(nameof(IsFeedbackFlyoutOpen))]
    [NotifyPropertyChangedFor(nameof(IsDevelopmentPlanFlyoutOpen))]
    private MeFlyoutType _activeFlyoutType = MeFlyoutType.None;

    [ObservableProperty]
    private TaskDetail? _selectedTask;

    [ObservableProperty]
    private MeetingDetail? _selectedMeeting;

    [ObservableProperty]
    private GoalDetail? _selectedGoal;

    [ObservableProperty]
    private FeedbackDetail? _selectedFeedback;

    [ObservableProperty]
    private DevelopmentPlan? _selectedDevelopmentPlan;

    /// <summary>
    /// True if the selected feedback was from the "Given" list (shows TO/Recipient),
    /// False if from "Received" list (shows FROM/Sender).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedFeedbackReceived))]
    private bool _isSelectedFeedbackGiven;

    /// <summary>
    /// True if the selected feedback was from the "Received" list.
    /// </summary>
    public bool IsSelectedFeedbackReceived => !IsSelectedFeedbackGiven;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Tasks sorted by urgency: Overdue → Due Today → Due Soon → Future → No Date
    /// </summary>
    public IEnumerable<TaskDetail> SortedTasks => MyTasks
        .Where(t => !t.IsCompleted)
        .OrderBy(t => GetTaskUrgencyOrder(t))
        .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

    /// <summary>
    /// Completed tasks (for reference)
    /// </summary>
    public IEnumerable<TaskDetail> CompletedTasks => MyTasks
        .Where(t => t.IsCompleted)
        .OrderByDescending(t => t.CompletedAt);

    /// <summary>
    /// Goals sorted by due date, then by health
    /// </summary>
    public IEnumerable<GoalDetail> SortedGoals => MyGoals
        .Where(g => g.Lifecycle == GoalLifecycle.Active || g.Lifecycle == GoalLifecycle.Evolving)
        .OrderBy(g => g.DueDate ?? DateTime.MaxValue);

    /// <summary>
    /// Upcoming meetings (next 7 days)
    /// </summary>
    public IEnumerable<MeetingDetail> UpcomingMeetings => MyMeetings
        .Where(m => m.ScheduledAt >= DateTime.Today && m.ScheduledAt <= DateTime.Today.AddDays(7))
        .OrderBy(m => m.ScheduledAt);

    public bool IsReceivedFeedbackTab => SelectedFeedbackTab == 0;
    public bool IsGivenFeedbackTab => SelectedFeedbackTab == 1;
    
    // Alias for XAML binding
    public bool IsShowingReceivedFeedback => IsReceivedFeedbackTab;

    // Counts for UI
    public int OpenTaskCount => MyTasks.Count(t => !t.IsCompleted);
    public int OverdueTaskCount => MyTasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Today);
    public int ActiveGoalCount => SortedGoals.Count();
    public int UpcomingMeetingCount => UpcomingMeetings.Count();
    public int InsightCount => MyInsights.Count;
    public bool HasInsights => MyInsights.Count > 0;
    
    // Development plan counts
    public int ActivePlanCount => MyDevelopmentPlans.Count(p => p.IsActive);
    public int TotalPlanItemCount => MyDevelopmentPlans.SelectMany(p => p.Items).Count();
    public int CompletedPlanItemCount => MyDevelopmentPlans.SelectMany(p => p.Items).Count(i => i.IsCompleted);
    public bool HasDevelopmentPlans => MyDevelopmentPlans.Count > 0;
    
    /// <summary>
    /// Development plans sorted by status and date - active first, then by target date.
    /// </summary>
    public IEnumerable<DevelopmentPlan> SortedDevelopmentPlans => MyDevelopmentPlans
        .OrderByDescending(p => p.IsActive)
        .ThenBy(p => p.TargetDate ?? DateTime.MaxValue);

    /// <summary>
    /// Insights grouped by type for organized display
    /// </summary>
    public IEnumerable<InsightGroup> GroupedInsights => MyInsights
        .GroupBy(i => i.Type)
        .OrderByDescending(g => g.Max(i => i.Severity))
        .ThenBy(g => g.Key.ToString())
        .Select(g => new InsightGroup(g.Key, g.OrderByDescending(i => i.CreatedAt).ToList()));

    // Flyout visibility helpers
    public bool IsFlyoutOpen => ActiveFlyoutType != MeFlyoutType.None;
    public bool IsTaskFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Task;
    public bool IsMeetingFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Meeting;
    public bool IsGoalFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Goal;
    public bool IsFeedbackFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Feedback;
    public bool IsDevelopmentPlanFlyoutOpen => ActiveFlyoutType == MeFlyoutType.DevelopmentPlan;

    #endregion

    #region Meetings Calendar Properties

    /// <summary>
    /// View mode for meetings calendar: Day, Week, Month, or List
    /// </summary>
    [ObservableProperty]
    private MeetingsViewMode _meetingsViewMode = MeetingsViewMode.List;

    /// <summary>
    /// Current date for calendar navigation
    /// </summary>
    [ObservableProperty]
    private DateTime _currentCalendarDate = DateTime.Today;

    /// <summary>
    /// Current view date header text.
    /// </summary>
    public string CalendarDateHeader => MeetingsViewMode switch
    {
        MeetingsViewMode.Day => CurrentCalendarDate.ToString("dddd, MMMM d, yyyy"),
        MeetingsViewMode.Week => $"{GetWeekStart(CurrentCalendarDate):MMM d} - {GetWeekStart(CurrentCalendarDate).AddDays(6):MMM d, yyyy}",
        MeetingsViewMode.Month => CurrentCalendarDate.ToString("MMMM yyyy"),
        _ => "Upcoming Meetings"
    };

    /// <summary>
    /// Meetings filtered for the current view
    /// </summary>
    public ObservableCollection<MeetingDetail> FilteredMeetings { get; } = new();

    /// <summary>
    /// Meetings grouped by date for list view
    /// </summary>
    public ObservableCollection<MeetingGroup> GroupedMeetings { get; } = new();

    /// <summary>
    /// Calendar days for month view
    /// </summary>
    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();

    /// <summary>
    /// Week days for week view
    /// </summary>
    public ObservableCollection<CalendarWeekDay> WeekDays { get; } = new();

    /// <summary>
    /// Meetings for the current day in day view
    /// </summary>
    public ObservableCollection<MeetingDetail> DayMeetings { get; } = new();

    /// <summary>
    /// Are there no meetings for the day view?
    /// </summary>
    public bool HasNoDayMeetings => DayMeetings.Count == 0;

    /// <summary>
    /// Are there no grouped meetings for list view?
    /// </summary>
    public bool HasNoGroupedMeetings => GroupedMeetings.Count == 0;

    /// <summary>
    /// Current tab in the meeting detail flyout for Me view.
    /// Defaults to Prep (personal prep focus, not meeting admin).
    /// </summary>
    [ObservableProperty]
    private MeMeetingTab _meMeetingTab = MeMeetingTab.Prep;

    /// <summary>
    /// Whether we're showing shared notes (true) or my notes (false) in the Notes tab.
    /// Defaults to my notes (false).
    /// </summary>
    [ObservableProperty]
    private bool _isShowingSharedNotes = false;

    /// <summary>
    /// Display text for the notes privacy indicator.
    /// </summary>
    public string NotesPrivacyText => IsShowingSharedNotes 
        ? "Visible to all attendees" 
        : "Private - only visible to you";

    /// <summary>
    /// Current notes to display based on toggle state.
    /// </summary>
    public List<MeetingNote> CurrentNotes => IsShowingSharedNotes 
        ? SelectedMeeting?.SharedNotes ?? new() 
        : SelectedMeeting?.MyNotes ?? new();

    /// <summary>
    /// Count of current notes.
    /// </summary>
    public int CurrentNotesCount => CurrentNotes.Count;

    /// <summary>
    /// Whether we have notes to display.
    /// </summary>
    public bool HasCurrentNotes => CurrentNotesCount > 0;

    partial void OnIsShowingSharedNotesChanged(bool value)
    {
        // Notify that computed properties have changed
        OnPropertyChanged(nameof(NotesPrivacyText));
        OnPropertyChanged(nameof(CurrentNotes));
        OnPropertyChanged(nameof(CurrentNotesCount));
        OnPropertyChanged(nameof(HasCurrentNotes));
    }

    /// <summary>
    /// Reset meeting tab when meeting changes.
    /// Always defaults to Prep - the flyout is for "working the meeting".
    /// </summary>
    partial void OnSelectedMeetingChanged(MeetingDetail? value)
    {
        if (value != null)
        {
            // Always start with Prep - working the meeting starts with preparation
            MeMeetingTab = MeMeetingTab.Prep;
            
            // Load prep items asynchronously
            _ = LoadPrepItemsForMeetingAsync(value);
            
            // Load follow-ups asynchronously
            _ = LoadFollowUpsForMeetingAsync(value);
            
            // Load notes asynchronously
            _ = LoadNotesForMeetingAsync(value);
        }
    }

    /// <summary>
    /// Loads prep items for the selected meeting.
    /// </summary>
    private async Task LoadPrepItemsForMeetingAsync(MeetingDetail meeting)
    {
        try
        {
            Log($"[MeViewModel] Loading prep items for meeting: {meeting.Id}");
            
            var prepItems = await MeetingPrepItemService.Instance.GetPrepItemsForMeetingAsync(meeting.Id);
            
            // Update the meeting's prep items
            meeting.PrepItems = prepItems;
            
            Log($"[MeViewModel] Loaded {prepItems.Count} prep items");
            
            // Notify UI that prep item collections changed
            OnPropertyChanged(nameof(SelectedMeeting));
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Error loading prep items: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads follow-up tasks for the selected meeting.
    /// Follow-ups are tasks sourced from the meeting or its agenda items.
    /// Separates into MyFollowUps (assigned to me) and TeamFollowUps (assigned to others).
    /// </summary>
    private async Task LoadFollowUpsForMeetingAsync(MeetingDetail meeting)
    {
        try
        {
            Log($"[MeViewModel] Loading follow-ups for meeting: {meeting.Id}");
            
            var currentUserId = AuthService.Instance.CurrentProfile?.Id;
            
            // Get agenda item IDs for this meeting
            var agendaItemIds = meeting.AgendaItems.Select(a => a.Id).ToList();
            
            // Load all follow-ups
            var allFollowUps = await TaskService.Instance.GetMeetingFollowUpsAsync(
                meeting.Id, 
                agendaItemIds,
                includeCompleted: true);

            // Separate into my tasks and team tasks
            if (currentUserId.HasValue)
            {
                meeting.MyFollowUps = allFollowUps
                    .Where(t => t.OwnerTeamMemberId == currentUserId.Value)
                    .ToList();
                
                meeting.TeamFollowUps = allFollowUps
                    .Where(t => t.OwnerTeamMemberId != currentUserId.Value)
                    .ToList();
            }
            else
            {
                // No current user - all tasks go to team
                meeting.MyFollowUps = new List<TaskDetail>();
                meeting.TeamFollowUps = allFollowUps;
            }
            
            Log($"[MeViewModel] Loaded {meeting.MyFollowUps.Count} my follow-ups, {meeting.TeamFollowUps.Count} team follow-ups");
            
            // Notify UI that follow-up collections changed
            OnPropertyChanged(nameof(SelectedMeeting));
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Error loading follow-ups: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads notes for the selected meeting.
    /// Separates into MyNotes (private) and SharedNotes (visible to all).
    /// </summary>
    private async Task LoadNotesForMeetingAsync(MeetingDetail meeting)
    {
        try
        {
            Log($"[MeViewModel] Loading notes for meeting: {meeting.Id}");
            
            var (myNotes, sharedNotes) = await MeetingNoteService.Instance.GetNotesForMeetingAsync(meeting.Id);

            meeting.MyNotes = myNotes;
            meeting.SharedNotes = sharedNotes;
            
            Log($"[MeViewModel] Loaded {myNotes.Count} personal notes, {sharedNotes.Count} shared notes");
            
            // Reset to "My Notes" view when changing meetings
            IsShowingSharedNotes = false;
            
            // Notify UI that notes collections changed
            OnPropertyChanged(nameof(SelectedMeeting));
            OnPropertyChanged(nameof(CurrentNotes));
            OnPropertyChanged(nameof(CurrentNotesCount));
            OnPropertyChanged(nameof(HasCurrentNotes));
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Error loading notes: {ex.Message}");
        }
    }

    #endregion

    #region Surface Activation (CR Fix Plan Phase 3)

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
    /// Timestamp of the last successful data load.
    /// </summary>
    private DateTime _lastLoadTimestamp = DateTime.MinValue;

    /// <summary>
    /// Whether the surface has been marked dirty by external edits.
    /// </summary>
    private bool _isDirty;

    /// <summary>
    /// Staleness threshold - if last refresh exceeds this, trigger refresh.
    /// Me surface uses 30 minutes (personal data should be current).
    /// </summary>
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Called when the Me surface is activated (navigated to).
    /// This is the single entry point for refresh logic.
    /// Idempotent and safe to call repeatedly.
    /// </summary>
    public void OnSurfaceActivated()
    {
        Log("[MeViewModel] OnSurfaceActivated called");

        // If already loading, don't trigger another load
        if (IsLoading)
        {
            Log("[MeViewModel] OnSurfaceActivated: already loading, skipping");
            return;
        }

        // If data has never been loaded, trigger initial load
        if (_lastLoadTimestamp == DateTime.MinValue)
        {
            Log("[MeViewModel] OnSurfaceActivated: first activation, triggering initial load");
            _ = LoadDataAsync();
            return;
        }

        // Check for staleness
        var now = DateTime.UtcNow;
        var isStale = (now - _lastLoadTimestamp) > StalenessThreshold;

        if (isStale)
        {
            Log($"[MeViewModel] OnSurfaceActivated: data is stale ({(now - _lastLoadTimestamp).TotalMinutes:F0} min old), triggering refresh");
            _ = LoadDataAsync();
            return;
        }

        // If marked dirty by external edits, trigger background refresh
        if (_isDirty)
        {
            Log("[MeViewModel] OnSurfaceActivated: dirty flag set, triggering background refresh");
            _isDirty = false;
            _ = LoadDataAsync();
            return;
        }

        // Data already loaded, fresh, and not dirty - render cached data immediately
        Log("[MeViewModel] OnSurfaceActivated: using cached data");
    }

    /// <summary>
    /// Marks the surface as dirty, requiring refresh on next activation.
    /// Called when tasks, goals, meetings, or feedback are edited elsewhere.
    /// </summary>
    public void MarkDirty()
    {
        Log("[MeViewModel] MarkDirty called");
        _isDirty = true;
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

    #region Constructor

    public MeViewModel()
    {
        // Set greeting based on time of day
        var hour = DateTime.Now.Hour;
        UserGreeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
        
        Log("[MeViewModel] Constructor called");
        
        // Subscribe to profile changes via AuthService
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Load data if profile already exists
        if (AuthService.Instance.CurrentProfile != null)
        {
            UserName = AuthService.Instance.CurrentProfile.FirstName ?? 
                       AuthService.Instance.CurrentProfile.DisplayName ?? "User";
            _ = LoadDataAsync();
        }
    }

    #endregion

    #region Data Loading

    private void OnProfileChanged(object? sender, UserProfile? profile)
    {
        Log($"[MeViewModel] ProfileChanged: {(profile != null ? profile.Email : "NULL")}");
        if (profile != null)
        {
            UserName = profile.FirstName ?? profile.DisplayName ?? profile.Email ?? "User";
            _ = LoadDataAsync();
        }
    }

    public async Task LoadDataAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Loading your data...";
            Log("[MeViewModel] LoadDataAsync started");

            // Start the 400ms delay timer for refresh status
            _updateDelayTokenSource?.Cancel();
            _updateDelayTokenSource = new CancellationTokenSource();
            _ = ShowUpdatingStatusAfterDelayAsync(_updateDelayTokenSource.Token);

            var profile = AuthService.Instance.CurrentProfile;
            if (profile == null)
            {
                Log("[MeViewModel] No profile available");
                return;
            }

            // Get my team member ID
            var visibleMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            var selfMember = visibleMembers.FirstOrDefault(m => m.Relation == "self");
            var currentUserId = selfMember?.Id ?? Guid.Empty;
            
            if (currentUserId == Guid.Empty)
            {
                Log("[MeViewModel] No team member ID available - using profile ID");
                currentUserId = profile.Id;
            }

            Log($"[MeViewModel] Loading data for user: {profile.Email}, TeamMemberId: {currentUserId}");

            // Load dashboard data (contains all tasks, goals, meetings, feedback)
            var data = await DashboardService.Instance.LoadDashboardDataAsync();
            
            // Filter to MY data
            FilterMyData(data, currentUserId);

            // Notify computed properties
            OnPropertyChanged(nameof(SortedTasks));
            OnPropertyChanged(nameof(CompletedTasks));
            OnPropertyChanged(nameof(SortedGoals));
            OnPropertyChanged(nameof(UpcomingMeetings));
            OnPropertyChanged(nameof(OpenTaskCount));
            OnPropertyChanged(nameof(OverdueTaskCount));
            OnPropertyChanged(nameof(ActiveGoalCount));
            OnPropertyChanged(nameof(UpcomingMeetingCount));

            // Refresh calendar views
            RefreshMeetingsView();

            // Update load timestamp for staleness tracking
            _lastLoadTimestamp = DateTime.UtcNow;

            StatusMessage = string.Empty;
            Log("[MeViewModel] LoadDataAsync completed");

            // Cancel the delay timer and show "Updated" status
            _updateDelayTokenSource?.Cancel();
            RefreshStatus = RefreshStatus.Updated;
            _ = FadeRefreshStatusToIdleAsync();
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] ERROR: {ex.Message}");
            StatusMessage = "Failed to load data";
            _updateDelayTokenSource?.Cancel();
            RefreshStatus = RefreshStatus.Idle;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterMyData(DashboardData data, Guid currentUserId)
    {
        // My Tasks - where I'm the owner or creator
        var myTasks = data.Tasks
            .Where(t => t.OwnerTeamMemberId == currentUserId || t.CreatedByTeamMemberId == currentUserId)
            .ToList();
        MyTasks = new ObservableCollection<TaskDetail>(myTasks);
        Log($"[MeViewModel] Loaded {MyTasks.Count} tasks (total in dashboard: {data.Tasks.Count})");
        
        // Log task details for debugging
        foreach (var t in MyTasks)
        {
            Log($"  - Task '{t.Title}' Status={t.Status}, IsCompleted={t.IsCompleted}, DueDate={t.DueDate}");
        }
        
        var openTasks = MyTasks.Count(t => !t.IsCompleted);
        Log($"[MeViewModel] Open tasks (not completed): {openTasks}");

        // My Goals - where I'm the owner
        var myGoals = data.Goals
            .Where(g => g.OwnerTeamMemberId == currentUserId)
            .ToList();
        MyGoals = new ObservableCollection<GoalDetail>(myGoals);
        Log($"[MeViewModel] Loaded {MyGoals.Count} goals (total in dashboard: {data.Goals.Count})");
        
        // Log goal details for debugging
        foreach (var g in MyGoals)
        {
            Log($"  - Goal '{g.Title}' Status={g.Status}, DueDate={g.DueDate?.ToShortDateString() ?? "none"}");
        }
        
        var activeGoals = MyGoals.Count(g => g.Lifecycle == GoalLifecycle.Active || g.Lifecycle == GoalLifecycle.Evolving);
        Log($"[MeViewModel] Active/Evolving goals: {activeGoals}");

        // My Meetings - all meetings (they're already filtered for the user)
        // Set current user ID for ownership checks
        foreach (var meeting in data.Meetings)
        {
            meeting.CurrentUserTeamMemberId = currentUserId;
        }
        MyMeetings = new ObservableCollection<MeetingDetail>(data.Meetings);
        Log($"[MeViewModel] Loaded {MyMeetings.Count} meetings");
        
        // Log upcoming meetings for debugging
        var upcoming = MyMeetings.Where(m => m.ScheduledAt >= DateTime.Today && m.ScheduledAt <= DateTime.Today.AddDays(7)).ToList();
        Log($"[MeViewModel] Upcoming meetings (next 7 days): {upcoming.Count}");
        foreach (var m in upcoming.Take(5))
        {
            Log($"  - Meeting '{m.Title}' ScheduledAt={m.ScheduledAt}, IsOwned={m.IsOwnedByCurrentUser}");
        }

        // My Feedback - split by recipient vs author
        // TeamMemberId = recipient, FromMemberId = author
        var received = data.Feedback.Where(f => f.TeamMemberId == currentUserId).ToList();
        var given = data.Feedback.Where(f => f.FromMemberId == currentUserId).ToList();
        ReceivedFeedback = new ObservableCollection<FeedbackDetail>(received);
        GivenFeedback = new ObservableCollection<FeedbackDetail>(given);
        Log($"[MeViewModel] Loaded {ReceivedFeedback.Count} received, {GivenFeedback.Count} given feedback");
        
        // Load AI Insights
        _ = LoadInsightsAsync(currentUserId);
        
        // Load Development Plans
        _ = LoadDevelopmentPlansAsync();
    }

    /// <summary>
    /// Loads development plans for the current user.
    /// </summary>
    private async Task LoadDevelopmentPlansAsync()
    {
        try
        {
            Log("[MeViewModel] Loading development plans...");
            
            var plans = await DevelopmentService.Instance.GetMyPlansAsync();
            
            MyDevelopmentPlans.Clear();
            foreach (var plan in plans)
            {
                MyDevelopmentPlans.Add(plan);
            }
            
            OnPropertyChanged(nameof(ActivePlanCount));
            OnPropertyChanged(nameof(TotalPlanItemCount));
            OnPropertyChanged(nameof(CompletedPlanItemCount));
            OnPropertyChanged(nameof(HasDevelopmentPlans));
            OnPropertyChanged(nameof(SortedDevelopmentPlans));
            
            Log($"[MeViewModel] Loaded {MyDevelopmentPlans.Count} development plans");
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Failed to load development plans: {ex.Message}");
            // Non-critical, don't throw
        }
    }

    /// <summary>
    /// Loads AI insights for the current user.
    /// </summary>
    private async Task LoadInsightsAsync(Guid teamMemberId)
    {
        try
        {
            Log($"[MeViewModel] Loading insights for team member: {teamMemberId}");
            
            var insights = await InsightEngine.Instance.GetActiveInsightsAsync(teamMemberId);
            
            MyInsights.Clear();
            foreach (var insight in insights.OrderByDescending(i => i.Severity).ThenByDescending(i => i.CreatedAt))
            {
                MyInsights.Add(insight);
            }
            
            OnPropertyChanged(nameof(InsightCount));
            OnPropertyChanged(nameof(HasInsights));
            OnPropertyChanged(nameof(GroupedInsights));
            
            Log($"[MeViewModel] Loaded {MyInsights.Count} insights");
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Failed to load insights: {ex.Message}");
            // Non-critical, don't throw
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void SelectTab(MeTab tab)
    {
        SelectedTab = tab;
        CloseFlyout(); // Close any open flyout when switching tabs
        Log($"[MeViewModel] Selected tab: {tab}");
    }

    [RelayCommand]
    private void SetFeedbackTab(string tab)
    {
        SelectedFeedbackTab = tab == "given" ? 1 : 0;
        OnPropertyChanged(nameof(IsShowingReceivedFeedback));
    }

    [RelayCommand]
    private void ShowReceivedFeedback()
    {
        SelectedFeedbackTab = 0;
        OnPropertyChanged(nameof(IsShowingReceivedFeedback));
    }

    [RelayCommand]
    private void ShowGivenFeedback()
    {
        SelectedFeedbackTab = 1;
        OnPropertyChanged(nameof(IsShowingReceivedFeedback));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Create a new task - opens the add task dialog.
    /// </summary>
    [RelayCommand]
    private void CreateTask()
    {
        Log("[MeViewModel] CreateTask command - opening dialog");
        CreateTaskDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Edit an existing task - opens the edit task dialog with the task loaded.
    /// </summary>
    [RelayCommand]
    private void EditTask(TaskDetail? task)
    {
        if (task == null)
        {
            Log("[MeViewModel] EditTask command - no task provided");
            return;
        }
        Log($"[MeViewModel] EditTask command - opening dialog for: {task.Title}");
        EditTaskDialogRequested?.Invoke(this, task);
    }

    [RelayCommand]
    private void CreateGoal()
    {
        Log("[MeViewModel] CreateGoal command - opening dialog");
        CreateGoalDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CreateNote()
    {
        Log("[MeViewModel] CreateNote command - opening dialog");
        CreateNoteDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Create a new development plan - opens the create plan dialog.
    /// </summary>
    [RelayCommand]
    private void CreateDevelopmentPlan()
    {
        Log("[MeViewModel] CreateDevelopmentPlan command - opening dialog");
        CreateDevelopmentPlanDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Edit an existing development plan - opens the edit plan dialog with the plan loaded.
    /// </summary>
    [RelayCommand]
    private void EditDevelopmentPlan(DevelopmentPlan? plan)
    {
        if (plan == null)
        {
            Log("[MeViewModel] EditDevelopmentPlan command - no plan provided");
            return;
        }
        Log($"[MeViewModel] EditDevelopmentPlan command - opening dialog for: {plan.Title}");
        EditDevelopmentPlanDialogRequested?.Invoke(this, plan);
    }

    /// <summary>
    /// Delete a development plan after confirmation.
    /// </summary>
    [RelayCommand]
    private async Task DeleteDevelopmentPlanAsync(DevelopmentPlan? plan)
    {
        if (plan == null)
        {
            Log("[MeViewModel] DeleteDevelopmentPlan command - no plan provided");
            return;
        }
        
        Log($"[MeViewModel] DeleteDevelopmentPlan command - deleting: {plan.Title}");
        
        var success = await DevelopmentService.Instance.DeletePlanAsync(plan.Id);
        if (success)
        {
            MyDevelopmentPlans.Remove(plan);
            if (SelectedDevelopmentPlan?.Id == plan.Id)
            {
                CloseFlyout();
            }
            OnPropertyChanged(nameof(ActivePlanCount));
            OnPropertyChanged(nameof(TotalPlanItemCount));
            OnPropertyChanged(nameof(CompletedPlanItemCount));
            OnPropertyChanged(nameof(HasDevelopmentPlans));
            OnPropertyChanged(nameof(SortedDevelopmentPlans));
            Log($"[MeViewModel] Development plan deleted successfully");
        }
        else
        {
            Log($"[MeViewModel] Failed to delete development plan: {DevelopmentService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Toggle item completion status.
    /// </summary>
    [RelayCommand]
    private async Task TogglePlanItemStatusAsync(DevelopmentPlanItem? item)
    {
        if (item == null) return;
        
        var newStatus = item.IsCompleted ? "not_started" : "completed";
        var success = await DevelopmentService.Instance.UpdateItemStatusAsync(item.Id, newStatus);
        
        if (success)
        {
            item.Status = newStatus;
            item.CompletedAt = newStatus == "completed" ? DateTime.UtcNow : null;
            
            OnPropertyChanged(nameof(TotalPlanItemCount));
            OnPropertyChanged(nameof(CompletedPlanItemCount));
            OnPropertyChanged(nameof(SortedDevelopmentPlans));
            
            Log($"[MeViewModel] Item status toggled: {item.Title} -> {newStatus}");
        }
    }

    /// <summary>
    /// Create a new meeting - opens the edit meeting dialog.
    /// </summary>
    [RelayCommand]
    private void CreateMeeting()
    {
        Log("[MeViewModel] CreateMeeting command - opening dialog");
        CreateMeetingDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Edit an existing meeting - opens the edit meeting dialog with the meeting loaded.
    /// </summary>
    [RelayCommand]
    private void EditMeeting(MeetingDetail? meeting)
    {
        if (meeting == null) return;
        Log($"[MeViewModel] EditMeeting command - opening dialog for {meeting.Title}");
        EditMeetingDialogRequested?.Invoke(this, meeting);
    }

    /// <summary>
    /// Add a new personal prep item to a meeting.
    /// </summary>
    [RelayCommand]
    private async Task AddPrepItem(MeetingDetail? meeting)
    {
        if (meeting == null) return;
        Log($"[MeViewModel] AddPrepItem command for meeting: {meeting.Title}");
        
        // For now, create a quick placeholder - could show a dialog in the future
        var prepItem = await MeetingPrepItemService.Instance.CreateQuickPrepAsync(
            meeting.Id, 
            "New prep item");
        
        if (prepItem != null)
        {
            meeting.PrepItems.Add(prepItem);
            OnPropertyChanged(nameof(SelectedMeeting));
            Log($"[MeViewModel] Added prep item: {prepItem.Id}");
        }
        else
        {
            Log($"[MeViewModel] Failed to add prep item: {MeetingPrepItemService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Update the status of a prep item (toggle done/open).
    /// </summary>
    [RelayCommand]
    private async Task TogglePrepItemStatus(MeetingPrepItem? item)
    {
        if (item == null) return;
        
        var newStatus = item.Status == "done" ? "open" : "done";
        Log($"[MeViewModel] TogglePrepItemStatus: {item.Id} -> {newStatus}");
        
        var success = await MeetingPrepItemService.Instance.UpdateStatusAsync(item.Id, newStatus);
        
        if (success)
        {
            item.Status = newStatus;
            OnPropertyChanged(nameof(SelectedMeeting));
        }
        else
        {
            Log($"[MeViewModel] Failed to update prep item status: {MeetingPrepItemService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Add a new follow-up task to a meeting.
    /// Creates a quick task with default values that can be edited later.
    /// </summary>
    [RelayCommand]
    private async Task AddFollowUp(MeetingDetail? meeting)
    {
        if (meeting == null) return;
        Log($"[MeViewModel] AddFollowUp command for meeting: {meeting.Title}");
        
        var currentUserId = AuthService.Instance.CurrentProfile?.Id;
        
        // Create a quick follow-up task assigned to current user
        var task = await TaskService.Instance.CreateMeetingFollowUpAsync(
            meetingId: meeting.Id,
            title: "New follow-up",
            description: $"Follow-up from meeting: {meeting.Title}",
            priority: "medium",
            assignedTo: currentUserId);
        
        if (task != null)
        {
            meeting.MyFollowUps.Add(task);
            OnPropertyChanged(nameof(SelectedMeeting));
            Log($"[MeViewModel] Added follow-up: {task.Id}");
        }
        else
        {
            Log($"[MeViewModel] Failed to add follow-up: {TaskService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Toggle the completion status of a follow-up task.
    /// </summary>
    [RelayCommand]
    private async Task ToggleFollowUpStatus(TaskDetail? task)
    {
        if (task == null) return;
        
        var wasCompleted = task.IsCompleted;
        Log($"[MeViewModel] ToggleFollowUpStatus: {task.Id} -> {(wasCompleted ? "uncompleting" : "completing")}");
        
        bool success;
        if (wasCompleted)
        {
            success = await TaskService.Instance.UncompleteTaskAsync(task.Id);
            if (success)
            {
                task.Status = "not_started";
                task.CompletedAt = null;
            }
        }
        else
        {
            success = await TaskService.Instance.CompleteTaskAsync(task.Id);
            if (success)
            {
                task.Status = "completed";
                task.CompletedAt = DateTime.UtcNow;
            }
        }
        
        if (success)
        {
            OnPropertyChanged(nameof(SelectedMeeting));
        }
        else
        {
            Log($"[MeViewModel] Failed to update task status: {TaskService.Instance.LastError}");
        }
    }

    /// <summary>
    /// Toggle between My Notes and Shared Notes views.
    /// </summary>
    [RelayCommand]
    private void ToggleNotesView()
    {
        IsShowingSharedNotes = !IsShowingSharedNotes;
        Log($"[MeViewModel] ToggleNotesView: showing {(IsShowingSharedNotes ? "shared" : "personal")} notes");
    }

    /// <summary>
    /// Set notes view to My Notes (private).
    /// </summary>
    [RelayCommand]
    private void ShowMyNotes()
    {
        IsShowingSharedNotes = false;
    }

    /// <summary>
    /// Set notes view to Shared Notes.
    /// </summary>
    [RelayCommand]
    private void ShowSharedNotes()
    {
        IsShowingSharedNotes = true;
    }

    /// <summary>
    /// Add a new note to the meeting.
    /// Creates a personal (private) or shared note based on current toggle state.
    /// </summary>
    [RelayCommand]
    private async Task AddNote(MeetingDetail? meeting)
    {
        if (meeting == null) return;
        Log($"[MeViewModel] AddNote command for meeting: {meeting.Title} (shared: {IsShowingSharedNotes})");
        
        MeetingNote? note;
        if (IsShowingSharedNotes)
        {
            note = await MeetingNoteService.Instance.CreateSharedNoteAsync(meeting.Id, "New shared note");
            if (note != null)
            {
                meeting.SharedNotes.Insert(0, note);
            }
        }
        else
        {
            note = await MeetingNoteService.Instance.CreateQuickNoteAsync(meeting.Id, "New note");
            if (note != null)
            {
                meeting.MyNotes.Insert(0, note);
            }
        }
        
        if (note != null)
        {
            OnPropertyChanged(nameof(SelectedMeeting));
            OnPropertyChanged(nameof(CurrentNotes));
            OnPropertyChanged(nameof(CurrentNotesCount));
            OnPropertyChanged(nameof(HasCurrentNotes));
            Log($"[MeViewModel] Added note: {note.Id}");
        }
        else
        {
            Log($"[MeViewModel] Failed to add note: {MeetingNoteService.Instance.LastError}");
        }
    }

    #region Dialog Events

    /// <summary>
    /// Event to request showing the Create Meeting dialog.
    /// </summary>
    public event EventHandler? CreateMeetingDialogRequested;

    /// <summary>
    /// Event to request showing the Edit Meeting dialog with an existing meeting.
    /// </summary>
    public event EventHandler<MeetingDetail>? EditMeetingDialogRequested;

    /// <summary>
    /// Event to request showing the Create Task dialog.
    /// </summary>
    public event EventHandler? CreateTaskDialogRequested;

    /// <summary>
    /// Event to request showing the Edit Task dialog with an existing task.
    /// </summary>
    public event EventHandler<TaskDetail>? EditTaskDialogRequested;

    /// <summary>
    /// Event to request showing the Create Goal dialog.
    /// </summary>
    public event EventHandler? CreateGoalDialogRequested;

    /// <summary>
    /// Event to request showing the Create Note dialog.
    /// </summary>
    public event EventHandler? CreateNoteDialogRequested;

    /// <summary>
    /// Event to request showing the Create Development Plan dialog.
    /// </summary>
    public event EventHandler? CreateDevelopmentPlanDialogRequested;

    /// <summary>
    /// Event to request showing the Edit Development Plan dialog with an existing plan.
    /// </summary>
    public event EventHandler<DevelopmentPlan>? EditDevelopmentPlanDialogRequested;

    #endregion

    /// <summary>
    /// Called when a meeting is saved (created or updated) from the dialog.
    /// The dialog is now the complete workspace, so we just add/update in the list.
    /// User can click the meeting later to open the flyout if they want.
    /// </summary>
    public void OnMeetingSaved(MeetingDetail meeting)
    {
        Log($"[MeViewModel] Meeting saved: {meeting.Title}");
        
        // Set current user ID for ownership/organizer checks
        var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;
        if (currentUserId.HasValue)
        {
            meeting.CurrentUserTeamMemberId = currentUserId;
        }
        
        // Add to collection if new
        var existing = MyMeetings.FirstOrDefault(m => m.Id == meeting.Id);
        if (existing == null)
        {
            MyMeetings.Add(meeting);
            Log("[MeViewModel] Added new meeting to collection");
        }
        else
        {
            // Update existing (replace in collection)
            var index = MyMeetings.IndexOf(existing);
            MyMeetings[index] = meeting;
            Log("[MeViewModel] Updated existing meeting in collection");
        }

        // Refresh views
        RefreshMeetingsView();
        OnPropertyChanged(nameof(UpcomingMeetings));
        OnPropertyChanged(nameof(UpcomingMeetingCount));
        
        // Don't auto-open flyout - the dialog is the workspace now.
        // User can click the meeting to open flyout if they want to continue working.
    }

    /// <summary>
    /// Called when a meeting is deleted from the dialog.
    /// </summary>
    public void OnMeetingDeleted(Guid meetingId)
    {
        Log($"[MeViewModel] Meeting deleted: {meetingId}");
        
        var existing = MyMeetings.FirstOrDefault(m => m.Id == meetingId);
        if (existing != null)
        {
            MyMeetings.Remove(existing);
        }

        // Close flyout if this meeting was selected
        if (SelectedMeeting?.Id == meetingId)
        {
            CloseFlyout();
        }

        // Refresh views
        RefreshMeetingsView();
        OnPropertyChanged(nameof(UpcomingMeetings));
        OnPropertyChanged(nameof(UpcomingMeetingCount));
    }

    /// <summary>
    /// Called when a task is saved (created or updated) from the dialog.
    /// </summary>
    public void OnTaskSaved(TaskDetail task)
    {
        Log($"[MeViewModel] Task saved: {task.Title}");
        
        // Add to collection if new
        var existing = MyTasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
        {
            MyTasks.Add(task);
            Log("[MeViewModel] Added new task to collection");
        }
        else
        {
            // Update existing (replace in collection)
            var index = MyTasks.IndexOf(existing);
            MyTasks[index] = task;
            Log("[MeViewModel] Updated existing task in collection");
        }

        // Notify property changes for task counts
        OnPropertyChanged(nameof(MyTasks));
    }

    /// <summary>
    /// Called when a goal is saved (created or updated) from the dialog.
    /// </summary>
    public void OnGoalSaved(GoalDetail goal)
    {
        Log($"[MeViewModel] Goal saved: {goal.Title}");
        
        // Check if this goal belongs to current user
        var currentTeamMember = AuthService.Instance.CurrentTeamMember;
        if (currentTeamMember == null || goal.OwnerTeamMemberId != currentTeamMember.Id)
        {
            Log("[MeViewModel] Goal not owned by current user, skipping");
            return;
        }
        
        // Add to collection if new
        var existing = MyGoals.FirstOrDefault(g => g.Id == goal.Id);
        if (existing == null)
        {
            MyGoals.Add(goal);
            Log("[MeViewModel] Added new goal to collection");
        }
        else
        {
            // Update existing (replace in collection)
            var index = MyGoals.IndexOf(existing);
            MyGoals[index] = goal;
            Log("[MeViewModel] Updated existing goal in collection");
        }

        // Notify property changes
        OnPropertyChanged(nameof(MyGoals));
        OnPropertyChanged(nameof(SortedGoals));
    }

    /// <summary>
    /// Called when a task is deleted from the dialog.
    /// </summary>
    public void OnTaskDeleted(Guid taskId)
    {
        Log($"[MeViewModel] Task deleted: {taskId}");
        
        var existing = MyTasks.FirstOrDefault(t => t.Id == taskId);
        if (existing != null)
        {
            MyTasks.Remove(existing);
        }

        // Notify property changes for task counts
        OnPropertyChanged(nameof(MyTasks));
    }

    /// <summary>
    /// Called when a development plan is saved (created or updated) from the dialog.
    /// </summary>
    public void OnDevelopmentPlanSaved(DevelopmentPlan plan)
    {
        Log($"[MeViewModel] Development plan saved: {plan.Title}");
        
        // Add to collection if new
        var existing = MyDevelopmentPlans.FirstOrDefault(p => p.Id == plan.Id);
        if (existing == null)
        {
            MyDevelopmentPlans.Add(plan);
            Log("[MeViewModel] Added new development plan to collection");
        }
        else
        {
            // Update existing (replace in collection)
            var index = MyDevelopmentPlans.IndexOf(existing);
            MyDevelopmentPlans[index] = plan;
            Log("[MeViewModel] Updated existing development plan in collection");
        }

        // Update flyout if this is the selected plan
        if (SelectedDevelopmentPlan?.Id == plan.Id)
        {
            SelectedDevelopmentPlan = plan;
        }

        // Notify property changes
        OnPropertyChanged(nameof(ActivePlanCount));
        OnPropertyChanged(nameof(TotalPlanItemCount));
        OnPropertyChanged(nameof(CompletedPlanItemCount));
        OnPropertyChanged(nameof(HasDevelopmentPlans));
        OnPropertyChanged(nameof(SortedDevelopmentPlans));
    }

    // ==================== Calendar Commands ====================

    [RelayCommand]
    private void SetMeetingsViewMode(MeetingsViewMode mode)
    {
        MeetingsViewMode = mode;
        CloseFlyout();
        RefreshMeetingsView();
        Log($"[MeViewModel] Set meetings view mode: {mode}");
    }

    [RelayCommand]
    private void SetMeMeetingTab(MeMeetingTab tab)
    {
        MeMeetingTab = tab;
    }

    [RelayCommand]
    private void NavigatePrevious()
    {
        CurrentCalendarDate = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => CurrentCalendarDate.AddDays(-1),
            MeetingsViewMode.Week => CurrentCalendarDate.AddDays(-7),
            MeetingsViewMode.Month => CurrentCalendarDate.AddMonths(-1),
            _ => CurrentCalendarDate.AddDays(-7)
        };
        RefreshMeetingsView();
    }

    [RelayCommand]
    private void NavigateNext()
    {
        CurrentCalendarDate = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => CurrentCalendarDate.AddDays(1),
            MeetingsViewMode.Week => CurrentCalendarDate.AddDays(7),
            MeetingsViewMode.Month => CurrentCalendarDate.AddMonths(1),
            _ => CurrentCalendarDate.AddDays(7)
        };
        RefreshMeetingsView();
    }

    [RelayCommand]
    private void NavigateToday()
    {
        CurrentCalendarDate = DateTime.Today;
        RefreshMeetingsView();
    }

    private void RefreshMeetingsView()
    {
        OnPropertyChanged(nameof(CalendarDateHeader));

        // Update filtered meetings based on view
        FilteredMeetings.Clear();
        var meetings = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => MyMeetings.Where(m => m.LocalDate == CurrentCalendarDate.Date),
            MeetingsViewMode.Week => MyMeetings.Where(m =>
            {
                var weekStart = GetWeekStart(CurrentCalendarDate);
                var weekEnd = weekStart.AddDays(7);
                var date = m.LocalDate;
                return date >= weekStart && date < weekEnd;
            }),
            MeetingsViewMode.Month => MyMeetings.Where(m =>
            {
                var date = m.ScheduledAtLocal;
                return date?.Year == CurrentCalendarDate.Year && date?.Month == CurrentCalendarDate.Month;
            }),
            _ => MyMeetings.OrderBy(m => m.ScheduledAt)
        };

        foreach (var m in meetings.OrderBy(m => m.ScheduledAt))
        {
            FilteredMeetings.Add(m);
        }

        // Update day meetings for day view
        DayMeetings.Clear();
        var dayMeetings = MyMeetings
            .Where(m => m.LocalDate == CurrentCalendarDate.Date)
            .OrderBy(m => m.ScheduledAt);
        foreach (var m in dayMeetings)
        {
            DayMeetings.Add(m);
        }
        OnPropertyChanged(nameof(DayMeetings));
        OnPropertyChanged(nameof(HasNoDayMeetings));

        // Update grouped meetings for list view
        GroupedMeetings.Clear();
        var grouped = MyMeetings
            .Where(m => m.ScheduledAt >= DateTime.Now.AddDays(-1))
            .OrderBy(m => m.ScheduledAt)
            .GroupBy(m => m.DateGroupDisplay);

        foreach (var group in grouped)
        {
            GroupedMeetings.Add(new MeetingGroup
            {
                Date = group.Key,
                Meetings = new ObservableCollection<MeetingDetail>(group)
            });
        }
        OnPropertyChanged(nameof(HasNoGroupedMeetings));

        // Update week days
        RefreshWeekDays();

        // Update calendar days for month view
        RefreshCalendarDays();
    }

    private void RefreshWeekDays()
    {
        WeekDays.Clear();
        var weekStart = GetWeekStart(CurrentCalendarDate);
        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            WeekDays.Add(new CalendarWeekDay
            {
                Date = date,
                DayName = date.ToString("ddd"),
                DayNumber = date.Day.ToString(),
                IsToday = date.Date == DateTime.Today,
                Meetings = new ObservableCollection<MeetingDetail>(
                    MyMeetings.Where(m => m.LocalDate == date.Date)
                              .OrderBy(m => m.ScheduledAt))
            });
        }
        OnPropertyChanged(nameof(WeekDays));
    }

    private void RefreshCalendarDays()
    {
        CalendarDays.Clear();

        var firstOfMonth = new DateTime(CurrentCalendarDate.Year, CurrentCalendarDate.Month, 1);
        var calendarStart = GetWeekStart(firstOfMonth);

        // Fill 6 weeks (42 days)
        for (int i = 0; i < 42; i++)
        {
            var date = calendarStart.AddDays(i);
            var dayMeetings = MyMeetings
                .Where(m => m.LocalDate == date.Date)
                .OrderBy(m => m.ScheduledAt)
                .Take(3)
                .ToList();

            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                DayNumber = date.Day,
                IsCurrentMonth = date.Month == CurrentCalendarDate.Month,
                IsToday = date.Date == DateTime.Today,
                Meetings = new ObservableCollection<MeetingDetail>(dayMeetings),
                HasMoreMeetings = MyMeetings.Count(m => m.LocalDate == date.Date) > 3
            });
        }
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff).Date;
    }

    // ==================== Flyout Commands ====================

    /// <summary>
    /// Selects a meeting by its ID, opening the meeting flyout.
    /// Used for cross-tab navigation.
    /// </summary>
    public void SelectMeetingById(Guid meetingId)
    {
        var meeting = MyMeetings.FirstOrDefault(m => m.Id == meetingId);
        if (meeting != null)
        {
            OpenMeetingFlyout(meeting);
        }
    }

    [RelayCommand]
    private void OpenTaskFlyout(TaskDetail task)
    {
        SelectedTask = task;
        ActiveFlyoutType = MeFlyoutType.Task;
        Log($"[MeViewModel] Opened task flyout: {task.Title}");
    }

    [RelayCommand]
    private void OpenMeetingFlyout(MeetingDetail meeting)
    {
        SelectedMeeting = meeting;
        ActiveFlyoutType = MeFlyoutType.Meeting;
        Log($"[MeViewModel] Opened meeting flyout: {meeting.Title}");
    }

    [RelayCommand]
    private void OpenGoalFlyout(GoalDetail goal)
    {
        SelectedGoal = goal;
        ActiveFlyoutType = MeFlyoutType.Goal;
        Log($"[MeViewModel] Opened goal flyout: {goal.Title}");
    }

    [RelayCommand]
    private void OpenFeedbackFlyout(FeedbackDetail feedback)
    {
        SelectedFeedback = feedback;
        // Determine if this is from the given list (viewing given feedback shows recipient)
        // or from received list (viewing received feedback shows sender)
        IsSelectedFeedbackGiven = !IsShowingReceivedFeedback;
        ActiveFlyoutType = MeFlyoutType.Feedback;
        Log($"[MeViewModel] Opened feedback flyout: {feedback.Id}, IsGiven={IsSelectedFeedbackGiven}");
    }

    [RelayCommand]
    private void OpenDevelopmentPlanFlyout(DevelopmentPlan plan)
    {
        SelectedDevelopmentPlan = plan;
        ActiveFlyoutType = MeFlyoutType.DevelopmentPlan;
        Log($"[MeViewModel] Opened development plan flyout: {plan.Title}");
    }

    [RelayCommand]
    private void CloseFlyout()
    {
        ActiveFlyoutType = MeFlyoutType.None;
        Log("[MeViewModel] Closed flyout");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns urgency order for task sorting.
    /// 0 = Overdue, 1 = Due today, 2 = Due soon (1-3 days), 3 = Future, 4 = No date
    /// </summary>
    private int GetTaskUrgencyOrder(TaskDetail task)
    {
        if (task.DueDate == null) return 4;
        
        var today = DateTime.Today;
        var dueDate = task.DueDate.Value.Date;
        
        if (dueDate < today) return 0; // Overdue
        if (dueDate == today) return 1; // Due today
        if (dueDate <= today.AddDays(3)) return 2; // Due soon
        return 3; // Future
    }

    private void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    #endregion

    #region Insight Actions

    /// <summary>
    /// Dismisses an insight (marks it as dismissed).
    /// </summary>
    [RelayCommand]
    private async Task DismissInsight(Insight insight)
    {
        if (insight == null) return;

        try
        {
            Log($"[MeViewModel] Dismissing insight: {insight.Id}");
            
            if (!string.IsNullOrEmpty(insight.SignatureHash))
            {
                await InsightEngine.Instance.DismissInsightAsync(insight.SignatureHash);
            }
            else
            {
                Log($"[MeViewModel] Insight has no signature hash, cannot dismiss properly");
            }

            // Remove from collection
            MyInsights.Remove(insight);
            OnPropertyChanged(nameof(InsightCount));
            OnPropertyChanged(nameof(HasInsights));
            OnPropertyChanged(nameof(GroupedInsights));
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Failed to dismiss insight: {ex.Message}");
        }
    }

    /// <summary>
    /// Snoozes an insight for 24 hours.
    /// </summary>
    [RelayCommand]
    private async Task SnoozeInsight(Insight insight)
    {
        if (insight == null) return;

        try
        {
            Log($"[MeViewModel] Snoozing insight: {insight.Id}");
            
            if (!string.IsNullOrEmpty(insight.SignatureHash))
            {
                await InsightEngine.Instance.SnoozeInsightAsync(insight.SignatureHash, TimeSpan.FromHours(24));
            }
            else
            {
                Log($"[MeViewModel] Insight has no signature hash, cannot snooze properly");
            }

            // Remove from collection (will reappear after snooze expires)
            MyInsights.Remove(insight);
            OnPropertyChanged(nameof(InsightCount));
            OnPropertyChanged(nameof(HasInsights));
            OnPropertyChanged(nameof(GroupedInsights));
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] Failed to snooze insight: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates to the entity referenced by the insight.
    /// </summary>
    [RelayCommand]
    private void ViewInsight(Insight insight)
    {
        if (insight?.SourceId == null) return;

        Log($"[MeViewModel] Viewing insight entity: {insight.SourceType} {insight.SourceId}");

        // Navigate based on entity type
        switch (insight.SourceType?.ToLowerInvariant())
        {
            case "task":
                NavigateToTaskRequested?.Invoke(this, insight.SourceId.Value);
                break;
            case "goal":
                NavigateToGoalRequested?.Invoke(this, insight.SourceId.Value);
                break;
            case "meeting":
                NavigateToMeetingRequested?.Invoke(this, insight.SourceId.Value);
                break;
            case "metric":
                NavigateToMetricRequested?.Invoke(this, insight.SourceId.Value);
                break;
            default:
                Log($"[MeViewModel] Unknown entity type: {insight.SourceType}");
                break;
        }
    }

    // Navigation events for insight entity links
    public event EventHandler<Guid>? NavigateToTaskRequested;
    public event EventHandler<Guid>? NavigateToGoalRequested;
    public event EventHandler<Guid>? NavigateToMeetingRequested;
    public event EventHandler<Guid>? NavigateToMetricRequested;

    #endregion
}

/// <summary>
/// Groups insights by type for organized display in the Me view.
/// </summary>
public class InsightGroup
{
    public InsightType Type { get; }
    public string Title { get; }
    public string Icon { get; }
    public List<Insight> Insights { get; }
    public int Count => Insights.Count;

    public InsightGroup(InsightType type, List<Insight> insights)
    {
        Type = type;
        Insights = insights;
        Title = GetTitleForType(type);
        Icon = GetIconForType(type);
    }

    private static string GetTitleForType(InsightType type) => type switch
    {
        InsightType.StaleActionItem => "Stale Tasks",
        InsightType.TaskOverdue => "Overdue Tasks",
        InsightType.GoalOffTrack => "Goals Off Track",
        InsightType.GoalOnTrack => "Goals On Track",
        InsightType.MeetingOverdue => "Overdue Meetings",
        InsightType.MeetingUpcoming => "Upcoming Meetings",
        InsightType.MetricDeclining => "Declining Metrics",
        InsightType.MetricMissing => "Missing Metrics",
        InsightType.PersonalDate => "Personal Dates",
        InsightType.SentimentDeclining => "Declining Sentiment",
        InsightType.SentimentImproving => "Improving Sentiment",
        _ => type.ToString()
    };

    private static string GetIconForType(InsightType type) => type switch
    {
        InsightType.StaleActionItem or InsightType.TaskOverdue => 
            "M3,5H9V11H3V5M5,7V9H7V7H5M11,7H21V9H11V7M11,15H21V17H11V15M5,20L1.5,16.5L2.91,15.09L5,17.17L9.59,12.59L11,14L5,20Z",
        InsightType.GoalOffTrack or InsightType.GoalOnTrack => 
            "M5,21L7.5,13L1,9H8.5L11,1L13.5,9H21L14.5,13L17,21L11,16L5,21Z",
        InsightType.MeetingOverdue or InsightType.MeetingUpcoming => 
            "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1M17,12H12V17H17V12Z",
        InsightType.MetricDeclining or InsightType.MetricMissing => 
            "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z",
        InsightType.PersonalDate => 
            "M12,6A3.5,3.5 0 0,1 15.5,9.5A3.5,3.5 0 0,1 12,13A3.5,3.5 0 0,1 8.5,9.5A3.5,3.5 0 0,1 12,6M12,2A7.5,7.5 0 0,0 4.5,9.5C4.5,13.09 7.36,16 10.95,16.67L12,22L13.05,16.67C16.64,16 19.5,13.09 19.5,9.5A7.5,7.5 0 0,0 12,2Z",
        InsightType.SentimentDeclining or InsightType.SentimentImproving => 
            "M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4C22,2.89 21.1,2 20,2M6,9H18V11H6M14,14H6V12H14M18,8H6V6H18",
        _ => "M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6M11,10V14H13V10H11M11,16V18H13V16H11Z"
    };
}
