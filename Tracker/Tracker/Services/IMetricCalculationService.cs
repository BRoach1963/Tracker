using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating metric values from data sources.
    /// Handles both simple metrics (manual or single-source) and composite metrics (calculated from children).
    /// </summary>
    public interface IMetricCalculationService
    {
        /// <summary>
        /// Calculates the current value for a metric based on its data sources.
        /// For manual metrics (no sources), returns the existing value.
        /// For sourced metrics, aggregates values from all data sources.
        /// </summary>
        /// <param name="metricId">The metric ID.</param>
        /// <returns>The calculated value.</returns>
        Task<decimal> CalculateMetricValueAsync(Guid metricId);

        /// <summary>
        /// Calculates the value for a composite metric from its child metrics.
        /// Uses the aggregation settings defined on the metric's data sources.
        /// </summary>
        /// <param name="metricId">The composite metric ID.</param>
        /// <returns>The calculated composite value.</returns>
        Task<decimal> CalculateCompositeMetricValueAsync(Guid metricId);

        /// <summary>
        /// Refreshes calculated values for all metrics that have data sources.
        /// Useful for batch updates or scheduled refresh operations.
        /// </summary>
        /// <returns>Number of metrics updated.</returns>
        Task<int> RefreshAllMetricValuesAsync();

        /// <summary>
        /// Refreshes a single metric's value and saves to database.
        /// </summary>
        /// <param name="metricId">The metric ID to refresh.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        Task<bool> RefreshMetricValueAsync(Guid metricId);

        /// <summary>
        /// Resolves all data sources for a metric and populates their runtime values.
        /// </summary>
        /// <param name="metricId">The metric ID.</param>
        /// <returns>List of resolved data sources with current values.</returns>
        Task<List<MetricDataSource>> GetResolvedDataSourcesAsync(Guid metricId);

        /// <summary>
        /// Gets the value from a single data source.
        /// Handles different source types (Project, TaskQuery, ChildMetric, Manual).
        /// </summary>
        /// <param name="dataSource">The data source to resolve.</param>
        /// <returns>The value from the data source.</returns>
        Task<decimal> GetDataSourceValueAsync(MetricDataSource dataSource);

        /// <summary>
        /// Validates that a metric's data sources don't create circular dependencies.
        /// </summary>
        /// <param name="metricId">The metric ID to validate.</param>
        /// <returns>True if valid (no cycles), false if circular dependency detected.</returns>
        Task<bool> ValidateNoCyclicDependencyAsync(Guid metricId);
    }
}
