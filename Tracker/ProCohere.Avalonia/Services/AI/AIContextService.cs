using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Service that gathers contextual information about the user's data
/// for AI conversations. Uses focused retrieval instead of dumping everything.
/// </summary>
public class AIContextService
{
    #region Singleton

    private static readonly Lazy<AIContextService> _instance =
        new(() => new AIContextService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static AIContextService Instance => _instance.Value;

    #endregion

    private readonly DashboardService _dashboardService;
    private readonly ProjectService _projectService;
    private readonly TaskService _taskService;
    private readonly GoalsService _goalsService;
    
    // Cache dashboard data to avoid repeated fetches in same session
    private DashboardData? _cachedDashboard;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private AIContextService()
    {
        _dashboardService = DashboardService.Instance;
        _projectService = ProjectService.Instance;
        _taskService = TaskService.Instance;
        _goalsService = GoalsService.Instance;
    }

    /// <summary>
    /// Gets MINIMAL base context - just user info and current date.
    /// Used as system prompt foundation. ~200 tokens.
    /// </summary>
    public string GetBaseContext()
    {
        var context = new StringBuilder();
        var now = DateTime.Now;
        
        var currentUser = AuthService.Instance.CurrentTeamMember;
        if (currentUser != null)
        {
            context.AppendLine($"User: {currentUser.FirstName} {currentUser.LastName}");
            if (!string.IsNullOrEmpty(currentUser.JobTitle))
                context.AppendLine($"Role: {currentUser.JobTitle}");
        }
        context.AppendLine($"Date: {now:dddd, MMMM dd, yyyy}");
        context.AppendLine($"Time: {now:h:mm tt}");
        
        return context.ToString();
    }

    /// <summary>
    /// Gets focused context based on query intent detection.
    /// Only retrieves data relevant to the user's question. ~500-1500 tokens.
    /// </summary>
    public async Task<string> GetFocusedContextAsync(string userQuery)
    {
        var context = new StringBuilder();
        context.AppendLine(GetBaseContext());
        context.AppendLine();
        
        var queryLower = userQuery.ToLowerInvariant();
        
        // Detect intent and only load relevant data
        var needsTeamMembers = DetectNeedsTeamContext(queryLower);
        var needsMeetings = DetectNeedsMeetingContext(queryLower);
        var needsTasks = DetectNeedsTaskContext(queryLower);
        var needsGoals = DetectNeedsGoalContext(queryLower);
        var needsMetrics = DetectNeedsMetricContext(queryLower);
        var needsFeedback = DetectNeedsFeedbackContext(queryLower);
        var needsInsights = DetectNeedsInsightContext(queryLower);
        var needsProjects = DetectNeedsProjectContext(queryLower);
        
        // Load cached dashboard data if needed
        DashboardData? dashboard = null;
        if (needsTeamMembers || needsMeetings || needsFeedback)
        {
            dashboard = await GetCachedDashboardAsync();
        }
        
        var now = DateTime.Now;
        
        // Team Members (only if asking about people, 1:1s, team)
        if (needsTeamMembers && dashboard?.TeamMembers?.Any() == true)
        {
            context.AppendLine($"=== TEAM ({dashboard.TeamMembers.Count}) ===");
            
            // Calculate 1:1 status if asking about meetings
            if (needsMeetings)
            {
                var oneOnOneMeetings = dashboard.Meetings?
                    .Where(m => m.MeetingType == "one_on_one" && m.ScheduledAt.HasValue)
                    .ToList() ?? new List<MeetingDetail>();
                
                foreach (var member in dashboard.TeamMembers.OrderBy(m => m.FullName))
                {
                    var lastMeeting = oneOnOneMeetings
                        .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true)
                        .OrderByDescending(m => m.ScheduledAt)
                        .FirstOrDefault();
                    
                    var daysSince = lastMeeting?.ScheduledAt.HasValue == true
                        ? (int)(now - lastMeeting.ScheduledAt.Value).TotalDays
                        : -1;
                    
                    var status = daysSince switch
                    {
                        -1 => "⚠️ NEVER MET",
                        > 14 => $"🔴 OVERDUE ({daysSince}d)",
                        > 7 => $"🟡 DUE SOON ({daysSince}d)",
                        _ => $"✅ Recent ({daysSince}d)"
                    };
                    
                    context.AppendLine($"• {member.FullName}: {status}");
                }
            }
            else
            {
                // Just list team members
                foreach (var member in dashboard.TeamMembers.Take(10))
                {
                    context.AppendLine($"• {member.FullName} ({member.JobTitle ?? "No title"})");
                }
            }
            context.AppendLine();
        }
        
        // Meetings (only if asking about meetings, schedule, 1:1)
        if (needsMeetings && dashboard?.Meetings?.Any() == true)
        {
            var upcoming = dashboard.Meetings
                .Where(m => m.ScheduledAt >= now)
                .OrderBy(m => m.ScheduledAt)
                .Take(5)
                .ToList();
            
            var recent = dashboard.Meetings
                .Where(m => m.ScheduledAt < now && m.ScheduledAt >= now.AddDays(-14))
                .OrderByDescending(m => m.ScheduledAt)
                .Take(5)
                .ToList();
            
            if (upcoming.Any())
            {
                context.AppendLine($"=== UPCOMING MEETINGS ===");
                foreach (var m in upcoming)
                {
                    var attendees = m.Attendees?.Any() == true
                        ? $" with {string.Join(", ", m.Attendees.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)).Take(2))}"
                        : "";
                    context.AppendLine($"• {m.ScheduledAt:MMM dd h:mm tt} - {m.Title}{attendees}");
                }
                context.AppendLine();
            }
            
            if (recent.Any())
            {
                context.AppendLine($"=== RECENT MEETINGS ===");
                foreach (var m in recent)
                {
                    var attendees = m.Attendees?.Any() == true
                        ? $" with {string.Join(", ", m.Attendees.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)).Take(2))}"
                        : "";
                    context.AppendLine($"• {m.ScheduledAt:MMM dd} - {m.Title}{attendees}");
                }
                context.AppendLine();
            }
        }
        
        // Tasks (only if asking about tasks, todos, work)
        if (needsTasks)
        {
            var tasks = await _taskService.GetTasksAsync(includeCompleted: false);
            if (tasks?.Any() == true)
            {
                var overdue = tasks.Where(t => t.DueDate < now.Date).Take(3).ToList();
                var dueSoon = tasks.Where(t => t.DueDate >= now.Date && t.DueDate <= now.Date.AddDays(7)).Take(3).ToList();
                
                context.AppendLine($"=== TASKS ({tasks.Count} open) ===");
                if (overdue.Any())
                {
                    context.AppendLine("🔴 Overdue:");
                    foreach (var t in overdue)
                        context.AppendLine($"  • {t.Title} (due {t.DueDate:MMM dd})");
                }
                if (dueSoon.Any())
                {
                    context.AppendLine("🟡 This week:");
                    foreach (var t in dueSoon)
                        context.AppendLine($"  • {t.Title} (due {t.DueDate:MMM dd})");
                }
                context.AppendLine();
            }
        }
        
        // Goals (only if asking about goals, OKRs, objectives)
        if (needsGoals)
        {
            var goals = await _goalsService.GetMyGoalsAsync();
            var active = goals?.Where(g => g.Lifecycle == GoalLifecycle.Active).Take(5).ToList();
            if (active?.Any() == true)
            {
                context.AppendLine($"=== ACTIVE GOALS ===");
                foreach (var g in active)
                {
                    var progress = g.ProgressPercent > 0 ? $" ({g.ProgressPercent}%)" : "";
                    context.AppendLine($"• {g.Title}{progress}");
                }
                context.AppendLine();
            }
        }
        
        // Metrics (only if asking about metrics, KPIs, numbers)
        if (needsMetrics)
        {
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            if (metrics?.Any() == true)
            {
                context.AppendLine($"=== METRICS ({metrics.Count}) ===");
                foreach (var m in metrics.Take(5))
                {
                    var current = m.CurrentValue?.ToString("N1") ?? "—";
                    var target = m.TargetValue?.ToString("N1") ?? "—";
                    context.AppendLine($"• {m.Name}: {current}{m.Unit} / {target}{m.Unit}");
                }
                context.AppendLine();
            }
        }
        
        // Projects (only if asking about projects)
        if (needsProjects)
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var active = projects?.Where(p => p.Status == ProjectStatus.Active).Take(5).ToList();
            if (active?.Any() == true)
            {
                context.AppendLine($"=== ACTIVE PROJECTS ===");
                foreach (var p in active)
                    context.AppendLine($"• {p.Name}");
                context.AppendLine();
            }
        }
        
        return context.ToString();
    }

    /// <summary>
    /// Gets COMPREHENSIVE user context for AI conversations.
    /// WARNING: This is expensive (~3000+ tokens). Use GetFocusedContextAsync instead.
    /// Only use when user explicitly asks for "everything" or comprehensive overview.
    /// </summary>
    public async Task<string> GetCurrentContextAsync()
    {
        var context = new StringBuilder();
        var now = DateTime.Now;
        
        // Current user info
        var currentUser = AuthService.Instance.CurrentTeamMember;
        if (currentUser != null)
        {
            context.AppendLine($"=== CURRENT USER ===");
            context.AppendLine($"Name: {currentUser.FirstName} {currentUser.LastName}");
            if (!string.IsNullOrEmpty(currentUser.JobTitle))
                context.AppendLine($"Role: {currentUser.JobTitle}");
            context.AppendLine($"Current Date: {now:dddd, MMMM dd, yyyy}");
            context.AppendLine($"Current Time: {now:h:mm tt}");
            context.AppendLine();
        }

        // Load ALL dashboard data
        var dashboard = await _dashboardService.LoadDashboardDataAsync();
        
        // === TEAM MEMBERS WITH MEETING STATUS ===
        if (dashboard.TeamMembers?.Any() == true)
        {
            context.AppendLine($"=== YOUR TEAM ({dashboard.TeamMembers.Count} members) ===");
            
            // Calculate last meeting date for each team member
            var oneOnOneMeetings = dashboard.Meetings?
                .Where(m => m.MeetingType == "one_on_one" && m.ScheduledAt.HasValue)
                .ToList() ?? new List<MeetingDetail>();
            
            foreach (var member in dashboard.TeamMembers.OrderBy(m => m.FullName))
            {
                // Find most recent 1:1 with this team member
                var lastMeeting = oneOnOneMeetings
                    .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true)
                    .OrderByDescending(m => m.ScheduledAt)
                    .FirstOrDefault();
                
                var lastMeetingDate = lastMeeting?.ScheduledAt;
                var daysSinceMeeting = lastMeetingDate.HasValue 
                    ? (int)(now - lastMeetingDate.Value).TotalDays 
                    : -1;
                
                // Determine status
                string meetingStatus;
                if (!lastMeetingDate.HasValue)
                {
                    meetingStatus = "⚠️ NEVER MET";
                }
                else if (daysSinceMeeting > 14)
                {
                    meetingStatus = $"🔴 OVERDUE ({daysSinceMeeting} days since last 1:1 on {lastMeetingDate:MMM dd})";
                }
                else if (daysSinceMeeting > 7)
                {
                    meetingStatus = $"🟡 DUE SOON ({daysSinceMeeting} days since last 1:1 on {lastMeetingDate:MMM dd})";
                }
                else
                {
                    meetingStatus = $"✅ Recent (last 1:1 on {lastMeetingDate:MMM dd}, {daysSinceMeeting} days ago)";
                }
                
                // Find next scheduled 1:1
                var nextMeeting = oneOnOneMeetings
                    .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true && m.ScheduledAt >= now)
                    .OrderBy(m => m.ScheduledAt)
                    .FirstOrDefault();
                
                var nextMeetingInfo = nextMeeting != null 
                    ? $" | Next: {nextMeeting.ScheduledAt:MMM dd}" 
                    : "";
                
                context.AppendLine($"  • {member.FullName} ({member.JobTitle ?? "No title"})");
                context.AppendLine($"    1:1 Status: {meetingStatus}{nextMeetingInfo}");
            }
            context.AppendLine();
            
            // Summary of overdue
            var overdueCount = dashboard.TeamMembers.Count(member =>
            {
                var lastMeeting = oneOnOneMeetings
                    .Where(m => m.Attendees?.Any(a => a.TeamMemberId == member.Id) == true)
                    .OrderByDescending(m => m.ScheduledAt)
                    .FirstOrDefault();
                if (lastMeeting?.ScheduledAt == null) return true; // Never met
                return (now - lastMeeting.ScheduledAt.Value).TotalDays > 14;
            });
            
            if (overdueCount > 0)
            {
                context.AppendLine($"⚠️ ATTENTION: {overdueCount} team member(s) are overdue for a 1:1 (>14 days)");
                context.AppendLine();
            }
        }
        
        // === ALL MEETINGS ===
        if (dashboard.Meetings?.Any() == true)
        {
            // Upcoming meetings
            var upcomingMeetings = dashboard.Meetings
                .Where(m => m.ScheduledAt >= now)
                .OrderBy(m => m.ScheduledAt)
                .Take(10)
                .ToList();
            
            if (upcomingMeetings.Any())
            {
                context.AppendLine($"=== UPCOMING MEETINGS ({upcomingMeetings.Count}) ===");
                foreach (var meeting in upcomingMeetings)
                {
                    var attendees = meeting.Attendees?.Any() == true 
                        ? $" with {string.Join(", ", meeting.Attendees.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)))}"
                        : "";
                    context.AppendLine($"  • {meeting.ScheduledAt:MMM dd h:mm tt} - {meeting.Title}{attendees}");
                }
                context.AppendLine();
            }
            
            // Recent meetings (last 30 days)
            var recentMeetings = dashboard.Meetings
                .Where(m => m.ScheduledAt < now && m.ScheduledAt >= now.AddDays(-30))
                .OrderByDescending(m => m.ScheduledAt)
                .Take(15)
                .ToList();
            
            if (recentMeetings.Any())
            {
                context.AppendLine($"=== RECENT MEETINGS (last 30 days) ===");
                foreach (var meeting in recentMeetings)
                {
                    var attendees = meeting.Attendees?.Any() == true 
                        ? $" with {string.Join(", ", meeting.Attendees.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)))}"
                        : "";
                    context.AppendLine($"  • {meeting.ScheduledAt:MMM dd} - {meeting.Title}{attendees}");
                }
                context.AppendLine();
            }
        }

        // === TASKS ===
        var tasks = await _taskService.GetTasksAsync(includeCompleted: false);
        if (tasks?.Any() == true)
        {
            var overdueTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate < now.Date).ToList();
            var dueSoonTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate >= now.Date && t.DueDate <= now.Date.AddDays(7)).ToList();
            
            context.AppendLine($"=== TASKS ({tasks.Count} open) ===");
            
            if (overdueTasks.Any())
            {
                context.AppendLine($"🔴 OVERDUE ({overdueTasks.Count}):");
                foreach (var task in overdueTasks.Take(5))
                {
                    context.AppendLine($"  • {task.Title} (due {task.DueDate:MMM dd})");
                }
            }
            
            if (dueSoonTasks.Any())
            {
                context.AppendLine($"🟡 Due this week ({dueSoonTasks.Count}):");
                foreach (var task in dueSoonTasks.Take(5))
                {
                    context.AppendLine($"  • {task.Title} (due {task.DueDate:MMM dd})");
                }
            }
            
            var highPriority = tasks.Where(t => t.Priority == "High" || t.Priority == "Critical").ToList();
            if (highPriority.Any())
            {
                context.AppendLine($"⚡ High Priority ({highPriority.Count}):");
                foreach (var task in highPriority.Take(5))
                {
                    context.AppendLine($"  • {task.Title}");
                }
            }
            context.AppendLine();
        }

        // === GOALS ===
        var goals = await _goalsService.GetMyGoalsAsync();
        if (goals?.Any() == true)
        {
            var activeGoals = goals.Where(g => g.Lifecycle == GoalLifecycle.Active || g.Lifecycle == GoalLifecycle.Evolving).ToList();
            
            if (activeGoals.Any())
            {
                context.AppendLine($"=== ACTIVE GOALS ({activeGoals.Count}) ===");
                foreach (var goal in activeGoals)
                {
                    var progress = goal.ProgressPercent > 0 ? $" ({goal.ProgressPercent}% complete)" : "";
                    var dueInfo = goal.DueDate.HasValue ? $" - Due {goal.DueDate:MMM dd}" : "";
                    context.AppendLine($"  • {goal.Title}{progress}{dueInfo}");
                }
                context.AppendLine();
            }
        }

        // === METRICS ===
        var metrics = await MetricsService.Instance.GetAllMetricsAsync();
        if (metrics?.Any() == true)
        {
            context.AppendLine($"=== METRICS ({metrics.Count}) ===");
            foreach (var metric in metrics.Take(15))
            {
                var currentVal = metric.CurrentValue?.ToString("N1") ?? "—";
                var targetVal = metric.TargetValue?.ToString("N1") ?? "—";
                var unit = metric.Unit ?? "";
                // Calculate on-target status based on direction
                var isOnTarget = (metric.CurrentValue.HasValue && metric.TargetValue.HasValue) &&
                    (metric.TargetDirection?.ToLower() == "decrease" 
                        ? metric.CurrentValue <= metric.TargetValue
                        : metric.CurrentValue >= metric.TargetValue);
                var status = isOnTarget ? "✅" : "⚠️";
                context.AppendLine($"  {status} {metric.Name}: {currentVal}{unit} / {targetVal}{unit} target");
            }
            context.AppendLine();
        }

        // === FEEDBACK ===
        if (dashboard.Feedback?.Any() == true)
        {
            var recentFeedback = dashboard.Feedback
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList();
            
            if (recentFeedback.Any())
            {
                context.AppendLine($"=== RECENT FEEDBACK ({recentFeedback.Count}) ===");
                foreach (var fb in recentFeedback)
                {
                    var fromName = fb.SenderName ?? "Unknown";
                    var toName = fb.RecipientName ?? "Unknown";
                    var type = fb.FeedbackType ?? "General";
                    context.AppendLine($"  • {type}: From {fromName} to {toName} ({fb.CreatedAt:MMM dd})");
                    if (!string.IsNullOrEmpty(fb.Title))
                        context.AppendLine($"    \"{fb.Title}\"");
                }
                context.AppendLine();
            }
        }

        // === PROJECTS ===
        var projects = await _projectService.GetAllProjectsAsync();
        var activeProjects = projects?.Where(p => p.Status == ProjectStatus.Active).ToList();
        if (activeProjects?.Any() == true)
        {
            context.AppendLine($"=== ACTIVE PROJECTS ({activeProjects.Count}) ===");
            foreach (var project in activeProjects.Take(10))
            {
                var dueInfo = project.DueDate.HasValue ? $" (due {project.DueDate:MMM dd})" : "";
                context.AppendLine($"  • {project.Name}{dueInfo}");
            }
            context.AppendLine();
        }

        // === AI INSIGHTS ===
        try
        {
            var currentTeamMemberId = AuthService.Instance.CurrentTeamMember?.Id;
            var insightRepository = new Insights.InsightRepository();
            var insights = currentTeamMemberId.HasValue
                ? await insightRepository.GetActiveInsightsAsync(currentTeamMemberId.Value)
                : new List<Insight>();
            if (insights?.Any() == true)
            {
                var critical = insights.Where(i => i.Severity == InsightSeverity.Critical).ToList();
                var warnings = insights.Where(i => i.Severity == InsightSeverity.High || i.Severity == InsightSeverity.Medium).ToList();
                
                context.AppendLine($"=== AI INSIGHTS ({insights.Count} active) ===");
                if (critical.Any())
                {
                    context.AppendLine($"🔴 CRITICAL ({critical.Count}):");
                    foreach (var insight in critical.Take(5))
                    {
                        context.AppendLine($"  • {insight.Title}");
                    }
                }
                if (warnings.Any())
                {
                    context.AppendLine($"🟡 WARNINGS ({warnings.Count}):");
                    foreach (var insight in warnings.Take(5))
                    {
                        context.AppendLine($"  • {insight.Title}");
                    }
                }
                context.AppendLine();
            }
        }
        catch
        {
            // Insights service may not be available
        }

        return context.ToString();
    }

    /// <summary>
    /// Gets a brief context summary for display in UI.
    /// </summary>
    public async Task<string> GetContextSummaryAsync()
    {
        var currentUser = AuthService.Instance.CurrentTeamMember;
        if (currentUser == null)
            return "Not logged in";

        var projects = await _projectService.GetAllProjectsAsync();
        var activeProjects = projects?.Count(p => p.Status == ProjectStatus.Active) ?? 0;

        var tasks = await _taskService.GetTasksAsync(includeCompleted: false);
        var openTasks = tasks?.Count ?? 0;

        return $"{currentUser.FirstName} • {activeProjects} projects • {openTasks} tasks";
    }
    
    #region Intent Detection
    
    private async Task<DashboardData?> GetCachedDashboardAsync()
    {
        if (_cachedDashboard != null && DateTime.Now < _cacheExpiry)
            return _cachedDashboard;
        
        _cachedDashboard = await _dashboardService.LoadDashboardDataAsync();
        _cacheExpiry = DateTime.Now.Add(CacheDuration);
        return _cachedDashboard;
    }
    
    /// <summary>Invalidates cached data (call after data changes)</summary>
    public void InvalidateCache()
    {
        _cachedDashboard = null;
        _cacheExpiry = DateTime.MinValue;
    }
    
    private static bool DetectNeedsTeamContext(string query)
    {
        var keywords = new[] { "team", "member", "who", "person", "people", "1:1", "one on one", 
            "1-1", "overdue", "meet with", "janet", "sarah", "john", "manager", "report" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsMeetingContext(string query)
    {
        var keywords = new[] { "meeting", "1:1", "one on one", "1-1", "schedule", "met with", 
            "last met", "when", "upcoming", "calendar", "overdue" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsTaskContext(string query)
    {
        var keywords = new[] { "task", "todo", "to-do", "to do", "work", "action", "overdue", 
            "deadline", "due", "priority", "assign" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsGoalContext(string query)
    {
        var keywords = new[] { "goal", "okr", "objective", "key result", "target", "progress" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsMetricContext(string query)
    {
        var keywords = new[] { "metric", "kpi", "measure", "number", "value", "target", "performance" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsFeedbackContext(string query)
    {
        var keywords = new[] { "feedback", "praise", "recognition", "kudos", "review" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsInsightContext(string query)
    {
        var keywords = new[] { "insight", "alert", "attention", "critical", "warning", "issue" };
        return keywords.Any(k => query.Contains(k));
    }
    
    private static bool DetectNeedsProjectContext(string query)
    {
        var keywords = new[] { "project", "initiative", "work on", "working on" };
        return keywords.Any(k => query.Contains(k));
    }
    
    #endregion
}
