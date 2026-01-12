using Tracker.Database;
using Tracker.DataModels;
using Microsoft.EntityFrameworkCore;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating metric values from data sources.
    /// </summary>
    public class MetricCalculationService : IMetricCalculationService
    {
        private readonly TrackerDbContext _context;

        public MetricCalculationService(TrackerDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateMetricValueAsync(Guid metricId)
        {
            var metric = await _context.Metrics
                .Include(m => m.DataSources)
                .FirstOrDefaultAsync(m => m.Id == metricId && !m.IsDeleted);

            if (metric == null || metric.DataSources == null || metric.DataSources.Count == 0)
                return metric?.CurrentValue ?? 0m;

            // For now, use the first data source value or calculate from all sources
            // This would need full implementation based on aggregation rules
            return metric.CurrentValue;
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateCompositeMetricValueAsync(Guid metricId)
        {
            // Composite metric calculation from child metrics
            var metric = await _context.Metrics
                .FirstOrDefaultAsync(m => m.Id == metricId && !m.IsDeleted);

            return metric?.CurrentValue ?? 0m;
        }

        /// <inheritdoc />
        public async Task<int> RefreshAllMetricValuesAsync()
        {
            var metrics = await _context.Metrics
                .Where(m => !m.IsDeleted && m.DataSources != null && m.DataSources.Any())
                .ToListAsync();

            // Placeholder for batch refresh logic
            return metrics.Count;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshMetricValueAsync(Guid metricId)
        {
            var metric = await _context.Metrics
                .FirstOrDefaultAsync(m => m.Id == metricId && !m.IsDeleted);

            if (metric == null)
                return false;

            var oldValue = metric.CurrentValue;
            var newValue = await CalculateMetricValueAsync(metricId);

            if (oldValue != newValue)
            {
                metric.CurrentValue = newValue;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<List<MetricDataSource>> GetResolvedDataSourcesAsync(Guid metricId)
        {
            return await _context.MetricDataSources
                .Where(ds => ds.MetricId == metricId && !ds.IsDeleted)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<decimal> GetDataSourceValueAsync(MetricDataSource dataSource)
        {
            // Placeholder for getting data source value
            return 0m;
        }

        /// <inheritdoc />
        public async Task<bool> ValidateNoCyclicDependencyAsync(Guid metricId)
        {
            // Placeholder for cycle detection logic
            // Would need to traverse the metric hierarchy and detect cycles
            return true;
        }
    }
}
