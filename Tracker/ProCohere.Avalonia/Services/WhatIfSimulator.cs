using System;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Simulates "what-if" scenarios for goal trajectories.
/// Takes a current trajectory and applies hypothetical adjustments to predict outcomes.
/// 
/// Example scenarios:
/// - "What if we increase our velocity by 20%?"
/// - "What if we extend the deadline by 2 weeks?"
/// - "What if we change our target value?"
/// </summary>
public class WhatIfSimulator
{
    /// <summary>
    /// Simulates adjusting the velocity (rate of progress) by a percentage.
    /// </summary>
    /// <param name="currentTrajectory">The current trajectory prediction.</param>
    /// <param name="velocityChangePercent">Change in velocity (e.g., +20 = 20% faster, -10 = 10% slower).</param>
    /// <returns>Simulated trajectory with adjusted velocity.</returns>
    public ScenarioResult SimulateVelocityChange(TrajectoryResult currentTrajectory, double velocityChangePercent)
    {
        if (currentTrajectory.Status == TrajectoryStatus.Completed)
        {
            return ScenarioResult.NoChangeNeeded(currentTrajectory, ScenarioType.Velocity);
        }

        var velocityMultiplier = 1.0 + (velocityChangePercent / 100.0);
        
        // Adjust probability based on velocity change
        // Higher velocity = higher probability (diminishing returns above 1.0)
        var newProbability = CalculateAdjustedProbability(
            currentTrajectory.CompletionProbability,
            velocityMultiplier
        );

        // Estimate new projected completion date
        DateTime? newProjectedDate = null;
        if (currentTrajectory.ProjectedCompletionDate.HasValue && velocityMultiplier > 0)
        {
            var daysUntilProjected = (currentTrajectory.ProjectedCompletionDate.Value - DateTime.UtcNow).TotalDays;
            var adjustedDays = daysUntilProjected / velocityMultiplier;
            newProjectedDate = DateTime.UtcNow.AddDays(adjustedDays);
        }
        else if (currentTrajectory.DaysRemaining > 0 && velocityMultiplier > 0)
        {
            // Estimate using days remaining and current probability
            var effectiveDays = currentTrajectory.DaysRemaining / (currentTrajectory.CompletionProbability * velocityMultiplier);
            newProjectedDate = DateTime.UtcNow.AddDays(effectiveDays);
        }

        // Determine new status
        var newStatus = DetermineStatus(newProbability);

        return new ScenarioResult
        {
            ScenarioType = ScenarioType.Velocity,
            ScenarioDescription = FormatVelocityDescription(velocityChangePercent),
            OriginalProbability = currentTrajectory.CompletionProbability,
            SimulatedProbability = newProbability,
            OriginalStatus = currentTrajectory.Status,
            SimulatedStatus = newStatus,
            OriginalProjectedDate = currentTrajectory.ProjectedCompletionDate,
            SimulatedProjectedDate = newProjectedDate,
            Impact = DescribeImpact(currentTrajectory.CompletionProbability, newProbability),
            IsPositiveChange = newProbability > currentTrajectory.CompletionProbability,
            ProbabilityChange = newProbability - currentTrajectory.CompletionProbability,
            Confidence = currentTrajectory.Confidence * 0.9 // Slightly reduced confidence for projections
        };
    }

    /// <summary>
    /// Simulates extending or compressing the timeline.
    /// </summary>
    /// <param name="currentTrajectory">The current trajectory prediction.</param>
    /// <param name="daysDelta">Days to add (positive) or remove (negative) from deadline.</param>
    /// <returns>Simulated trajectory with adjusted timeline.</returns>
    public ScenarioResult SimulateTimelineChange(TrajectoryResult currentTrajectory, int daysDelta)
    {
        if (currentTrajectory.Status == TrajectoryStatus.Completed)
        {
            return ScenarioResult.NoChangeNeeded(currentTrajectory, ScenarioType.Timeline);
        }

        if (!currentTrajectory.DueDate.HasValue)
        {
            return ScenarioResult.NoDueDate(ScenarioType.Timeline);
        }

        var originalDaysRemaining = currentTrajectory.DaysRemaining;
        var newDaysRemaining = originalDaysRemaining + daysDelta;

        // Calculate probability based on time extension/compression
        double newProbability;
        if (newDaysRemaining <= 0)
        {
            // Already past or at deadline
            newProbability = currentTrajectory.CompletionProbability * 0.8; // Penalty
        }
        else if (originalDaysRemaining > 0)
        {
            // Scale probability based on additional time
            var timeRatio = (double)newDaysRemaining / originalDaysRemaining;
            newProbability = CalculateTimeAdjustedProbability(currentTrajectory.CompletionProbability, timeRatio);
        }
        else
        {
            // Was overdue, now has time
            newProbability = Math.Min(0.8, currentTrajectory.CompletionProbability + (daysDelta * 0.02));
        }

        var newDueDate = currentTrajectory.DueDate.Value.AddDays(daysDelta);
        var newStatus = DetermineStatus(newProbability);

        return new ScenarioResult
        {
            ScenarioType = ScenarioType.Timeline,
            ScenarioDescription = FormatTimelineDescription(daysDelta),
            OriginalProbability = currentTrajectory.CompletionProbability,
            SimulatedProbability = newProbability,
            OriginalStatus = currentTrajectory.Status,
            SimulatedStatus = newStatus,
            OriginalProjectedDate = currentTrajectory.DueDate,
            SimulatedProjectedDate = newDueDate,
            Impact = DescribeImpact(currentTrajectory.CompletionProbability, newProbability),
            IsPositiveChange = newProbability > currentTrajectory.CompletionProbability,
            ProbabilityChange = newProbability - currentTrajectory.CompletionProbability,
            Confidence = currentTrajectory.Confidence * 0.85
        };
    }

    /// <summary>
    /// Simulates changing the target scope (effectively adjusting what "100%" means).
    /// </summary>
    /// <param name="currentTrajectory">The current trajectory prediction.</param>
    /// <param name="targetChangePercent">Change in target (-20 = 20% less ambitious, +10 = 10% more ambitious).</param>
    /// <returns>Simulated trajectory with adjusted target.</returns>
    public ScenarioResult SimulateTargetChange(TrajectoryResult currentTrajectory, double targetChangePercent)
    {
        if (currentTrajectory.Status == TrajectoryStatus.Completed)
        {
            return ScenarioResult.NoChangeNeeded(currentTrajectory, ScenarioType.Target);
        }

        var targetMultiplier = 1.0 + (targetChangePercent / 100.0);

        // Reducing target = easier = higher probability
        // Increasing target = harder = lower probability
        var effectiveMultiplier = 1.0 / targetMultiplier;
        var newProbability = CalculateAdjustedProbability(
            currentTrajectory.CompletionProbability,
            effectiveMultiplier
        );

        var newStatus = DetermineStatus(newProbability);

        return new ScenarioResult
        {
            ScenarioType = ScenarioType.Target,
            ScenarioDescription = FormatTargetDescription(targetChangePercent),
            OriginalProbability = currentTrajectory.CompletionProbability,
            SimulatedProbability = newProbability,
            OriginalStatus = currentTrajectory.Status,
            SimulatedStatus = newStatus,
            OriginalProjectedDate = currentTrajectory.ProjectedCompletionDate,
            SimulatedProjectedDate = currentTrajectory.ProjectedCompletionDate, // Target change doesn't affect projected date directly
            Impact = DescribeImpact(currentTrajectory.CompletionProbability, newProbability),
            IsPositiveChange = newProbability > currentTrajectory.CompletionProbability,
            ProbabilityChange = newProbability - currentTrajectory.CompletionProbability,
            Confidence = currentTrajectory.Confidence * 0.9
        };
    }

    #region Private Helpers

    /// <summary>
    /// Calculates adjusted probability with diminishing returns near extremes.
    /// </summary>
    private static double CalculateAdjustedProbability(double currentProbability, double multiplier)
    {
        if (multiplier <= 0) return 0;

        // Use logarithmic scaling to prevent unrealistic probabilities
        var logOdds = Math.Log(currentProbability / (1.0 - currentProbability + 0.001));
        var adjustedLogOdds = logOdds + Math.Log(multiplier);
        var newProbability = 1.0 / (1.0 + Math.Exp(-adjustedLogOdds));

        return Math.Clamp(newProbability, 0.01, 0.99);
    }

    /// <summary>
    /// Calculates probability adjustment for timeline changes.
    /// </summary>
    private static double CalculateTimeAdjustedProbability(double currentProbability, double timeRatio)
    {
        // More time increases probability, but with diminishing returns
        if (timeRatio <= 0) return currentProbability * 0.5;

        var adjustment = Math.Log(timeRatio + 0.5) * 0.2; // Logarithmic scaling
        var newProbability = currentProbability + adjustment;

        return Math.Clamp(newProbability, 0.01, 0.99);
    }

    /// <summary>
    /// Determines status based on probability.
    /// </summary>
    private static TrajectoryStatus DetermineStatus(double probability)
    {
        return probability switch
        {
            >= 0.7 => TrajectoryStatus.OnTrack,
            >= 0.4 => TrajectoryStatus.AtRisk,
            _ => TrajectoryStatus.OffTrack
        };
    }

    /// <summary>
    /// Describes the impact of probability change.
    /// </summary>
    private static string DescribeImpact(double original, double simulated)
    {
        var change = simulated - original;
        var changePercent = change * 100;

        if (Math.Abs(changePercent) < 1)
            return "Minimal impact on trajectory";

        var direction = change > 0 ? "improves" : "reduces";
        var magnitude = Math.Abs(changePercent) switch
        {
            >= 20 => "significantly",
            >= 10 => "moderately",
            _ => "slightly"
        };

        return $"This {magnitude} {direction} completion probability by {Math.Abs(changePercent):F0}%";
    }

    private static string FormatVelocityDescription(double changePercent)
    {
        if (changePercent > 0)
            return $"Increase velocity by {changePercent:F0}%";
        if (changePercent < 0)
            return $"Decrease velocity by {Math.Abs(changePercent):F0}%";
        return "No velocity change";
    }

    private static string FormatTimelineDescription(int daysDelta)
    {
        if (daysDelta > 0)
            return daysDelta == 1 ? "Extend deadline by 1 day" : $"Extend deadline by {daysDelta} days";
        if (daysDelta < 0)
            return daysDelta == -1 ? "Compress deadline by 1 day" : $"Compress deadline by {Math.Abs(daysDelta)} days";
        return "No timeline change";
    }

    private static string FormatTargetDescription(double changePercent)
    {
        if (changePercent > 0)
            return $"Increase target by {changePercent:F0}%";
        if (changePercent < 0)
            return $"Reduce target by {Math.Abs(changePercent):F0}%";
        return "No target change";
    }

    #endregion
}

/// <summary>
/// Type of what-if scenario being simulated.
/// </summary>
public enum ScenarioType
{
    Velocity,
    Timeline,
    Target
}

/// <summary>
/// Result of a what-if scenario simulation.
/// </summary>
public class ScenarioResult
{
    /// <summary>Type of scenario simulated.</summary>
    public ScenarioType ScenarioType { get; init; }

    /// <summary>Human-readable description of the scenario.</summary>
    public string ScenarioDescription { get; init; } = string.Empty;

    /// <summary>Original completion probability (0-1).</summary>
    public double OriginalProbability { get; init; }

    /// <summary>Simulated completion probability (0-1).</summary>
    public double SimulatedProbability { get; init; }

    /// <summary>Original trajectory status.</summary>
    public TrajectoryStatus OriginalStatus { get; init; }

    /// <summary>Simulated trajectory status.</summary>
    public TrajectoryStatus SimulatedStatus { get; init; }

    /// <summary>Original projected completion date.</summary>
    public DateTime? OriginalProjectedDate { get; init; }

    /// <summary>Simulated projected completion date.</summary>
    public DateTime? SimulatedProjectedDate { get; init; }

    /// <summary>Description of the impact.</summary>
    public string Impact { get; init; } = string.Empty;

    /// <summary>Whether this is a positive change.</summary>
    public bool IsPositiveChange { get; init; }

    /// <summary>Change in probability (simulated - original).</summary>
    public double ProbabilityChange { get; init; }

    /// <summary>Confidence in the simulation (0-1).</summary>
    public double Confidence { get; init; }

    #region Computed Properties

    /// <summary>Original probability as display string.</summary>
    public string OriginalProbabilityDisplay => $"{OriginalProbability * 100:F0}%";

    /// <summary>Simulated probability as display string.</summary>
    public string SimulatedProbabilityDisplay => $"{SimulatedProbability * 100:F0}%";

    /// <summary>Probability change as display string with sign.</summary>
    public string ProbabilityChangeDisplay
    {
        get
        {
            var change = ProbabilityChange * 100;
            if (change > 0) return $"+{change:F0}%";
            if (change < 0) return $"{change:F0}%";
            return "0%";
        }
    }

    /// <summary>Arrow indicator for change direction.</summary>
    public string ChangeArrow => ProbabilityChange switch
    {
        > 0 => "↑",
        < 0 => "↓",
        _ => "→"
    };

    /// <summary>Status icon for simulated status.</summary>
    public string SimulatedStatusIcon => SimulatedStatus switch
    {
        TrajectoryStatus.OnTrack => "✓",
        TrajectoryStatus.AtRisk => "⚠",
        TrajectoryStatus.OffTrack => "✗",
        TrajectoryStatus.Completed => "★",
        _ => "?"
    };

    /// <summary>Simulated status display name.</summary>
    public string SimulatedStatusDisplay => SimulatedStatus switch
    {
        TrajectoryStatus.OnTrack => "On Track",
        TrajectoryStatus.AtRisk => "At Risk",
        TrajectoryStatus.OffTrack => "Off Track",
        TrajectoryStatus.Completed => "Completed",
        _ => "Unknown"
    };

    /// <summary>Whether status changed.</summary>
    public bool StatusChanged => OriginalStatus != SimulatedStatus;

    /// <summary>Confidence level description.</summary>
    public string ConfidenceLevel => Confidence switch
    {
        >= 0.7 => "High",
        >= 0.4 => "Medium",
        _ => "Low"
    };

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a result indicating no change is needed (goal already completed).
    /// </summary>
    public static ScenarioResult NoChangeNeeded(TrajectoryResult trajectory, ScenarioType type) => new()
    {
        ScenarioType = type,
        ScenarioDescription = "Goal already completed",
        OriginalProbability = 1.0,
        SimulatedProbability = 1.0,
        OriginalStatus = TrajectoryStatus.Completed,
        SimulatedStatus = TrajectoryStatus.Completed,
        OriginalProjectedDate = trajectory.ProjectedCompletionDate,
        SimulatedProjectedDate = trajectory.ProjectedCompletionDate,
        Impact = "No action needed - goal is already complete",
        IsPositiveChange = false,
        ProbabilityChange = 0,
        Confidence = 1.0
    };

    /// <summary>
    /// Creates a result indicating no due date is set.
    /// </summary>
    public static ScenarioResult NoDueDate(ScenarioType type) => new()
    {
        ScenarioType = type,
        ScenarioDescription = "Cannot simulate - no due date",
        OriginalProbability = 0,
        SimulatedProbability = 0,
        OriginalStatus = TrajectoryStatus.Unknown,
        SimulatedStatus = TrajectoryStatus.Unknown,
        OriginalProjectedDate = null,
        SimulatedProjectedDate = null,
        Impact = "Set a due date to enable trajectory simulation",
        IsPositiveChange = false,
        ProbabilityChange = 0,
        Confidence = 0
    };

    #endregion
}
