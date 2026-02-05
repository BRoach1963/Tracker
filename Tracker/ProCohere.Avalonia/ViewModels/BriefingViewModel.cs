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
using ProCohere.Avalonia.Services.Insights;
using ProCohere.Avalonia.Services.Insights.Analyzers;

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

    /// <summary>
    /// Project signals (passive awareness indicators).
    /// Shows projects needing attention: due soon, overdue tasks, stale, etc.
    /// These inform awareness - clicking navigates to Projects tab.
    /// </summary>
    public ObservableCollection<ProjectSignal> ProjectSignals { get; } = new();

    /// <summary>
    /// Whether there are any project signals to display.
    /// </summary>
    public bool HasProjectSignals => ProjectSignals.Count > 0;

    /// <summary>
    /// AI-generated insights (actionable recommendations).
    /// Populated by InsightEngine running all registered analyzers.
    /// </summary>
    public ObservableCollection<Insight> AIInsights { get; } = new();

    /// <summary>
    /// Whether there are any AI insights to display.
    /// </summary>
    public bool HasAIInsights => AIInsights.Count > 0;

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
        Log("[BriefingViewModel] AddTask command - requesting dialog");
        CreateTaskDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddGoal()
    {
        Log("[BriefingViewModel] AddGoal command - requesting dialog");
        CreateGoalDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddMeeting()
    {
        Log("[BriefingViewModel] AddMeeting command - requesting dialog");
        CreateMeetingDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddNote()
    {
        Log("[BriefingViewModel] AddNote command - requesting dialog");
        CreateNoteDialogRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region User Info

    [ObservableProperty]
    private string _welcomeMessage = "Welcome";

    #endregion

    #region Services

    /// <summary>
    /// Repository for insight data operations.
    /// Instantiated once per ViewModel lifecycle.
    /// </summary>
    private readonly IInsightRepository _insightRepository = new InsightRepository();

    #endregion

    public BriefingViewModel()
    {
        Log("[BriefingViewModel] Constructor called");
        
        // Register all AI analyzers (one-time setup)
        RegisterAnalyzers();
        
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

    /// <summary>
    /// Registers all AI insight analyzers with the engine.
    /// Called once during initialization.
    /// </summary>
    private void RegisterAnalyzers()
    {
        var engine = InsightEngine.Instance;
        engine.RegisterAnalyzer(new ActionItemStalenessAnalyzer());
        engine.RegisterAnalyzer(new GoalTrajectoryAnalyzer());
        engine.RegisterAnalyzer(new MeetingCadenceAnalyzer());
        engine.RegisterAnalyzer(new MetricGapAnalyzer());
        engine.RegisterAnalyzer(new PersonalDateAnalyzer());
        engine.RegisterAnalyzer(new SurveySentimentAnalyzer());
        Log("[BriefingViewModel] Registered 6 AI analyzers");
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
            // Per GOALS_SPEC: Use DerivedHealth (computed from linked metrics) not legacy Status
            var activeGoals = data.Goals.Where(g => g.Lifecycle == GoalLifecycle.Active).ToList();
            ActiveGoalsCount = activeGoals.Count;
            // Goals needing attention based on DerivedHealth (AtRisk or OffTrack)
            GoalsNeedingAttention = activeGoals.Count(g => 
                g.DerivedHealth == GoalDerivedHealth.AtRisk || 
                g.DerivedHealth == GoalDerivedHealth.OffTrack);
            
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
            // Per GOALS_SPEC: Sort by DerivedHealth (OffTrack first, then AtRisk)
            Goals.Clear();
            var goalsToShow = activeGoals
                .OrderByDescending(g => g.DerivedHealth == GoalDerivedHealth.OffTrack)
                .ThenByDescending(g => g.DerivedHealth == GoalDerivedHealth.AtRisk)
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

            // Load project signals (passive awareness indicators)
            await LoadProjectSignalsAsync();

            // Load AI insights (run analysis and get results)
            await LoadAIInsightsAsync();

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
    /// Loads AI insights by running all registered analyzers.
    /// Insights are generated based on current user/org data.
    /// </summary>
    private async Task LoadAIInsightsAsync()
    {
        try
        {
            Log("[BriefingViewModel] Loading AI insights...");
            
            var teamMember = AuthService.Instance.CurrentTeamMember;
            var profile = AuthService.Instance.CurrentProfile;
            
            if (teamMember == null || profile?.OrganizationId == null)
            {
                Log("[BriefingViewModel] No current team member or org, skipping insights");
                return;
            }

            // Run all analyzers
            var createdCount = await InsightEngine.Instance.RunAnalysisAsync(teamMember.Id, profile.OrganizationId.Value);
            Log($"[BriefingViewModel] InsightEngine created {createdCount} new insights");

            // Get active insights
            var insights = await InsightEngine.Instance.GetActiveInsightsAsync(teamMember.Id);
            Log($"[BriefingViewModel] Retrieved {insights.Count} active insights");

            // Update collection
            AIInsights.Clear();
            foreach (var insight in insights.OrderByDescending(i => i.Severity).ThenByDescending(i => i.CreatedAt))
            {
                AIInsights.Add(insight);
            }

            OnPropertyChanged(nameof(HasAIInsights));
        }
        catch (Exception ex)
        {
            Log($"[BriefingViewModel] Failed to load AI insights: {ex}");
            // Don't throw - insights are non-critical
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

    #region Insight Actions

    /// <summary>
    /// Dismisses an insight (marks it as dismissed).
    /// </summary>
    [RelayCommand]
    private async Task DismissInsight(Insight insight)
    {
        if (insight == null)
        {
            Log("[BriefingViewModel] Cannot dismiss null insight");
            return;
        }

        try
        {
            Log($"[BriefingViewModel] Dismissing insight: {insight.Id} - {insight.Title}");
            
            var teamMember = AuthService.Instance.CurrentTeamMember;
            if (teamMember == null)
            {
                Log("[BriefingViewModel] No current team member, cannot dismiss insight");
                ErrorMessage = "You must be logged in to dismiss insights.";
                HasError = true;
                return;
            }
            
            await _insightRepository.DismissInsightAsync(insight.Id, teamMember.Id);
            
            // Remove from UI immediately for responsive UX
            AIInsights.Remove(insight);
            OnPropertyChanged(nameof(HasAIInsights));
            
            Log($"[BriefingViewModel] Insight dismissed successfully");
        }
        catch (Exception ex)
        {
            Log($"[BriefingViewModel] Failed to dismiss insight: {ex}");
            ErrorMessage = "Failed to dismiss insight. Please try again.";
            HasError = true;
        }
    }

    /// <summary>
    /// Snoozes an insight until a specific time.
    /// Currently snoozes for 24 hours.
    /// </summary>
    [RelayCommand]
    private async Task SnoozeInsight(Insight insight)
    {
        if (insight == null)
        {
            Log("[BriefingViewModel] Cannot snooze null insight");
            return;
        }

        try
        {
            Log($"[BriefingViewModel] Snoozing insight: {insight.Id} - {insight.Title}");
            
            // Snooze for 24 hours from now
            var snoozeUntil = DateTime.UtcNow.AddDays(1);
            
            await _insightRepository.SnoozeInsightAsync(insight.Id, snoozeUntil);
            
            // Remove from UI immediately for responsive UX
            AIInsights.Remove(insight);
            OnPropertyChanged(nameof(HasAIInsights));
            
            Log($"[BriefingViewModel] Insight snoozed until {snoozeUntil:yyyy-MM-dd HH:mm} UTC");
        }
        catch (Exception ex)
        {
            Log($"[BriefingViewModel] Failed to snooze insight: {ex}");
            ErrorMessage = "Failed to snooze insight. Please try again.";
            HasError = true;
        }
    }

    /// <summary>
    /// Navigates to the entity referenced by an insight and marks it as acted upon.
    /// </summary>
    [RelayCommand]
    private async Task ViewInsight(Insight insight)
    {
        if (insight == null)
        {
            Log("[BriefingViewModel] Cannot view null insight");
            return;
        }

        try
        {
            Log($"[BriefingViewModel] Viewing insight entity: {insight.EntityType}/{insight.EntityId} - {insight.Title}");
            
            // Mark as acted on first
            await _insightRepository.MarkInsightActionedAsync(insight.Id);
            
            // Navigate based on entity type
            if (insight.EntityId.HasValue && !string.IsNullOrEmpty(insight.EntityType))
            {
                RaiseNavigationEvent(insight.EntityType, insight.EntityId.Value);
            }
            
            // Remove from UI after navigation is initiated
            AIInsights.Remove(insight);
            OnPropertyChanged(nameof(HasAIInsights));
            
            Log($"[BriefingViewModel] Insight marked as acted on, navigation initiated");
        }
        catch (Exception ex)
        {
            Log($"[BriefingViewModel] Failed to view insight: {ex}");
            ErrorMessage = "Failed to navigate to insight. Please try again.";
            HasError = true;
        }
    }

    /// <summary>
    /// Raises the appropriate navigation event based on entity type.
    /// MainWindowViewModel subscribes to these events.
    /// </summary>
    private void RaiseNavigationEvent(string entityType, Guid entityId)
    {
        Log($"[BriefingViewModel] Raising navigation event for {entityType} with ID {entityId}");
        
        switch (entityType.ToLowerInvariant())
        {
            case "task":
            case "action_item":
                NavigateToTaskRequested?.Invoke(this, entityId);
                break;
                
            case "goal":
                NavigateToGoalRequested?.Invoke(this, entityId);
                break;
                
            case "meeting":
                NavigateToMeetingRequested?.Invoke(this, entityId);
                break;
                
            case "metric":
                NavigateToMetricRequested?.Invoke(this, entityId);
                break;
                
            default:
                Log($"[BriefingViewModel] Unknown entity type for navigation: {entityType}");
                break;
        }
    }

    #endregion

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
    
    /// <summary>
    /// Computes project signals based on current project data.
    /// Signals are passive awareness indicators - they inform, not prescribe.
    /// </summary>
    private async Task LoadProjectSignalsAsync()
    {
        try
        {
            Log("[BriefingViewModel] Loading project signals...");
            
            // Get projects the current user is involved with
            var teamMember = AuthService.Instance.CurrentTeamMember;
            if (teamMember == null)
            {
                Log("[BriefingViewModel] No current team member, skipping project signals");
                return;
            }
            
            var projects = await ProjectService.Instance.GetProjectsForTeamMemberAsync(teamMember.Id);
            var activeProjects = projects.Where(p => p.Status != ProjectStatus.Completed && !p.IsDeleted).ToList();
            
            if (activeProjects.Count == 0)
            {
                Log("[BriefingViewModel] No active projects, skipping project signals");
                return;
            }
            
            var signals = new List<ProjectSignal>();
            var today = DateTime.Today;
            
            // Build project lookup for name resolution
            var projectLookup = activeProjects.ToDictionary(p => p.Id);
            
            // Get batch signals for all projects in ONE RPC call (replaces 2N queries)
            var projectIds = activeProjects.Select(p => p.Id).ToList();
            var batchSignals = await ProjectService.Instance.GetProjectSignalsBatchAsync(projectIds);
            var signalLookup = batchSignals.ToDictionary(s => s.ProjectId);
            
            foreach (var project in activeProjects)
            {
                // Check for due soon (within 7 days)
                if (project.DueDate.HasValue)
                {
                    var daysUntilDue = (project.DueDate.Value.Date - today).Days;
                    if (daysUntilDue < 0)
                    {
                        // Overdue project
                        signals.Add(new ProjectSignal
                        {
                            ProjectId = project.Id,
                            ProjectName = project.Name ?? "Untitled",
                            SignalType = ProjectSignalType.DueSoon,
                            Summary = $"Overdue by {Math.Abs(daysUntilDue)} day{(Math.Abs(daysUntilDue) == 1 ? "" : "s")}",
                            Priority = 100 + Math.Abs(daysUntilDue) // Higher priority the more overdue
                        });
                    }
                    else if (daysUntilDue <= 7)
                    {
                        signals.Add(new ProjectSignal
                        {
                            ProjectId = project.Id,
                            ProjectName = project.Name ?? "Untitled",
                            SignalType = ProjectSignalType.DueSoon,
                            Summary = daysUntilDue == 0 ? "Due today" : $"Due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")}",
                            Priority = 50 + (7 - daysUntilDue) // Higher priority the sooner
                        });
                    }
                }
                
                // Check for overdue tasks and goals needing attention from batch result
                if (signalLookup.TryGetValue(project.Id, out var batchResult))
                {
                    if (batchResult.OverdueTaskCount > 0)
                    {
                        signals.Add(new ProjectSignal
                        {
                            ProjectId = project.Id,
                            ProjectName = project.Name ?? "Untitled",
                            SignalType = ProjectSignalType.OverdueTasks,
                            Summary = $"{batchResult.OverdueTaskCount} overdue task{(batchResult.OverdueTaskCount == 1 ? "" : "s")}",
                            Priority = 80 + batchResult.OverdueTaskCount
                        });
                    }
                    
                    if (batchResult.GoalsNeedingAttention > 0)
                    {
                        signals.Add(new ProjectSignal
                        {
                            ProjectId = project.Id,
                            ProjectName = project.Name ?? "Untitled",
                            SignalType = ProjectSignalType.GoalsNeedAttention,
                            Summary = $"{batchResult.GoalsNeedingAttention} goal{(batchResult.GoalsNeedingAttention == 1 ? "" : "s")} need{(batchResult.GoalsNeedingAttention == 1 ? "s" : "")} attention",
                            Priority = 60 + batchResult.GoalsNeedingAttention
                        });
                    }
                }
                
                // Check for stale projects (no activity in 14+ days)
                var lastActivity = project.UpdatedAt;
                var daysSinceActivity = (today - lastActivity.Date).Days;
                if (daysSinceActivity >= 14)
                {
                    signals.Add(new ProjectSignal
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name ?? "Untitled",
                        SignalType = ProjectSignalType.Stale,
                        Summary = $"No activity for {daysSinceActivity} days",
                        Priority = 20
                    });
                }
            }
            
            // Sort by priority (descending) and limit to top signals
            ProjectSignals.Clear();
            foreach (var signal in signals.OrderByDescending(s => s.Priority).Take(5))
            {
                ProjectSignals.Add(signal);
            }
            
            OnPropertyChanged(nameof(HasProjectSignals));
            Log($"[BriefingViewModel] Loaded {ProjectSignals.Count} project signals");
        }
        catch (Exception ex)
        {
            Log($"[BriefingViewModel] Error loading project signals: {ex.Message}");
            // Don't fail briefing for signal errors
        }
    }

    #region Dialog Events

    /// <summary>
    /// Event raised when the user wants to create a new task.
    /// The View subscribes to this and shows the dialog using AppDialogService.
    /// </summary>
    public event EventHandler? CreateTaskDialogRequested;

    /// <summary>
    /// Event raised when the user wants to create a new meeting.
    /// The View subscribes to this and shows the dialog using AppDialogService.
    /// </summary>
    public event EventHandler? CreateMeetingDialogRequested;

    /// <summary>
    /// Event raised when the user wants to create a new goal.
    /// The View subscribes to this and shows the dialog using AppDialogService.
    /// </summary>
    public event EventHandler? CreateGoalDialogRequested;

    /// <summary>
    /// Event raised when the user wants to create a new note.
    /// The View subscribes to this and shows the dialog using AppDialogService.
    /// </summary>
    public event EventHandler? CreateNoteDialogRequested;

    /// <summary>
    /// Called by the View when a meeting is saved from the dialog.
    /// </summary>
    public void OnMeetingSaved(MeetingDetail meeting)
    {
        Log($"[BriefingViewModel] Meeting saved: {meeting.Title}");
        
        // Add to the meetings collection if it's scheduled for today/this week
        var existing = UpcomingMeetings.FirstOrDefault(m => m.Id == meeting.Id);
        if (existing == null)
        {
            // Check if it belongs in the current view
            if (meeting.ScheduledAtLocal.HasValue)
            {
                var scheduledDate = meeting.ScheduledAtLocal.Value.Date;
                var today = DateTime.Now.Date;
                var endOfWeek = today.AddDays(7);
                
                if ((IsTodayScope && scheduledDate == today) || 
                    (IsWeekScope && scheduledDate >= today && scheduledDate < endOfWeek))
                {
                    UpcomingMeetings.Add(meeting);
                    Log($"[BriefingViewModel] Added new meeting to collection");
                }
            }
        }
        else
        {
            // Update existing meeting in place
            var index = UpcomingMeetings.IndexOf(existing);
            UpcomingMeetings[index] = meeting;
            Log($"[BriefingViewModel] Updated existing meeting in collection");
        }
    }
    
    /// <summary>
    /// Called by the View when a task is saved from the dialog.
    /// </summary>
    public void OnTaskSaved(TaskDetail task)
    {
        Log($"[BriefingViewModel] Task saved: {task.Title}");
        
        // Add to the tasks collection if it's due today/this week
        var existing = UpcomingTasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
        {
            // Check if it belongs in the current view
            if (task.DueDate.HasValue)
            {
                var dueDate = task.DueDate.Value.Date;
                var today = DateTime.Now.Date;
                var endOfWeek = today.AddDays(7);
                
                if ((IsTodayScope && dueDate == today) || 
                    (IsWeekScope && dueDate >= today && dueDate < endOfWeek))
                {
                    UpcomingTasks.Add(task);
                    Log($"[BriefingViewModel] Added new task to collection");
                }
            }
            else
            {
                // Tasks without due date go in the backlog/general list
                UpcomingTasks.Add(task);
                Log($"[BriefingViewModel] Added new task (no due date) to collection");
            }
        }
        else
        {
            // Update existing task in place
            var index = UpcomingTasks.IndexOf(existing);
            UpcomingTasks[index] = task;
            Log($"[BriefingViewModel] Updated existing task in collection");
        }
    }
    
    /// <summary>
    /// Event raised when user clicks a project signal.
    /// The shell should navigate to Projects tab.
    /// </summary>
    public event EventHandler<Guid>? NavigateToProjectRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to a specific task.
    /// </summary>
    public event EventHandler<Guid>? NavigateToTaskRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to a specific goal.
    /// </summary>
    public event EventHandler<Guid>? NavigateToGoalRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to a specific metric.
    /// </summary>
    public event EventHandler<Guid>? NavigateToMetricRequested;
    
    /// <summary>
    /// Event raised when user wants to navigate to a specific meeting.
    /// </summary>
    public event EventHandler<Guid>? NavigateToMeetingRequested;
    
    /// <summary>
    /// Command to handle clicking on a project signal.
    /// Navigates to the Projects tab (no flyout - just navigation).
    /// </summary>
    [RelayCommand]
    private void NavigateToProject(ProjectSignal signal)
    {
        if (signal == null) return;
        Log($"[BriefingViewModel] Project signal clicked: {signal.ProjectName} ({signal.SignalType})");
        NavigateToProjectRequested?.Invoke(this, signal.ProjectId);
    }

    #endregion
}

/// <summary>
/// Briefing view scope options.
/// </summary>
public enum BriefingScope
{
    Today,
    Week
}
