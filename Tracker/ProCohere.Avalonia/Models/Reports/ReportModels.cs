using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Models.Reports;

/// <summary>
/// Report type enumeration for the Reports view.
/// </summary>
public enum ReportType
{
    Overview,
    Goals,
    Metrics,
    Tasks,
    Meetings,
    Team
}

/// <summary>
/// Overview report data - high-level summary of all areas.
/// </summary>
public class OverviewReportData
{
    // Summary counts
    public int TotalGoals { get; init; }
    public int GoalsOnTrack { get; init; }
    public int GoalsAtRisk { get; init; }
    public int GoalsCompleted { get; init; }
    
    public int TotalTasks { get; init; }
    public int TasksCompleted { get; init; }
    public int TasksOverdue { get; init; }
    public int TasksOpen { get; init; }
    
    public int TotalMeetings { get; init; }
    public int TotalMeetingMinutes { get; init; }
    public int OneOnOneMeetings { get; init; }
    public int TeamMeetings { get; init; }
    
    public int TotalMetrics { get; init; }
    public int MetricsImproving { get; init; }
    public int MetricsDeclining { get; init; }
    
    public int TeamMemberCount { get; init; }
    public int FeedbackGiven { get; init; }
    public int FeedbackReceived { get; init; }
    
    // Time series for charts
    public List<DateValuePoint> GoalProgressOverTime { get; init; } = new();
    public List<DateValuePoint> TaskCompletionOverTime { get; init; } = new();
    public List<DateValuePoint> MeetingMinutesOverTime { get; init; } = new();
    
    // Computed properties
    public double GoalOnTrackPercent => TotalGoals > 0 ? (double)GoalsOnTrack / TotalGoals * 100 : 0;
    public double TaskCompletionPercent => TotalTasks > 0 ? (double)TasksCompleted / TotalTasks * 100 : 0;
    public string TotalMeetingHoursDisplay => $"{TotalMeetingMinutes / 60}h {TotalMeetingMinutes % 60}m";
}

/// <summary>
/// Goals report data - detailed goal analysis.
/// </summary>
public class GoalsReportData
{
    public int TotalGoals { get; init; }
    public int OnTrack { get; init; }
    public int AtRisk { get; init; }
    public int NeedsAttention { get; init; }
    public int Completed { get; init; }
    public int OffTrack { get; init; }
    
    public double AverageProgress { get; init; }
    public double AverageCompletionProbability { get; init; }
    
    // Chart data
    public List<DateValuePoint> ProgressOverTime { get; init; } = new();
    public List<GoalHealthCount> HealthDistribution { get; init; } = new();
    public List<GoalTypeCount> TypeDistribution { get; init; } = new();
    public List<GoalSummaryItem> Goals { get; init; } = new();
    
    // Computed
    public double OnTrackPercent => TotalGoals > 0 ? (double)OnTrack / TotalGoals * 100 : 0;
}

/// <summary>
/// Metrics report data - metric trends and analysis.
/// </summary>
public class MetricsReportData
{
    public int TotalMetrics { get; init; }
    public int Improving { get; init; }
    public int Stable { get; init; }
    public int Declining { get; init; }
    public int Unknown { get; init; }
    
    // Chart data - each metric gets its own trend line
    public List<MetricTrendSeries> MetricTrends { get; init; } = new();
    public List<MetricSummaryItem> Metrics { get; init; } = new();
    
    // Computed
    public double ImprovingPercent => TotalMetrics > 0 ? (double)Improving / TotalMetrics * 100 : 0;
}

/// <summary>
/// Tasks report data - task completion analysis.
/// </summary>
public class TasksReportData
{
    public int TotalTasks { get; init; }
    public int Completed { get; init; }
    public int InProgress { get; init; }
    public int NotStarted { get; init; }
    public int Overdue { get; init; }
    public int Blocked { get; init; }
    
    // Time analysis
    public double AverageCompletionDays { get; init; }
    public int TasksCreatedInPeriod { get; init; }
    public int TasksCompletedInPeriod { get; init; }
    
    // Chart data
    public List<DateValuePoint> CompletionOverTime { get; init; } = new();
    public List<TaskStatusCount> StatusDistribution { get; init; } = new();
    public List<DateValuePoint> CreatedVsCompletedOverTime { get; init; } = new();
    public List<TaskSummaryItem> RecentTasks { get; init; } = new();
    
    // Computed
    public double CompletionRate => TotalTasks > 0 ? (double)Completed / TotalTasks * 100 : 0;
    public int NetTaskChange => TasksCompletedInPeriod - TasksCreatedInPeriod;
}

/// <summary>
/// Meetings report data - meeting time analysis.
/// </summary>
public class MeetingsReportData
{
    public int TotalMeetings { get; init; }
    public int OneOnOnes { get; init; }
    public int TeamMeetings { get; init; }
    public int OtherMeetings { get; init; }
    
    public int TotalMinutes { get; init; }
    public int AverageDurationMinutes { get; init; }
    public int TotalActionItems { get; init; }
    public int CompletedActionItems { get; init; }
    
    // Chart data
    public List<DateValuePoint> MeetingMinutesOverTime { get; init; } = new();
    public List<MeetingTypeCount> TypeDistribution { get; init; } = new();
    public List<DateValuePoint> MeetingCountOverTime { get; init; } = new();
    public List<MeetingSummaryItem> Meetings { get; init; } = new();
    
    // Computed
    public string TotalTimeDisplay => $"{TotalMinutes / 60}h {TotalMinutes % 60}m";
    public double ActionItemCompletionRate => TotalActionItems > 0 ? (double)CompletedActionItems / TotalActionItems * 100 : 0;
}

/// <summary>
/// Team report data - team member activity analysis.
/// </summary>
public class TeamReportData
{
    public int TotalMembers { get; init; }
    public int ActiveMembers { get; init; }
    
    public int TotalFeedbackGiven { get; init; }
    public int TotalFeedbackReceived { get; init; }
    public int PositiveFeedback { get; init; }
    public int ConstructiveFeedback { get; init; }
    public int RecognitionFeedback { get; init; }
    
    // Chart data
    public List<FeedbackTypeCount> FeedbackDistribution { get; init; } = new();
    public List<DateValuePoint> FeedbackOverTime { get; init; } = new();
    public List<TeamMemberActivityItem> MemberActivity { get; init; } = new();
    
    // Computed
    public double FeedbackRatio => TotalFeedbackReceived > 0 ? (double)TotalFeedbackGiven / TotalFeedbackReceived : 0;
}

#region Supporting Types

/// <summary>
/// A point in a time series (date, value).
/// </summary>
public record DateValuePoint(DateTime Date, double Value);

/// <summary>
/// Goal health count for pie chart.
/// </summary>
public record GoalHealthCount(string Health, int Count, string Color);

/// <summary>
/// Goal type count for distribution chart.
/// </summary>
public record GoalTypeCount(string Type, int Count);

/// <summary>
/// Goal summary for list display.
/// </summary>
public record GoalSummaryItem(
    Guid Id,
    string Title,
    string Owner,
    double Progress,
    string Health,
    string HealthColor,
    DateTime? DueDate
);

/// <summary>
/// Metric trend series for multi-line chart.
/// </summary>
public class MetricTrendSeries
{
    public Guid MetricId { get; init; }
    public string MetricName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public MetricTrend Trend { get; init; }
    public string TrendArrow { get; init; } = string.Empty;
    public List<DateValuePoint> DataPoints { get; init; } = new();
}

/// <summary>
/// Metric summary for list display.
/// </summary>
public record MetricSummaryItem(
    Guid Id,
    string Name,
    double CurrentValue,
    double? TargetValue,
    string Unit,
    MetricTrend Trend,
    string TrendArrow,
    double? ChangePercent
);

/// <summary>
/// Task status count for pie chart.
/// </summary>
public record TaskStatusCount(string Status, int Count, string Color);

/// <summary>
/// Task summary for list display.
/// </summary>
public record TaskSummaryItem(
    Guid Id,
    string Title,
    string Assignee,
    string Status,
    DateTime? DueDate,
    DateTime? CompletedAt
);

/// <summary>
/// Meeting type count for pie chart.
/// </summary>
public record MeetingTypeCount(string Type, int Count, string Color);

/// <summary>
/// Meeting summary for list display.
/// </summary>
public record MeetingSummaryItem(
    Guid Id,
    string Title,
    string MeetingType,
    DateTime ScheduledAt,
    int DurationMinutes,
    int AttendeeCount,
    int ActionItemCount
);

/// <summary>
/// Feedback type count for distribution.
/// </summary>
public record FeedbackTypeCount(string Type, int Count, string Color);

/// <summary>
/// Team member activity summary.
/// </summary>
public record TeamMemberActivityItem(
    Guid Id,
    string Name,
    string AvatarUrl,
    int GoalsOwned,
    int TasksAssigned,
    int TasksCompleted,
    int MeetingsAttended,
    int FeedbackGiven,
    int FeedbackReceived
);

#endregion
