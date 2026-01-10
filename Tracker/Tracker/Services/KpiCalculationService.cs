using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Interfaces;

namespace Tracker.Services
{
    /// <summary>
    /// Implementation of IKpiCalculationService for calculating KPI values from data sources.
    /// </summary>
    public class KpiCalculationService : IKpiCalculationService
    {
        private readonly TrackerDbContext _context;

        public KpiCalculationService(TrackerDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateKpiValueAsync(int kpiId)
        {
            var kpi = await _context.KeyPerformanceIndicators
                .Include(k => k.DataSources)
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kpi == null)
                return 0m;

            // If it's a composite KPI, delegate to composite calculation
            if (kpi.IsComposite)
                return await CalculateCompositeKpiValueAsync(kpiId);

            // If no data sources, return existing manual value
            var sources = kpi.DataSources?.Where(s => !s.IsDeleted).ToList();
            if (sources == null || sources.Count == 0)
                return (decimal)kpi.Value;

            // Calculate from data sources
            var values = new List<(decimal Value, decimal Weight, AggregationTypeEnum AggType)>();
            foreach (var source in sources)
            {
                var value = await GetDataSourceValueAsync(source);
                values.Add((value, source.Weight, source.AggregationType));
            }

            return AggregateValues(values);
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateCompositeKpiValueAsync(int kpiId)
        {
            var kpi = await _context.KeyPerformanceIndicators
                .Include(k => k.ChildKpis)
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kpi == null || !kpi.IsComposite)
                return 0m;

            var childKpis = kpi.ChildKpis?.Where(c => !c.IsDeleted).ToList();
            if (childKpis == null || childKpis.Count == 0)
                return 0m;

            // Get the aggregation type from data sources if specified, default to Average
            var aggregationType = await GetCompositeAggregationType(kpiId);

            var values = new List<decimal>();
            foreach (var child in childKpis)
            {
                // Recursively calculate child KPI values
                var childValue = child.IsComposite
                    ? await CalculateCompositeKpiValueAsync(child.KpiId)
                    : (decimal)child.Value;
                values.Add(childValue);
            }

            return aggregationType switch
            {
                AggregationTypeEnum.Sum => values.Sum(),
                AggregationTypeEnum.Average => values.Count > 0 ? values.Average() : 0m,
                AggregationTypeEnum.Min => values.Count > 0 ? values.Min() : 0m,
                AggregationTypeEnum.Max => values.Count > 0 ? values.Max() : 0m,
                _ => values.Count > 0 ? values.Average() : 0m
            };
        }

        /// <inheritdoc />
        public async Task<int> RefreshAllKpiValuesAsync()
        {
            var kpisToRefresh = await _context.KeyPerformanceIndicators
                .Include(k => k.DataSources)
                .Include(k => k.ChildKpis)
                .Where(k => !k.IsDeleted && (k.DataSources.Any() || k.IsComposite))
                .ToListAsync();

            var updatedCount = 0;

            // Process non-composite KPIs first (they don't depend on other KPIs)
            foreach (var kpi in kpisToRefresh.Where(k => !k.IsComposite))
            {
                var newValue = await CalculateKpiValueAsync(kpi.KpiId);
                if ((decimal)kpi.Value != newValue)
                {
                    kpi.Value = (double)newValue;
                    kpi.LastUpdated = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            // Process composite KPIs (they depend on child KPIs, which were just updated)
            // Sort by depth to handle nested composites correctly
            var compositeKpis = await GetSortedCompositeKpisAsync(kpisToRefresh.Where(k => k.IsComposite));
            foreach (var kpi in compositeKpis)
            {
                var newValue = await CalculateCompositeKpiValueAsync(kpi.KpiId);
                if ((decimal)kpi.Value != newValue)
                {
                    kpi.Value = (double)newValue;
                    kpi.LastUpdated = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
                await _context.SaveChangesAsync();

            return updatedCount;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshKpiValueAsync(int kpiId)
        {
            var kpi = await _context.KeyPerformanceIndicators
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kpi == null)
                return false;

            var newValue = await CalculateKpiValueAsync(kpiId);
            if ((decimal)kpi.Value == newValue)
                return false;

            kpi.Value = (double)newValue;
            kpi.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<List<KpiDataSource>> GetResolvedDataSourcesAsync(int kpiId)
        {
            var dataSources = await _context.KpiDataSources
                .Where(ds => ds.KpiId == kpiId && !ds.IsDeleted)
                .OrderBy(ds => ds.SortOrder)
                .ToListAsync();

            foreach (var source in dataSources)
            {
                source.CurrentValue = await GetDataSourceValueAsync(source);
                source.DisplayName = await GetDataSourceDisplayNameAsync(source);
            }

            return dataSources;
        }

        /// <inheritdoc />
        public async Task<decimal> GetDataSourceValueAsync(KpiDataSource dataSource)
        {
            return dataSource.SourceType switch
            {
                KpiSourceType.Project => await GetProjectValueAsync(dataSource.SourceId),
                KpiSourceType.TaskQuery => await GetTaskQueryValueAsync(dataSource),
                KpiSourceType.ChildKpi => await GetChildKpiValueAsync(dataSource.SourceId),
                KpiSourceType.Manual => 0m, // Manual sources don't contribute automatically
                _ => 0m
            };
        }

        /// <inheritdoc />
        public async Task<bool> ValidateNoCyclicDependencyAsync(int kpiId)
        {
            var visited = new HashSet<int>();
            return await CheckNoCycleAsync(kpiId, visited);
        }

        #region Private Helper Methods

        private async Task<decimal> GetProjectValueAsync(int? projectId)
        {
            if (!projectId.HasValue)
                return 0m;

            var project = await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.ID == projectId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            return project?.Progress ?? 0m;
        }

        private async Task<decimal> GetTaskQueryValueAsync(KpiDataSource dataSource)
        {
            if (string.IsNullOrEmpty(dataSource.QueryCriteria))
            {
                // If no criteria, count all non-deleted tasks
                return await _context.Tasks.CountAsync(t => !t.IsDeleted);
            }

            try
            {
                var criteria = JsonSerializer.Deserialize<TaskQueryCriteria>(dataSource.QueryCriteria);
                if (criteria == null)
                    return 0m;

                var query = _context.Tasks.Where(t => !t.IsDeleted);

                // Apply criteria filters
                if (criteria.Status != null)
                {
                    query = criteria.Status.ToLower() switch
                    {
                        "completed" => query.Where(t => t.IsCompleted),
                        "incomplete" or "pending" => query.Where(t => !t.IsCompleted),
                        _ => query
                    };
                }

                if (criteria.ProjectId.HasValue)
                    query = query.Where(t => t.ProjectId == criteria.ProjectId);

                if (criteria.AssigneeId.HasValue)
                    query = query.Where(t => t.Owner != null && t.Owner.Id == criteria.AssigneeId);

                if (criteria.DueBefore.HasValue)
                    query = query.Where(t => t.DueDate <= criteria.DueBefore);

                if (criteria.DueAfter.HasValue)
                    query = query.Where(t => t.DueDate >= criteria.DueAfter);

                return await query.CountAsync();
            }
            catch
            {
                // If JSON parsing fails, return 0
                return 0m;
            }
        }

        private async Task<decimal> GetChildKpiValueAsync(int? kpiId)
        {
            if (!kpiId.HasValue)
                return 0m;

            var kpi = await _context.KeyPerformanceIndicators
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            return kpi != null ? (decimal)kpi.Value : 0m;
        }

        private async Task<string> GetDataSourceDisplayNameAsync(KpiDataSource dataSource)
        {
            return dataSource.SourceType switch
            {
                KpiSourceType.Project when dataSource.SourceId.HasValue =>
                    (await _context.Projects.FindAsync(dataSource.SourceId.Value))?.Name ?? "Unknown Project",
                KpiSourceType.ChildKpi when dataSource.SourceId.HasValue =>
                    (await _context.KeyPerformanceIndicators.FindAsync(dataSource.SourceId.Value))?.Name ?? "Unknown KPI",
                KpiSourceType.TaskQuery => "Task Query",
                KpiSourceType.Manual => "Manual Entry",
                _ => "Unknown Source"
            };
        }

        private async Task<AggregationTypeEnum> GetCompositeAggregationType(int kpiId)
        {
            var firstSource = await _context.KpiDataSources
                .Where(ds => ds.KpiId == kpiId && !ds.IsDeleted)
                .FirstOrDefaultAsync();

            return firstSource?.AggregationType ?? AggregationTypeEnum.Average;
        }

        private decimal AggregateValues(List<(decimal Value, decimal Weight, AggregationTypeEnum AggType)> values)
        {
            if (values.Count == 0)
                return 0m;

            // Use the most common aggregation type, or first if tied
            var primaryAggregation = values
                .GroupBy(v => v.AggType)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            return primaryAggregation switch
            {
                AggregationTypeEnum.Latest => values.Last().Value,
                AggregationTypeEnum.Sum => values.Sum(v => v.Value),
                AggregationTypeEnum.Average => values.Average(v => v.Value),
                AggregationTypeEnum.Min => values.Min(v => v.Value),
                AggregationTypeEnum.Max => values.Max(v => v.Value),
                AggregationTypeEnum.WeightedAverage => CalculateWeightedAverage(values),
                _ => values.Last().Value
            };
        }

        private decimal CalculateWeightedAverage(List<(decimal Value, decimal Weight, AggregationTypeEnum AggType)> values)
        {
            var totalWeight = values.Sum(v => v.Weight);
            if (totalWeight == 0)
                return values.Average(v => v.Value);

            var weightedSum = values.Sum(v => v.Value * v.Weight);
            return Math.Round(weightedSum / totalWeight, 2);
        }

        private Task<List<KeyPerformanceIndicator>> GetSortedCompositeKpisAsync(
            IEnumerable<KeyPerformanceIndicator> compositeKpis)
        {
            // Sort composite KPIs by dependency depth (leaf nodes first)
            var sorted = new List<KeyPerformanceIndicator>();
            var remaining = compositeKpis.ToList();
            var processed = new HashSet<int>();

            while (remaining.Count > 0)
            {
                var batch = new List<KeyPerformanceIndicator>();

                foreach (var kpi in remaining)
                {
                    var childIds = kpi.ChildKpis?
                        .Where(c => c.IsComposite && !c.IsDeleted)
                        .Select(c => c.KpiId)
                        .ToList() ?? new List<int>();

                    // Can process if all composite children have been processed
                    if (childIds.All(id => processed.Contains(id)))
                    {
                        batch.Add(kpi);
                    }
                }

                if (batch.Count == 0 && remaining.Count > 0)
                {
                    // Break potential infinite loop - add remaining in any order
                    batch.AddRange(remaining);
                }

                sorted.AddRange(batch);
                foreach (var kpi in batch)
                {
                    processed.Add(kpi.KpiId);
                    remaining.Remove(kpi);
                }
            }

            return Task.FromResult(sorted);
        }

        private async Task<bool> CheckNoCycleAsync(int kpiId, HashSet<int> visited)
        {
            if (visited.Contains(kpiId))
                return false; // Cycle detected

            visited.Add(kpiId);

            var kpi = await _context.KeyPerformanceIndicators
                .Include(k => k.DataSources)
                .Include(k => k.ChildKpis)
                .Where(k => k.KpiId == kpiId && !k.IsDeleted)
                .FirstOrDefaultAsync();

            if (kpi == null)
                return true;

            // Check child KPIs
            if (kpi.ChildKpis != null)
            {
                foreach (var child in kpi.ChildKpis.Where(c => !c.IsDeleted))
                {
                    if (!await CheckNoCycleAsync(child.KpiId, new HashSet<int>(visited)))
                        return false;
                }
            }

            // Check data sources that reference other KPIs
            if (kpi.DataSources != null)
            {
                foreach (var source in kpi.DataSources.Where(s => !s.IsDeleted && s.SourceType == KpiSourceType.ChildKpi))
                {
                    if (source.SourceId.HasValue)
                    {
                        if (!await CheckNoCycleAsync(source.SourceId.Value, new HashSet<int>(visited)))
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Criteria for filtering tasks in a TaskQuery data source.
        /// </summary>
        private class TaskQueryCriteria
        {
            public string? Status { get; set; }
            public int? ProjectId { get; set; }
            public Guid? AssigneeId { get; set; }
            public DateTime? DueBefore { get; set; }
            public DateTime? DueAfter { get; set; }
        }
    }
}

