using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes Metric values against targets and generates insights when
    /// metrics are significantly below their target values.
    /// </summary>
    public class MetricGapAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;
        private readonly IMetricRepository? _metricRepository;

        public string Name => "Metric Gap Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] { InsightType.MetricOffTarget };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Percentage below target to trigger a warning (e.g., 0.8 = 80% of target).
        /// </summary>
        public double WarningThreshold { get; set; } = 0.7;

        /// <summary>
        /// Percentage below target to trigger a critical alert (e.g., 0.5 = 50% of target).
        /// </summary>
        public double CriticalThreshold { get; set; } = 0.5;

        /// <summary>
        /// Only analyze metrics that haven't been updated in less than this many days.
        /// Helps avoid alerting on stale/abandoned metrics.
        /// </summary>
        public int MaxDaysSinceUpdate { get; set; } = 30;

        public MetricGapAnalyzer(IMetricRepository? metricRepository = null)
        {
            _logger = LoggingManager.GetComponentLogger("MetricGapAnalyzer");
            _metricRepository = metricRepository;
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                if (_metricRepository == null)
                {
                    _logger.Debug("MetricRepository not available, skipping metric gap analysis");
                    return insights;
                }

                // Get all metrics
                var metrics = await _metricRepository.GetMetricsAsync();
                if (metrics == null || metrics.Count() == 0)
                {
                    _logger.Debug("No metrics found");
                    return insights;
                }

                var today = DateTime.Now.Date;
                var cutoffDate = today.AddDays(-MaxDaysSinceUpdate);

                foreach (var metric in metrics)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Skip metrics that haven't been updated recently (likely abandoned)
                    if (metric.LastUpdatedAt.HasValue && metric.LastUpdatedAt.Value < cutoffDate)
                    {
                        _logger.Debug("Skipping stale metric '{0}' - last updated {1}", 
                            metric.Name, metric.LastUpdatedAt);
                        continue;
                    }

                    // Skip metrics with no target
                    if (!metric.TargetValue.HasValue || metric.TargetValue == 0)
                        continue;

                    // Analyze the gap
                    var insight = AnalyzeMetricGap(metric);
                    if (insight != null)
                    {
                        insights.Add(insight);
                    }
                }

                _logger.Info("Metric gap analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing metric gaps: {0}", ex.Message);
            }

            return insights;
        }

        /// <summary>
        /// Analyzes a single metric and creates an insight if significantly off target.
        /// </summary>
        private Insight? AnalyzeMetricGap(Metric metric)
        {
            if (!metric.TargetValue.HasValue || metric.TargetValue.Value == 0)
                return null;

            // Calculate progress ratio based on direction
            double progressRatio;

            if (metric.TargetDirection == MetricTargetDirection.HigherIsBetter)
            {
                // Higher is better: ratio = current / target
                progressRatio = (double)(metric.CurrentValue / metric.TargetValue.Value);
            }
            else if (metric.TargetDirection == MetricTargetDirection.LowerIsBetter)
            {
                // Lower is better: ratio = target / current (inverted)
                // If current is 0, consider it perfect (avoid division by zero)
                if (metric.CurrentValue == 0)
                    return null; // At or below zero for "lower is better" is great
                    
                progressRatio = (double)(metric.TargetValue.Value / metric.CurrentValue);
            }
            else
            {
                // Target value - check how close we are
                var diff = Math.Abs(metric.CurrentValue - metric.TargetValue.Value);
                progressRatio = metric.TargetValue.Value != 0 
                    ? 1.0 - (double)(diff / metric.TargetValue.Value) 
                    : 1.0;
            }

            _logger.Debug("Metric '{0}': Value={1}, Target={2}, Direction={3}, Ratio={4:F2}",
                metric.Name, metric.CurrentValue, metric.TargetValue, metric.TargetDirection, progressRatio);

            // Determine severity
            if (progressRatio < CriticalThreshold)
            {
                return CreateOffTargetInsight(metric, progressRatio, InsightSeverity.Critical);
            }
            else if (progressRatio < WarningThreshold)
            {
                return CreateOffTargetInsight(metric, progressRatio, InsightSeverity.Warning);
            }

            return null;
        }

        /// <summary>
        /// Creates an insight for a metric that's significantly below target.
        /// </summary>
        private Insight CreateOffTargetInsight(Metric metric, double progressRatio, InsightSeverity severity)
        {
            var percentOfTarget = progressRatio * 100;
            var gap = Math.Abs(metric.TargetValue!.Value - metric.CurrentValue);
            var direction = metric.TargetDirection == MetricTargetDirection.HigherIsBetter 
                ? "below" : "above";
            var severityText = severity == InsightSeverity.Critical 
                ? "significantly off target" : "off target";

            var unitDisplay = string.IsNullOrWhiteSpace(metric.Unit) ? "" : $" {metric.Unit}";

            return new Insight
            {
                UniqueKey = $"metric_off_target_{metric.Id}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.MetricOffTarget,
                Severity = severity,
                Title = $"Metric {severityText}: {TruncateTitle(metric.Name, 25)}",
                Description = $"\"{metric.Name}\" is currently at {metric.CurrentValue:N1}{unitDisplay}, which is {direction} " +
                              $"the target of {metric.TargetValue:N1}{unitDisplay} ({percentOfTarget:F0}% of target). " +
                              $"Gap: {gap:N1}{unitDisplay}.",
                ActionSuggestion = "Review the metric trend and identify root causes. Consider action plans or adjust the target if circumstances have changed.",
                EntityType = "Metric",
                // EntityId not set - metric.Id is Guid, EntityId is int?
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
