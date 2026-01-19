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
using System.Threading.Tasks;

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
        FilteredTeamMembers.Clear();
        
        var filtered = _allTeamMembers.AsEnumerable();
        
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
        
        foreach (var member in filtered)
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

    private readonly ObservableCollection<TeamMemberDetail> _allTeamMembers = new();

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

    [ObservableProperty]
    private MemberDetailTab _memberDetailTab = MemberDetailTab.Overview;

    /// <summary>
    /// Goals owned by the selected team member.
    /// </summary>
    public ObservableCollection<GoalDetail> MemberGoals { get; } = new();

    /// <summary>
    /// Meetings involving the selected team member.
    /// </summary>
    public ObservableCollection<MeetingDetail> MemberMeetings { get; } = new();

    /// <summary>
    /// Feedback for the selected team member.
    /// </summary>
    public ObservableCollection<FeedbackDetail> MemberFeedback { get; } = new();

    /// <summary>
    /// Direct reports of the selected team member (for managers).
    /// </summary>
    public ObservableCollection<TeamMemberDetail> MemberDirectReports { get; } = new();

    /// <summary>
    /// Tasks assigned to the selected team member.
    /// </summary>
    public ObservableCollection<TaskDetail> MemberTasks { get; } = new();

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
            newValue.IsSelected = true;
        
        // Reset to Overview tab when member changes
        MemberDetailTab = MemberDetailTab.Overview;
        LoadMemberRelatedData();
        
        // Notify that SelectedMemberIsManager may have changed
        OnPropertyChanged(nameof(SelectedMemberIsManager));
    }

    private void LoadMemberRelatedData()
    {
        MemberGoals.Clear();
        MemberMeetings.Clear();
        MemberFeedback.Clear();
        MemberDirectReports.Clear();
        MemberTasks.Clear();

        if (SelectedTeamMember == null) return;

        // Load tasks asynchronously
        _ = LoadMemberTasksAsync();

        // Get direct reports for this member (if they're a manager)
        foreach (var report in _allTeamMembers.Where(m => m.ManagerTeamMemberId == SelectedTeamMember.Id))
        {
            MemberDirectReports.Add(report);
        }

        // Filter goals owned by this member
        foreach (var goal in _allGoals.Where(g => g.OwnerTeamMemberId == SelectedTeamMember.Id))
        {
            MemberGoals.Add(goal);
        }

        // Filter meetings with this member (by linked team member or attendee name match)
        foreach (var meeting in Meetings.Where(m => 
            m.TeamMemberId == SelectedTeamMember.Id ||
            m.Attendees?.Any(a => a.Name == SelectedTeamMember.FullName || a.Email == SelectedTeamMember.Email) == true))
        {
            MemberMeetings.Add(meeting);
        }

        // Filter feedback for this member
        foreach (var fb in _allFeedback.Where(f => f.TeamMemberId == SelectedTeamMember.Id))
        {
            MemberFeedback.Add(fb);
        }
    }

    /// <summary>
    /// Loads tasks for the selected team member asynchronously.
    /// </summary>
    private async System.Threading.Tasks.Task LoadMemberTasksAsync()
    {
        if (SelectedTeamMember == null) return;

        try
        {
            var tasks = await TaskService.Instance.GetTasksByAssigneeAsync(SelectedTeamMember.Id, includeCompleted: false);
            MemberTasks.Clear();
            foreach (var task in tasks)
            {
                MemberTasks.Add(task);
            }
        }
        catch (Exception ex)
        {
            Log($"Error loading member tasks: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SetMemberDetailTab(MemberDetailTab tab)
    {
        MemberDetailTab = tab;
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

    [ObservableProperty]
    private MeetingDetailTab _meetingDetailTab = MeetingDetailTab.Overview;

    partial void OnSelectedMeetingChanged(MeetingDetail? value)
    {
        // Reset to Overview tab when meeting changes
        MeetingDetailTab = MeetingDetailTab.Overview;
    }

    [RelayCommand]
    private void SetMeetingDetailTab(MeetingDetailTab tab)
    {
        MeetingDetailTab = tab;
    }

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
    /// </summary>
    public IEnumerable<MeetingDetail> DayMeetings => Meetings
        .Where(m => m.LocalDate == CurrentDate.Date)
        .OrderBy(m => m.ScheduledAt);

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
    private void SelectMeeting(MeetingDetail? meeting)
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

    private static DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private void RefreshMeetingsView()
    {
        OnPropertyChanged(nameof(CurrentDateHeader));
        OnPropertyChanged(nameof(DayMeetings));

        // Update filtered meetings based on view
        FilteredMeetings.Clear();
        var meetings = MeetingsViewMode switch
        {
            MeetingsViewMode.Day => Meetings.Where(m => m.LocalDate == CurrentDate.Date),
            MeetingsViewMode.Week => Meetings.Where(m => 
            {
                var weekStart = GetWeekStart(CurrentDate);
                var weekEnd = weekStart.AddDays(7);
                var date = m.LocalDate;
                return date >= weekStart && date < weekEnd;
            }),
            MeetingsViewMode.Month => Meetings.Where(m => 
            {
                var date = m.ScheduledAtLocal;
                return date?.Year == CurrentDate.Year && date?.Month == CurrentDate.Month;
            }),
            _ => Meetings.OrderBy(m => m.ScheduledAt)
        };

        foreach (var m in meetings.OrderBy(m => m.ScheduledAt))
        {
            FilteredMeetings.Add(m);
        }

        // Update grouped meetings for list view
        GroupedMeetings.Clear();
        var grouped = Meetings
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
        
        var firstOfMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        
        // Get the Sunday before (or of) the first day
        var calendarStart = GetWeekStart(firstOfMonth);
        
        // Fill 6 weeks (42 days)
        for (int i = 0; i < 42; i++)
        {
            var date = calendarStart.AddDays(i);
            var dayMeetings = Meetings
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
                HasMoreMeetings = Meetings.Count(m => m.LocalDate == date.Date) > 3
            });
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void AddTeamMember()
    {
        Debug.WriteLine("Add Team Member clicked");
        // TODO: Open add team member dialog
    }

    [RelayCommand]
    private void EditTeamMember(TeamMemberDetail? member)
    {
        if (member == null) return;
        Debug.WriteLine($"Edit Team Member: {member.FullName}");
        // TODO: Open edit team member dialog
    }

    [RelayCommand]
    private void ScheduleMeeting(TeamMemberDetail? member)
    {
        Debug.WriteLine($"Schedule Meeting with: {member?.FullName ?? "team"}");
        // TODO: Open schedule meeting dialog
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
    private void AddGoal()
    {
        Debug.WriteLine("Add Goal clicked");
        // TODO: Open add goal dialog
    }

    [RelayCommand]
    private void GiveFeedback()
    {
        Debug.WriteLine("Give Feedback clicked");
        // TODO: Open feedback dialog
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
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

    // Goal stats
    public int OnTrackGoalsCount => _allGoals.Count(g => g.Status?.ToLower() is "on_track" or "on-track");
    public int AtRiskGoalsCount => _allGoals.Count(g => g.Status?.ToLower() is "at_risk" or "at-risk");
    public int OffTrackGoalsCount => _allGoals.Count(g => g.Status?.ToLower() is "off_track" or "off-track");
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
        
        var filtered = GoalFilter switch
        {
            GoalFilter.OnTrack => _allGoals.Where(g => g.Status?.ToLower() is "on_track" or "on-track"),
            GoalFilter.AtRisk => _allGoals.Where(g => g.Status?.ToLower() is "at_risk" or "at-risk"),
            GoalFilter.OffTrack => _allGoals.Where(g => g.Status?.ToLower() is "off_track" or "off-track"),
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

        if (SelectedGoal == null) return;

        // Load targets and tasks asynchronously
        _ = LoadGoalTargetsAndTasksAsync();
    }

    private async System.Threading.Tasks.Task LoadGoalTargetsAndTasksAsync()
    {
        if (SelectedGoal == null) return;

        try
        {
            // Load tasks linked to this goal
            var tasks = await TaskService.Instance.GetTasksBySourceAsync("goal", SelectedGoal.Id);
            GoalTasks.Clear();
            foreach (var task in tasks)
            {
                GoalTasks.Add(task);
            }

            // TODO: Load targets when TargetService is implemented
            // For now, just clear
            GoalTargets.Clear();
        }
        catch (Exception ex)
        {
            Log($"Error loading goal related data: {ex.Message}");
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
    private async System.Threading.Tasks.Task DeleteGoalAsync(GoalDetail? goal)
    {
        if (goal == null) return;
        // TODO: Confirm and delete goal
        Log($"Delete goal: {goal.Title}");
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

    private void LoadSampleGoals()
    {
        _allGoals.Clear();
        var members = _allTeamMembers.ToList();
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Increase Customer Satisfaction Score",
            Description = "Improve NPS from 42 to 55 through targeted initiatives focusing on response time and product quality.",
            Status = "on_track",
            ProgressPercent = 75,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddMonths(1),
            OwnerName = members.FirstOrDefault()?.FullName ?? "Team"
        });
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Launch Mobile App v2.0",
            Description = "Complete redesign and launch of mobile application with new features including offline mode and push notifications.",
            Status = "at_risk",
            ProgressPercent = 45,
            StartDate = DateTime.Today.AddMonths(-3),
            EndDate = DateTime.Today.AddDays(21),
            OwnerName = members.Skip(1).FirstOrDefault()?.FullName ?? "Engineering"
        });
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Reduce Support Ticket Volume",
            Description = "Decrease monthly support tickets by 30% through improved documentation and self-service tools.",
            Status = "on_track",
            ProgressPercent = 88,
            StartDate = DateTime.Today.AddMonths(-4),
            EndDate = DateTime.Today.AddDays(14),
            OwnerName = members.Skip(2).FirstOrDefault()?.FullName ?? "Support"
        });
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Implement CI/CD Pipeline",
            Description = "Establish automated testing and deployment pipeline to reduce release cycle time from 2 weeks to 2 days.",
            Status = "off_track",
            ProgressPercent = 25,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddDays(-7), // Overdue
            OwnerName = members.Skip(3).FirstOrDefault()?.FullName ?? "DevOps"
        });
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Expand to European Market",
            Description = "Complete GDPR compliance and launch marketing campaigns in UK, Germany, and France.",
            Status = "on_track",
            ProgressPercent = 60,
            StartDate = DateTime.Today.AddMonths(-1),
            EndDate = DateTime.Today.AddMonths(3),
            OwnerName = members.Skip(4).FirstOrDefault()?.FullName ?? "Marketing"
        });
        
        _allGoals.Add(new GoalDetail
        {
            Id = Guid.NewGuid(),
            Title = "Team Training Program",
            Description = "Complete leadership training for all senior team members and technical certifications for engineers.",
            Status = "at_risk",
            ProgressPercent = 35,
            StartDate = DateTime.Today.AddMonths(-2),
            EndDate = DateTime.Today.AddMonths(1),
            OwnerName = "HR"
        });

        ApplyGoalFilters();
        OnPropertyChanged(nameof(OnTrackGoalsCount));
        OnPropertyChanged(nameof(AtRiskGoalsCount));
        OnPropertyChanged(nameof(OffTrackGoalsCount));
        OnPropertyChanged(nameof(TotalGoalsCount));
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

    private void LoadSampleFeedback()
    {
        _allFeedback.Clear();
        var members = _allTeamMembers.ToList();
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.FirstOrDefault()?.Id,
            RecipientName = members.FirstOrDefault()?.FullName ?? "Alex Martinez",
            FeedbackType = "praise",
            Content = "Outstanding presentation at the quarterly review! Your ability to communicate complex technical concepts to non-technical stakeholders was impressive. The visualizations really helped everyone understand the project status.",
            Context = "Q4 Quarterly Review",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.Skip(1).FirstOrDefault()?.Id,
            RecipientName = members.Skip(1).FirstOrDefault()?.FullName ?? "Sarah Chen",
            FeedbackType = "constructive",
            Content = "The code review feedback could be more specific. Instead of 'this needs work', try pointing to specific lines and suggesting concrete improvements. This will help junior developers learn faster.",
            Context = "Code Review Process",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.Skip(2).FirstOrDefault()?.Id,
            RecipientName = members.Skip(2).FirstOrDefault()?.FullName ?? "Michael Johnson",
            FeedbackType = "coaching",
            Content = "Great progress on stakeholder management! For next level growth, focus on anticipating questions before meetings and preparing concise data-backed answers. Consider shadowing a senior PM for a week.",
            Context = "Career Development",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.Skip(3).FirstOrDefault()?.Id,
            RecipientName = members.Skip(3).FirstOrDefault()?.FullName ?? "Emily Davis",
            FeedbackType = "praise",
            Content = "Thank you for mentoring the new team members! Your patience and clear explanations have helped them ramp up 50% faster than expected. This is exactly the culture we want to build.",
            Context = "New Hire Onboarding",
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.FirstOrDefault()?.Id,
            RecipientName = members.FirstOrDefault()?.FullName ?? "Alex Martinez",
            FeedbackType = "constructive",
            Content = "Meeting facilitation could be improved. Try using timeboxing for each agenda item and designating someone to take notes. This will help keep discussions focused and ensure action items are captured.",
            Context = "Team Meetings",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.Skip(4).FirstOrDefault()?.Id,
            RecipientName = members.Skip(4).FirstOrDefault()?.FullName ?? "David Kim",
            FeedbackType = "praise",
            Content = "Exceptional debugging skills demonstrated on the production incident last week. You remained calm under pressure and methodically isolated the issue. The post-mortem documentation was thorough and actionable.",
            Context = "Production Incident Response",
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        });
        
        _allFeedback.Add(new FeedbackDetail
        {
            Id = Guid.NewGuid(),
            TeamMemberId = members.Skip(2).FirstOrDefault()?.Id,
            RecipientName = members.Skip(2).FirstOrDefault()?.FullName ?? "Michael Johnson",
            FeedbackType = "coaching",
            Content = "To develop your technical leadership, consider leading a tech talk on your recent architecture decisions. Also, try writing RFC documents for major changes - this builds trust and creates alignment.",
            Context = "Technical Leadership",
            CreatedAt = DateTime.UtcNow.AddDays(-21)
        });

        ApplyFeedbackFilters();
        OnPropertyChanged(nameof(PraiseFeedbackCount));
        OnPropertyChanged(nameof(ConstructiveFeedbackCount));
        OnPropertyChanged(nameof(CoachingFeedbackCount));
        OnPropertyChanged(nameof(TotalFeedbackCount));
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
            _allTeamMembers.Clear();
            foreach (var member in visibleMembers.Where(m => m.Relation != "self"))
            {
                _allTeamMembers.Add(member);
            }
            
            // Calculate stats
            TotalMemberCount = _allTeamMembers.Count;
            ActiveMemberCount = _allTeamMembers.Count(m => m.IsActive);
            MeetingsOnTrackCount = _allTeamMembers.Count(m => !m.NeedsAttention);
            MeetingsOverdueCount = _allTeamMembers.Count(m => m.NeedsAttention);
            MembersWithOpenTasksCount = _allTeamMembers.Count(m => m.OpenTaskCount > 0);
            
            // Notify stat text properties
            OnPropertyChanged(nameof(TotalMemberCountText));
            OnPropertyChanged(nameof(ActiveMemberCountText));
            OnPropertyChanged(nameof(MeetingsOnTrackCountText));
            OnPropertyChanged(nameof(MeetingsOverdueCountText));
            
            // Apply filters
            ApplyFilters();

            // Load meetings from dashboard data
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
            _allGoals.Clear();
            var memberDict = _allTeamMembers.ToDictionary(m => m.Id);
            foreach (var goal in dashboardData.Goals)
            {
                // Enrich with owner name
                if (goal.OwnerTeamMemberId.HasValue && memberDict.TryGetValue(goal.OwnerTeamMemberId.Value, out var owner))
                {
                    goal.OwnerName = owner.FullName;
                }
                _allGoals.Add(goal);
            }
            ApplyGoalFilters();
            Log($"[CircleViewModel] Loaded {_allGoals.Count} goals from database");
            
            // Load real feedback from database
            _allFeedback.Clear();
            foreach (var feedback in dashboardData.Feedback)
            {
                // Enrich with recipient name
                if (feedback.TeamMemberId.HasValue && memberDict.TryGetValue(feedback.TeamMemberId.Value, out var recipient))
                {
                    feedback.RecipientName = recipient.FullName;
                }
                _allFeedback.Add(feedback);
            }
            ApplyFeedbackFilters();
            OnPropertyChanged(nameof(PraiseFeedbackCount));
            OnPropertyChanged(nameof(ConstructiveFeedbackCount));
            OnPropertyChanged(nameof(CoachingFeedbackCount));
            OnPropertyChanged(nameof(TotalFeedbackCount));
            Log($"[CircleViewModel] Loaded {_allFeedback.Count} feedback from database");

            Log("[CircleViewModel] LoadDataAsync completed");
        }
        catch (Exception ex)
        {
            Log($"[CircleViewModel] ERROR: {ex.Message}");
            HasError = true;
            ErrorMessage = $"Failed to load data: {ex.Message}";
        }
        finally
        {
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
/// Tabs within the team member detail flyout.
/// </summary>
public enum MemberDetailTab
{
    Overview,
    Goals,
    Tasks,
    Meetings,
    Feedback,
    Team
}

/// <summary>
/// Tabs within the meeting detail flyout.
/// </summary>
public enum MeetingDetailTab
{
    Overview,
    Agenda,
    Attendees,
    Notes
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
