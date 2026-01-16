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

    #region Filter & Search

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private TeamMemberFilter _memberFilter = TeamMemberFilter.All;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnMemberFilterChanged(TeamMemberFilter value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilteredTeamMembers.Clear();
        
        var filtered = _allTeamMembers.AsEnumerable();
        
        // Apply filter
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
        
        foreach (var member in filtered)
        {
            FilteredTeamMembers.Add(member);
        }
        
        OnPropertyChanged(nameof(FilteredMemberCount));
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
    /// Hours for day/week view (8 AM to 6 PM).
    /// </summary>
    public List<CalendarHour> CalendarHours { get; } = Enumerable.Range(8, 11)
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
        .Where(m => m.ScheduledAt?.ToLocalTime().Date == CurrentDate.Date)
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
            MeetingsViewMode.Day => Meetings.Where(m => m.ScheduledAt?.ToLocalTime().Date == CurrentDate.Date),
            MeetingsViewMode.Week => Meetings.Where(m => 
            {
                var weekStart = GetWeekStart(CurrentDate);
                var weekEnd = weekStart.AddDays(7);
                var date = m.ScheduledAt?.ToLocalTime().Date;
                return date >= weekStart && date < weekEnd;
            }),
            MeetingsViewMode.Month => Meetings.Where(m => 
            {
                var date = m.ScheduledAt?.ToLocalTime();
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
                    Meetings.Where(m => m.ScheduledAt?.ToLocalTime().Date == date.Date)
                           .OrderBy(m => m.ScheduledAt))
            });
        }
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
                .Where(m => m.ScheduledAt?.ToLocalTime().Date == date.Date)
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
                HasMoreMeetings = Meetings.Count(m => m.ScheduledAt?.ToLocalTime().Date == date.Date) > 3
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
    private void GiveFeedback(TeamMemberDetail? member)
    {
        Debug.WriteLine($"Give Feedback to: {member?.FullName ?? "team member"}");
        // TODO: Open feedback dialog
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
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

            // Load dashboard data which includes team members
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            Log($"[CircleViewModel] Got {dashboardData.TeamMembers.Count} team members");
            
            _allTeamMembers.Clear();
            foreach (var member in dashboardData.TeamMembers)
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

            // Load sample meetings for testing
            LoadSampleMeetings();
            RefreshMeetingsView();

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

    private void LoadSampleMeetings()
    {
        Meetings.Clear();
        var today = DateTime.Today;
        var members = _allTeamMembers.ToList();

        // Today's meetings
        Meetings.Add(CreateMeeting("1:1 with Alex Martinez", "one_on_one", today.AddHours(9), 30, members.FirstOrDefault(m => m.FirstName == "Alex")));
        Meetings.Add(CreateMeeting("Team Standup", "standup", today.AddHours(10), 15, null, true));
        Meetings.Add(CreateMeeting("Project Review", "review", today.AddHours(14), 60, null, true));

        // Tomorrow
        Meetings.Add(CreateMeeting("1:1 with David Kim", "one_on_one", today.AddDays(1).AddHours(10), 30, members.FirstOrDefault(m => m.FirstName == "David")));
        Meetings.Add(CreateMeeting("Sprint Planning", "team", today.AddDays(1).AddHours(13), 90, null, true));

        // This week
        Meetings.Add(CreateMeeting("1:1 with Emily Rodriguez", "one_on_one", today.AddDays(2).AddHours(11), 45, members.FirstOrDefault(m => m.FirstName == "Emily")));
        Meetings.Add(CreateMeeting("Design Review", "review", today.AddDays(3).AddHours(15), 60, null, true));
        Meetings.Add(CreateMeeting("1:1 with Jessica Thompson", "one_on_one", today.AddDays(4).AddHours(9).AddMinutes(30), 30, members.FirstOrDefault(m => m.FirstName == "Jessica")));

        // Next week
        Meetings.Add(CreateMeeting("Quarterly Planning", "team", today.AddDays(7).AddHours(10), 120, null, true));
        Meetings.Add(CreateMeeting("1:1 with Michael Chen", "one_on_one", today.AddDays(8).AddHours(14), 30, members.FirstOrDefault(m => m.FirstName == "Michael")));
    }

    private MeetingDetail CreateMeeting(string title, string type, DateTime scheduledAt, int duration, TeamMemberDetail? member, bool isTeamMeeting = false)
    {
        var meeting = new MeetingDetail
        {
            Id = Guid.NewGuid(),
            Title = title,
            MeetingType = type,
            ScheduledAt = scheduledAt,
            DurationMinutes = duration,
            TeamMemberId = member?.Id,
            TeamMemberName = member?.FullName,
            Location = isTeamMeeting ? "Conference Room A" : null,
            VideoLink = isTeamMeeting ? "https://teams.microsoft.com/meet/123" : null
        };

        // Add attendees
        if (member != null)
        {
            meeting.Attendees.Add(new MeetingAttendee { Id = member.Id, Name = member.FullName, Email = member.Email ?? "", IsOrganizer = false, ResponseStatus = "accepted" });
        }
        
        if (isTeamMeeting)
        {
            foreach (var m in _allTeamMembers.Take(5))
            {
                meeting.Attendees.Add(new MeetingAttendee { Id = m.Id, Name = m.FullName, Email = m.Email ?? "", IsOrganizer = false, ResponseStatus = "accepted" });
            }
        }

        // Add sample agenda items
        meeting.AgendaItems.Add(new MeetingAgendaItem { Id = Guid.NewGuid(), Title = "Review progress", SortOrder = 1, IsCompleted = false });
        meeting.AgendaItems.Add(new MeetingAgendaItem { Id = Guid.NewGuid(), Title = "Discuss blockers", SortOrder = 2, IsCompleted = false });

        return meeting;
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
