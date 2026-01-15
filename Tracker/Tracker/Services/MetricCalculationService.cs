using Tracker.DataModels;
using Tracker.Managers;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating metric values from data sources.
    /// </summary>
    public class MetricCalculationService : IMetricCalculationService
    {
        private readonly IMetricRepository _metricRepository;

        public MetricCalculationService(IMetricRepository metricRepository)
        {
            _metricRepository = metricRepository;
        }

        /// <inheritdoc />
        public Task<decimal> CalculateMetricValueAsync(Guid metricId)
        {
            var metric = TrackerDataManager.Instance.Metrics
                .FirstOrDefault(m => m.Id == metricId && !m.IsDeleted);

            if (metric == null)
                return Task.FromResult(0m);

            // For now, use the first data source value or calculate from all sources
            // This would need full implementation based on aggregation rules
            return Task.FromResult(metric.CurrentValue);
        }

        /// <inheritdoc />
        public Task<decimal> CalculateCompositeMetricValueAsync(Guid metricId)
        {
            // Composite metric calculation from child metrics
            var metric = TrackerDataManager.Instance.Metrics
                .FirstOrDefault(m => m.Id == metricId && !m.IsDeleted);

            return Task.FromResult(metric?.CurrentValue ?? 0m);
        }

        /// <inheritdoc />
        public Task<int> RefreshAllMetricValuesAsync()
        {
            var metrics = TrackerDataManager.Instance.Metrics
                .Where(m => !m.IsDeleted)
                .ToList();

            // Placeholder for batch refresh logic
            return Task.FromResult(metrics.Count);
        }

        /// <inheritdoc />
        public async Task<bool> RefreshMetricValueAsync(Guid metricId)
        {
            var metric = TrackerDataManager.Instance.Metrics
                .FirstOrDefault(m => m.Id == metricId && !m.IsDeleted);

            if (metric == null)
                return false;

            var oldValue = metric.CurrentValue;
            var newValue = await CalculateMetricValueAsync(metricId);

            if (oldValue != newValue)
            {
                metric.CurrentValue = newValue;
                await _metricRepository.UpdateAsync(metric);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public Task<List<MetricDataSource>> GetResolvedDataSourcesAsync(Guid metricId)
        {
            var sources = TrackerDataManager.Instance.MetricDataSources
                .Where(ds => ds.MetricId == metricId && !ds.IsDeleted)
                .ToList();
            return Task.FromResult(sources);
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
