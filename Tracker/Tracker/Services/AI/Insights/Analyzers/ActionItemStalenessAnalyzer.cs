using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.Services.AI.Insights.Analyzers
{
    /// <summary>
    /// Analyzes action items (meeting tasks) that have become stale
    /// and generates insights to remind managers to follow up.
    /// </summary>
    public class ActionItemStalenessAnalyzer : IInsightAnalyzer
    {
        private readonly ILogger _logger;

        public string Name => "Action Item Staleness Analyzer";

        public IEnumerable<InsightType> SupportedInsightTypes => new[]
        {
            InsightType.StaleActionItem,
            InsightType.TaskOverdue
        };

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Days after which an uncompleted action item is considered stale.
        /// </summary>
        public int StaleThresholdDays { get; set; } = 14;

        public ActionItemStalenessAnalyzer()
        {
            _logger = LoggingManager.GetComponentLogger("ActionItemStalenessAnalyzer");

            // Load thresholds from settings if available
            var settings = UserSettingsManager.Instance?.Settings?.Insights;
            if (settings != null)
            {
                StaleThresholdDays = settings.ActionItemStaleDays;
            }
        }

        public async Task<List<Insight>> AnalyzeAsync(CancellationToken cancellationToken = default)
        {
            var insights = new List<Insight>();

            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    _logger.Debug("No current user available, skipping action item analysis");
                    return insights;
                }

                var today = DateTime.Today;
                var staleDate = today.AddDays(-StaleThresholdDays);

                // Get all uncompleted tasks from TrackerDataManager
                var dataManager = TrackerDataManager.Instance;
                var uncompletedTasks = dataManager.Tasks
                    .Where(t => !t.IsCompleted && !t.IsDeleted)
                    .ToList();

                foreach (var task in uncompletedTasks)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Check if overdue
                    if (task.DueDate.HasValue && task.DueDate.Value.Date < today)
                    {
                        var daysOverdue = (today - task.DueDate.Value.Date).Days;
                        var severity = daysOverdue > 7 ? InsightSeverity.Critical : InsightSeverity.High;

                        insights.Add(CreateOverdueInsight(task, daysOverdue, severity));
                    }
                    // Check if stale (no due date or future due date, but task is old)
                    else if (task.CreatedAt.Date <= staleDate)
                    {
                        var daysOld = (today - task.CreatedAt.Date).Days;
                        insights.Add(CreateStaleInsight(task, daysOld));
                    }
                }

                _logger.Info("Action item analysis complete: {0} insights generated", insights.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during action item staleness analysis");
            }

            return insights;
        }

        private static Insight CreateOverdueInsight(TrackerTask task, int daysOverdue, InsightSeverity severity)
        {
            var ownerName = task.Owner?.FullName ?? "Unknown";
            var truncatedDesc = TruncateDescription(task.Description, 50);

            return new Insight
            {
                UniqueKey = $"task_overdue_{task.Id}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.TaskOverdue,
                Severity = severity,
                Title = $"⚠️ Overdue: \"{truncatedDesc}\"",
                Description = $"Action item for {ownerName} was due {daysOverdue} day{(daysOverdue != 1 ? "s" : "")} ago ({task.DueDate:MMM d}). Consider following up or rescheduling.",
                ActionSuggestion = "View Task",
                EntityType = "TrackerTask",
                // EntityId not set - task.Id is Guid, EntityId is int?
                GeneratedAt = DateTime.Now
            };
        }

        private static Insight CreateStaleInsight(TrackerTask task, int daysOld)
        {
            var ownerName = task.Owner?.FullName ?? "Unknown";
            var truncatedDesc = TruncateDescription(task.Description, 50);

            return new Insight
            {
                UniqueKey = $"task_stale_{task.Id}_{DateTime.Now:yyyy-MM}",
                Type = InsightType.StaleActionItem,
                Severity = InsightSeverity.Low,
                Title = $"📋 Stale action item: \"{truncatedDesc}\"",
                Description = $"Action item for {ownerName} has been open for {daysOld} days. Consider completing it, updating status, or removing if no longer relevant.",
                ActionSuggestion = "View Task",
                EntityType = "TrackerTask",
                // EntityId not set - task.Id is Guid, EntityId is int?
                GeneratedAt = DateTime.Now
            };
        }

        private static string TruncateDescription(string description, int maxLength)
        {
            if (string.IsNullOrEmpty(description))
                return "Untitled task";

            if (description.Length <= maxLength)
                return description;

            return description.Substring(0, maxLength - 3) + "...";
        }
    }
}
