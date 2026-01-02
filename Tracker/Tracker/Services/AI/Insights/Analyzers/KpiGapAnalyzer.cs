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
    /// Analyzes KPI values against targets and generates insights when
    /// KPIs are significantly below their target values.
    /// </summary>
    public class KpiGapAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "KPI Gap Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[] { InsightType.KpiOffTarget };

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
        /// Only analyze KPIs that haven't been updated in less than this many days.
        /// Helps avoid alerting on stale/abandoned KPIs.
        /// </summary>
        public int MaxDaysSinceUpdate { get; set; } = 30;

        public KpiGapAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("KpiGapAnalyzer");
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping KPI gap analysis");
                    return insights;
                }

                // Get all KPIs
                var kpis = await dbManager.GetKPIsAsync();
                if (kpis == null || kpis.Count == 0)
                {
                    _logger.Debug("No KPIs found");
                    return insights;
                }

                var today = DateTime.Now.Date;
                var cutoffDate = today.AddDays(-MaxDaysSinceUpdate);

                foreach (var kpi in kpis)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Skip KPIs that haven't been updated recently (likely abandoned)
                    if (kpi.LastUpdated < cutoffDate)
                    {
                        _logger.Debug("Skipping stale KPI '{0}' - last updated {1}", 
                            kpi.Name, kpi.LastUpdated);
                        continue;
                    }

                    // Skip KPIs with no target
                    if (kpi.TargetValue == 0)
                        continue;

                    // Analyze the gap
                    var insight = AnalyzeKpiGap(kpi);
                    if (insight != null)
                    {
                        insights.Add(insight);
                    }
                }

                _logger.Info("KPI gap analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Error analyzing KPI gaps: {0}", ex.Message);
            }

            return insights;
        }

        /// <summary>
        /// Analyzes a single KPI and creates an insight if significantly off target.
        /// </summary>
        private Insight? AnalyzeKpiGap(KeyPerformanceIndicator kpi)
        {
            // Calculate progress ratio based on direction
            double progressRatio;

            if (kpi.TargetDirection == TargetDirectionEnum.GreaterOrEqual)
            {
                // Higher is better: ratio = current / target
                progressRatio = kpi.TargetValue != 0 ? kpi.Value / kpi.TargetValue : 0;
            }
            else
            {
                // Lower is better: ratio = target / current (inverted)
                // If current is 0, consider it perfect (avoid division by zero)
                if (kpi.Value == 0)
                    return null; // At or below zero for "lower is better" is great
                    
                progressRatio = kpi.TargetValue / kpi.Value;
            }

            _logger.Debug("KPI '{0}': Value={1}, Target={2}, Direction={3}, Ratio={4:F2}",
                kpi.Name, kpi.Value, kpi.TargetValue, kpi.TargetDirection, progressRatio);

            // Determine severity
            if (progressRatio < CriticalThreshold)
            {
                return CreateOffTargetInsight(kpi, progressRatio, InsightSeverity.Critical);
            }
            else if (progressRatio < WarningThreshold)
            {
                return CreateOffTargetInsight(kpi, progressRatio, InsightSeverity.Warning);
            }

            return null;
        }

        /// <summary>
        /// Creates an insight for a KPI that's significantly below target.
        /// </summary>
        private Insight CreateOffTargetInsight(KeyPerformanceIndicator kpi, double progressRatio, InsightSeverity severity)
        {
            var percentOfTarget = progressRatio * 100;
            var gap = Math.Abs(kpi.TargetValue - kpi.Value);
            var direction = kpi.TargetDirection == TargetDirectionEnum.GreaterOrEqual 
                ? "below" : "above";
            var severityText = severity == InsightSeverity.Critical 
                ? "significantly off target" : "off target";

            var unitDisplay = string.IsNullOrWhiteSpace(kpi.Unit) ? "" : $" {kpi.Unit}";

            return new Insight
            {
                UniqueKey = $"kpi_off_target_{kpi.KpiId}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.KpiOffTarget,
                Severity = severity,
                Title = $"KPI {severityText}: {TruncateTitle(kpi.Name, 25)}",
                Description = $"\"{kpi.Name}\" is currently at {kpi.Value:N1}{unitDisplay}, which is {direction} " +
                              $"the target of {kpi.TargetValue:N1}{unitDisplay} ({percentOfTarget:F0}% of target). " +
                              $"Gap: {gap:N1}{unitDisplay}.",
                ActionSuggestion = "Review the KPI trend and identify root causes. Consider action plans or adjust the target if circumstances have changed.",
                EntityType = "KPI",
                EntityId = kpi.KpiId,
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
