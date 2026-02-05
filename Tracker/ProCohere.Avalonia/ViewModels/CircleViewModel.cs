using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dispatcher = global::Avalonia.Threading.Dispatcher;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Circle (Team) area.
/// Manages Team Members, Goals, Feedback, and Meetings tabs.
/// </summary>
public partial class CircleViewModel : ViewModelBase
{
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "circle.log");

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
    }

    #region Tab Navigation

    /// <summary>
    /// The currently selected tab.
    /// </summary>
    [ObservableProperty]
    private CircleTab _selectedTab = CircleTab.Team;

    [RelayCommand]
    private void SelectTab(CircleTab tab)
    {
        SelectedTab = tab;
    }

    #endregion

    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingStatus = "Loading...";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    #endregion

    #region Team Stats

    [ObservableProperty]
    private int _totalMemberCount;

    [ObservableProperty]
    private int _activeMemberCount;

    [ObservableProperty]
    private int _meetingsOnTrackCount;

    [ObservableProperty]
    private int _meetingsOverdueCount;

    [ObservableProperty]
    private int _membersWithOpenTasksCount;

    public string TotalMemberCountText => TotalMemberCount.ToString();
    public string ActiveMemberCountText => ActiveMemberCount.ToString();
    public string MeetingsOnTrackCountText => MeetingsOnTrackCount.ToString();
    public string MeetingsOverdueCountText => MeetingsOverdueCount.ToString();

    #endregion

    #region Hierarchy Properties

    /// <summary>
    /// The current user's team member record.
    /// </summary>
    [ObservableProperty]
    private TeamMemberDetail? _currentTeamMember;

    /// <summary>
    /// The current user's manager (if any).
    /// </summary>
    [ObservableProperty]
    private TeamMemberDetail? _myManager;

    /// <summary>
    /// Direct reports of the current user.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> MyDirectReports { get; } = new();

    /// <summary>
    /// Peers of the current user (same manager).
    /// </summary>
    public ObservableCollection<TeamMemberDetail> MyPeers { get; } = new();

    /// <summary>
    /// Number of direct reports for the current user.
    /// </summary>
    public int DirectReportCount => MyDirectReports.Count;

    /// <summary>
    /// Number of peers for the current user.
    /// </summary>
    public int PeerCount => MyPeers.Count;

    /// <summary>
    /// Whether the current user is a manager (has direct reports).
    /// </summary>
    public bool IsCurrentUserManager => MyDirectReports.Count > 0;

    #endregion

    #region View Mode

    /// <summary>
    /// Current view mode for the team list (Flat or Tree).
    /// </summary>
    [ObservableProperty]
    private TeamViewMode _teamViewMode = TeamViewMode.Flat;

    /// <summary>
    /// Whether the current view mode is Flat.
    /// </summary>
    public bool IsFlatView => TeamViewMode == TeamViewMode.Flat;

    /// <summary>
    /// Whether the current view mode is Tree.
    /// </summary>
    public bool IsTreeView => TeamViewMode == TeamViewMode.Tree;

    partial void OnTeamViewModeChanged(TeamViewMode value)
    {
        OnPropertyChanged(nameof(IsFlatView));
        OnPropertyChanged(nameof(IsTreeView));
        ApplyFilters(); // Re-apply to re-sort for tree view
    }

    [RelayCommand]
    private void SetViewMode(TeamViewMode mode)
    {
        TeamViewMode = mode;
    }

    #endregion

    #region Filter & Search

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private TeamMemberFilter _memberFilter = TeamMemberFilter.All;

    /// <summary>
    /// When set, filters to show only this manager and their direct reports.
    /// </summary>
    [ObservableProperty]
    private TeamMemberDetail? _filterByManager;

    /// <summary>
    /// Display name for the manager filter breadcrumb.
    /// </summary>
    public string FilterByManagerName => FilterByManager?.FullName ?? string.Empty;

    /// <summary>
    /// Whether a manager filter is active.
    /// </summary>
    public bool HasManagerFilter => FilterByManager != null;

    partial void OnFilterByManagerChanged(TeamMemberDetail? value)
    {
        OnPropertyChanged(nameof(FilterByManagerName));
        OnPropertyChanged(nameof(HasManagerFilter));
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnMemberFilterChanged(TeamMemberFilter value)
    {
        ApplyFilters();
    }

    /// <summary>
    /// Sets a manager filter to show their team.
    /// </summary>
    [RelayCommand]
    private void SetManagerFilter(TeamMemberDetail? manager)
    {
        if (manager?.IsManager == true)
        {
            FilterByManager = manager;
        }
    }

    /// <summary>
    /// Clears the manager filter.
    /// </summary>
    [RelayCommand]
    private void ClearManagerFilter()
    {
        FilterByManager = null;
    }

    private void ApplyFilters()
    {
        // Skip if data is being updated to avoid concurrent modification
        if (_isUpdatingData) return;
        
        // Build filtered list first, then update collection on UI thread
        var filtered = _allTeamMembers.ToList().AsEnumerable();
        
        // Apply manager filter first (show manager + their direct reports)
        if (FilterByManager != null)
        {
            var managerId = FilterByManager.Id;
            filtered = filtered.Where(m => 
                m.Id == managerId || m.ManagerTeamMemberId == managerId);
        }
        
        // Apply status filter
        filtered = MemberFilter switch
        {
            TeamMemberFilter.Active => filtered.Where(m => m.IsActive),
            TeamMemberFilter.Inactive => filtered.Where(m => !m.IsActive),
            TeamMemberFilter.NeedsAttention => filtered.Where(m => m.NeedsAttention),
            _ => filtered
        };
        
        // Apply search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(m => 
                m.FullName.ToLower().Contains(search) ||
                (m.Email?.ToLower().Contains(search) ?? false) ||
                (m.JobTitle?.ToLower().Contains(search) ?? false));
        }
        
        // Apply sorting based on view mode
        if (IsTreeView)
        {
            // Tree view: sort by hierarchy (manager first, then by depth, then alphabetically)
            // Group by manager chain to create proper tree ordering
            filtered = BuildTreeOrder(filtered);
        }
        else
        {
            // Flat view: sort alphabetically by name, reset display depth
            foreach (var m in filtered)
            {
                m.DisplayDepth = 0;
            }
            filtered = filtered.OrderBy(m => m.FullName);
        }
        
        // Materialize the list before updating the collection
        var finalList = filtered.ToList();
        
        // Update collection - clear and add in one batch to minimize UI updates
        FilteredTeamMembers.Clear();
        foreach (var member in finalList)
        {
            FilteredTeamMembers.Add(member);
        }
        
        OnPropertyChanged(nameof(FilteredMemberCount));
    }

    /// <summary>
    /// Builds a tree-ordered sequence from team members based on hierarchy.
    /// Order: At each level, leaf nodes (no reports) come first alphabetically,
    /// then managers with their subtrees (manager shown, then their subtree indented).
    /// </summary>
    private IEnumerable<TeamMemberDetail> BuildTreeOrder(IEnumerable<TeamMemberDetail> members)
    {
        var memberList = members.ToList();
        var memberDict = memberList.ToDictionary(m => m.Id);
        var result = new List<TeamMemberDetail>();
        var visited = new HashSet<Guid>();
        
        // Pre-compute who has reports in the visible set
        var hasReportsInSet = new HashSet<Guid>(
            memberList
                .Where(m => m.ManagerTeamMemberId.HasValue && memberDict.ContainsKey(m.ManagerTeamMemberId.Value))
                .Select(m => m.ManagerTeamMemberId!.Value)
        );
        
        // Find root nodes (no manager or manager not in visible set)
        var roots = memberList
            .Where(m => m.ManagerTeamMemberId == null || !memberDict.ContainsKey(m.ManagerTeamMemberId.Value))
            .ToList();
        
        void AddSubtree(IEnumerable<TeamMemberDetail> nodes, int depth)
        {
            var nodeList = nodes.ToList();
            
            // Split into leaf nodes (no reports in visible set) and managers (have reports)
            var leafNodes = nodeList
                .Where(m => !hasReportsInSet.Contains(m.Id))
                .OrderBy(m => m.FullName)
                .ToList();
            
            var managerNodes = nodeList
                .Where(m => hasReportsInSet.Contains(m.Id))
                .OrderBy(m => m.FullName)
                .ToList();
            
            // First add all leaf nodes at this level
            foreach (var leaf in leafNodes)
            {
                if (visited.Contains(leaf.Id)) continue;
                visited.Add(leaf.Id);
                leaf.DisplayDepth = depth;
                result.Add(leaf);
            }
            
            // Then add each manager followed by their subtree
            foreach (var manager in managerNodes)
            {
                if (visited.Contains(manager.Id)) continue;
                visited.Add(manager.Id);
                manager.DisplayDepth = depth;
                result.Add(manager);
                
                // Get this manager's direct reports and recurse
                var reports = memberList
                    .Where(m => m.ManagerTeamMemberId == manager.Id)
                    .ToList();
                
                if (reports.Any())
                {
                    AddSubtree(reports, depth + 1);
                }
            }
        }
        
        AddSubtree(roots, 0);
        
        // Add any orphaned members (shouldn't happen but safety net)
        foreach (var member in memberList.Where(m => !visited.Contains(m.Id)))
        {
            member.DisplayDepth = 0;
            result.Add(member);
        }
        
        return result;
    }

    public int FilteredMemberCount => FilteredTeamMembers.Count;

    [RelayCommand]
    private void SetFilter(TeamMemberFilter filter)
    {
        MemberFilter = filter;
    }

    [RelayCommand]
    private void ClearFilter()
    {
        MemberFilter = TeamMemberFilter.All;
        SearchText = string.Empty;
    }

    #endregion

    #region Data Collections

    private readonly List<TeamMemberDetail> _allTeamMembers = new();
    private bool _isUpdatingData;

    /// <summary>
    /// Filtered team members displayed in the list.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> FilteredTeamMembers { get; } = new();

    /// <summary>
    /// Goals for the team.
    /// </summary>
    public ObservableCollection<GoalDetail> Goals { get; } = new();

    /// <summary>
    /// Feedback items.
    /// </summary>
    public ObservableCollection<FeedbackDetail> Feedback { get; } = new();

    /// <summary>
    /// Meetings list.
    /// </summary>
    public ObservableCollection<MeetingDetail> Meetings { get; } = new();

    #endregion

    #region Selected Item & Detail Panel

    [ObservableProperty]
    private TeamMemberDetail? _selectedTeamMember;

    [ObservableProperty]
    private bool _isDetailPanelOpen;

    /// <summary>
    /// Whether the selected team member is a manager (has direct reports).
    /// </summary>
    public bool SelectedMemberIsManager => SelectedTeamMember?.IsManager == true;

    partial void OnSelectedTeamMemberChanged(TeamMemberDetail? oldValue, TeamMemberDetail? newValue)
    {
        // Update selection state on the models
        if (oldValue != null)
            oldValue.IsSelected = false;
        if (newValue != null)
        {
            newValue.IsSelected = true;
            // Reset to Overview tab when member changes
            newValue.MemberDetailTab = MemberDetailTab.Overview;
            // Load related data onto the member
            LoadMemberRelatedData(newValue);
        }
        
        // Notify that SelectedMemberIsManager may have changed
        OnPropertyChanged(nameof(SelectedMemberIsManager));
    }

    private void LoadMemberRelatedData(TeamMemberDetail member)
    {
        member.MemberGoals.Clear();
        member.MemberMeetings.Clear();
        member.MemberFeedback.Clear();
        member.MemberKudos.Clear();
        member.MemberDirectReports.Clear();
        member.MemberTasks.Clear();

        // Load tasks asynchronously
        _ = LoadMemberTasksAsync(member);
        
        // Load kudos asynchronously
        _ = LoadMemberKudosAsync(member);

        // Snapshot collections to avoid enumeration modification errors
        var teamMembers = _allTeamMembers.ToList();
        var goals = _allGoals.ToList();
        var meetings = Meetings.ToList();
        var feedback = _allFeedback.ToList();

        // Get direct reports for this member (if they're a manager)
        foreach (var report in teamMembers.Where(m => m.ManagerTeamMemberId == member.Id))
        {
            member.MemberDirectReports.Add(report);
        }

        // Filter goals owned by this member
        foreach (var goal in goals.Where(g => g.OwnerTeamMemberId == member.Id))
        {
            member.MemberGoals.Add(goal);
        }

        // Filter meetings with this member (by linked team member or attendee name match)
        foreach (var meeting in meetings.Where(m => 
            m.TeamMemberId == member.Id ||
            m.Attendees?.Any(a => a.Name == member.FullName || a.Email == member.Email) == true))
        {
            member.MemberMeetings.Add(meeting);
        }

        // Filter feedback for this member
        foreach (var fb in feedback.Where(f => f.TeamMemberId == member.Id))
        {
            member.MemberFeedback.Add(fb);
        }
    }

    /// <summary>
    /// Loads tasks for the specified team member asynchronously.
    /// </summary>
    private async System.Threading.Tasks.Task LoadMemberTasksAsync(TeamMemberDetail member)
    {
        try
        {
            var tasks = await TaskService.Instance.GetTasksByAssigneeAsync(member.Id, includeCompleted: false);
            member.MemberTasks.Clear();
            foreach (var task in tasks)
            {
                member.MemberTasks.Add(task);
            }
        }
        catch (Exception ex)
        {
            Log($"Error loading member tasks: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads kudos received by the specified team member asynchronously.
    /// </summary>
    private async System.Threading.Tasks.Task LoadMemberKudosAsync(TeamMemberDetail member)
    {
        try
        {
            var kudos = await KudosService.Instance.GetKudosReceivedAsync(member.Id);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                member.MemberKudos.Clear();
                foreach (var k in kudos)
                {
                    member.MemberKudos.Add(k);
                }
            });
        }
        catch (Exception ex)
        {
            Log($"Error loading member kudos: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SetMemberDetailTab(MemberDetailTab tab)
    {
        if (SelectedTeamMember != null)
            SelectedTeamMember.MemberDetailTab = tab;
    }

    /// <summary>
    /// Select a team member and open the detail panel.
    /// </summary>
    [RelayCommand]
    private void SelectTeamMember(TeamMemberDetail? member)
    {
        if (member == null)
        {
            SelectedTeamMember = null;
            IsDetailPanelOpen = false;
            return;
        }

        // If clicking the same member, toggle the panel
        if (SelectedTeamMember?.Id == member.Id)
        {
            IsDetailPanelOpen = !IsDetailPanelOpen;
            if (!IsDetailPanelOpen)
                SelectedTeamMember = null;
        }
        else
        {
            // Wire IDetailEntity commands before setting selection
            member.CloseCommand = CloseDetailPanelCommand;
            member.EditCommand = new RelayCommand(() => EditTeamMember(member));
            member.GiveKudosCommand = new RelayCommand(() => GiveKudos(member));
            member.SendMessageCommand = new RelayCommand(() => SendMessage(member));
            // Team members are deactivated via the edit dialog, not deleted directly
            member.DeleteCommand = null;
            member.SetMemberDetailTabCommand = new RelayCommand<MemberDetailTab>(tab => member.MemberDetailTab = tab);
            
            SelectedTeamMember = member;
            IsDetailPanelOpen = true;
        }
    }

    /// <summary>
    /// Close the detail panel.
    /// </summary>
    [RelayCommand]
    private void CloseDetailPanel()
    {
        IsDetailPanelOpen = false;
        SelectedTeamMember = null;
    }

    /// <summary>
    /// Open a URL in the default browser.
    /// </summary>
    [RelayCommand]
    private void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    #endregion

    #region Meetings Tab

    [ObservableProperty]
    private MeetingsViewMode _meetingsViewMode = MeetingsViewMode.Week;

    [ObservableProperty]
    private DateTime _currentDate = DateTime.Today;

    [ObservableProperty]
    private MeetingDetail? _selectedMeeting;

    [ObservableProperty]
    private bool _isMeetingDetailOpen;

    /// <summary>
    /// Meetings filtered for the current view (based on date range).
    /// </summary>
    public ObservableCollection<MeetingDetail> FilteredMeetings { get; } = new();

    /// <summary>
    /// Meetings grouped by date for list view.
    /// </summary>
    public ObservableCollection<MeetingGroup> GroupedMeetings { get; } = new();

    /// <summary>
    /// Calendar days for month view.
    /// </summary>
    public ObservableCollection<CalendarDay> CalendarDays { get; } = new();

    /// <summary>
    /// Hours for day/week view (5 AM to 8 PM).
    /// </summary>
    public List<CalendarHour> CalendarHours { get; } = Enumerable.Range(5, 16)
        .Select(h => new CalendarHour { Hour = h, DisplayText = DateTime.Today.AddHours(h).ToString("h tt") })
        .ToList();

    /// <summary>
    /// Week days for the current week in week view.
    /// </summary>
    public ObservableCollection<CalendarWeekDay> WeekDays { get; } = new();

    /// <summary>
    /// Current view date header text.
    /// </summary>
    public string CurrentDateHeader => MeetingsViewMode switch
    {
        MeetingsViewMode.Day => CurrentDate.ToString("dddd, MMMM d, yyyy"),
        MeetingsViewMode.Week => $"{GetWeekStart(CurrentDate):MMM d} - {GetWeekStart(CurrentDate).AddDays(6):MMM d, yyyy}",
        MeetingsViewMode.Month => CurrentDate.ToString("MMMM yyyy"),
        _ => CurrentDate.ToString("MMMM yyyy")
    };

    /// <summary>
    /// Meetings for the selected day in day view.
    /// Returns a snapshot to prevent collection modified errors during enumeration.
    /// </summary>
    public IEnumerable<MeetingDetail> DayMeetings => Meetings
        .Where(m => m.LocalDate == CurrentDate.Date)
        .OrderBy(m => m.ScheduledAt)
        .ToList();

    [RelayCommand]
    private void SetMeetingsViewMode(MeetingsViewMode mode)
    {
        MeetingsViewMode = mode;
        // Close flyout when switching views
        IsMeetingDetailOpen = false;
        SelectedMeeting = null;
        RefreshMeetingsView();
    }

    [RelayCommand]
    private void NavigatePrevious()
    {
        CurrentDate = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => CurrentDate.AddDays(-1),
            MeetingsViewMode.Week => CurrentDate.AddDays(-7),
            MeetingsViewMode.Month => CurrentDate.AddMonths(-1),
            _ => CurrentDate.AddDays(-7)
        };
        RefreshMeetingsView();
    }

    [RelayCommand]
    private void NavigateNext()
    {
        CurrentDate = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => CurrentDate.AddDays(1),
            MeetingsViewMode.Week => CurrentDate.AddDays(7),
            MeetingsViewMode.Month => CurrentDate.AddMonths(1),
            _ => CurrentDate.AddDays(7)
        };
        RefreshMeetingsView();
    }

    [RelayCommand]
    private void NavigateToday()
    {
        CurrentDate = DateTime.Today;
        RefreshMeetingsView();
    }

    [RelayCommand]
    private async Task SelectMeeting(MeetingDetail? meeting)
    {
        if (meeting == null)
        {
            SelectedMeeting = null;
            IsMeetingDetailOpen = false;
            return;
        }

        if (SelectedMeeting?.Id == meeting.Id)
        {
            IsMeetingDetailOpen = !IsMeetingDetailOpen;
            if (!IsMeetingDetailOpen)
                SelectedMeeting = null;
        }
        else
        {
            // Wire up IDetailEntity commands before setting the meeting
            meeting.CloseCommand = CloseMeetingDetailCommand;
            meeting.EditCommand = new RelayCommand(() => EditMeeting(meeting));
            meeting.DeleteCommand = new AsyncRelayCommand(() => DeleteMeetingAsync(meeting));
            meeting.SetMeetingDetailTabCommand = new RelayCommand<MeetingDetailTab>(tab => meeting.MeetingDetailTab = tab);
            
            // Load linked tasks and prep items for this meeting
            await LoadLinkedTasksForMeetingAsync(meeting);
            await LoadPrepItemsForMeetingAsync(meeting);
            
            // Reset to Overview tab when switching meetings
            meeting.MeetingDetailTab = MeetingDetailTab.Overview;
            
            SelectedMeeting = meeting;
            IsMeetingDetailOpen = true;
        }
    }

    [RelayCommand]
    private void CloseMeetingDetail()
    {
        IsMeetingDetailOpen = false;
        SelectedMeeting = null;
    }

    /// <summary>
    /// Loads all tasks linked to a meeting (created from its agenda items).
    /// </summary>
    private async Task LoadLinkedTasksForMeetingAsync(MeetingDetail meeting)
    {
        try
        {
            var tasks = await TaskService.Instance.GetTasksForMeetingAsync(meeting.Id);
            
            meeting.LinkedTasks.Clear();
            foreach (var task in tasks)
            {
                meeting.LinkedTasks.Add(task);
            }
            
            Log($"[CircleViewModel] Loaded {tasks.Count} linked tasks for meeting: {meeting.Title}");
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] Error loading linked tasks for meeting: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads all prep items for a meeting.
    /// </summary>
    private async Task LoadPrepItemsForMeetingAsync(MeetingDetail meeting)
    {
        try
        {
            var prepItems = await MeetingPrepItemService.Instance.GetPrepItemsForMeetingAsync(meeting.Id);
            
            meeting.LinkedPrepItems.Clear();
            foreach (var prepItem in prepItems)
            {
                meeting.LinkedPrepItems.Add(prepItem);
            }
            
            Log($"[CircleViewModel] Loaded {prepItems.Count} prep items for meeting: {meeting.Title}");
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] Error loading prep items for meeting: {ex.Message}");
        }
    }

    private void EditMeeting(MeetingDetail meeting)
    {
        // TODO: Implement edit meeting dialog
        Log($"[CircleViewModel] Edit meeting requested: {meeting.Title}");
    }

    private async Task DeleteMeetingAsync(MeetingDetail meeting)
    {
        // Show confirmation dialog
        var confirmed = await ConfirmationService.Instance.ShowDestructiveConfirmationAsync(
            "Delete Meeting",
            $"Are you sure you want to delete '{meeting.Title}'? This action cannot be undone.",
            "Delete Meeting",
            "Cancel");
        
        if (!confirmed)
            return;
        
        try
        {
            var success = await MeetingService.Instance.DeleteMeetingAsync(meeting.Id);
            if (success)
            {
                OnMeetingDeleted(meeting.Id);
                CloseMeetingDetail();
                NotificationService.Instance.ShowSuccess("Meeting Deleted", $"'{meeting.Title}' has been removed.");
            }
            else
            {
                NotificationService.Instance.ShowError("Delete Failed", MeetingService.Instance.LastError ?? "Failed to delete meeting");
            }
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] Delete meeting failed: {ex.Message}");
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
        }
    }

    /// <summary>
    /// Creates a task from an agenda item.
    /// </summary>
    [RelayCommand]
    private async Task CreateTaskFromAgendaItemAsync(MeetingAgendaItem? item)
    {
        if (item == null) return;

        Log($"Creating task from agenda item: {item.Title}");

        try
        {
            // Create the task via service
            var task = await MeetingAgendaItemService.Instance.CreateTaskFromAgendaItemAsync(
                item.Id,
                item.Description,
                "medium",   // Default priority
                null,       // No due date by default
                null        // Unassigned by default
            );

            if (task != null)
            {
                Log($"Task created: {task.Id} - {task.Title}");

                // Update the local item state
                item.Status = "action_created";
                item.IsCompleted = true;
                item.LinkedEntityType = "task";
                item.LinkedEntityId = task.Id;

                // Refresh the meeting to show updated status
                if (SelectedMeeting != null)
                {
                    var updatedItem = SelectedMeeting.AgendaItems.FirstOrDefault(a => a.Id == item.Id);
                    if (updatedItem != null)
                    {
                        updatedItem.Status = "action_created";
                        updatedItem.IsCompleted = true;
                        updatedItem.LinkedEntityType = "task";
                        updatedItem.LinkedEntityId = task.Id;
                    }
                }
            }
            else
            {
                Log($"Failed to create task: {MeetingAgendaItemService.Instance.LastError}");
            }
        }
        catch (Exception ex)
        {
            Log($"Error creating task from agenda item: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets agenda item status to Open.
    /// </summary>
    [RelayCommand]
    private async Task SetAgendaItemOpenAsync(MeetingAgendaItem? item)
    {
        await SetAgendaItemStatusInternalAsync(item, "open");
    }

    /// <summary>
    /// Sets agenda item status to Discussed.
    /// </summary>
    [RelayCommand]
    private async Task SetAgendaItemDiscussedAsync(MeetingAgendaItem? item)
    {
        await SetAgendaItemStatusInternalAsync(item, "discussed");
    }

    /// <summary>
    /// Sets agenda item status to Deferred.
    /// </summary>
    [RelayCommand]
    private async Task SetAgendaItemDeferredAsync(MeetingAgendaItem? item)
    {
        await SetAgendaItemStatusInternalAsync(item, "deferred");
    }

    /// <summary>
    /// Sets agenda item status to Dropped.
    /// </summary>
    [RelayCommand]
    private async Task SetAgendaItemDroppedAsync(MeetingAgendaItem? item)
    {
        await SetAgendaItemStatusInternalAsync(item, "dropped");
    }

    /// <summary>
    /// Internal helper to update agenda item status.
    /// </summary>
    private async Task SetAgendaItemStatusInternalAsync(MeetingAgendaItem? item, string newStatus)
    {
        if (item == null) return;

        Log($"Setting agenda item '{item.Title}' status to: {newStatus}");

        try
        {
            var updated = await MeetingAgendaItemService.Instance.UpdateStatusAsync(item.Id, newStatus);
            if (updated)
            {
                // Update local state
                item.Status = newStatus;
                
                // If marked as discussed/deferred/dropped, also mark completed
                if (newStatus == "discussed" || newStatus == "deferred" || newStatus == "dropped")
                {
                    item.IsCompleted = true;
                }
                else if (newStatus == "open")
                {
                    item.IsCompleted = false;
                }

                Log($"Agenda item status updated to: {newStatus}");
            }
            else
            {
                Log($"Failed to update status: {MeetingAgendaItemService.Instance.LastError}");
            }
        }
        catch (Exception ex)
        {
            Log($"Error updating agenda item status: {ex.Message}");
        }
    }

    #region Agenda Item Outcome Commands

    /// <summary>
    /// Records an outcome (decision, feedback, or notes) for an agenda item.
    /// Called by View after showing RecordOutcomeDialog.
    /// </summary>
    public async Task RecordOutcomeAsync(Guid agendaItemId, string outcomeType, string content, string visibility)
    {
        try
        {
            var outcomeService = AgendaItemOutcomeService.Instance;
            
            switch (outcomeType)
            {
                case OutcomeType.DecisionRecorded:
                    await outcomeService.RecordDecisionAsync(agendaItemId, content, visibility);
                    break;
                case OutcomeType.FeedbackCaptured:
                    await outcomeService.CaptureFeedbackAsync(agendaItemId, content, visibility);
                    break;
                case OutcomeType.NotesAdded:
                    await outcomeService.AddNotesAsync(agendaItemId, content, visibility);
                    break;
            }
            
            Log($"Recorded {outcomeType} for agenda item: {agendaItemId}");
        }
        catch (Exception ex)
        {
            Log($"Error recording outcome: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads outcomes for an agenda item.
    /// </summary>
    public async Task<List<AgendaItemOutcomeDetail>> LoadAgendaItemOutcomesAsync(Guid agendaItemId)
    {
        try
        {
            return await AgendaItemOutcomeService.Instance.GetOutcomesForAgendaItemAsync(agendaItemId);
        }
        catch (Exception ex)
        {
            Log($"Error loading outcomes: {ex.Message}");
            return new List<AgendaItemOutcomeDetail>();
        }
    }

    /// <summary>
    /// Defers an agenda item with carry-forward.
    /// Called by View after showing DeferAgendaItemDialog.
    /// </summary>
    public async Task DeferAgendaItemWithCarryForwardAsync(Guid agendaItemId, Guid anchorTeamMemberId, int expirationDays)
    {
        try
        {
            await CarryForwardService.Instance.DeferAgendaItemAsync(agendaItemId, anchorTeamMemberId, expirationDays);
            Log($"Deferred agenda item: {agendaItemId} to team member {anchorTeamMemberId}");
        }
        catch (Exception ex)
        {
            Log($"Error deferring agenda item: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets team members for defer dialog.
    /// </summary>
    public async Task<List<TeamMemberDetail>> GetTeamMembersForDeferAsync()
    {
        try
        {
            return await TeamService.Instance.GetVisibleTeamMembersAsync();
        }
        catch (Exception ex)
        {
            Log($"Error getting team members: {ex.Message}");
            return new List<TeamMemberDetail>();
        }
    }

    #endregion

    private static DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private void RefreshMeetingsView()
    {
        OnPropertyChanged(nameof(CurrentDateHeader));
        OnPropertyChanged(nameof(DayMeetings));

        // Take a snapshot of Meetings to prevent collection modified errors
        var meetingsSnapshot = Meetings.ToList();
        
        // Update filtered meetings based on view
        FilteredMeetings.Clear();
        var meetings = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => meetingsSnapshot.Where(m => m.LocalDate == CurrentDate.Date),
            MeetingsViewMode.Week => meetingsSnapshot.Where(m => 
            {
                var weekStart = GetWeekStart(CurrentDate);
                var weekEnd = weekStart.AddDays(7);
                var date = m.LocalDate;
                return date >= weekStart && date < weekEnd;
            }),
            MeetingsViewMode.Month => meetingsSnapshot.Where(m => 
            {
                var date = m.ScheduledAtLocal;
                return date?.Year == CurrentDate.Year && date?.Month == CurrentDate.Month;
            }),
            _ => meetingsSnapshot.OrderBy(m => m.ScheduledAt)
        };

        foreach (var m in meetings.OrderBy(m => m.ScheduledAt))
        {
            FilteredMeetings.Add(m);
        }

        // Update grouped meetings for list view
        GroupedMeetings.Clear();
        var grouped = meetingsSnapshot
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

        // Update week days
        RefreshWeekDays();

        // Update calendar days for month view
        RefreshCalendarDays();
    }

    private void RefreshWeekDays()
    {
        WeekDays.Clear();
        var weekStart = GetWeekStart(CurrentDate);
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
                    Meetings.Where(m => m.LocalDate == date.Date)
                           .OrderBy(m => m.ScheduledAt))
            });
        }
        // Notify view to rebuild week view (ObservableCollection changes don't trigger PropertyChanged)
        OnPropertyChanged(nameof(WeekDays));
    }

    private void RefreshCalendarDays()
    {
        CalendarDays.Clear();
        
        // Take a snapshot to prevent collection modified errors
        var meetingsSnapshot = Meetings.ToList();
        
        var firstOfMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        
        // Get the Sunday before (or of) the first day
        var calendarStart = GetWeekStart(firstOfMonth);
        
        // Fill 6 weeks (42 days)
        for (int i = 0; i < 42; i++)
        {
            var date = calendarStart.AddDays(i);
            var dayMeetings = meetingsSnapshot
                .Where(m => m.LocalDate == date.Date)
                .OrderBy(m => m.ScheduledAt)
                .Take(3) // Show max 3 in month view
                .ToList();

            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                DayNumber = date.Day,
                IsCurrentMonth = date.Month == CurrentDate.Month,
                IsToday = date.Date == DateTime.Today,
                Meetings = new ObservableCollection<MeetingDetail>(dayMeetings),
                HasMoreMeetings = meetingsSnapshot.Count(m => m.LocalDate == date.Date) > 3
            });
        }
    }

    #endregion

    #region Dialog Events

    /// <summary>
    /// Event to request showing the Edit Team Member dialog.
    /// </summary>
    public event EventHandler<TeamMemberDetail>? EditTeamMemberDialogRequested;

    /// <summary>
    /// Event to request showing the Invite Team Member dialog.
    /// </summary>
    public event EventHandler? InviteTeamMemberDialogRequested;

    /// <summary>
    /// Event to request showing the Create Meeting dialog.
    /// The TeamMemberDetail parameter is the pre-selected attendee (for "Schedule Meeting with [Person]").
    /// Pass null for a general meeting creation without pre-selection.
    /// </summary>
    public event EventHandler<TeamMemberDetail?>? CreateMeetingDialogRequested;

    /// <summary>
    /// Event to request showing the Add/Create Goal dialog.
    /// </summary>
    public event EventHandler? AddGoalDialogRequested;

    public event EventHandler<TeamMemberDetail>? GiveFeedbackDialogRequested;

    /// <summary>
    /// Event to request showing the Give Kudos dialog.
    /// </summary>
    public event EventHandler<TeamMemberDetail>? GiveKudosDialogRequested;

    /// <summary>
    /// Event to request showing the Quick Message dialog.
    /// </summary>
    public event EventHandler<TeamMemberDetail>? SendMessageDialogRequested;

    #endregion

    #region Commands

    [RelayCommand]
    private void AddTeamMember()
    {
        Log("AddTeamMember command - requesting invite dialog");
        InviteTeamMemberDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void EditTeamMember(TeamMemberDetail? member)
    {
        if (member == null) return;
        Log($"EditTeamMember command - requesting details dialog for {member.FullName}");
        EditTeamMemberDialogRequested?.Invoke(this, member);
    }

    [RelayCommand]
    private void ScheduleMeeting(TeamMemberDetail? member)
    {
        Log($"ScheduleMeeting command - requesting dialog for {member?.FullName ?? "team"}");
        CreateMeetingDialogRequested?.Invoke(this, member);
    }

    [RelayCommand]
    private void SendEmail(TeamMemberDetail? member)
    {
        if (member == null || string.IsNullOrEmpty(member.Email)) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = $"mailto:{member.Email}",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open email: {ex.Message}");
        }
    }

    [RelayCommand]
    private void GiveKudos(TeamMemberDetail? member)
    {
        if (member == null) return;
        Log($"GiveKudos command - requesting dialog for {member.FullName}");
        GiveKudosDialogRequested?.Invoke(this, member);
    }

    [RelayCommand]
    private void SendMessage(TeamMemberDetail? member)
    {
        if (member == null) return;
        Log($"SendMessage command - requesting dialog for {member.FullName}");
        SendMessageDialogRequested?.Invoke(this, member);
    }

    [RelayCommand]
    private void AddGoal()
    {
        Debug.WriteLine("Add Goal clicked");
        AddGoalDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task GiveFeedbackAsync()
    {
        if (SelectedTeamMember == null)
        {
            Debug.WriteLine("Give Feedback clicked but no team member selected");
            return;
        }
        
        Debug.WriteLine($"Give Feedback clicked for {SelectedTeamMember.FullName}");
        GiveFeedbackDialogRequested?.Invoke(this, SelectedTeamMember);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    #endregion

    #region Meeting Dialog Callbacks

    /// <summary>
    /// Called by the View when a meeting is saved from the dialog.
    /// </summary>
    public void OnMeetingSaved(MeetingDetail meeting)
    {
        Log($"[CircleViewModel] Meeting saved: {meeting.Title}");
        
        // Update main meetings collection
        var existing = Meetings.FirstOrDefault(m => m.Id == meeting.Id);
        if (existing == null)
        {
            Meetings.Add(meeting);
            Log("[CircleViewModel] Added new meeting to Meetings collection");
        }
        else
        {
            var index = Meetings.IndexOf(existing);
            Meetings[index] = meeting;
            Log("[CircleViewModel] Updated existing meeting in Meetings collection");
        }
        
        // Update filtered meetings if visible
        var existingFiltered = FilteredMeetings.FirstOrDefault(m => m.Id == meeting.Id);
        if (existingFiltered == null)
        {
            // Check if this meeting should be in filtered view based on selected member
            if (SelectedTeamMember == null || 
                meeting.Attendees?.Any(a => a.TeamMemberId == SelectedTeamMember.Id) == true)
            {
                FilteredMeetings.Add(meeting);
            }
        }
        else
        {
            var index = FilteredMeetings.IndexOf(existingFiltered);
            FilteredMeetings[index] = meeting;
        }
        
        // Refresh calendar views to reflect changes
        RefreshWeekDays();
        RefreshCalendarDays();
    }

    /// <summary>
    /// Called by the View when a meeting is deleted from the dialog.
    /// </summary>
    public void OnMeetingDeleted(Guid meetingId)
    {
        Log($"[CircleViewModel] Meeting deleted: {meetingId}");
        
        var existing = Meetings.FirstOrDefault(m => m.Id == meetingId);
        if (existing != null)
        {
            Meetings.Remove(existing);
        }
        
        var existingFiltered = FilteredMeetings.FirstOrDefault(m => m.Id == meetingId);
        if (existingFiltered != null)
        {
            FilteredMeetings.Remove(existingFiltered);
        }
        
        // Refresh calendar views
        RefreshWeekDays();
        RefreshCalendarDays();
    }

    #endregion

    #region Goals Tab

    [ObservableProperty]
    private GoalDetail? _selectedGoal;

    [ObservableProperty]
    private bool _isGoalDetailOpen;

    [ObservableProperty]
    private GoalFilter _goalFilter = GoalFilter.All;

    [ObservableProperty]
    private GoalDetailTab _goalDetailTab = GoalDetailTab.Overview;

    /// <summary>
    /// All goals.
    /// </summary>
    private readonly ObservableCollection<GoalDetail> _allGoals = new();

    /// <summary>
    /// Filtered goals displayed in the list.
    /// </summary>
    public ObservableCollection<GoalDetail> FilteredGoals { get; } = new();

    /// <summary>
    /// Targets (key results) for the selected goal.
    /// </summary>
    public ObservableCollection<TargetDetail> GoalTargets { get; } = new();

    /// <summary>
    /// Tasks linked to the selected goal.
    /// </summary>
    public ObservableCollection<TaskDetail> GoalTasks { get; } = new();

    /// <summary>
    /// Metrics linked to the selected goal.
    /// Per CIRCLE_METRICS_SPEC: Shows signal (trend arrow), not raw values.
    /// </summary>
    public ObservableCollection<MetricDetail> GoalLinkedMetrics { get; } = new();

    /// <summary>
    /// Recent meetings where this goal was discussed.
    /// Per GOALS_SPEC: Goal drill-in shows recent discussions.
    /// </summary>
    public ObservableCollection<GoalDiscussion> GoalRecentDiscussions { get; } = new();

    /// <summary>
    /// Whether linked metrics are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingLinkedMetrics;

    /// <summary>
    /// Whether recent discussions are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingDiscussions;

    /// <summary>
    /// Error message for linking operations.
    /// </summary>
    [ObservableProperty]
    private string? _linkError;

    // Goal stats - uses DerivedHealth from linked metrics per GOALS_SPEC
    // Circle must NOT use legacy goals.status field
    public int OnTrackGoalsCount => _allGoals.Count(g => g.DerivedHealth == GoalDerivedHealth.OnTrack);
    public int AtRiskGoalsCount => _allGoals.Count(g => g.DerivedHealth == GoalDerivedHealth.AtRisk);
    public int OffTrackGoalsCount => _allGoals.Count(g => g.DerivedHealth == GoalDerivedHealth.OffTrack);
    public int UnknownGoalsCount => _allGoals.Count(g => g.DerivedHealth == GoalDerivedHealth.Unknown);
    public int TotalGoalsCount => _allGoals.Count;

    partial void OnGoalFilterChanged(GoalFilter value)
    {
        ApplyGoalFilters();
        // Close detail panel when filter changes
        IsGoalDetailOpen = false;
        SelectedGoal = null;
    }

    private void ApplyGoalFilters()
    {
        FilteredGoals.Clear();
        
        // Use DerivedHealth from linked metrics per GOALS_SPEC
        // Circle must NOT use legacy goals.status field
        var filtered = GoalFilter switch
        {
            GoalFilter.OnTrack => _allGoals.Where(g => g.DerivedHealth == GoalDerivedHealth.OnTrack),
            GoalFilter.AtRisk => _allGoals.Where(g => g.DerivedHealth == GoalDerivedHealth.AtRisk),
            GoalFilter.OffTrack => _allGoals.Where(g => g.DerivedHealth == GoalDerivedHealth.OffTrack),
            _ => _allGoals.AsEnumerable()
        };

        foreach (var goal in filtered)
        {
            FilteredGoals.Add(goal);
        }
    }

    [RelayCommand]
    private void SetGoalFilter(GoalFilter filter)
    {
        GoalFilter = filter;
    }

    /// <summary>
    /// Computes derived health for all loaded goals using batch RPC.
    /// Uses procohere.get_goal_health_batch_v2 for single-round-trip health computation.
    /// Per GOALS_SPEC: Uses worst-state logic across supporting metrics.
    /// - If any linked metric is Off Track → Goal = Off Track
    /// - Else if any linked metric is At Risk → Goal = At Risk  
    /// - Else if all linked metrics are On Track → Goal = On Track
    /// - Else (no metrics or insufficient data) → Goal = Unknown
    /// </summary>
    private async Task ComputeDerivedHealthForGoalsAsync(CancellationToken ct = default)
    {
        Log("[CircleViewModel] Computing derived health for goals using batch RPC...");
        
        // Take a snapshot to prevent collection modified errors
        var goalsSnapshot = _allGoals.ToList();
        
        if (goalsSnapshot.Count == 0)
        {
            Log("[CircleViewModel] No goals to compute health for");
            return;
        }

        try
        {
            // Collect goal IDs
            var goalIds = goalsSnapshot.Select(g => g.Id).ToList();
            Log($"[CircleViewModel] Requesting health for {goalIds.Count} goals");

            // Single batch RPC call - replaces N+1 queries
            var healthResults = await GoalsService.Instance.GetGoalHealthBatchAsync(goalIds, ct);

            ct.ThrowIfCancellationRequested();

            // Build lookup dictionary for O(1) access
            var healthLookup = healthResults.ToDictionary(r => r.GoalId);

            // Apply results to goals in memory
            foreach (var goal in goalsSnapshot)
            {
                if (healthLookup.TryGetValue(goal.Id, out var result))
                {
                    goal.LinkedMetricsCount = result.LinkedMetricsCount;
                    goal.DerivedHealth = result.DerivedHealth;
                }
                else
                {
                    // Fallback: RPC didn't return this goal (shouldn't happen, but be safe)
                    goal.LinkedMetricsCount = 0;
                    goal.DerivedHealth = GoalDerivedHealth.Unknown;
                }
            }

            Log($"[CircleViewModel] Derived health computed: OnTrack={OnTrackGoalsCount}, AtRisk={AtRiskGoalsCount}, OffTrack={OffTrackGoalsCount}, Unknown={UnknownGoalsCount}");
        }
        catch (OperationCanceledException)
        {
            Log("[CircleViewModel] Derived health computation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] Error computing derived health batch: {ex.Message}");
            // On error, set all goals to Unknown
            foreach (var goal in goalsSnapshot)
            {
                goal.DerivedHealth = GoalDerivedHealth.Unknown;
                goal.LinkedMetricsCount = 0;
            }
        }
    }

    [RelayCommand]
    private void SetGoalDetailTab(GoalDetailTab tab)
    {
        GoalDetailTab = tab;
    }

    partial void OnSelectedGoalChanged(GoalDetail? oldValue, GoalDetail? newValue)
    {
        // Reset to Overview tab when goal changes
        GoalDetailTab = GoalDetailTab.Overview;
        LoadGoalRelatedData();
    }

    private void LoadGoalRelatedData()
    {
        GoalTargets.Clear();
        GoalTasks.Clear();
        GoalLinkedMetrics.Clear();
        GoalRecentDiscussions.Clear();

        if (SelectedGoal == null) return;

        // Load targets, tasks, metrics, and discussions asynchronously
        _ = LoadGoalRelatedDataAsync();
    }

    private async System.Threading.Tasks.Task LoadGoalRelatedDataAsync()
    {
        if (SelectedGoal == null) return;

        IsLoadingLinkedMetrics = true;
        IsLoadingDiscussions = true;
        
        try
        {
            // Load tasks linked to this goal
            var tasks = await TaskService.Instance.GetTasksBySourceAsync("goal", SelectedGoal.Id);
            GoalTasks.Clear();
            foreach (var task in tasks)
            {
                GoalTasks.Add(task);
            }

            // Load metrics linked to this goal
            var metrics = await GoalsService.Instance.GetAssociatedMetricsAsync(SelectedGoal.Id);
            GoalLinkedMetrics.Clear();
            foreach (var metric in metrics)
            {
                // Calculate trend for signal display
                if (metric.Trend == MetricTrend.Unknown)
                {
                    metric.Trend = await MetricsService.Instance.CalculateTrendAsync(metric.Id);
                }
                GoalLinkedMetrics.Add(metric);
            }
            IsLoadingLinkedMetrics = false;

            // Load recent discussions where this goal was discussed
            // Per GOALS_SPEC: Goal drill-in shows recent discussions
            var discussions = await MeetingAgendaItemService.Instance
                .GetRecentDiscussionsForEntityAsync("goal", SelectedGoal.Id, maxResults: 5);
            GoalRecentDiscussions.Clear();
            foreach (var discussion in discussions)
            {
                GoalRecentDiscussions.Add(discussion);
            }

            // TODO: Load targets when TargetService is implemented
            GoalTargets.Clear();
        }
        catch (Exception ex)
        {
            Log($"Error loading goal related data: {ex.Message}");
        }
        finally
        {
            IsLoadingLinkedMetrics = false;
            IsLoadingDiscussions = false;
        }
    }

    [RelayCommand]
    private void SelectGoal(GoalDetail? goal)
    {
        if (goal == null)
        {
            SelectedGoal = null;
            IsGoalDetailOpen = false;
            return;
        }

        if (SelectedGoal?.Id == goal.Id)
        {
            IsGoalDetailOpen = !IsGoalDetailOpen;
            if (!IsGoalDetailOpen)
                SelectedGoal = null;
        }
        else
        {
            // Wire up IDetailEntity commands before setting
            goal.CloseCommand = CloseGoalDetailCommand;
            goal.EditCommand = EditGoalCommand;
            goal.DeleteCommand = DeleteGoalCommand;
            
            SelectedGoal = goal;
            IsGoalDetailOpen = true;
        }
    }

    [RelayCommand]
    private void CloseGoalDetail()
    {
        IsGoalDetailOpen = false;
        SelectedGoal = null;
    }

    [RelayCommand]
    private void EditGoal(GoalDetail? goal)
    {
        if (goal == null) return;
        // TODO: Open goal edit dialog
        Log($"Edit goal: {goal.Title}");
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
            Log($"Deleting goal: {goal.Id}");
            var success = await GoalsService.Instance.DeleteGoalAsync(goal.Id);

            if (success)
            {
                // Remove from local collection
                _allGoals.Remove(goal);
                
                // Close detail if this goal was selected
                if (SelectedGoal?.Id == goal.Id)
                {
                    CloseGoalDetail();
                }
                
                // Refresh display
                ApplyGoalFilters();
                
                NotificationService.Instance.ShowSuccess("Goal Deleted", $"'{goal.Title}' has been removed.");
                Log($"Goal deleted successfully: {goal.Id}");
            }
            else
            {
                var errorMessage = GoalsService.Instance.LastError ?? "Failed to delete goal";
                NotificationService.Instance.ShowError("Delete Failed", errorMessage);
                Log($"Failed to delete goal: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to delete goal: {ex.Message}";
            NotificationService.Instance.ShowError("Delete Failed", ex.Message);
            Log(errorMessage);
        }
    }

    [RelayCommand]
    private void AddTarget()
    {
        if (SelectedGoal == null) return;
        // TODO: Open add target dialog
        Log($"Add target to goal: {SelectedGoal.Title}");
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CreateTaskFromGoalAsync()
    {
        if (SelectedGoal == null) return;

        try
        {
            var created = await TaskService.Instance.CreateTaskAsync(
                title: $"Task for: {SelectedGoal.Title}",
                sourceType: "goal",
                sourceId: SelectedGoal.Id
            );
            if (created != null)
            {
                GoalTasks.Add(created);
                Log($"Created task from goal: {SelectedGoal.Title}");
            }
        }
        catch (Exception ex)
        {
            Log($"Error creating task from goal: {ex.Message}");
        }
    }

    #region Goal-Metric Association

    /// <summary>
    /// Event raised when the metric picker should be shown for linking.
    /// </summary>
    public event EventHandler? LinkMetricToGoalRequested;

    /// <summary>
    /// Links a metric to the currently selected goal.
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task LinkMetricAsync(MetricDetail metric)
    {
        if (SelectedGoal == null || metric == null) return;

        LinkError = null;
        try
        {
            var success = await GoalsService.Instance.AssociateMetricAsync(SelectedGoal.Id, metric.Id);
            if (success)
            {
                // Calculate trend for display
                if (metric.Trend == MetricTrend.Unknown)
                {
                    metric.Trend = await MetricsService.Instance.CalculateTrendAsync(metric.Id);
                }
                GoalLinkedMetrics.Add(metric);
                Log($"Linked metric '{metric.Name}' to goal '{SelectedGoal.Title}'");
            }
            else
            {
                LinkError = GoalsService.Instance.LastError ?? "Failed to link metric";
                Log($"Failed to link metric: {LinkError}");
            }
        }
        catch (Exception ex)
        {
            LinkError = $"Error: {ex.Message}";
            Log($"Error linking metric: {ex.Message}");
        }
    }

    /// <summary>
    /// Unlinks a metric from the currently selected goal.
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task UnlinkMetricAsync(MetricDetail metric)
    {
        if (SelectedGoal == null || metric == null) return;

        LinkError = null;
        try
        {
            var success = await GoalsService.Instance.RemoveMetricAssociationAsync(SelectedGoal.Id, metric.Id);
            if (success)
            {
                GoalLinkedMetrics.Remove(metric);
                Log($"Unlinked metric '{metric.Name}' from goal '{SelectedGoal.Title}'");
            }
            else
            {
                LinkError = GoalsService.Instance.LastError ?? "Failed to unlink metric";
                Log($"Failed to unlink metric: {LinkError}");
            }
        }
        catch (Exception ex)
        {
            LinkError = $"Error: {ex.Message}";
            Log($"Error unlinking metric: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the metric picker to add a metric to the selected goal.
    /// </summary>
    [RelayCommand]
    private void OpenMetricPicker()
    {
        if (SelectedGoal == null) return;
        LinkMetricToGoalRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Navigates to a specific metric from a linked goal.
    /// Switches to the Metrics tab and opens the metric detail.
    /// </summary>
    [RelayCommand]
    private void NavigateToMetric(MetricDetail? metric)
    {
        if (metric == null) return;
        
        // Close goal detail flyout
        IsGoalDetailOpen = false;
        
        // Switch to Metrics tab
        SelectedTab = CircleTab.Metrics;
        
        // Find and select the metric in filtered list
        var targetMetric = FilteredMetrics.FirstOrDefault(m => m.Id == metric.Id);
        if (targetMetric == null)
        {
            // Metric might be filtered out - reset filter and try again
            MetricFilter = MetricFilter.All;
            ApplyMetricFilters();
            targetMetric = FilteredMetrics.FirstOrDefault(m => m.Id == metric.Id);
        }
        
        if (targetMetric != null)
        {
            SelectedMetric = targetMetric;
            IsMetricDetailOpen = true;
            Log($"Navigated to metric '{metric.Name}'");
        }
        else
        {
            Log($"Could not find metric '{metric.Name}' in list");
        }
    }

    #endregion

    #endregion

    #region Metrics Tab

    [ObservableProperty]
    private MetricDetail? _selectedMetric;

    [ObservableProperty]
    private bool _isMetricDetailOpen;

    [ObservableProperty]
    private MetricFilter _metricFilter = MetricFilter.All;

    /// <summary>
    /// All metrics visible in Circle view.
    /// </summary>
    private readonly ObservableCollection<MetricDetail> _allMetrics = new();

    /// <summary>
    /// Filtered metrics displayed in the list.
    /// </summary>
    public ObservableCollection<MetricDetail> FilteredMetrics { get; } = new();

    /// <summary>
    /// Goals linked to the currently selected metric.
    /// </summary>
    public ObservableCollection<GoalDetail> MetricLinkedGoals { get; } = new();

    /// <summary>
    /// Whether linked goals are currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingLinkedGoals;

    // Cached metric stats (updated after collection is populated)
    [ObservableProperty]
    private int _totalMetricsCount;
    
    [ObservableProperty]
    private int _onTrackMetricsCount;
    
    [ObservableProperty]
    private int _needsAttentionMetricsCount;
    
    [ObservableProperty]
    private int _offTrackMetricsCount;

    /// <summary>
    /// Gets the signal state for a metric based on trend and direction.
    /// Signal-first approach per CIRCLE_METRICS_SPEC.
    /// </summary>
    private static SignalState GetSignalState(MetricDetail metric)
    {
        // Determine signal based on trend and desired direction
        var direction = metric.TargetDirection?.ToLower() ?? "neutral";
        var trend = metric.Trend;

        return (direction, trend) switch
        {
            // Higher is better: trending up = good, trending down = bad
            ("higher_is_better", MetricTrend.TrendingUp) => SignalState.OnTrack,
            ("higher_is_better", MetricTrend.Stable) => SignalState.NeedsAttention,
            ("higher_is_better", MetricTrend.TrendingDown) => SignalState.OffTrack,
            
            // Lower is better: trending down = good, trending up = bad
            ("lower_is_better", MetricTrend.TrendingDown) => SignalState.OnTrack,
            ("lower_is_better", MetricTrend.Stable) => SignalState.NeedsAttention,
            ("lower_is_better", MetricTrend.TrendingUp) => SignalState.OffTrack,
            
            // Neutral or unknown: always needs attention
            _ => SignalState.NeedsAttention
        };
    }

    partial void OnMetricFilterChanged(MetricFilter value)
    {
        ApplyMetricFilters();
        // Close detail panel when filter changes
        IsMetricDetailOpen = false;
        SelectedMetric = null;
    }

    private void ApplyMetricFilters()
    {
        FilteredMetrics.Clear();
        
        // Use ToList() to force enumeration before iterating (avoids collection modified exception)
        var filtered = MetricFilter switch
        {
            MetricFilter.OnTrack => _allMetrics.Where(m => GetSignalState(m) == SignalState.OnTrack).ToList(),
            MetricFilter.NeedsAttention => _allMetrics.Where(m => GetSignalState(m) == SignalState.NeedsAttention).ToList(),
            MetricFilter.OffTrack => _allMetrics.Where(m => GetSignalState(m) == SignalState.OffTrack).ToList(),
            _ => _allMetrics.ToList()
        };

        foreach (var metric in filtered)
        {
            FilteredMetrics.Add(metric);
        }
    }

    [RelayCommand]
    private void SetMetricFilter(MetricFilter filter)
    {
        MetricFilter = filter;
    }

    partial void OnSelectedMetricChanged(MetricDetail? oldValue, MetricDetail? newValue)
    {
        MetricLinkedGoals.Clear();
        if (newValue != null)
        {
            _ = LoadMetricLinkedGoalsAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadMetricLinkedGoalsAsync()
    {
        if (SelectedMetric == null) return;

        IsLoadingLinkedGoals = true;
        try
        {
            var goals = await GoalsService.Instance.GetGoalsForMetricAsync(SelectedMetric.Id);
            MetricLinkedGoals.Clear();
            foreach (var goal in goals)
            {
                MetricLinkedGoals.Add(goal);
            }
            Log($"Loaded {goals.Count} linked goals for metric '{SelectedMetric.Name}'");
        }
        catch (Exception ex)
        {
            Log($"Error loading linked goals for metric: {ex.Message}");
        }
        finally
        {
            IsLoadingLinkedGoals = false;
        }
    }

    [RelayCommand]
    private void SelectMetric(MetricDetail? metric)
    {
        if (metric == null)
        {
            IsMetricDetailOpen = false;
            SelectedMetric = null;
        }
        else if (SelectedMetric?.Id == metric.Id && IsMetricDetailOpen)
        {
            // Toggle off if same metric clicked
            IsMetricDetailOpen = false;
            SelectedMetric = null;
        }
        else
        {
            SelectedMetric = metric;
            IsMetricDetailOpen = true;
        }
    }

    [RelayCommand]
    private void CloseMetricDetail()
    {
        IsMetricDetailOpen = false;
        SelectedMetric = null;
    }

    /// <summary>
    /// Navigates to a specific goal from a linked metric.
    /// Switches to the Goals tab and opens the goal detail.
    /// </summary>
    [RelayCommand]
    private void NavigateToGoal(GoalDetail? goal)
    {
        if (goal == null) return;
        
        // Close metric detail flyout
        IsMetricDetailOpen = false;
        
        // Switch to Goals tab
        SelectedTab = CircleTab.Goals;
        
        // Find and select the goal in filtered list
        var targetGoal = FilteredGoals.FirstOrDefault(g => g.Id == goal.Id);
        if (targetGoal == null)
        {
            // Goal might be filtered out - reset filter and try again
            GoalFilter = GoalFilter.All;
            ApplyGoalFilters();
            targetGoal = FilteredGoals.FirstOrDefault(g => g.Id == goal.Id);
        }
        
        if (targetGoal != null)
        {
            SelectedGoal = targetGoal;
            IsGoalDetailOpen = true;
            Log($"Navigated to goal '{goal.Title}'");
        }
        else
        {
            Log($"Could not find goal '{goal.Title}' in list");
        }
    }

    /// <summary>
    /// Gets the last update age for display (e.g., "2d ago", "1w ago").
    /// </summary>
    public static string GetLastUpdateAge(MetricDetail metric)
    {
        var elapsed = DateTime.UtcNow - metric.UpdatedAt;
        
        return elapsed.TotalDays switch
        {
            < 1 => "today",
            < 2 => "1d ago",
            < 7 => $"{(int)elapsed.TotalDays}d ago",
            < 14 => "1w ago",
            < 30 => $"{(int)(elapsed.TotalDays / 7)}w ago",
            < 60 => "1mo ago",
            _ => $"{(int)(elapsed.TotalDays / 30)}mo ago"
        };
    }

    private async System.Threading.Tasks.Task LoadMetricsAsync()
    {
        try
        {
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            
            // Build list first, then populate collection to avoid concurrent modification
            var metricsToAdd = new List<MetricDetail>();
            foreach (var metric in metrics.Where(m => !m.IsDeleted))
            {
                // Calculate trend if not set
                if (metric.Trend == MetricTrend.Unknown)
                {
                    metric.Trend = await MetricsService.Instance.CalculateTrendAsync(metric.Id);
                }
                metricsToAdd.Add(metric);
            }
            
            // Now update collections on UI thread
            _allMetrics.Clear();
            foreach (var metric in metricsToAdd)
            {
                _allMetrics.Add(metric);
            }

            // Update cached stat counts (after collection is populated)
            TotalMetricsCount = _allMetrics.Count;
            OnTrackMetricsCount = _allMetrics.Count(m => GetSignalState(m) == SignalState.OnTrack);
            NeedsAttentionMetricsCount = _allMetrics.Count(m => GetSignalState(m) == SignalState.NeedsAttention);
            OffTrackMetricsCount = _allMetrics.Count(m => GetSignalState(m) == SignalState.OffTrack);

            ApplyMetricFilters();
        }
        catch (Exception ex)
        {
            Log($"Error loading metrics: {ex.Message}");
        }
    }

    #endregion

    #region Feedback Tab

    [ObservableProperty]
    private FeedbackDetail? _selectedFeedback;

    [ObservableProperty]
    private bool _isFeedbackDetailOpen;

    [ObservableProperty]
    private FeedbackFilter _feedbackFilter = FeedbackFilter.All;

    /// <summary>
    /// All feedback items.
    /// </summary>
    private readonly ObservableCollection<FeedbackDetail> _allFeedback = new();

    /// <summary>
    /// Filtered feedback displayed in the list.
    /// </summary>
    public ObservableCollection<FeedbackDetail> FilteredFeedback { get; } = new();

    // Feedback stats
    public int PraiseFeedbackCount => _allFeedback.Count(f => f.FeedbackType?.ToLower() == "praise");
    public int ConstructiveFeedbackCount => _allFeedback.Count(f => f.FeedbackType?.ToLower() == "constructive");
    public int CoachingFeedbackCount => _allFeedback.Count(f => f.FeedbackType?.ToLower() == "coaching");
    public int TotalFeedbackCount => _allFeedback.Count;

    partial void OnFeedbackFilterChanged(FeedbackFilter value)
    {
        ApplyFeedbackFilters();
        // Close detail panel when filter changes
        IsFeedbackDetailOpen = false;
        SelectedFeedback = null;
    }

    private void ApplyFeedbackFilters()
    {
        FilteredFeedback.Clear();
        
        var filtered = FeedbackFilter switch
        {
            FeedbackFilter.Praise => _allFeedback.Where(f => f.FeedbackType?.ToLower() == "praise"),
            FeedbackFilter.Constructive => _allFeedback.Where(f => f.FeedbackType?.ToLower() == "constructive"),
            FeedbackFilter.Coaching => _allFeedback.Where(f => f.FeedbackType?.ToLower() == "coaching"),
            _ => _allFeedback.AsEnumerable()
        };

        foreach (var feedback in filtered.OrderByDescending(f => f.CreatedAt))
        {
            FilteredFeedback.Add(feedback);
        }
    }

    [RelayCommand]
    private void SetFeedbackFilter(FeedbackFilter filter)
    {
        FeedbackFilter = filter;
    }

    [RelayCommand]
    private void SelectFeedback(FeedbackDetail? feedback)
    {
        if (feedback == null)
        {
            SelectedFeedback = null;
            IsFeedbackDetailOpen = false;
            return;
        }

        if (SelectedFeedback?.Id == feedback.Id)
        {
            IsFeedbackDetailOpen = !IsFeedbackDetailOpen;
            if (!IsFeedbackDetailOpen)
                SelectedFeedback = null;
        }
        else
        {
            SelectedFeedback = feedback;
            IsFeedbackDetailOpen = true;
        }
    }

    [RelayCommand]
    private void CloseFeedbackDetail()
    {
        IsFeedbackDetailOpen = false;
        SelectedFeedback = null;
    }

    #endregion

    public CircleViewModel()
    {
        Log("[CircleViewModel] Constructor called");
        
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Load data
        _ = LoadDataAsync();
    }

    private void OnProfileChanged(object? sender, UserProfile? profile)
    {
        Log($"[CircleViewModel] ProfileChanged: {(profile != null ? profile.Email : "NULL")}");
        if (profile != null)
        {
            _ = LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            LoadingStatus = "Loading team members...";
            HasError = false;
            ErrorMessage = string.Empty;
            Log("[CircleViewModel] LoadDataAsync started");

            var profile = AuthService.Instance.CurrentProfile;
            if (profile == null)
            {
                Log("[CircleViewModel] No profile yet");
                return;
            }

            // Load visible team members using TeamService (hierarchy-aware)
            var visibleMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            Log($"[CircleViewModel] Got {visibleMembers.Count} visible team members");
            
            // Also load dashboard data for goals, meetings, feedback
            LoadingStatus = "Loading dashboard data...";
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            Log($"[CircleViewModel] Dashboard loaded");
            
            // Populate hierarchy-related collections
            CurrentTeamMember = visibleMembers.FirstOrDefault(m => m.Relation == "self");
            MyManager = visibleMembers.FirstOrDefault(m => m.Relation == "manager");
            
            MyDirectReports.Clear();
            MyPeers.Clear();
            foreach (var member in visibleMembers.Where(m => m.Relation == "direct"))
            {
                MyDirectReports.Add(member);
            }
            foreach (var member in visibleMembers.Where(m => m.Relation == "peer"))
            {
                MyPeers.Add(member);
            }
            
            OnPropertyChanged(nameof(DirectReportCount));
            OnPropertyChanged(nameof(PeerCount));
            OnPropertyChanged(nameof(IsCurrentUserManager));
            
            Log($"[CircleViewModel] Hierarchy: Manager={MyManager?.FullName ?? "none"}, DirectReports={DirectReportCount}, Peers={PeerCount}");
            
            // Populate all team members (excluding self for the list)
            // Set flag to prevent ApplyFilters from running during update
            _isUpdatingData = true;
            try
            {
                var membersToAdd = visibleMembers.Where(m => m.Relation != "self").ToList();
                
                _allTeamMembers.Clear();
                _allTeamMembers.AddRange(membersToAdd);
                
                // Calculate stats (collection is now stable)
                TotalMemberCount = _allTeamMembers.Count;
                ActiveMemberCount = _allTeamMembers.Count(m => m.IsActive);
                MeetingsOnTrackCount = _allTeamMembers.Count(m => !m.NeedsAttention);
                MeetingsOverdueCount = _allTeamMembers.Count(m => m.NeedsAttention);
                MembersWithOpenTasksCount = _allTeamMembers.Count(m => m.OpenTaskCount > 0);
            }
            finally
            {
                _isUpdatingData = false;
            }
            
            // Notify stat text properties
            OnPropertyChanged(nameof(TotalMemberCountText));
            OnPropertyChanged(nameof(ActiveMemberCountText));
            OnPropertyChanged(nameof(MeetingsOnTrackCountText));
            OnPropertyChanged(nameof(MeetingsOverdueCountText));
            
            // Apply filters
            ApplyFilters();

            // Load meetings from dashboard data
            LoadingStatus = "Loading meetings...";
            Meetings.Clear();
            foreach (var meeting in dashboardData.Meetings)
            {
                // Enrich with team member name if available
                if (meeting.TeamMemberId.HasValue)
                {
                    var member = _allTeamMembers.FirstOrDefault(m => m.Id == meeting.TeamMemberId.Value);
                    if (member != null)
                    {
                        meeting.TeamMemberName = member.FullName;
                        // Only add attendee if we don't already have them from the database
                        if (!meeting.Attendees.Any(a => a.TeamMemberId == member.Id))
                        {
                            meeting.Attendees.Add(new MeetingAttendee 
                            { 
                                Id = Guid.NewGuid(), 
                                TeamMemberId = member.Id,
                                MeetingId = meeting.Id,
                                Name = member.FullName, 
                                Email = member.Email ?? "", 
                                Role = "attendee",
                                ResponseStatus = "accepted" 
                            });
                        }
                    }
                }
                Meetings.Add(meeting);
            }
            RefreshMeetingsView();

            // Load real goals from database
            LoadingStatus = "Loading goals...";
            _allGoals.Clear();
            var memberDict = _allTeamMembers.ToDictionary(m => m.Id);
            foreach (var goal in dashboardData.Goals)
            {
                // Enrich with owner name, avatar, initials
                // OwnerTeamMemberId is non-nullable Guid
                if (goal.OwnerTeamMemberId != Guid.Empty && memberDict.TryGetValue(goal.OwnerTeamMemberId, out var owner))
                {
                    goal.OwnerName = owner.FullName;
                    goal.OwnerAvatarUrl = owner.UserAvatarUrl;
                    goal.OwnerInitials = owner.Initials;
                }
                _allGoals.Add(goal);
            }
            
            // NOTE: Derived health computation is deferred to background after main load completes
            // to avoid blocking the UI with N+1 queries. See end of LoadDataAsync.
            
            ApplyGoalFilters();
            OnPropertyChanged(nameof(OnTrackGoalsCount));
            OnPropertyChanged(nameof(AtRiskGoalsCount));
            OnPropertyChanged(nameof(OffTrackGoalsCount));
            OnPropertyChanged(nameof(UnknownGoalsCount));
            OnPropertyChanged(nameof(TotalGoalsCount));
            Log($"[CircleViewModel] Loaded {_allGoals.Count} goals from database");
            
            // Load real feedback from database
            LoadingStatus = "Loading feedback...";
            _allFeedback.Clear();
            foreach (var feedback in dashboardData.Feedback)
            {
                // Enrich with recipient name and avatar
                // TeamMemberId is non-nullable Guid
                if (feedback.TeamMemberId != Guid.Empty && memberDict.TryGetValue(feedback.TeamMemberId, out var recipient))
                {
                    feedback.RecipientName = recipient.FullName;
                    feedback.RecipientAvatarUrl = recipient.UserAvatarUrl;
                    // RecipientInitials is computed from RecipientName
                }
                _allFeedback.Add(feedback);
            }
            ApplyFeedbackFilters();
            OnPropertyChanged(nameof(PraiseFeedbackCount));
            OnPropertyChanged(nameof(ConstructiveFeedbackCount));
            OnPropertyChanged(nameof(CoachingFeedbackCount));
            OnPropertyChanged(nameof(TotalFeedbackCount));
            Log($"[CircleViewModel] Loaded {_allFeedback.Count} feedback from database");

            // Load metrics for Circle view
            LoadingStatus = "Loading metrics...";
            await LoadMetricsAsync();
            Log($"[CircleViewModel] Loaded {_allMetrics.Count} metrics from database");

            Log("[CircleViewModel] LoadDataAsync completed - starting background health computation");
            
            // Mark main load as complete so UI is responsive
            IsLoading = false;
            
            // Apply goal filters now (they'll show Unknown health initially)
            ApplyGoalFilters();
            OnPropertyChanged(nameof(OnTrackGoalsCount));
            OnPropertyChanged(nameof(AtRiskGoalsCount));
            OnPropertyChanged(nameof(OffTrackGoalsCount));
            OnPropertyChanged(nameof(UnknownGoalsCount));
            OnPropertyChanged(nameof(TotalGoalsCount));
            
            // Compute derived health using single batch RPC call.
            // Still runs in background to keep UI responsive during first paint.
            _ = Task.Run(async () =>
            {
                try
                {
                    await ComputeDerivedHealthForGoalsAsync();
                    // Update UI on main thread after health is computed
                    Dispatcher.UIThread.Post(() =>
                    {
                        ApplyGoalFilters();
                        OnPropertyChanged(nameof(OnTrackGoalsCount));
                        OnPropertyChanged(nameof(AtRiskGoalsCount));
                        OnPropertyChanged(nameof(OffTrackGoalsCount));
                        OnPropertyChanged(nameof(UnknownGoalsCount));
                    });
                }
                catch (Exception ex)
                {
                    Log($"[CircleViewModel] Background health computation failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] ERROR: {ex.Message}");
            Log($"[CircleViewModel] STACK TRACE:\n{ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"[CircleViewModel] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CircleViewModel] STACK TRACE:\n{ex.StackTrace}");
            HasError = true;
            ErrorMessage = $"Failed to load data: {ex.Message}";
            IsLoading = false;
        }
    }
}

/// <summary>
/// Tabs in the Circle area.
/// </summary>
public enum CircleTab
{
    Team,
    Goals,
    Metrics,
    Feedback,
    Meetings
}

/// <summary>
/// Filter options for team members.
/// </summary>
public enum TeamMemberFilter
{
    All,
    Active,
    Inactive,
    NeedsAttention
}

/// <summary>
/// View modes for the meetings tab.
/// </summary>
public enum MeetingsViewMode
{
    Day,
    Week,
    Month,
    List
}

/// <summary>
/// Filter options for goals.
/// </summary>
public enum GoalFilter
{
    All,
    OnTrack,
    AtRisk,
    OffTrack
}

/// <summary>
/// Filter options for feedback.
/// </summary>
public enum FeedbackFilter
{
    All,
    Praise,
    Constructive,
    Coaching
}

/// <summary>
/// Filter options for metrics (signal-based per CIRCLE_METRICS_SPEC).
/// </summary>
public enum MetricFilter
{
    All,
    OnTrack,
    NeedsAttention,
    OffTrack
}

/// <summary>
/// Signal state for metrics - derived from trend and direction.
/// This is NOT the raw value - it's the signal for leadership attention.
/// </summary>
public enum SignalState
{
    OnTrack,
    NeedsAttention,
    OffTrack
}

/// <summary>
/// Tabs within the goal detail flyout.
/// </summary>
public enum GoalDetailTab
{
    Overview,
    Targets,
    Tasks
}

/// <summary>
/// Group of meetings for a specific date in list view.
/// </summary>
public class MeetingGroup
{
    public string Date { get; set; } = string.Empty;
    public ObservableCollection<MeetingDetail> Meetings { get; set; } = new();
}

/// <summary>
/// Hour slot for day/week calendar view.
/// </summary>
public class CalendarHour
{
    public int Hour { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}

/// <summary>
/// Day column in week view.
/// </summary>
public class CalendarWeekDay
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public bool IsToday { get; set; }
    public ObservableCollection<MeetingDetail> Meetings { get; set; } = new();
}

/// <summary>
/// Day cell in month view.
/// </summary>
public class CalendarDay
{
    public DateTime Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public bool HasMoreMeetings { get; set; }
    public ObservableCollection<MeetingDetail> Meetings { get; set; } = new();
}

/// <summary>
/// View mode for the team members list.
/// </summary>
public enum TeamViewMode
{
    /// <summary>
    /// Flat card grid (default).
    /// </summary>
    Flat,
    
    /// <summary>
    /// Tree view with indentation based on hierarchy depth.
    /// </summary>
    Tree
}
