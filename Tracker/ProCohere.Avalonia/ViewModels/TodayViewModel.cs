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
/// ViewModel for the Today/Dashboard view.
/// Displays team overview, stat cards, and upcoming tasks.
/// </summary>
public partial class TodayViewModel : ViewModelBase
{
    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Scope (Today/Week)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTodayScope))]
    [NotifyPropertyChangedFor(nameof(IsWeekScope))]
    [NotifyPropertyChangedFor(nameof(DateRangeText))]
    private BriefingScope _currentScope = BriefingScope.Today;

    /// <summary>
    /// Whether the current scope is Today.
    /// </summary>
    public bool IsTodayScope => CurrentScope == BriefingScope.Today;

    /// <summary>
    /// Whether the current scope is Week.
    /// </summary>
    public bool IsWeekScope => CurrentScope == BriefingScope.Week;

    /// <summary>
    /// Date range text based on scope.
    /// </summary>
    public string DateRangeText => CurrentScope switch
    {
        BriefingScope.Today => DateTime.Now.ToString("dddd, MMMM d, yyyy"),
        BriefingScope.Week => GetWeekRangeText(),
        _ => DateTime.Now.ToString("dddd, MMMM d, yyyy")
    };

    private static string GetWeekRangeText()
    {
        var today = DateTime.Now;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        
        if (startOfWeek.Month == endOfWeek.Month)
            return $"{startOfWeek:MMMM d} - {endOfWeek:d}, {endOfWeek:yyyy}";
        else if (startOfWeek.Year == endOfWeek.Year)
            return $"{startOfWeek:MMM d} - {endOfWeek:MMM d}, {endOfWeek:yyyy}";
        else
            return $"{startOfWeek:MMM d, yyyy} - {endOfWeek:MMM d, yyyy}";
    }

    [RelayCommand]
    private async Task SetScope(string scope)
    {
        CurrentScope = scope switch
        {
            "Week" => BriefingScope.Week,
            _ => BriefingScope.Today
        };
        
        // Refresh data with new scope
        await LoadDataAsync();
    }

    #endregion

    #region Stats

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TeamMemberCountText))]
    private int _teamMemberCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TaskCompletionText))]
    [NotifyPropertyChangedFor(nameof(TaskCompletionColor))]
    private int _taskCompletionPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GoalsOnTrackText))]
    [NotifyPropertyChangedFor(nameof(GoalsOnTrackColor))]
    private int _goalsOnTrackPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveProjectCountText))]
    private int _activeProjectCount;

    #endregion

    #region Computed Display Text

    public string TeamMemberCountText => TeamMemberCount.ToString();
    public string TaskCompletionText => $"{TaskCompletionPercent}%";
    public string GoalsOnTrackText => $"{GoalsOnTrackPercent}%";
    public string ActiveProjectCountText => ActiveProjectCount.ToString();

    /// <summary>
    /// Color for task completion (green > 75%, amber 50-75%, red < 50%).
    /// </summary>
    public string TaskCompletionColor => TaskCompletionPercent switch
    {
        >= 75 => "#22C55E", // Green
        >= 50 => "#F59E0B", // Amber
        _ => "#EF4444"      // Red
    };

    /// <summary>
    /// Color for goals on track (green > 75%, amber 50-75%, red < 50%).
    /// </summary>
    public string GoalsOnTrackColor => GoalsOnTrackPercent switch
    {
        >= 75 => "#22C55E", // Green
        >= 50 => "#F59E0B", // Amber
        _ => "#EF4444"      // Red
    };

    #endregion

    #region Collections

    /// <summary>
    /// Team members managed by the current user.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> TeamMembers { get; } = new();

    /// <summary>
    /// Upcoming tasks (due within 7 days or overdue).
    /// </summary>
    public ObservableCollection<TaskDetail> UpcomingTasks { get; } = new();

    /// <summary>
    /// Active goals owned by the user or their team.
    /// </summary>
    public ObservableCollection<GoalDetail> Goals { get; } = new();

    /// <summary>
    /// Upcoming meetings.
    /// </summary>
    public ObservableCollection<MeetingDetail> UpcomingMeetings { get; } = new();

    #endregion

    #region Quick Action Commands

    [RelayCommand]
    private void AddTask()
    {
        // TODO: Navigate to add task view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Task clicked");
    }

    [RelayCommand]
    private void AddGoal()
    {
        // TODO: Navigate to add goal view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Goal clicked");
    }

    [RelayCommand]
    private void AddMeeting()
    {
        // TODO: Navigate to schedule meeting view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Meeting clicked");
    }

    [RelayCommand]
    private void AddNote()
    {
        // TODO: Navigate to add note view or show dialog
        System.Diagnostics.Debug.WriteLine("Add Note clicked");
    }

    #endregion

    #region User Info

    [ObservableProperty]
    private string _welcomeMessage = "Welcome";

    #endregion

    public TodayViewModel()
    {
        Log("[TodayViewModel] Constructor called");
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Only load data if profile is already available (auto-login case)
        // Otherwise wait for ProfileChanged event
        if (AuthService.Instance.CurrentProfile != null)
        {
            Log("[TodayViewModel] Profile already available, loading data");
            _ = LoadDataAsync();
        }
        else
        {
            Log("[TodayViewModel] Profile not yet available, waiting for ProfileChanged");
        }
    }

    private void OnProfileChanged(object? sender, Models.UserProfile? profile)
    {
        Log($"[TodayViewModel] ProfileChanged event received: {(profile != null ? profile.Email : "NULL")}");
        if (profile != null)
        {
            // Profile just loaded, reload dashboard data
            _ = LoadDataAsync();
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProCohere", "dashboard.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        Console.WriteLine(message);
    }

    /// <summary>
    /// Loads all dashboard data from the database.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Log("[TodayViewModel] LoadDataAsync starting...");
        if (IsLoading)
        {
            Log("[TodayViewModel] Already loading, skipping");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            Log("[TodayViewModel] IsLoading = true");
            ErrorMessage = null;

            // Set welcome message
            var profile = AuthService.Instance.CurrentProfile;
            Log($"[TodayViewModel] Profile: {(profile != null ? profile.Email : "NULL")}");
            if (profile != null)
            {
                var firstName = profile.FirstName ?? profile.DisplayName?.Split(' ').FirstOrDefault() ?? "there";
                WelcomeMessage = $"Welcome back, {ToTitleCase(firstName)}";
            }

            // Load dashboard data
            Log("[TodayViewModel] Calling DashboardService.LoadDashboardDataAsync...");
            var data = await DashboardService.Instance.LoadDashboardDataAsync();
            Log($"[TodayViewModel] Data loaded: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks");
            
            // Load visible team members (excludes self)
            var visibleMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            var teamMembersExcludingSelf = visibleMembers.Where(m => m.Relation != "self").ToList();
            Log($"[TodayViewModel] Visible team members (excluding self): {teamMembersExcludingSelf.Count}");

            // Merge stats from dashboard data into visible members
            // DashboardService computes OpenTaskCount/ActiveGoalCount, but TeamService loads hierarchy
            var dashboardMemberDict = data.TeamMembers.ToDictionary(m => m.Id);
            foreach (var member in teamMembersExcludingSelf)
            {
                if (dashboardMemberDict.TryGetValue(member.Id, out var dashMember))
                {
                    member.OpenTaskCount = dashMember.OpenTaskCount;
                    member.ActiveGoalCount = dashMember.ActiveGoalCount;
                    member.LastMeetingDate = dashMember.LastMeetingDate;
                }
                else
                {
                    // Compute stats directly from dashboard data
                    member.OpenTaskCount = data.Tasks.Count(t => 
                        t.OwnerTeamMemberId == member.Id && t.Status != "completed");
                    member.ActiveGoalCount = data.Goals.Count(g => 
                        g.OwnerTeamMemberId == member.Id && g.Status != "completed");
                    // Find last meeting for this member
                    var lastMeeting = data.Meetings
                        .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true)
                        .OrderByDescending(m => m.ScheduledAt)
                        .FirstOrDefault();
                    member.LastMeetingDate = lastMeeting?.ScheduledAt;
                }
            }
            Log($"[TodayViewModel] Stats merged into visible members");

            // Update stats - use visible count instead of dashboard count
            TeamMemberCount = teamMembersExcludingSelf.Count;
            TaskCompletionPercent = data.Stats.TaskCompletionPercent;
            GoalsOnTrackPercent = data.Stats.GoalsOnTrackPercent;
            ActiveProjectCount = data.Stats.ActiveProjectCount;
            Log($"[TodayViewModel] Stats: {TeamMemberCount} members, {TaskCompletionPercent}% tasks, {GoalsOnTrackPercent}% goals, {ActiveProjectCount} projects");

            // Update collections - use visible members excluding self
            TeamMembers.Clear();
            foreach (var member in teamMembersExcludingSelf)
            {
                TeamMembers.Add(member);
            }

            UpcomingTasks.Clear();
            foreach (var task in data.UpcomingTasks)
            {
                UpcomingTasks.Add(task);
            }

            // Update goals collection
            Goals.Clear();
            foreach (var goal in data.Goals.Where(g => g.Status != "completed").Take(5))
            {
                Goals.Add(goal);
            }

            // Load upcoming meetings (placeholder for now - will need MeetingDetail model)
            UpcomingMeetings.Clear();
            // TODO: Load actual meetings when meeting service is implemented

            Log($"[TodayViewModel] Collections updated: {TeamMembers.Count} members, {UpcomingTasks.Count} tasks, {Goals.Count} goals");

            // Check for errors
            if (!string.IsNullOrEmpty(DashboardService.Instance.LastError))
            {
                HasError = true;
                ErrorMessage = DashboardService.Instance.LastError;
                Log($"[TodayViewModel] Service error: {ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
            Log($"[TodayViewModel] EXCEPTION: {ex}");
        }
        finally
        {
            IsLoading = false;
            Log("[TodayViewModel] LoadDataAsync complete, IsLoading = false");
        }
    }

    /// <summary>
    /// Refreshes all dashboard data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Converts a string to Title Case.
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) +
                          (words[i].Length > 1 ? words[i][1..].ToLower() : string.Empty);
            }
        }
        return string.Join(" ", words);
    }
}

/// <summary>
/// Briefing view scope options.
/// </summary>
public enum BriefingScope
{
    Today,
    Week
}
