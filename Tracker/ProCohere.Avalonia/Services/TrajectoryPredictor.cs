using System;
using System.Collections.Generic;
using System.Linq;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Predicts goal completion trajectory using linked metrics and trend analysis.
/// Works with TrendAnalyzer to determine probability of on-time goal completion.
/// 
/// Philosophy: Predictions are guidance, not certainty. Always show confidence level.
/// </summary>
public class TrajectoryPredictor
{
    private readonly TrendAnalyzer _trendAnalyzer = new();

    #region Configuration

    /// <summary>
    /// Confidence threshold above which trajectory is considered reliable.
    /// </summary>
    public double ReliableConfidenceThreshold { get; set; } = 0.5;

    /// <summary>
    /// Days before due date to consider "buffer" zone for completion.
    /// </summary>
    public int CompletionBufferDays { get; set; } = 7;

    #endregion

    /// <summary>
    /// Predicts the trajectory for a goal based on its linked metrics.
    /// </summary>
    /// <param name="goal">The goal to analyze.</param>
    /// <param name="linkedMetrics">Metrics linked to this goal.</param>
    /// <param name="metricHistories">History entries for each metric (keyed by metric ID).</param>
    /// <returns>Trajectory prediction result.</returns>
    public TrajectoryResult PredictTrajectory(
        GoalDetail goal,
        IEnumerable<MetricDetail> linkedMetrics,
        Dictionary<Guid, List<MetricHistoryEntry>> metricHistories)
    {
        var metrics = linkedMetrics?.ToList() ?? new List<MetricDetail>();

        // If goal has no due date, we can't predict trajectory
        if (!goal.DueDate.HasValue)
        {
            return TrajectoryResult.NoDueDate(goal);
        }

        // If goal is already completed, return completed status
        if (goal.CompletedAt.HasValue || goal.Status == "completed")
        {
            return TrajectoryResult.Completed(goal);
        }

        // If no linked metrics, base trajectory on goal progress alone
        if (!metrics.Any())
        {
            return PredictFromGoalProgress(goal);
        }

        // Analyze trend for each linked metric
        var metricPredictions = new List<MetricPrediction>();
        foreach (var metric in metrics)
        {
            if (metricHistories.TryGetValue(metric.Id, out var history) && history.Count >= 3)
            {
                var trend = _trendAnalyzer.Analyze(history);
                var prediction = AnalyzeMetricAgainstGoal(goal, metric, trend);
                metricPredictions.Add(prediction);
            }
            else
            {
                // Insufficient history for this metric
                metricPredictions.Add(new MetricPrediction
                {
                    MetricId = metric.Id,
                    MetricName = metric.Name,
                    HasSufficientData = false,
                    IsOnTrack = null,
                    Confidence = 0
                });
            }
        }

        // Aggregate predictions into overall trajectory
        return AggregateTrajectory(goal, metricPredictions);
    }

    /// <summary>
    /// Predicts trajectory when goal has no linked metrics.
    /// Uses goal's progress_percent and time remaining.
    /// </summary>
    private TrajectoryResult PredictFromGoalProgress(GoalDetail goal)
    {
        var dueDate = goal.DueDate!.Value;
        var daysRemaining = (dueDate - DateTime.UtcNow).TotalDays;
        var startDate = goal.StartDate ?? goal.CreatedAt;
        var totalDays = (dueDate - startDate).TotalDays;
        var daysElapsed = (DateTime.UtcNow - startDate).TotalDays;

        // Calculate expected progress percentage based on time
        var expectedProgress = totalDays > 0 ? (daysElapsed / totalDays) * 100 : 100;
        var actualProgress = goal.ProgressPercent;

        // Compare actual vs expected
        var progressGap = actualProgress - expectedProgress;

        TrajectoryStatus status;
        double probability;
        string description;

        if (daysRemaining <= 0)
        {
            // Past due
            status = actualProgress >= 90 ? TrajectoryStatus.AtRisk : TrajectoryStatus.OffTrack;
            probability = actualProgress / 100.0;
            description = actualProgress >= 90
                ? "Goal is past due but nearly complete"
                : "Goal is past due with significant work remaining";
        }
        else if (progressGap >= 10)
        {
            // Ahead of schedule
            status = TrajectoryStatus.OnTrack;
            probability = Math.Min(0.95, 0.7 + (progressGap / 100.0));
            description = $"Goal is ahead of schedule by {progressGap:F0}%";
        }
        else if (progressGap >= -10)
        {
            // Roughly on track
            status = TrajectoryStatus.OnTrack;
            probability = 0.7;
            description = "Goal is progressing at expected pace";
        }
        else if (progressGap >= -25)
        {
            // Slightly behind
            status = TrajectoryStatus.AtRisk;
            probability = 0.5 + (progressGap / 50.0);
            description = $"Goal is behind schedule by {Math.Abs(progressGap):F0}%";
        }
        else
        {
            // Significantly behind
            status = TrajectoryStatus.OffTrack;
            probability = Math.Max(0.1, 0.3 + (progressGap / 100.0));
            description = $"Goal is significantly behind schedule";
        }

        return new TrajectoryResult
        {
            GoalId = goal.Id,
            GoalTitle = goal.Title,
            DueDate = dueDate,
            DaysRemaining = (int)Math.Ceiling(daysRemaining),
            Status = status,
            CompletionProbability = probability,
            Confidence = 0.5, // Low confidence without metric data
            Description = description,
            ProjectedCompletionDate = null, // Can't project without trend data
            MetricPredictions = new List<MetricPrediction>(),
            HasLinkedMetrics = false
        };
    }

    /// <summary>
    /// Analyzes a single metric's trend against the goal's timeline.
    /// </summary>
    private MetricPrediction AnalyzeMetricAgainstGoal(
        GoalDetail goal,
        MetricDetail metric,
        TrendResult trend)
    {
        var dueDate = goal.DueDate!.Value;
        var daysRemaining = (dueDate - DateTime.UtcNow).TotalDays;

        // Project metric value at goal due date
        var projectedValue = _trendAnalyzer.ProjectValue(trend, dueDate);

        // Determine if metric is trending in the right direction
        bool? isOnTrack = null;
        string assessment;

        // Check metric's target direction
        var isHigherBetter = metric.TargetDirection?.Equals("higher_is_better", StringComparison.OrdinalIgnoreCase) ?? false;
        var isLowerBetter = metric.TargetDirection?.Equals("lower_is_better", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isHigherBetter)
        {
            isOnTrack = trend.Direction == MetricTrend.TrendingUp || trend.Direction == MetricTrend.Stable;
            assessment = trend.Direction switch
            {
                MetricTrend.TrendingUp => "Improving as expected",
                MetricTrend.Stable => "Stable (may need acceleration)",
                MetricTrend.TrendingDown => "Moving in wrong direction",
                _ => "Insufficient data"
            };
        }
        else if (isLowerBetter)
        {
            isOnTrack = trend.Direction == MetricTrend.TrendingDown || trend.Direction == MetricTrend.Stable;
            assessment = trend.Direction switch
            {
                MetricTrend.TrendingDown => "Improving as expected",
                MetricTrend.Stable => "Stable (may need acceleration)",
                MetricTrend.TrendingUp => "Moving in wrong direction",
                _ => "Insufficient data"
            };
        }
        else
        {
            // Neutral metric - stability is good
            isOnTrack = trend.Direction == MetricTrend.Stable;
            assessment = trend.Direction == MetricTrend.Stable 
                ? "Stable as expected" 
                : "More variable than expected";
        }

        return new MetricPrediction
        {
            MetricId = metric.Id,
            MetricName = metric.Name,
            HasSufficientData = true,
            IsOnTrack = isOnTrack,
            Confidence = trend.RSquared,
            CurrentValue = trend.LatestValue,
            ProjectedValueAtDueDate = projectedValue,
            TrendDirection = trend.Direction,
            Assessment = assessment
        };
    }

    /// <summary>
    /// Aggregates multiple metric predictions into an overall goal trajectory.
    /// </summary>
    private TrajectoryResult AggregateTrajectory(
        GoalDetail goal,
        List<MetricPrediction> metricPredictions)
    {
        var dueDate = goal.DueDate!.Value;
        var daysRemaining = (dueDate - DateTime.UtcNow).TotalDays;

        // Filter to predictions with sufficient data
        var validPredictions = metricPredictions.Where(p => p.HasSufficientData).ToList();

        if (!validPredictions.Any())
        {
            // Fall back to goal progress prediction
            var fallback = PredictFromGoalProgress(goal);
            fallback.MetricPredictions = metricPredictions;
            fallback.HasLinkedMetrics = true;
            fallback.Description = "Linked metrics have insufficient data; using goal progress";
            return fallback;
        }

        // Calculate aggregate probability
        var onTrackCount = validPredictions.Count(p => p.IsOnTrack == true);
        var offTrackCount = validPredictions.Count(p => p.IsOnTrack == false);
        var totalPredictions = validPredictions.Count;

        // Weight by confidence
        var weightedOnTrack = validPredictions
            .Where(p => p.IsOnTrack == true)
            .Sum(p => p.Confidence);
        var totalWeight = validPredictions.Sum(p => p.Confidence);

        var probability = totalWeight > 0 ? weightedOnTrack / totalWeight : 0.5;
        var avgConfidence = validPredictions.Average(p => p.Confidence);

        // Determine status
        TrajectoryStatus status;
        string description;

        if (daysRemaining <= 0)
        {
            status = goal.ProgressPercent >= 90 ? TrajectoryStatus.AtRisk : TrajectoryStatus.OffTrack;
            description = "Goal is past due date";
        }
        else if (probability >= 0.7)
        {
            status = TrajectoryStatus.OnTrack;
            description = $"{onTrackCount} of {totalPredictions} metrics trending positively";
        }
        else if (probability >= 0.4)
        {
            status = TrajectoryStatus.AtRisk;
            description = $"Mixed signals: {onTrackCount} on track, {offTrackCount} need attention";
        }
        else
        {
            status = TrajectoryStatus.OffTrack;
            description = $"{offTrackCount} of {totalPredictions} metrics trending negatively";
        }

        // Estimate projected completion date based on progress velocity
        DateTime? projectedCompletion = null;
        if (goal.ProgressPercent < 100 && validPredictions.Any())
        {
            var avgSlope = validPredictions.Average(p => Math.Abs(p.ProjectedValueAtDueDate - p.CurrentValue));
            if (avgSlope > 0)
            {
                // Rough estimate: project when progress might reach 100%
                var remainingProgress = 100 - goal.ProgressPercent;
                var daysPerPercent = daysRemaining / Math.Max(1, remainingProgress);
                projectedCompletion = DateTime.UtcNow.AddDays(remainingProgress * daysPerPercent / probability);
            }
        }

        return new TrajectoryResult
        {
            GoalId = goal.Id,
            GoalTitle = goal.Title,
            DueDate = dueDate,
            DaysRemaining = (int)Math.Ceiling(Math.Max(0, daysRemaining)),
            Status = status,
            CompletionProbability = probability,
            Confidence = avgConfidence,
            Description = description,
            ProjectedCompletionDate = projectedCompletion,
            MetricPredictions = metricPredictions,
            HasLinkedMetrics = true
        };
    }
}

/// <summary>
/// Status of goal trajectory.
/// </summary>
public enum TrajectoryStatus
{
    /// <summary>Goal is on track for on-time completion.</summary>
    OnTrack,

    /// <summary>Goal may need attention to complete on time.</summary>
    AtRisk,

    /// <summary>Goal is unlikely to complete on time without intervention.</summary>
    OffTrack,

    /// <summary>Goal is already completed.</summary>
    Completed,

    /// <summary>Cannot determine trajectory (no due date, etc.).</summary>
    Unknown
}

/// <summary>
/// Result of trajectory prediction for a goal.
/// </summary>
public class TrajectoryResult
{
    /// <summary>Goal ID being analyzed.</summary>
    public Guid GoalId { get; init; }

    /// <summary>Goal title for display.</summary>
    public string GoalTitle { get; init; } = string.Empty;

    /// <summary>Goal due date.</summary>
    public DateTime? DueDate { get; init; }

    /// <summary>Days remaining until due date.</summary>
    public int DaysRemaining { get; init; }

    /// <summary>Overall trajectory status.</summary>
    public TrajectoryStatus Status { get; init; }

    /// <summary>Probability of on-time completion (0-1).</summary>
    public double CompletionProbability { get; init; }

    /// <summary>Confidence in the prediction (based on data quality, 0-1).</summary>
    public double Confidence { get; init; }

    /// <summary>Human-readable description of trajectory.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Projected completion date based on current trajectory.</summary>
    public DateTime? ProjectedCompletionDate { get; init; }

    /// <summary>Individual metric predictions.</summary>
    public List<MetricPrediction> MetricPredictions { get; set; } = new();

    /// <summary>Whether goal has any linked metrics.</summary>
    public bool HasLinkedMetrics { get; set; }

    #region Computed Properties

    /// <summary>Probability as percentage string.</summary>
    public string ProbabilityDisplay => $"{CompletionProbability * 100:F0}%";

    /// <summary>Confidence level description.</summary>
    public string ConfidenceLevel => Confidence switch
    {
        >= 0.8 => "High",
        >= 0.5 => "Medium",
        _ => "Low"
    };

    /// <summary>Status display name.</summary>
    public string StatusDisplay => Status switch
    {
        TrajectoryStatus.OnTrack => "On Track",
        TrajectoryStatus.AtRisk => "At Risk",
        TrajectoryStatus.OffTrack => "Off Track",
        TrajectoryStatus.Completed => "Completed",
        _ => "Unknown"
    };

    /// <summary>Status icon for UI.</summary>
    public string StatusIcon => Status switch
    {
        TrajectoryStatus.OnTrack => "✓",
        TrajectoryStatus.AtRisk => "⚠",
        TrajectoryStatus.OffTrack => "✗",
        TrajectoryStatus.Completed => "★",
        _ => "?"
    };

    /// <summary>Days remaining display.</summary>
    public string DaysRemainingDisplay => DaysRemaining switch
    {
        0 => "Due today",
        1 => "1 day left",
        < 0 => $"{Math.Abs(DaysRemaining)} days overdue",
        _ => $"{DaysRemaining} days left"
    };

    /// <summary>Projected completion date display with localized format.</summary>
    public string ProjectedCompletionDisplay => ProjectedCompletionDate.HasValue
        ? string.Format(LocalizationService.Instance.Get("GoalsTab_BasedOnCurrentProgress"), ProjectedCompletionDate.Value)
        : string.Empty;

    #endregion

    #region Factory Methods

    public static TrajectoryResult NoDueDate(GoalDetail goal) => new()
    {
        GoalId = goal.Id,
        GoalTitle = goal.Title,
        DueDate = null,
        DaysRemaining = 0,
        Status = TrajectoryStatus.Unknown,
        CompletionProbability = 0,
        Confidence = 0,
        Description = "No due date set for this goal",
        ProjectedCompletionDate = null,
        HasLinkedMetrics = false
    };

    public static TrajectoryResult Completed(GoalDetail goal) => new()
    {
        GoalId = goal.Id,
        GoalTitle = goal.Title,
        DueDate = goal.DueDate,
        DaysRemaining = 0,
        Status = TrajectoryStatus.Completed,
        CompletionProbability = 1.0,
        Confidence = 1.0,
        Description = $"Completed on {goal.CompletedAt?.ToString("MMM d, yyyy") ?? "unknown date"}",
        ProjectedCompletionDate = goal.CompletedAt,
        HasLinkedMetrics = false
    };

    #endregion
}

/// <summary>
/// Prediction for a single metric's contribution to goal trajectory.
/// </summary>
public class MetricPrediction
{
    public Guid MetricId { get; init; }
    public string MetricName { get; init; } = string.Empty;
    public bool HasSufficientData { get; init; }
    public bool? IsOnTrack { get; init; }
    public double Confidence { get; init; }
    public double CurrentValue { get; init; }
    public double ProjectedValueAtDueDate { get; init; }
    public MetricTrend TrendDirection { get; init; }
    public string Assessment { get; init; } = string.Empty;
}
