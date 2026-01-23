using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    Meetings
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
    Feedback
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
    private MeFlyoutType _activeFlyoutType = MeFlyoutType.None;

    [ObservableProperty]
    private TaskDetail? _selectedTask;

    [ObservableProperty]
    private MeetingDetail? _selectedMeeting;

    [ObservableProperty]
    private GoalDetail? _selectedGoal;

    [ObservableProperty]
    private FeedbackDetail? _selectedFeedback;

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

    // Flyout visibility helpers
    public bool IsFlyoutOpen => ActiveFlyoutType != MeFlyoutType.None;
    public bool IsTaskFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Task;
    public bool IsMeetingFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Meeting;
    public bool IsGoalFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Goal;
    public bool IsFeedbackFlyoutOpen => ActiveFlyoutType == MeFlyoutType.Feedback;

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

            StatusMessage = string.Empty;
            Log("[MeViewModel] LoadDataAsync completed");
        }
        catch (Exception ex)
        {
            Log($"[MeViewModel] ERROR: {ex.Message}");
            StatusMessage = "Failed to load data";
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

    [RelayCommand]
    private void CreateTask()
    {
        Log("[MeViewModel] CreateTask command - TODO: implement");
        // TODO: Open task creation dialog
    }

    [RelayCommand]
    private void CreateGoal()
    {
        Log("[MeViewModel] CreateGoal command - TODO: implement");
        // TODO: Open goal creation dialog
    }

    [RelayCommand]
    private void CreateNote()
    {
        Log("[MeViewModel] CreateNote command - TODO: implement");
        // TODO: Open note creation dialog
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

    #endregion

    /// <summary>
    /// Called when a meeting is saved (created or updated) from the dialog.
    /// The dialog is now the complete workspace, so we just add/update in the list.
    /// User can click the meeting later to open the flyout if they want.
    /// </summary>
    public void OnMeetingSaved(MeetingDetail meeting)
    {
        Log($"[MeViewModel] Meeting saved: {meeting.Title}");
        
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
}
