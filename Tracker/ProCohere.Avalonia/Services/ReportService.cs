using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Reports;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for generating report data with date range filtering.
/// Aggregates data from DashboardService and other services for report views.
/// </summary>
public class ReportService
{
    #region Singleton

    private static readonly Lazy<ReportService> _instance =
        new(() => new ReportService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static ReportService Instance => _instance.Value;

    private ReportService() { }

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "report_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    /// <summary>
    /// Generates overview report data for the specified date range.
    /// </summary>
    public async Task<OverviewReportData> GetOverviewReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetOverviewReportAsync: {startDate:d} - {endDate:d}");

        var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
        
        // Filter data by date range
        var goals = FilterGoalsByDateRange(dashboard.Goals, startDate, endDate);
        var tasks = FilterTasksByDateRange(dashboard.Tasks, startDate, endDate);
        var meetings = FilterMeetingsByDateRange(dashboard.Meetings, startDate, endDate);
        var feedback = FilterFeedbackByDateRange(dashboard.Feedback, startDate, endDate);

        // Calculate goal stats
        var goalsOnTrack = goals.Count(g => g.Health == GoalHealth.OnTrack);
        var goalsAtRisk = goals.Count(g => g.Health == GoalHealth.AtRisk || g.Health == GoalHealth.NeedsAttention);
        var goalsCompleted = goals.Count(g => g.Status == "completed");

        // Calculate task stats
        var tasksCompleted = tasks.Count(t => t.Status == "completed");
        var tasksOverdue = tasks.Count(t => t.IsOverdue);
        var tasksOpen = tasks.Count(t => t.Status != "completed");

        // Calculate meeting stats
        var totalMeetingMinutes = meetings.Sum(m => m.DurationMinutes);
        var oneOnOneMeetings = meetings.Count(m => m.MeetingType == "one_on_one");
        var teamMeetings = meetings.Count(m => m.MeetingType == "team");

        // Load metrics for trend analysis
        var metrics = await MetricsService.Instance.GetAllMetricsAsync();
        var metricsImproving = 0;
        var metricsDeclining = 0;

        foreach (var metric in metrics)
        {
            var trend = await MetricsService.Instance.GetTrendAnalysisAsync(metric.Id);
            if (trend != null)
            {
                if (trend.Direction == MetricTrend.TrendingUp)
                    metricsImproving++;
                else if (trend.Direction == MetricTrend.TrendingDown)
                    metricsDeclining++;
            }
        }

        return new OverviewReportData
        {
            TotalGoals = goals.Count,
            GoalsOnTrack = goalsOnTrack,
            GoalsAtRisk = goalsAtRisk,
            GoalsCompleted = goalsCompleted,
            
            TotalTasks = tasks.Count,
            TasksCompleted = tasksCompleted,
            TasksOverdue = tasksOverdue,
            TasksOpen = tasksOpen,
            
            TotalMeetings = meetings.Count,
            TotalMeetingMinutes = totalMeetingMinutes ?? 0,
            OneOnOneMeetings = oneOnOneMeetings,
            TeamMeetings = teamMeetings,
            
            TotalMetrics = metrics.Count,
            MetricsImproving = metricsImproving,
            MetricsDeclining = metricsDeclining,
            
            TeamMemberCount = dashboard.TeamMembers.Count,
            FeedbackGiven = feedback.Count(f => f.FromMemberId == AuthService.Instance.CurrentTeamMember?.Id),
            FeedbackReceived = feedback.Count(f => f.TeamMemberId == AuthService.Instance.CurrentTeamMember?.Id),
            
            GoalProgressOverTime = CalculateGoalProgressOverTime(goals, startDate, endDate),
            TaskCompletionOverTime = CalculateTaskCompletionOverTime(tasks, startDate, endDate),
            MeetingMinutesOverTime = CalculateMeetingMinutesOverTime(meetings, startDate, endDate)
        };
    }

    /// <summary>
    /// Generates goals report data for the specified date range.
    /// </summary>
    public async Task<GoalsReportData> GetGoalsReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetGoalsReportAsync: {startDate:d} - {endDate:d}");

        var allGoals = await GoalsService.Instance.GetMyGoalsAsync();
        var goals = FilterGoalsByDateRange(allGoals, startDate, endDate);

        // Health counts
        var onTrack = goals.Count(g => g.Health == GoalHealth.OnTrack);
        var atRisk = goals.Count(g => g.Health == GoalHealth.AtRisk);
        var needsAttention = goals.Count(g => g.Health == GoalHealth.NeedsAttention);
        var completed = goals.Count(g => g.Status == "completed");
        var offTrack = goals.Count(g => g.Health == GoalHealth.ReframingNeeded);

        // Average progress
        var avgProgress = goals.Count > 0 ? goals.Average(g => g.ProgressPercent) : 0;

        // Health distribution for pie chart
        var healthDistribution = new List<GoalHealthCount>
        {
            new("On Track", onTrack, "#22C55E"),
            new("At Risk", atRisk, "#EF4444"),
            new("Needs Attention", needsAttention, "#F59E0B"),
            new("Completed", completed, "#3B82F6"),
            new("Off Track", offTrack, "#8B5CF6")
        };

        // Type distribution
        var typeDistribution = goals
            .GroupBy(g => g.GoalTypeValue ?? "Other")
            .Select(g => new GoalTypeCount(g.Key, g.Count()))
            .ToList();

        // Goal summaries for table
        var goalSummaries = goals
            .OrderByDescending(g => g.DueDate)
            .Take(50)
            .Select(g => new GoalSummaryItem(
                g.Id,
                g.Title,
                g.OwnerName ?? "Unknown",
                g.ProgressPercent,
                g.Health.ToString(),
                GetHealthColor(g.Health),
                g.DueDate
            ))
            .ToList();

        // Calculate trajectory probability average
        double avgProbability = 0;
        var trajectoryCount = 0;
        foreach (var goal in goals.Where(g => g.DueDate.HasValue))
        {
            var trajectory = await GoalsService.Instance.GetGoalTrajectoryAsync(goal.Id);
            if (trajectory != null && trajectory.Status != TrajectoryStatus.Unknown)
            {
                avgProbability += trajectory.CompletionProbability;
                trajectoryCount++;
            }
        }
        if (trajectoryCount > 0)
            avgProbability /= trajectoryCount;

        return new GoalsReportData
        {
            TotalGoals = goals.Count,
            OnTrack = onTrack,
            AtRisk = atRisk,
            NeedsAttention = needsAttention,
            Completed = completed,
            OffTrack = offTrack,
            AverageProgress = avgProgress,
            AverageCompletionProbability = avgProbability * 100,
            ProgressOverTime = CalculateGoalProgressOverTime(goals, startDate, endDate),
            HealthDistribution = healthDistribution,
            TypeDistribution = typeDistribution,
            Goals = goalSummaries
        };
    }

    /// <summary>
    /// Generates metrics report data for the specified date range.
    /// </summary>
    public async Task<MetricsReportData> GetMetricsReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetMetricsReportAsync: {startDate:d} - {endDate:d}");

        var metrics = await MetricsService.Instance.GetAllMetricsAsync();

        var improving = 0;
        var stable = 0;
        var declining = 0;
        var unknown = 0;

        var metricTrends = new List<MetricTrendSeries>();
        var metricSummaries = new List<MetricSummaryItem>();

        foreach (var metric in metrics)
        {
            var history = await MetricsService.Instance.GetHistoryAsync(metric.Id);
            var trendResult = await MetricsService.Instance.GetTrendAnalysisAsync(metric.Id);

            // Count by trend direction
            var trend = trendResult?.Direction ?? MetricTrend.Unknown;
            switch (trend)
            {
                case MetricTrend.TrendingUp: improving++; break;
                case MetricTrend.Stable: stable++; break;
                case MetricTrend.TrendingDown: declining++; break;
                default: unknown++; break;
            }

            // Build trend series for chart
            var dataPoints = history
                .Where(h => h.RecordedAt >= startDate && h.RecordedAt <= endDate)
                .OrderBy(h => h.RecordedAt)
                .Select(h => new DateValuePoint(h.RecordedAt, (double)h.Value))
                .ToList();

            if (dataPoints.Any())
            {
                metricTrends.Add(new MetricTrendSeries
                {
                    MetricId = metric.Id,
                    MetricName = metric.Name,
                    Unit = metric.Unit ?? "",
                    Trend = trend,
                    TrendArrow = trend.GetArrow(),
                    DataPoints = dataPoints
                });
            }

            // Build summary item
            double? changePercent = null;
            if (dataPoints.Count >= 2)
            {
                var first = dataPoints.First().Value;
                var last = dataPoints.Last().Value;
                if (first != 0)
                    changePercent = ((last - first) / first) * 100;
            }

            metricSummaries.Add(new MetricSummaryItem(
                metric.Id,
                metric.Name,
                (double)(metric.CurrentValue ?? 0),
                (double?)metric.TargetValue,
                metric.Unit ?? "",
                trend,
                trend.GetArrow(),
                changePercent
            ));
        }

        return new MetricsReportData
        {
            TotalMetrics = metrics.Count,
            Improving = improving,
            Stable = stable,
            Declining = declining,
            Unknown = unknown,
            MetricTrends = metricTrends,
            Metrics = metricSummaries.OrderBy(m => m.Name).ToList()
        };
    }

    /// <summary>
    /// Generates tasks report data for the specified date range.
    /// </summary>
    public async Task<TasksReportData> GetTasksReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetTasksReportAsync: {startDate:d} - {endDate:d}");

        var allTasks = await TaskService.Instance.GetTasksAsync(includeCompleted: true);
        var tasks = FilterTasksByDateRange(allTasks, startDate, endDate);

        // Status counts
        var completed = tasks.Count(t => t.Status == "completed");
        var inProgress = tasks.Count(t => t.Status == "in_progress");
        var notStarted = tasks.Count(t => t.Status == "not_started" || t.Status == "pending");
        var overdue = tasks.Count(t => t.IsOverdue);
        var blocked = tasks.Count(t => t.Status == "blocked");

        // Tasks created and completed in period
        var createdInPeriod = allTasks.Count(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);
        var completedInPeriod = allTasks.Count(t => t.CompletedAt.HasValue && 
            t.CompletedAt.Value >= startDate && t.CompletedAt.Value <= endDate);

        // Average completion time
        var completedTasks = allTasks
            .Where(t => t.CompletedAt.HasValue && t.CreatedAt != default)
            .ToList();
        var avgCompletionDays = completedTasks.Any()
            ? completedTasks.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays)
            : 0;

        // Status distribution for pie chart
        var statusDistribution = new List<TaskStatusCount>
        {
            new("Completed", completed, "#22C55E"),
            new("In Progress", inProgress, "#3B82F6"),
            new("Not Started", notStarted, "#9CA3AF"),
            new("Overdue", overdue, "#EF4444"),
            new("Blocked", blocked, "#F59E0B")
        };

        // Recent tasks for table
        var recentTasks = tasks
            .OrderByDescending(t => t.UpdatedAt)
            .Take(50)
            .Select(t => new TaskSummaryItem(
                t.Id,
                t.Title,
                t.AssignedToName ?? "Unassigned",
                t.Status,
                t.DueDate,
                t.CompletedAt
            ))
            .ToList();

        return new TasksReportData
        {
            TotalTasks = tasks.Count,
            Completed = completed,
            InProgress = inProgress,
            NotStarted = notStarted,
            Overdue = overdue,
            Blocked = blocked,
            AverageCompletionDays = avgCompletionDays,
            TasksCreatedInPeriod = createdInPeriod,
            TasksCompletedInPeriod = completedInPeriod,
            CompletionOverTime = CalculateTaskCompletionOverTime(allTasks, startDate, endDate),
            StatusDistribution = statusDistribution,
            RecentTasks = recentTasks
        };
    }

    /// <summary>
    /// Generates meetings report data for the specified date range.
    /// </summary>
    public async Task<MeetingsReportData> GetMeetingsReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetMeetingsReportAsync: {startDate:d} - {endDate:d}");

        var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
        var meetings = FilterMeetingsByDateRange(dashboard.Meetings, startDate, endDate);

        // Type counts
        var oneOnOnes = meetings.Count(m => m.MeetingType == "one_on_one");
        var teamMeetings = meetings.Count(m => m.MeetingType == "team");
        var otherMeetings = meetings.Count(m => m.MeetingType != "one_on_one" && m.MeetingType != "team");

        // Time analysis
        var totalMinutes = meetings.Sum(m => m.DurationMinutes ?? 0);
        var avgDuration = meetings.Any() ? (int)meetings.Average(m => m.DurationMinutes ?? 0) : 0;

        // Action items (from agenda items)
        var totalActionItems = meetings.Sum(m => m.AgendaItems?.Count ?? 0);
        var completedActionItems = meetings.Sum(m => 
            m.AgendaItems?.Count(a => a.IsCompleted) ?? 0);

        // Type distribution for pie chart
        var typeDistribution = new List<MeetingTypeCount>
        {
            new("1:1 Meetings", oneOnOnes, "#3B82F6"),
            new("Team Meetings", teamMeetings, "#22C55E"),
            new("Other", otherMeetings, "#9CA3AF")
        };

        // Meeting summaries for table
        var meetingSummaries = meetings
            .Where(m => m.ScheduledAt.HasValue)
            .OrderByDescending(m => m.ScheduledAt!.Value)
            .Take(50)
            .Select(m => new MeetingSummaryItem(
                m.Id,
                m.Title,
                m.MeetingType ?? "other",
                m.ScheduledAt!.Value,
                m.DurationMinutes ?? 0,
                m.Attendees?.Count ?? 0,
                m.AgendaItems?.Count ?? 0
            ))
            .ToList();

        return new MeetingsReportData
        {
            TotalMeetings = meetings.Count,
            OneOnOnes = oneOnOnes,
            TeamMeetings = teamMeetings,
            OtherMeetings = otherMeetings,
            TotalMinutes = totalMinutes,
            AverageDurationMinutes = avgDuration,
            TotalActionItems = totalActionItems,
            CompletedActionItems = completedActionItems,
            MeetingMinutesOverTime = CalculateMeetingMinutesOverTime(meetings, startDate, endDate),
            TypeDistribution = typeDistribution,
            MeetingCountOverTime = CalculateMeetingCountOverTime(meetings, startDate, endDate),
            Meetings = meetingSummaries
        };
    }

    /// <summary>
    /// Generates team report data for the specified date range.
    /// </summary>
    public async Task<TeamReportData> GetTeamReportAsync(DateTime startDate, DateTime endDate)
    {
        Log($"GetTeamReportAsync: {startDate:d} - {endDate:d}");

        var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
        var feedback = FilterFeedbackByDateRange(dashboard.Feedback, startDate, endDate);

        var currentUserId = AuthService.Instance.CurrentTeamMember?.Id;

        // Feedback counts - using correct property names
        var feedbackGiven = feedback.Count(f => f.FromMemberId == currentUserId);
        var feedbackReceived = feedback.Count(f => f.TeamMemberId == currentUserId);
        var positiveFeedback = feedback.Count(f => f.FeedbackType == "positive" || f.FeedbackType == "praise");
        var constructiveFeedback = feedback.Count(f => f.FeedbackType == "constructive" || f.FeedbackType == "coaching");
        var recognitionFeedback = feedback.Count(f => f.FeedbackType == "praise");

        // Feedback distribution
        var feedbackDistribution = new List<FeedbackTypeCount>
        {
            new("Positive", feedback.Count(f => f.FeedbackType == "positive" || f.FeedbackType == "praise"), "#22C55E"),
            new("Constructive", constructiveFeedback, "#F59E0B"),
            new("Recognition", recognitionFeedback, "#3B82F6")
        };

        // Team member activity
        var memberActivity = new List<TeamMemberActivityItem>();
        foreach (var member in dashboard.TeamMembers)
        {
            var goalsOwned = dashboard.Goals.Count(g => g.OwnerTeamMemberId == member.Id);
            var tasksAssigned = dashboard.Tasks.Count(t => t.OwnerTeamMemberId == member.Id);
            var tasksCompleted = dashboard.Tasks.Count(t => 
                t.OwnerTeamMemberId == member.Id && t.Status == "completed");
            var meetingsAttended = dashboard.Meetings.Count(m => 
                m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true);
            var memberFeedbackGiven = feedback.Count(f => f.FromMemberId == member.Id);
            var memberFeedbackReceived = feedback.Count(f => f.TeamMemberId == member.Id);

            memberActivity.Add(new TeamMemberActivityItem(
                member.Id,
                member.FullName,
                member.UserAvatarUrl ?? "",
                goalsOwned,
                tasksAssigned,
                tasksCompleted,
                meetingsAttended,
                memberFeedbackGiven,
                memberFeedbackReceived
            ));
        }

        return new TeamReportData
        {
            TotalMembers = dashboard.TeamMembers.Count,
            ActiveMembers = memberActivity.Count(m => 
                m.TasksAssigned > 0 || m.GoalsOwned > 0 || m.MeetingsAttended > 0),
            TotalFeedbackGiven = feedbackGiven,
            TotalFeedbackReceived = feedbackReceived,
            PositiveFeedback = positiveFeedback,
            ConstructiveFeedback = constructiveFeedback,
            RecognitionFeedback = recognitionFeedback,
            FeedbackDistribution = feedbackDistribution,
            FeedbackOverTime = CalculateFeedbackOverTime(feedback, startDate, endDate),
            MemberActivity = memberActivity.OrderByDescending(m => m.TasksCompleted).ToList()
        };
    }

    #region Private Helpers

    private static List<GoalDetail> FilterGoalsByDateRange(List<GoalDetail> goals, DateTime startDate, DateTime endDate)
    {
        return goals.Where(g =>
            (g.StartDate == null || g.StartDate <= endDate) &&
            (g.DueDate == null || g.DueDate >= startDate) ||
            (g.CreatedAt >= startDate && g.CreatedAt <= endDate)
        ).ToList();
    }

    private static List<TaskDetail> FilterTasksByDateRange(List<TaskDetail> tasks, DateTime startDate, DateTime endDate)
    {
        return tasks.Where(t =>
            (t.CreatedAt >= startDate && t.CreatedAt <= endDate) ||
            (t.DueDate.HasValue && t.DueDate >= startDate && t.DueDate <= endDate) ||
            (t.CompletedAt.HasValue && t.CompletedAt >= startDate && t.CompletedAt <= endDate)
        ).ToList();
    }

    private static List<MeetingDetail> FilterMeetingsByDateRange(List<MeetingDetail> meetings, DateTime startDate, DateTime endDate)
    {
        return meetings.Where(m =>
            m.ScheduledAt.HasValue && m.ScheduledAt.Value >= startDate && m.ScheduledAt.Value <= endDate
        ).ToList();
    }

    private static List<FeedbackDetail> FilterFeedbackByDateRange(List<FeedbackDetail> feedback, DateTime startDate, DateTime endDate)
    {
        return feedback.Where(f =>
            f.CreatedAt >= startDate && f.CreatedAt <= endDate
        ).ToList();
    }

    private static List<DateValuePoint> CalculateGoalProgressOverTime(List<GoalDetail> goals, DateTime startDate, DateTime endDate)
    {
        var points = new List<DateValuePoint>();
        var days = (endDate - startDate).Days;
        var interval = Math.Max(1, days / 10); // ~10 data points

        for (var date = startDate; date <= endDate; date = date.AddDays(interval))
        {
            // Estimate progress at this date (simplified - use average progress)
            var avgProgress = goals.Count > 0 ? goals.Average(g => g.ProgressPercent) : 0;
            points.Add(new DateValuePoint(date, avgProgress));
        }

        return points;
    }

    private static List<DateValuePoint> CalculateTaskCompletionOverTime(List<TaskDetail> tasks, DateTime startDate, DateTime endDate)
    {
        var points = new List<DateValuePoint>();
        var days = (endDate - startDate).Days;
        var interval = Math.Max(1, days / 10);

        for (var date = startDate; date <= endDate; date = date.AddDays(interval))
        {
            var completedByDate = tasks.Count(t => 
                t.CompletedAt.HasValue && t.CompletedAt.Value <= date);
            points.Add(new DateValuePoint(date, completedByDate));
        }

        return points;
    }

    private static List<DateValuePoint> CalculateMeetingMinutesOverTime(List<MeetingDetail> meetings, DateTime startDate, DateTime endDate)
    {
        var points = new List<DateValuePoint>();
        
        // Filter to only meetings with scheduled date and group by week
        var meetingsWithDate = meetings.Where(m => m.ScheduledAt.HasValue);
        var grouped = meetingsWithDate
            .GroupBy(m => new DateTime(m.ScheduledAt!.Value.Year, m.ScheduledAt.Value.Month, 
                m.ScheduledAt.Value.Day).AddDays(-(int)m.ScheduledAt.Value.DayOfWeek))
            .OrderBy(g => g.Key);

        foreach (var week in grouped)
        {
            var totalMinutes = week.Sum(m => m.DurationMinutes ?? 0);
            points.Add(new DateValuePoint(week.Key, totalMinutes));
        }

        return points;
    }

    private static List<DateValuePoint> CalculateMeetingCountOverTime(List<MeetingDetail> meetings, DateTime startDate, DateTime endDate)
    {
        var points = new List<DateValuePoint>();
        
        // Filter to only meetings with scheduled date and group by week
        var meetingsWithDate = meetings.Where(m => m.ScheduledAt.HasValue);
        var grouped = meetingsWithDate
            .GroupBy(m => new DateTime(m.ScheduledAt!.Value.Year, m.ScheduledAt.Value.Month,
                m.ScheduledAt.Value.Day).AddDays(-(int)m.ScheduledAt.Value.DayOfWeek))
            .OrderBy(g => g.Key);

        foreach (var week in grouped)
        {
            points.Add(new DateValuePoint(week.Key, week.Count()));
        }

        return points;
    }

    private static List<DateValuePoint> CalculateFeedbackOverTime(List<FeedbackDetail> feedback, DateTime startDate, DateTime endDate)
    {
        var points = new List<DateValuePoint>();
        
        // Group by week
        var grouped = feedback
            .GroupBy(f => new DateTime(f.CreatedAt.Year, f.CreatedAt.Month,
                f.CreatedAt.Day).AddDays(-(int)f.CreatedAt.DayOfWeek))
            .OrderBy(g => g.Key);

        foreach (var week in grouped)
        {
            points.Add(new DateValuePoint(week.Key, week.Count()));
        }

        return points;
    }

    private static string GetHealthColor(GoalHealth health) => health switch
    {
        GoalHealth.OnTrack => "#22C55E",
        GoalHealth.AtRisk => "#EF4444",
        GoalHealth.NeedsAttention => "#F59E0B",
        GoalHealth.ReframingNeeded => "#8B5CF6",
        _ => "#9CA3AF"
    };

    #endregion
}
