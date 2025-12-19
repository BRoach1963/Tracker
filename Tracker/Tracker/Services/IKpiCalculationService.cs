using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Service for calculating KPI values from data sources.
    /// Handles both simple KPIs (manual or single-source) and composite KPIs (calculated from children).
    /// </summary>
    public interface IKpiCalculationService
    {
        /// <summary>
        /// Calculates the current value for a KPI based on its data sources.
        /// For manual KPIs (no sources), returns the existing value.
        /// For sourced KPIs, aggregates values from all data sources.
        /// </summary>
        /// <param name="kpiId">The KPI ID.</param>
        /// <returns>The calculated value.</returns>
        Task<decimal> CalculateKpiValueAsync(int kpiId);

        /// <summary>
        /// Calculates the value for a composite KPI from its child KPIs.
        /// Uses the aggregation settings defined on the KPI's data sources.
        /// </summary>
        /// <param name="kpiId">The composite KPI ID.</param>
        /// <returns>The calculated composite value.</returns>
        Task<decimal> CalculateCompositeKpiValueAsync(int kpiId);

        /// <summary>
        /// Refreshes calculated values for all KPIs that have data sources.
        /// Useful for batch updates or scheduled refresh operations.
        /// </summary>
        /// <returns>Number of KPIs updated.</returns>
        Task<int> RefreshAllKpiValuesAsync();

        /// <summary>
        /// Refreshes a single KPI's value and saves to database.
        /// </summary>
        /// <param name="kpiId">The KPI ID to refresh.</param>
        /// <returns>True if the value changed, false otherwise.</returns>
        Task<bool> RefreshKpiValueAsync(int kpiId);

        /// <summary>
        /// Resolves all data sources for a KPI and populates their runtime values.
        /// </summary>
        /// <param name="kpiId">The KPI ID.</param>
        /// <returns>List of resolved data sources with current values.</returns>
        Task<List<KpiDataSource>> GetResolvedDataSourcesAsync(int kpiId);

        /// <summary>
        /// Gets the value from a single data source.
        /// Handles different source types (Project, TaskQuery, ChildKpi, Manual).
        /// </summary>
        /// <param name="dataSource">The data source to resolve.</param>
        /// <returns>The value from the data source.</returns>
        Task<decimal> GetDataSourceValueAsync(KpiDataSource dataSource);

        /// <summary>
        /// Validates that a KPI's data sources don't create circular dependencies.
        /// </summary>
        /// <param name="kpiId">The KPI ID to validate.</param>
        /// <returns>True if valid (no cycles), false if circular dependency detected.</returns>
        Task<bool> ValidateNoCyclicDependencyAsync(int kpiId);
    }
}

