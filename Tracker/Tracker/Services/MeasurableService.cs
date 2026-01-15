using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Interfaces;
using Tracker.Managers;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IMeasurableService for resolving measurable progress
    /// from various sources (Metric, Project, TaskCollection).
    /// </summary>
    public class MeasurableService : IMeasurableService
    {
        public MeasurableService()
        {
        }

        /// <inheritdoc />
        public Task<decimal> GetProgressAsync(IMeasurable measurable)
        {
            return Task.FromResult(measurable.CurrentProgress);
        }

        /// <inheritdoc />
        public Task<string> GetDisplayValueAsync(IMeasurable measurable)
        {
            var displayValue = measurable switch
            {
                Metric metric => $"{metric.CurrentValue:F1} / {metric.TargetValue:F1}",
                Project project => $"{project.ProgressPercent:F0}%",
                TaskCollection tc => tc.DisplayValue,
                _ => string.Empty
            };
            return Task.FromResult(displayValue);
        }

        /// <inheritdoc />
        public async Task<List<TargetMeasurable>> GetMeasurablesForTargetAsync(Guid targetId)
        {
            var measurables = TrackerDataManager.Instance.Measurables
                .Where(m => m.TargetId == targetId && !m.IsDeleted)
                .ToList();

            foreach (var measurable in measurables)
            {
                var resolved = await ResolveMeasurableAsync(measurable);
                if (resolved != null)
                {
                    measurable.DisplayName = resolved.DisplayName;
                    measurable.CurrentProgress = resolved.CurrentProgress;
                }
            }

            return measurables;
        }

        /// <inheritdoc />
        public async Task<IMeasurable?> ResolveMeasurableAsync(TargetMeasurable measurableLink)
        {
            return measurableLink.MeasurableType switch
            {
                "metric" => await GetMetricAsync(measurableLink.MeasurableId),
                // Project doesn't implement IMeasurable
                "project" => null,
                "task_collection" => await GetTaskCollectionAsync(measurableLink.MeasurableId),
                _ => null
            };
        }

        /// <inheritdoc />
        public async Task<decimal?> CalculateAggregatedValueAsync(Guid targetId)
        {
            var measurables = await GetMeasurablesForTargetAsync(targetId);
            
            if (measurables.Count == 0)
                return null;

            var primaryAggregation = measurables
                .GroupBy(m => m.AggregationType)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return CalculateAggregation(measurables, primaryAggregation);
        }

        /// <inheritdoc />
        public Task<List<IMeasurable>> GetAvailableMeasurablesAsync(string type)
        {
            List<IMeasurable> result = type switch
            {
                "metric" => TrackerDataManager.Instance.Metrics
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Name)
                    .Cast<IMeasurable>()
                    .ToList(),

                // Project doesn't implement IMeasurable, return empty list
                "project" => new List<IMeasurable>(),

                "task_collection" => TrackerDataManager.Instance.TaskCollections
                    .Where(tc => !tc.IsDeleted)
                    .OrderBy(tc => tc.Name)
                    .Cast<IMeasurable>()
                    .ToList(),

                _ => new List<IMeasurable>()
            };
            return Task.FromResult(result);
        }

        #region Private Helpers

        private Task<Metric?> GetMetricAsync(Guid metricId)
        {
            var metric = TrackerDataManager.Instance.Metrics
                .FirstOrDefault(m => m.Id == metricId && !m.IsDeleted);
            return Task.FromResult(metric);
        }

        private Task<Project?> GetProjectAsync(Guid projectId)
        {
            var project = TrackerDataManager.Instance.Projects
                .FirstOrDefault(p => p.Id == projectId && !p.IsDeleted);
            return Task.FromResult(project);
        }

        private Task<TaskCollection?> GetTaskCollectionAsync(Guid collectionId)
        {
            var collection = TrackerDataManager.Instance.TaskCollections
                .FirstOrDefault(tc => tc.Id == collectionId && !tc.IsDeleted);
            return Task.FromResult(collection);
        }

        private string GetDisplayForMeasurable(IMeasurable measurable)
        {
            return measurable switch
            {
                Metric metric => $"{metric.CurrentValue:F1} / {metric.TargetValue:F1}",
                Project project => $"{project.ProgressPercent:F0}%",
                TaskCollection tc => tc.DisplayValue,
                _ => string.Empty
            };
        }

        private decimal CalculateAggregation(List<TargetMeasurable> measurables, AggregationTypeEnum aggregationType)
        {
            var values = measurables
                .Where(m => m.CurrentProgress.HasValue)
                .Select(m => new { Progress = m.CurrentProgress!.Value, Weight = 1.0m })
                .ToList();

            if (values.Count == 0)
                return 0m;

            return aggregationType switch
            {
                AggregationTypeEnum.Latest => values.Last().Progress,
                AggregationTypeEnum.Sum => values.Sum(v => v.Progress),
                AggregationTypeEnum.Average => values.Average(v => v.Progress),
                AggregationTypeEnum.Min => values.Min(v => v.Progress),
                AggregationTypeEnum.Max => values.Max(v => v.Progress),
                AggregationTypeEnum.WeightedAverage => CalculateWeightedAverage(values),
                _ => values.Last().Progress
            };
        }

        private decimal CalculateWeightedAverage(IEnumerable<dynamic> values)
        {
            var totalWeight = 0m;
            var weightedSum = 0m;

            foreach (var v in values)
            {
                weightedSum += v.Progress * v.Weight;
                totalWeight += (decimal)v.Weight;
            }

            return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0m;
        }

        #endregion
    }
}

