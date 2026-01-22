using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Briefing view.
/// Displays role-appropriate content: team overview for managers, personal focus for ICs.
/// Per spec: No percentages, no rankings, no performance scoring.
/// </summary>
public partial class BriefingViewModel : ViewModelBase
{
    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    #endregion

    #region Role Detection

    /// <summary>
    /// Whether the current user is a manager (has direct reports).
    /// Managers see team activity sparkline and team-level stats.
    /// ICs see personal inventory distribution bar.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIndividualContributor))]
    private bool _isManager;

    /// <summary>
    /// Whether the current user is an individual contributor (not a manager).
    /// </summary>
    public bool IsIndividualContributor => !IsManager;

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

    #region Stats (Counts Only - No Percentages Per Spec)

    /// <summary>
    /// Number of team members (manager view).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TeamMemberCountText))]
    private int _teamMemberCount;

    /// <summary>
    /// Tasks due today (both views).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasksDueTodayText))]
    [NotifyPropertyChangedFor(nameof(TasksDueTodayWidth))]
    [NotifyPropertyChangedFor(nameof(TasksDueLaterWidth))]
    private int _tasksDueToday;

    /// <summary>
    /// Tasks due later (IC view - for distribution bar).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasksDueLaterText))]
    [NotifyPropertyChangedFor(nameof(TasksDueTodayWidth))]
    [NotifyPropertyChangedFor(nameof(TasksDueLaterWidth))]
    private int _tasksDueLater;

    /// <summary>
    /// Meetings scheduled today (both views).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeetingsTodayText))]
    private int _meetingsToday;

    /// <summary>
    /// Active goals count (both views).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveGoalsText))]
    [NotifyPropertyChangedFor(nameof(GoalsNeedingAttentionText))]
    private int _activeGoalsCount;

    /// <summary>
    /// Goals that need attention (at risk, stalled, needs review).
    /// Manager view uses this for attention-focused display.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GoalsNeedingAttentionText))]
    private int _goalsNeedingAttention;

    /// <summary>
    /// Overdue tasks count.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasksOverdueText))]
    private int _tasksOverdue;

    /// <summary>
    /// Open items count (IC view - for distribution bar).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OpenItemsText))]
    [NotifyPropertyChangedFor(nameof(OpenItemsWidth))]
    [NotifyPropertyChangedFor(nameof(CompletedItemsWidth))]
    private int _openItemsCount;

    /// <summary>
    /// Completed items count (IC view - for distribution bar).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompletedItemsText))]
    [NotifyPropertyChangedFor(nameof(OpenItemsWidth))]
    [NotifyPropertyChangedFor(nameof(CompletedItemsWidth))]
    private int _completedItemsCount;

    #endregion

    #region Computed Display Text (Attention-Focused)

    public string TeamMemberCountText => TeamMemberCount.ToString();
    public string TasksDueTodayText => TasksDueToday.ToString();
    public string TasksDueLaterText => TasksDueLater.ToString();
    public string MeetingsTodayText => MeetingsToday.ToString();
    public string TasksOverdueText => TasksOverdue.ToString();
    public string ActiveGoalsText => ActiveGoalsCount.ToString();
    public string OpenItemsText => OpenItemsCount.ToString();
    public string CompletedItemsText => CompletedItemsCount.ToString();
    
    /// <summary>
    /// Attention-focused goals text for manager view.
    /// Shows "X goals need attention" (of Y total) format.
    /// </summary>
    public string GoalsNeedingAttentionText => GoalsNeedingAttention > 0 
        ? GoalsNeedingAttention.ToString() 
        : "0";
    
    /// <summary>
    /// Subtitle for goals needing attention.
    /// </summary>
    public string GoalsAttentionSubtitle => GoalsNeedingAttention > 0
        ? $"of {ActiveGoalsCount} need attention"
        : $"of {ActiveGoalsCount} on track";

    #endregion

    #region Distribution Bar Widths (for IC view - GridLength star values)

    /// <summary>
    /// Width proportion for "tasks due today" bar segment.
    /// Returns a GridLength star value based on the ratio.
    /// </summary>
    public GridLength TasksDueTodayWidth
    {
        get
        {
            var total = TasksDueToday + TasksDueLater;
            if (total == 0) return new GridLength(1, GridUnitType.Star);
            return new GridLength(Math.Max(TasksDueToday, 1), GridUnitType.Star);
        }
    }

    /// <summary>
    /// Width proportion for "tasks due later" bar segment.
    /// </summary>
    public GridLength TasksDueLaterWidth
    {
        get
        {
            var total = TasksDueToday + TasksDueLater;
            if (total == 0) return new GridLength(1, GridUnitType.Star);
            return new GridLength(Math.Max(TasksDueLater, 1), GridUnitType.Star);
        }
    }

    /// <summary>
    /// Width proportion for "open items" bar segment.
    /// </summary>
    public GridLength OpenItemsWidth
    {
        get
        {
            var total = OpenItemsCount + CompletedItemsCount;
            if (total == 0) return new GridLength(1, GridUnitType.Star);
            return new GridLength(Math.Max(OpenItemsCount, 1), GridUnitType.Star);
        }
    }

    /// <summary>
    /// Width proportion for "completed items" bar segment.
    /// </summary>
    public GridLength CompletedItemsWidth
    {
        get
        {
            var total = OpenItemsCount + CompletedItemsCount;
            if (total == 0) return new GridLength(1, GridUnitType.Star);
            return new GridLength(Math.Max(CompletedItemsCount, 1), GridUnitType.Star);
        }
    }

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
    /// Tasks sorted by urgency for IC Briefing display.
    /// Order: Overdue → Due today → Due soon (next 3-5 days) → Everything else
    /// </summary>
    public IEnumerable<TaskDetail> SortedUpcomingTasks => UpcomingTasks
        .OrderBy(t => GetTaskUrgencyOrder(t))
        .ThenBy(t => t.DueDate ?? DateTime.MaxValue);

    /// <summary>
    /// Active goals owned by the user or their team.
    /// </summary>
    public ObservableCollection<GoalDetail> Goals { get; } = new();

    /// <summary>
    /// Upcoming meetings.
    /// </summary>
    public ObservableCollection<MeetingDetail> UpcomingMeetings { get; } = new();

    #endregion

    #region Weekly Load Sparkline

    /// <summary>
    /// 7-day load data for the sparkline visualization.
    /// Shows tasks + meetings per day to answer "Is my week about to blow up?"
    /// </summary>
    public ObservableCollection<DailyLoad> WeeklyLoad { get; } = new();

    /// <summary>
    /// Maximum load in the week (for scaling sparkline bars).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxLoadText))]
    private int _maxWeeklyLoad;

    /// <summary>
    /// Display text for maximum load.
    /// </summary>
    public string MaxLoadText => MaxWeeklyLoad > 0 ? MaxWeeklyLoad.ToString() : "";

    /// <summary>
    /// The lightest day label (e.g., "Mon").
    /// </summary>
    [ObservableProperty]
    private string _lightestDayLabel = "";

    /// <summary>
    /// The heaviest day label (e.g., "Thu").
    /// </summary>
    [ObservableProperty]
    private string _heaviestDayLabel = "";

    /// <summary>
    /// Summary text for the week (e.g., "Light week" or "Heavy Tuesday").
    /// </summary>
    [ObservableProperty]
    private string _weekSummaryText = "";

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

    public BriefingViewModel()
    {
        Log("[BriefingViewModel] Constructor called");
        // Subscribe to profile changes
        AuthService.Instance.ProfileChanged += OnProfileChanged;
        
        // Only load data if profile is already available (auto-login case)
        // Otherwise wait for ProfileChanged event
        if (AuthService.Instance.CurrentProfile != null)
        {
            Log("[BriefingViewModel] Profile already available, loading data");
            _ = LoadDataAsync();
        }
        else
        {
            Log("[BriefingViewModel] Profile not yet available, waiting for ProfileChanged");
        }
    }

    private void OnProfileChanged(object? sender, Models.UserProfile? profile)
    {
        Log($"[BriefingViewModel] ProfileChanged event received: {(profile != null ? profile.Email : "NULL")}");
        if (profile != null)
        {
            // Profile just loaded, reload dashboard data
            _ = LoadDataAsync();
        }
    }

    private static void Log(string message)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProCohere", "briefing.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        Console.WriteLine(message);
    }

    /// <summary>
    /// Loads all briefing data from the database.
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Log("[BriefingViewModel] LoadDataAsync starting...");
        if (IsLoading)
        {
            Log("[BriefingViewModel] Already loading, skipping");
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            Log("[BriefingViewModel] IsLoading = true");
            ErrorMessage = null;

            // Set welcome message
            var profile = AuthService.Instance.CurrentProfile;
            Log($"[BriefingViewModel] Profile: {(profile != null ? profile.Email : "NULL")}");
            if (profile != null)
            {
                var firstName = profile.FirstName ?? profile.DisplayName?.Split(' ').FirstOrDefault() ?? "there";
                WelcomeMessage = $"Welcome back, {ToTitleCase(firstName)}";
            }

            // Load dashboard data
            Log("[BriefingViewModel] Calling DashboardService.LoadDashboardDataAsync...");
            var data = await DashboardService.Instance.LoadDashboardDataAsync();
            Log($"[BriefingViewModel] Data loaded: {data.TeamMembers.Count} members, {data.Tasks.Count} tasks, {data.Goals.Count} goals, {data.Meetings.Count} meetings");
            
            // Load visible team members (excludes self)
            var visibleMembers = await TeamService.Instance.GetVisibleTeamMembersAsync();
            var teamMembersExcludingSelf = visibleMembers.Where(m => m.Relation != "self").ToList();
            
            // === ROLE DETECTION (from AuthService, not inferred) ===
            var currentRole = AuthService.Instance.CurrentRole;
            var roleName = currentRole?.Name?.ToLowerInvariant() ?? "unknown";
            
            Log($"[BriefingViewModel] ========== ROLE DETECTION ==========");
            Log($"[BriefingViewModel] CurrentRole from AuthService: '{currentRole?.Name ?? "NULL"}' (Id: {currentRole?.Id})");
            Log($"[BriefingViewModel] Role name normalized: '{roleName}'");
            Log($"[BriefingViewModel] Total visible members: {visibleMembers.Count}");
            Log($"[BriefingViewModel] Members excluding self: {teamMembersExcludingSelf.Count}");
            foreach (var member in visibleMembers.Take(10)) // Log first 10 for debugging
            {
                Log($"[BriefingViewModel]   - {member.FullName} | Relation: {member.Relation} | Id: {member.Id}");
            }
            Log($"[BriefingViewModel] =====================================");

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
            Log($"[BriefingViewModel] Stats merged into visible members");

            // Determine if user is a manager based on their ROLE from AuthService
            // Admin and Manager roles → Manager view
            // Team Member and Viewer roles → IC view
            var normalizedRole = roleName?.ToLowerInvariant() ?? "";
            IsManager = normalizedRole == "admin" || normalizedRole == "manager";
            Log($"[BriefingViewModel] *** IsManager = {IsManager} (role: '{roleName}', normalized: '{normalizedRole}') ***");
            Log($"[BriefingViewModel] *** IsIndividualContributor = {IsIndividualContributor} ***");

            // === LOAD WEEKLY SPARKLINE DATA ===
            Log("[BriefingViewModel] Loading weekly load sparkline data...");
            var weeklyLoadData = await DashboardService.Instance.GetWeeklyLoadAsync();
            
            // Calculate max first so we can set it on each item
            var maxLoad = weeklyLoadData.Count > 0 ? weeklyLoadData.Max(d => d.TotalLoad) : 0;
            MaxWeeklyLoad = maxLoad;
            
            // Set MaxLoad on each item for computed properties, then add to collection
            WeeklyLoad.Clear();
            foreach (var day in weeklyLoadData)
            {
                day.MaxLoad = maxLoad;
                WeeklyLoad.Add(day);
            }
            
            // Calculate sparkline stats (no judgmental labels - bars speak for themselves)
            if (weeklyLoadData.Count > 0)
            {
                var lightestDay = weeklyLoadData.OrderBy(d => d.TotalLoad).First();
                var heaviestDay = weeklyLoadData.OrderByDescending(d => d.TotalLoad).First();
                LightestDayLabel = lightestDay.DayLabel;
                HeaviestDayLabel = heaviestDay.DayLabel;
                
                // Neutral summary - just the count, no "light/heavy" judgment
                var totalItems = weeklyLoadData.Sum(d => d.TotalLoad);
                WeekSummaryText = totalItems > 0 ? $"{totalItems} items" : "";
            }
            else
            {
                LightestDayLabel = "";
                HeaviestDayLabel = "";
                WeekSummaryText = "";
            }
            Log($"[BriefingViewModel] Weekly load: {WeeklyLoad.Count} days, max={MaxWeeklyLoad}, summary='{WeekSummaryText}'");

            // Update stats - Attention-focused, not inventory
            TeamMemberCount = teamMembersExcludingSelf.Count;
            
            // Calculate task counts
            var today = DateTime.Today;
            var allTasks = data.Tasks.Where(t => t.Status != "completed").ToList();
            TasksDueToday = allTasks.Count(t => t.DueDate?.Date == today);
            TasksDueLater = allTasks.Count(t => t.DueDate?.Date > today);
            TasksOverdue = allTasks.Count(t => t.DueDate?.Date < today);
            
            // Meeting count for today
            MeetingsToday = data.Meetings.Count(m => m.ScheduledAt?.Date == today);
            
            // Goal counts - Active and those needing attention
            var activeGoals = data.Goals.Where(g => g.Status != "completed").ToList();
            ActiveGoalsCount = activeGoals.Count;
            // Goals needing attention: at_risk, needs_review, stalled, or no recent activity
            GoalsNeedingAttention = activeGoals.Count(g => 
                g.Status == "at_risk" || 
                g.Status == "needs_review" || 
                g.Status == "stalled" ||
                g.Status == "blocked");
            
            // Open/Completed for IC distribution bar
            OpenItemsCount = allTasks.Count;
            CompletedItemsCount = data.Tasks.Count(t => t.Status == "completed");
            
            Log($"[BriefingViewModel] Stats: {TeamMemberCount} members, {TasksDueToday} due today, {TasksOverdue} overdue, {MeetingsToday} meetings, {ActiveGoalsCount} goals ({GoalsNeedingAttention} need attention)");

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

            // Update goals collection - prioritize those needing attention
            Goals.Clear();
            var goalsToShow = activeGoals
                .OrderByDescending(g => g.Status == "at_risk" || g.Status == "needs_review" || g.Status == "blocked")
                .ThenBy(g => g.Title)
                .Take(5);
            foreach (var goal in goalsToShow)
            {
                Goals.Add(goal);
            }

            // Load upcoming meetings (placeholder for now - will need MeetingDetail model)
            UpcomingMeetings.Clear();
            // TODO: Load actual meetings when meeting service is implemented

            Log($"[BriefingViewModel] Collections updated: {TeamMembers.Count} members, {UpcomingTasks.Count} tasks, {Goals.Count} goals");

            // Check for errors
            if (!string.IsNullOrEmpty(DashboardService.Instance.LastError))
            {
                HasError = true;
                ErrorMessage = DashboardService.Instance.LastError;
                Log($"[BriefingViewModel] Service error: {ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load briefing: {ex.Message}";
            Log($"[BriefingViewModel] EXCEPTION: {ex}");
        }
        finally
        {
            IsLoading = false;
            Log("[BriefingViewModel] LoadDataAsync complete, IsLoading = false");
        }
    }

    /// <summary>
    /// Refreshes all briefing data.
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

    /// <summary>
    /// Returns urgency order for task sorting.
    /// 0 = Overdue, 1 = Due today, 2 = Due soon (1-5 days), 3 = Everything else
    /// </summary>
    private static int GetTaskUrgencyOrder(TaskDetail task)
    {
        if (!task.DueDate.HasValue)
            return 4; // No due date goes last

        var today = DateTime.UtcNow.Date;
        var dueDate = task.DueDate.Value.Date;

        if (dueDate < today)
            return 0; // Overdue
        if (dueDate == today)
            return 1; // Due today
        if ((dueDate - today).Days <= 5)
            return 2; // Due soon (next 5 days)
        
        return 3; // Everything else
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
