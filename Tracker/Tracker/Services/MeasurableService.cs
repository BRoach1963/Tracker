using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IMeasurableService for resolving measurable progress
    /// from various sources (KPI, Project, TaskCollection).
    /// </summary>
    public class MeasurableService : IMeasurableService
    {
        private readonly TrackerDbContext _context;

        public MeasurableService(TrackerDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task<decimal> GetProgressAsync(IMeasurable measurable)
        {
            // Progress is computed directly on the entity
            return Task.FromResult(measurable.Progress);
        }

        /// <inheritdoc />
        public Task<string> GetDisplayValueAsync(IMeasurable measurable)
        {
            // DisplayValue is computed directly on the entity
            return Task.FromResult(measurable.DisplayValue);
        }

        /// <inheritdoc />
        public async Task<List<KeyResultMeasurable>> GetMeasurablesForKeyResultAsync(int keyResultId)
        {
            var measurables = await _context.KeyResultMeasurables
                .Where(m => m.KeyResultId == keyResultId && !m.IsDeleted)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            // Resolve each measurable and populate computed properties
            foreach (var measurable in measurables)
            {
                var resolved = await ResolveMeasurableAsync(measurable);
                if (resolved != null)
                {
                    measurable.DisplayName = resolved.DisplayName;
                    measurable.CurrentProgress = resolved.Progress;
                    measurable.CurrentDisplayValue = resolved.DisplayValue;
                }
            }

            return measurables;
        }

        /// <inheritdoc />
        public async Task<IMeasurable?> ResolveMeasurableAsync(KeyResultMeasurable measurableLink)
        {
            return measurableLink.MeasurableType switch
            {
                MeasurableType.Kpi => await GetKpiAsync(measurableLink.MeasurableId),
                MeasurableType.Project => await GetProjectAsync(measurableLink.MeasurableId),
                MeasurableType.TaskCollection => await GetTaskCollectionAsync(measurableLink.MeasurableId),
                _ => null
            };
        }

        /// <inheritdoc />
        public async Task<decimal?> CalculateAggregatedValueAsync(int keyResultId)
        {
            var measurables = await GetMeasurablesForKeyResultAsync(keyResultId);
            
            if (measurables.Count == 0)
                return null;

            // Group by aggregation type to handle mixed aggregations
            // For simplicity, we use the most common aggregation type or the first one
            var primaryAggregation = measurables
                .GroupBy(m => m.AggregationType)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return CalculateAggregation(measurables, primaryAggregation);
        }

        /// <inheritdoc />
        public async Task<List<IMeasurable>> GetAvailableMeasurablesAsync(MeasurableType type)
        {
            return type switch
            {
                MeasurableType.Kpi => (await _context.KeyPerformanceIndicators
                    .Where(k => !k.IsDeleted)
                    .OrderBy(k => k.Name)
                    .ToListAsync())
                    .Cast<IMeasurable>()
                    .ToList(),

                MeasurableType.Project => (await _context.Projects
                    .Include(p => p.Tasks)
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Name)
                    .ToListAsync())
                    .Cast<IMeasurable>()
                    .ToList(),

                MeasurableType.TaskCollection => (await _context.TaskCollections
                    .Include(tc => tc.Items)
                        .ThenInclude(i => i.Task)
                    .Where(tc => !tc.IsDeleted)
                    .OrderBy(tc => tc.Name)
                    .ToListAsync())
                    .Cast<IMeasurable>()
                    .ToList(),

                _ => new List<IMeasurable>()
            };
        }

        #region Private Helper Methods

        private async Task<KeyPerformanceIndicator?> GetKpiAsync(int kpiId)
        {
            return await _context.KeyPerformanceIndicators
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();
        }

        private async Task<Project?> GetProjectAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.ID == projectId && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }

        private async Task<TaskCollection?> GetTaskCollectionAsync(int collectionId)
        {
            return await _context.TaskCollections
                .Include(tc => tc.Items)
                    .ThenInclude(i => i.Task)
                .Where(tc => tc.Id == collectionId && !tc.IsDeleted)
                .FirstOrDefaultAsync();
        }

        private decimal CalculateAggregation(List<KeyResultMeasurable> measurables, AggregationTypeEnum aggregationType)
        {
            var values = measurables
                .Where(m => m.CurrentProgress.HasValue)
                .Select(m => new { Progress = m.CurrentProgress!.Value, m.Weight })
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
                totalWeight += v.Weight;
            }

            return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0m;
        }

        #endregion
    }
}

