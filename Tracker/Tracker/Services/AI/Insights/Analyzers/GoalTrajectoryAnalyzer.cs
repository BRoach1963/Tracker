using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Microsoft.EntityFrameworkCore;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes Goal progress trajectories and generates insights when goals
    /// are projected to miss their targets based on current velocity.
    /// Goals represent organizational, team, and personal objectives.
    /// </summary>
    public class GoalTrajectoryAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;
        private readonly TrackerDbContext _context;

        public string Name => "Goal Trajectory Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] 
        { 
            InsightType.GoalAtRisk, 
            InsightType.GoalEndingSoon 
        };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Projected completion below this percentage triggers a warning.
        /// </summary>
        public double WarningThresholdPercent { get; set; } = 70.0;

        /// <summary>
        /// Projected completion below this percentage triggers a critical alert.
        /// </summary>
        public double CriticalThresholdPercent { get; set; } = 50.0;

        /// <summary>
        /// Days before end date to alert that Goal period is ending soon.
        /// </summary>
        public int EndingSoonDays { get; set; } = 14;

        public GoalTrajectoryAnalyzer(TrackerDbContext context)
        {
            _context = context;
            _logger = LoggingManager.GetComponentLogger("GoalTrajectoryAnalyzer");
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var today = DateTime.Now;

                // Get all active goals with targets
                var goals = await _context.Goals
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Where(g => !g.IsDeleted && g.EndDate > today)
                    .ToListAsync(cancellationToken);

                foreach (var goal in goals)
                {
                    // Check if ending soon
                    var daysRemaining = (goal.EndDate.Date - today.Date).Days;
                    if (daysRemaining >= 0 && daysRemaining <= EndingSoonDays)
                    {
                        var endingSoonInsight = CreateEndingSoonInsight(goal, daysRemaining);
                        insights.Add(endingSoonInsight);
                    }

                    // Analyze trajectory
                    var trajectoryInsight = AnalyzeTrajectory(goal, today);
                    if (trajectoryInsight != null)
                    {
                        insights.Add(trajectoryInsight);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing Goal trajectories: {0}", ex.Message);
            }

            return insights;
        }

        /// <summary>
        /// Analyzes a Goal's trajectory and creates an insight if at risk.
        /// </summary>
        private Insight? AnalyzeTrajectory(Goal goal, DateTime today)
        {
            // Need Targets to analyze
            var targets = goal.Targets?.Where(t => !t.IsDeleted).ToList();
            if (targets == null || targets.Count == 0)
                return null;

            // Calculate time elapsed and remaining
            var totalDays = (goal.EndDate.Date - goal.StartDate.Date).Days;
            if (totalDays <= 0)
                return null;

            var daysElapsed = (today - goal.StartDate.Date).Days;
            if (daysElapsed <= 7) // Need at least a week of data for meaningful velocity
                return null;

            var currentProgress = goal.Progress;

            // Calculate velocity (progress per day)
            var dailyVelocity = currentProgress / daysElapsed;

            // Project final completion
            var projectedFinal = dailyVelocity * totalDays;

            // Calculate expected progress at this point (for comparison)
            var expectedProgress = (daysElapsed / (double)totalDays) * 100.0;

            _logger.Debug("Goal '{0}': Days {1}/{2}, Progress {3:F1}%, Expected {4:F1}%, Projected {5:F1}%",
                goal.Title, daysElapsed, totalDays, currentProgress, expectedProgress, projectedFinal);

            // Determine if at risk
            if (projectedFinal < CriticalThresholdPercent)
            {
                return CreateAtRiskInsight(goal, projectedFinal, currentProgress, InsightSeverity.Critical);
            }
            else if (projectedFinal < WarningThresholdPercent)
            {
                return CreateAtRiskInsight(goal, projectedFinal, currentProgress, InsightSeverity.Warning);
            }

            return null;
        }

        /// <summary>
        /// Creates an insight for a Goal that's at risk of missing its target.
        /// </summary>
        private Insight CreateAtRiskInsight(Goal goal, double projectedFinal, double currentProgress, InsightSeverity severity)
        {
            var severityText = severity == InsightSeverity.Critical ? "significantly behind" : "falling behind";
            var projectedText = projectedFinal < 0 ? "negative progress" : $"{projectedFinal:F0}%";

            return new Insight
            {
                UniqueKey = $"goal_at_risk_{goal.Id}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.GoalAtRisk,
                Severity = severity,
                Title = $"Goal at risk: {TruncateTitle(goal.Title, 30)}",
                Description = $"\"{goal.Title}\" is {severityText}. Current progress is {currentProgress:F0}% " +
                              $"and at this pace, projected to reach only {projectedText} by the end date ({goal.EndDate:MMM d}).",
                ActionSuggestion = "Review Targets and identify blockers. Consider adjusting targets or reallocating resources.",
                EntityType = "Goal",
                EntityId = goal.Id.GetHashCode(),
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Creates an insight for a Goal period that's ending soon.
        /// </summary>
        private Insight CreateEndingSoonInsight(Goal goal, int daysRemaining)
        {
            var severity = daysRemaining <= 7 ? InsightSeverity.Warning : InsightSeverity.Info;
            var urgency = daysRemaining <= 7 ? "This week" : "Soon";
            var currentProgress = goal.Progress;
            var status = currentProgress >= 70 ? "on track" : (currentProgress >= 40 ? "needs attention" : "significantly behind");

            return new Insight
            {
                UniqueKey = $"goal_ending_{goal.Id}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.GoalEndingSoon,
                Severity = severity,
                Title = $"{urgency}: Goal ends in {daysRemaining} days",
                Description = $"\"{goal.Title}\" ends on {goal.EndDate:MMM d, yyyy}. " +
                              $"Current progress is {currentProgress:F0}% ({status}). " +
                              $"Consider final push or preparing retrospective.",
                ActionSuggestion = currentProgress < 70 
                    ? "Focus on achievable Targets or document learnings for future Goals." 
                    : "Final sprint to close out remaining items.",
                EntityType = "Goal",
                EntityId = goal.Id.GetHashCode(),
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Truncates a title to a maximum length with ellipsis.
        /// </summary>
        private static string TruncateTitle(string title, int maxLength)
        {
            if (string.IsNullOrEmpty(title) || title.Length <= maxLength)
                return title;
            return title.Substring(0, maxLength - 3) + "...";
        }
    }
}
