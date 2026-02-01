using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for generating Pulse signals from goals, metrics, meetings, and tasks.
/// 
/// Per spec:
/// - Pulse has no tables of its own
/// - All signals are derived from existing data
/// - Time windows are role-aware: IC=7d, Manager=14d, MoM=30d
/// - Attention Required: max 5 items
/// </summary>
public class PulseSignalService
{
    #region Singleton
    
    private static readonly Lazy<PulseSignalService> _instance =
        new(() => new PulseSignalService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    public static PulseSignalService Instance => _instance.Value;
    
    private PulseSignalService() { }
    
    #endregion
    
    #region Logging
    
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "pulse_signal_service.log");
    
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
    
    #region Goal Derived Health Calculation
    
    /// <summary>
    /// Computes derived health for multiple goals using the batch RPC.
    /// Replaces per-goal N+1 queries with single database call.
    /// </summary>
    private async Task ComputeGoalDerivedHealthBatchAsync(List<GoalDetail> goals, CancellationToken ct = default)
    {
        if (goals.Count == 0) return;
        
        try
        {
            var goalIds = goals.Select(g => g.Id).ToList();
            Log($"Computing derived health for {goalIds.Count} goals using batch RPC");
            
            var healthResults = await GoalsService.Instance.GetGoalHealthBatchAsync(goalIds, ct);
            var healthLookup = healthResults.ToDictionary(r => r.GoalId);
            
            foreach (var goal in goals)
            {
                if (healthLookup.TryGetValue(goal.Id, out var result))
                {
                    goal.LinkedMetricsCount = result.LinkedMetricsCount;
                    goal.DerivedHealth = result.DerivedHealth;
                }
                else
                {
                    goal.LinkedMetricsCount = 0;
                    goal.DerivedHealth = GoalDerivedHealth.Unknown;
                }
            }
            
            Log($"Batch health computed: {healthResults.Count} results");
        }
        catch (Exception ex)
        {
            Log($"Error computing batch health: {ex.Message}");
            // Fallback: set all to Unknown
            foreach (var goal in goals)
            {
                goal.LinkedMetricsCount = 0;
                goal.DerivedHealth = GoalDerivedHealth.Unknown;
            }
        }
    }
    
    /// <summary>
    /// Computes the derived health for a goal based on its linked metrics.
    /// This is the same logic used in CircleViewModel.
    /// </summary>
    [Obsolete("Use ComputeGoalDerivedHealthBatchAsync for better performance")]
    private async Task ComputeGoalDerivedHealthAsync(GoalDetail goal)
    {
        try
        {
            var metrics = await GoalsService.Instance.GetAssociatedMetricsAsync(goal.Id);
            goal.LinkedMetricsCount = metrics.Count;
            
            if (metrics.Count == 0)
            {
                goal.DerivedHealth = GoalDerivedHealth.Unknown;
                return;
            }
            
            // Calculate trend for each metric if not already set
            foreach (var metric in metrics)
            {
                if (metric.Trend == MetricTrend.Unknown)
                {
                    metric.Trend = await MetricsService.Instance.CalculateTrendAsync(metric.Id);
                }
            }
            
            // Apply worst-state logic
            bool hasOffTrack = false;
            bool hasAtRisk = false;
            
            foreach (var metric in metrics)
            {
                var signal = GetMetricSignalState(metric);
                if (signal == MetricSignalState.OffTrack)
                {
                    hasOffTrack = true;
                    break;
                }
                else if (signal == MetricSignalState.NeedsAttention)
                {
                    hasAtRisk = true;
                }
            }
            
            // Determine derived health
            if (hasOffTrack)
                goal.DerivedHealth = GoalDerivedHealth.OffTrack;
            else if (hasAtRisk)
                goal.DerivedHealth = GoalDerivedHealth.AtRisk;
            else
                goal.DerivedHealth = GoalDerivedHealth.OnTrack;
        }
        catch (Exception ex)
        {
            Log($"Error computing derived health for goal {goal.Id}: {ex.Message}");
            goal.DerivedHealth = GoalDerivedHealth.Unknown;
        }
    }
    
    /// <summary>
    /// Signal state for a metric (used in derived health calculation).
    /// </summary>
    private enum MetricSignalState { OnTrack, NeedsAttention, OffTrack }
    
    /// <summary>
    /// Gets the signal state for a metric based on trend and direction.
    /// </summary>
    private static MetricSignalState GetMetricSignalState(MetricDetail metric)
    {
        var direction = metric.TargetDirection?.ToLower() ?? "neutral";
        var trend = metric.Trend;
        
        return (direction, trend) switch
        {
            ("higher_is_better", MetricTrend.TrendingUp) => MetricSignalState.OnTrack,
            ("higher_is_better", MetricTrend.Stable) => MetricSignalState.NeedsAttention,
            ("higher_is_better", MetricTrend.TrendingDown) => MetricSignalState.OffTrack,
            
            ("lower_is_better", MetricTrend.TrendingDown) => MetricSignalState.OnTrack,
            ("lower_is_better", MetricTrend.Stable) => MetricSignalState.NeedsAttention,
            ("lower_is_better", MetricTrend.TrendingUp) => MetricSignalState.OffTrack,
            
            _ => MetricSignalState.NeedsAttention
        };
    }
    
    #endregion
    
    /// <summary>
    /// Maximum number of items in Attention Required section.
    /// </summary>
    private const int MaxAttentionItems = 5;
    
    /// <summary>
    /// Days considered "approaching" for deadlines.
    /// </summary>
    private const int DeadlineApproachingDays = 7;
    
    /// <summary>
    /// Days before a metric is considered stale.
    /// </summary>
    private const int StaleMetricDays = 14;
    
    /// <summary>
    /// Gets the time window in days based on user role.
    /// IC: 7 days, Manager: 14 days, Manager-of-managers: 30 days
    /// </summary>
    /// <param name="isManager">Whether the user is a manager.</param>
    /// <param name="isManagerOfManagers">Whether the user is a manager of managers.</param>
    public static int GetTimeWindowDays(bool isManager = false, bool isManagerOfManagers = false)
    {
        if (isManagerOfManagers) return 30;
        if (isManager) return 14;
        return 7;
    }
    
    /// <summary>
    /// Generates all Pulse signals for the current user.
    /// </summary>
    public async Task<PulseData> GenerateAllSignalsAsync(
        Guid userId,
        int timeWindowDays = 7,
        CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-timeWindowDays);
        
        // Generate signals from all sources in parallel
        var attentionTask = GenerateAttentionSignalsAsync(userId, cutoffDate, ct);
        var changeTask = GenerateChangeSignalsAsync(userId, cutoffDate, ct);
        var discussionTask = GenerateDiscussionSignalsAsync(userId, cutoffDate, ct);
        var actionTask = GenerateActionSignalsAsync(userId, cutoffDate, ct);
        
        await Task.WhenAll(attentionTask, changeTask, discussionTask, actionTask);
        
        return new PulseData
        {
            AttentionRequired = attentionTask.Result.Take(MaxAttentionItems).ToList(),
            WhatChanged = changeTask.Result,
            RecentDiscussions = discussionTask.Result,
            ActionsTaken = actionTask.Result,
            GeneratedAt = DateTime.UtcNow,
            TimeWindowDays = timeWindowDays
        };
    }
    
    #region Attention Required Signals
    
    /// <summary>
    /// Generates signals for Attention Required section.
    /// Triggers:
    /// - Metric Off Track
    /// - Metric At Risk + degrading
    /// - Repeated goal degradation
    /// - Stale critical metrics
    /// </summary>
    private async Task<List<PulseSignal>> GenerateAttentionSignalsAsync(
        Guid userId,
        DateTime cutoffDate,
        CancellationToken ct)
    {
        var signals = new List<PulseSignal>();
        
        try
        {
            Log("Generating attention signals...");
            
            // Get goals and compute their derived health using batch RPC
            var goals = await GoalsService.Instance.GetMyGoalsAsync(ct);
            Log($"Found {goals.Count} goals");
            
            // Compute derived health for all goals in one batch call
            await ComputeGoalDerivedHealthBatchAsync(goals, ct);
            
            // Now filter for goals at risk or off track
            foreach (var goal in goals.Where(g => 
                g.DerivedHealth == GoalDerivedHealth.AtRisk || 
                g.DerivedHealth == GoalDerivedHealth.OffTrack))
            {
                var severity = goal.DerivedHealth == GoalDerivedHealth.OffTrack 
                    ? PulseSignalSeverity.Critical 
                    : PulseSignalSeverity.Warning;
                
                signals.Add(new PulseSignalBuilder()
                    .ForGoal(goal.Id, goal.Title)
                    .ForUser(userId)
                    .WithTrigger(PulseTriggerReason.StatusChange)
                    .WithSeverity(severity)
                    .InSection(PulseSection.AttentionRequired)
                    .WithSummary($"{goal.Title} is {goal.DerivedHealthDisplay.ToLower()}")
                    .WithRecommendedAction("Review and update progress")
                    .WithSeverityBasedPriority()
                    .Build());
            }
            
            // Get metrics that need attention
            var metrics = await MetricsService.Instance.GetAllMetricsAsync(ct);
            foreach (var metric in metrics)
            {
                // Check for stale metrics (use UpdatedAt since LastUpdated doesn't exist)
                if (metric.UpdatedAt < DateTime.UtcNow.AddDays(-StaleMetricDays))
                {
                    signals.Add(new PulseSignalBuilder()
                        .ForMetric(metric.Id, metric.Name)
                        .ForUser(userId)
                        .WithTrigger(PulseTriggerReason.StaleMetric)
                        .WithSeverity(PulseSignalSeverity.Warning)
                        .InSection(PulseSection.AttentionRequired)
                        .WithSummary($"{metric.Name} hasn't been updated in {(DateTime.UtcNow - metric.UpdatedAt).Days} days")
                        .WithRecommendedAction("Update metric value")
                        .WithPriority(40)
                        .Build());
                }
                
                // Check for off-track metrics (based on trend)
                if (metric.Trend == MetricTrend.TrendingDown)
                {
                    signals.Add(new PulseSignalBuilder()
                        .ForMetric(metric.Id, metric.Name)
                        .ForUser(userId)
                        .WithTrigger(PulseTriggerReason.TrendReversal)
                        .WithSeverity(PulseSignalSeverity.Warning)
                        .InSection(PulseSection.AttentionRequired)
                        .WithSummary($"{metric.Name} is trending down")
                        .WithRecommendedAction("Investigate and address")
                        .WithPriority(60)
                        .Build());
                }
            }
            
            // Check for approaching deadlines on goals
            foreach (var goal in goals.Where(g => 
                g.DueDate.HasValue && 
                g.DueDate.Value <= DateTime.UtcNow.AddDays(DeadlineApproachingDays) &&
                g.DueDate.Value >= DateTime.UtcNow &&
                g.Status != "completed"))
            {
                var daysUntil = (goal.DueDate!.Value - DateTime.UtcNow).Days;
                var severity = daysUntil <= 2 ? PulseSignalSeverity.Critical : PulseSignalSeverity.Warning;
                
                signals.Add(new PulseSignalBuilder()
                    .ForGoal(goal.Id, goal.Title)
                    .ForUser(userId)
                    .WithTrigger(PulseTriggerReason.DeadlineApproaching)
                    .WithSeverity(severity)
                    .InSection(PulseSection.AttentionRequired)
                    .WithSummary($"{goal.Title} due in {daysUntil} day{(daysUntil != 1 ? "s" : "")}")
                    .WithRecommendedAction("Review progress and update")
                    .WithPriority(daysUntil <= 2 ? 90 : 45)
                    .Build());
            }
        }
        catch (Exception ex)
        {
            Log($"Error generating attention signals: {ex.Message}");
        }
        
        Log($"Generated {signals.Count} attention signals");
        
        // Sort by priority and return top items
        return signals
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.Severity)
            .Take(MaxAttentionItems)
            .ToList();
    }
    
    #endregion
    
    #region What Changed Signals
    
    /// <summary>
    /// Generates signals for What Changed section.
    /// Shows:
    /// - Threshold crossings
    /// - Trend inflections
    /// - Task completions from discussion
    /// - Goal health changes
    /// </summary>
    private async Task<List<PulseSignal>> GenerateChangeSignalsAsync(
        Guid userId,
        DateTime cutoffDate,
        CancellationToken ct)
    {
        var signals = new List<PulseSignal>();
        
        try
        {
            Log("Generating change signals...");
            
            // Get recently updated metrics
            var metrics = await MetricsService.Instance.GetAllMetricsAsync(ct);
            foreach (var metric in metrics.Where(m => 
                m.UpdatedAt >= cutoffDate))
            {
                // Only report meaningful changes
                if (metric.Trend != MetricTrend.Unknown && metric.Trend != MetricTrend.Stable)
                {
                    var trendText = metric.Trend switch
                    {
                        MetricTrend.TrendingUp => "improving",
                        MetricTrend.TrendingDown => "declining",
                        MetricTrend.MoreVariable => "more variable",
                        _ => "changed"
                    };
                    
                    signals.Add(new PulseSignalBuilder()
                        .ForMetric(metric.Id, metric.Name)
                        .ForUser(userId)
                        .WithTrigger(PulseTriggerReason.TrendReversal)
                        .WithSeverity(PulseSignalSeverity.Info)
                        .InSection(PulseSection.WhatChanged)
                        .WithSummary($"{metric.Name} is {trendText}")
                        .WithPriority(30)
                        .Build());
                }
            }
            
            // Get recently completed tasks that are linked to goals or meetings
            var tasks = await TaskService.Instance.GetTasksAsync(includeCompleted: true);
            foreach (var task in tasks.Where(t => 
                t.IsCompleted && 
                t.CompletedAt.HasValue &&
                t.CompletedAt.Value >= cutoffDate &&
                t.HasSource))
            {
                signals.Add(new PulseSignalBuilder()
                    .ForTask(task.Id, task.Title)
                    .ForUser(userId)
                    .WithTrigger(PulseTriggerReason.TaskCompleted)
                    .WithSeverity(PulseSignalSeverity.Info)
                    .InSection(PulseSection.WhatChanged)
                    .WithSummary($"Completed: {task.Title}")
                    .LinkedToTask(task.Id)
                    .WithPriority(20)
                    .Build());
            }
        }
        catch (Exception ex)
        {
            Log($"Error generating change signals: {ex.Message}");
        }
        
        Log($"Generated {signals.Count} change signals");
        
        return signals
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }
    
    #endregion
    
    #region Recent Discussions Signals
    
    /// <summary>
    /// Generates signals for Recent Discussions section.
    /// Derived from linked agenda items, grouped by entity.
    /// </summary>
    private async Task<List<PulseSignal>> GenerateDiscussionSignalsAsync(
        Guid userId,
        DateTime cutoffDate,
        CancellationToken ct)
    {
        var signals = new List<PulseSignal>();
        
        try
        {
            Log("Generating discussion signals...");
            
            // Load dashboard data which includes meetings with agenda items
            var dashboardData = await DashboardService.Instance.LoadDashboardDataAsync();
            
            foreach (var meeting in dashboardData.Meetings.Where(m => 
                m.ScheduledAt.HasValue &&
                m.ScheduledAt.Value >= cutoffDate &&
                m.ScheduledAt.Value <= DateTime.UtcNow))
            {
                // Get agenda items that have linked entities
                var linkedAgendaItems = meeting.AgendaItems?
                    .Where(a => a.LinkedEntityId.HasValue && !string.IsNullOrEmpty(a.LinkedEntityType))
                    .ToList() ?? new List<MeetingAgendaItem>();
                
                foreach (var agendaItem in linkedAgendaItems)
                {
                    signals.Add(new PulseSignalBuilder()
                        .ForMeeting(meeting.Id, meeting.Title ?? "Meeting")
                        .ForUser(userId)
                        .WithTrigger(PulseTriggerReason.MeetingDiscussion)
                        .WithSeverity(PulseSignalSeverity.Info)
                        .InSection(PulseSection.RecentDiscussions)
                        .WithSummary($"Discussed in {meeting.Title}: {agendaItem.Title}")
                        .LinkedToMeeting(meeting.Id)
                        .WithPriority(25)
                        .CreatedOn(meeting.ScheduledAt ?? DateTime.UtcNow)
                        .Build());
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error generating discussion signals: {ex.Message}");
        }
        
        Log($"Generated {signals.Count} discussion signals");
        
        return signals
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }
    
    #endregion
    
    #region Actions Taken Signals
    
    /// <summary>
    /// Generates signals for Actions Taken section.
    /// Derived from tasks sourced from goals or agenda items.
    /// Reinforces follow-through.
    /// </summary>
    private async Task<List<PulseSignal>> GenerateActionSignalsAsync(
        Guid userId,
        DateTime cutoffDate,
        CancellationToken ct)
    {
        var signals = new List<PulseSignal>();
        
        try
        {
            Log("Generating action signals...");
            
            // Get completed tasks from the time window
            var tasks = await TaskService.Instance.GetTasksAsync(includeCompleted: true);
            foreach (var task in tasks.Where(t => 
                t.IsCompleted && 
                t.CompletedAt.HasValue &&
                t.CompletedAt.Value >= cutoffDate))
            {
                // Include tasks that came from goals or meetings
                if (task.HasSource && (task.SourceType == "goal" || task.SourceType == "meeting" || task.SourceType == "agenda_item"))
                {
                    var builder = new PulseSignalBuilder()
                        .ForTask(task.Id, task.Title)
                        .ForUser(userId)
                        .WithTrigger(PulseTriggerReason.TaskCompleted)
                        .WithSeverity(PulseSignalSeverity.Info)
                        .InSection(PulseSection.ActionsTaken)
                        .WithSummary($"✓ {task.Title}")
                        .LinkedToTask(task.Id)
                        .WithPriority(10)
                        .CreatedOn(task.CompletedAt!.Value);
                    
                    // Link to meeting if sourced from meeting or agenda item
                    if (task.SourceType == "meeting" || task.SourceType == "agenda_item")
                    {
                        builder.LinkedToMeeting(task.SourceId);
                    }
                    
                    signals.Add(builder.Build());
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error generating action signals: {ex.Message}");
        }
        
        Log($"Generated {signals.Count} action signals");
        
        return signals
            .OrderByDescending(s => s.CreatedAt)
            .ToList();
    }
    
    #endregion
}

/// <summary>
/// Container for all Pulse signal data.
/// </summary>
public class PulseData
{
    /// <summary>
    /// Signals requiring immediate attention (max 5).
    /// </summary>
    public List<PulseSignal> AttentionRequired { get; init; } = new();
    
    /// <summary>
    /// Signals about what changed (awareness).
    /// </summary>
    public List<PulseSignal> WhatChanged { get; init; } = new();
    
    /// <summary>
    /// Signals from meeting discussions.
    /// </summary>
    public List<PulseSignal> RecentDiscussions { get; init; } = new();
    
    /// <summary>
    /// Completed actions that reinforce follow-through.
    /// </summary>
    public List<PulseSignal> ActionsTaken { get; init; } = new();
    
    /// <summary>
    /// When this data was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; }
    
    /// <summary>
    /// Time window in days that was used.
    /// </summary>
    public int TimeWindowDays { get; init; }
    
    /// <summary>
    /// Whether there are any signals at all.
    /// </summary>
    public bool HasAnySignals => 
        AttentionRequired.Count > 0 || 
        WhatChanged.Count > 0 || 
        RecentDiscussions.Count > 0 || 
        ActionsTaken.Count > 0;
}
