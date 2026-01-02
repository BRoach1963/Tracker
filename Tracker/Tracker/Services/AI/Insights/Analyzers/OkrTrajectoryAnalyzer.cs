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

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes OKR progress trajectories and generates insights when objectives
    /// are projected to miss their targets based on current velocity.
    /// </summary>
    public class OkrTrajectoryAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "OKR Trajectory Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] 
        { 
            InsightType.OkrAtRisk, 
            InsightType.OkrEndingSoon 
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
        /// Days before end date to alert that OKR period is ending soon.
        /// </summary>
        public int EndingSoonDays { get; set; } = 14;

        public OkrTrajectoryAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("OkrTrajectoryAnalyzer");
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping OKR trajectory analysis");
                    return insights;
                }

                // Get all OKRs
                var okrs = await dbManager.GetOKRsAsync();
                if (okrs == null || okrs.Count == 0)
                {
                    _logger.Debug("No OKRs found");
                    return insights;
                }

                var today = DateTime.Now.Date;

                foreach (var okr in okrs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Skip completed or future OKRs
                    if (okr.EndDate.Date < today || okr.StartDate.Date > today)
                        continue;

                    // Check if OKR period is ending soon
                    var daysUntilEnd = (okr.EndDate.Date - today).Days;
                    if (daysUntilEnd <= EndingSoonDays && daysUntilEnd > 0)
                    {
                        insights.Add(CreateEndingSoonInsight(okr, daysUntilEnd));
                    }

                    // Calculate trajectory for active OKRs with progress
                    var trajectoryInsight = AnalyzeTrajectory(okr, today);
                    if (trajectoryInsight != null)
                    {
                        insights.Add(trajectoryInsight);
                    }
                }

                _logger.Info("OKR trajectory analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing OKR trajectories: {0}", ex.Message);
            }

            return insights;
        }

        /// <summary>
        /// Analyzes an OKR's trajectory and creates an insight if at risk.
        /// </summary>
        private Insight? AnalyzeTrajectory(ObjectiveKeyResult okr, DateTime today)
        {
            // Need Key Results to analyze
            if (okr.KeyResults == null || okr.KeyResults.Count == 0)
                return null;

            // Calculate time elapsed and remaining
            var totalDays = (okr.EndDate.Date - okr.StartDate.Date).Days;
            if (totalDays <= 0)
                return null;

            var daysElapsed = (today - okr.StartDate.Date).Days;
            if (daysElapsed <= 7) // Need at least a week of data for meaningful velocity
                return null;

            var currentProgress = okr.CompletionPercentage;

            // Calculate velocity (progress per day)
            var dailyVelocity = currentProgress / daysElapsed;

            // Project final completion
            var projectedFinal = dailyVelocity * totalDays;

            // Calculate expected progress at this point (for comparison)
            var expectedProgress = (daysElapsed / (double)totalDays) * 100.0;

            _logger.Debug("OKR '{0}': Days {1}/{2}, Progress {3:F1}%, Expected {4:F1}%, Projected {5:F1}%",
                okr.Title, daysElapsed, totalDays, currentProgress, expectedProgress, projectedFinal);

            // Determine if at risk
            if (projectedFinal < CriticalThresholdPercent)
            {
                return CreateAtRiskInsight(okr, projectedFinal, currentProgress, InsightSeverity.Critical);
            }
            else if (projectedFinal < WarningThresholdPercent)
            {
                return CreateAtRiskInsight(okr, projectedFinal, currentProgress, InsightSeverity.Warning);
            }

            return null;
        }

        /// <summary>
        /// Creates an insight for an OKR that's at risk of missing its target.
        /// </summary>
        private Insight CreateAtRiskInsight(ObjectiveKeyResult okr, double projectedFinal, double currentProgress, InsightSeverity severity)
        {
            var severityText = severity == InsightSeverity.Critical ? "significantly behind" : "falling behind";
            var projectedText = projectedFinal < 0 ? "negative progress" : $"{projectedFinal:F0}%";

            return new Insight
            {
                UniqueKey = $"okr_at_risk_{okr.ObjectiveId}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.OkrAtRisk,
                Severity = severity,
                Title = $"OKR at risk: {TruncateTitle(okr.Title, 30)}",
                Description = $"\"{okr.Title}\" is {severityText}. Current progress is {currentProgress:F0}% " +
                              $"and at this pace, projected to reach only {projectedText} by the end date ({okr.EndDate:MMM d}).",
                ActionSuggestion = "Review Key Results and identify blockers. Consider adjusting targets or reallocating resources.",
                EntityType = "OKR",
                EntityId = okr.ObjectiveId,
                GeneratedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Creates an insight for an OKR period that's ending soon.
        /// </summary>
        private Insight CreateEndingSoonInsight(ObjectiveKeyResult okr, int daysRemaining)
        {
            var severity = daysRemaining <= 7 ? InsightSeverity.Warning : InsightSeverity.Info;
            var urgency = daysRemaining <= 7 ? "This week" : "Soon";
            var currentProgress = okr.CompletionPercentage;
            var status = currentProgress >= 70 ? "on track" : (currentProgress >= 40 ? "needs attention" : "significantly behind");

            return new Insight
            {
                UniqueKey = $"okr_ending_{okr.ObjectiveId}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.OkrEndingSoon,
                Severity = severity,
                Title = $"{urgency}: OKR ends in {daysRemaining} days",
                Description = $"\"{okr.Title}\" ends on {okr.EndDate:MMM d, yyyy}. " +
                              $"Current progress is {currentProgress:F0}% ({status}). " +
                              $"Consider final push or preparing retrospective.",
                ActionSuggestion = currentProgress < 70 
                    ? "Focus on achievable Key Results or document learnings for future OKRs." 
                    : "Final sprint to close out remaining items.",
                EntityType = "OKR",
                EntityId = okr.ObjectiveId,
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
