using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Metric operations (formerly KPI/KeyPerformanceIndicator).
    /// Handles all data access for metrics, including composite metrics and data sources.
    /// </summary>
    public interface IMetricRepository
    {
        /// <summary>
        /// Retrieves all metrics for the current user.
        /// </summary>
        Task<List<Metric>> GetMetricsAsync();

        /// <summary>
        /// Retrieves a specific metric by ID.
        /// </summary>
        Task<Metric?> GetMetricByIdAsync(Guid id);

        /// <summary>
        /// Adds a new metric.
        /// </summary>
        Task<Guid> AddMetricAsync(Metric metric);

        /// <summary>
        /// Updates an existing metric.
        /// </summary>
        Task<bool> UpdateMetricAsync(Metric metric);

        /// <summary>
        /// Deletes a metric by ID.
        /// </summary>
        Task<bool> DeleteMetricAsync(Guid id);

        /// <summary>
        /// Gets metrics for a specific category.
        /// </summary>
        Task<List<Metric>> GetMetricsByCategoryAsync(string category);

        /// <summary>
        /// Gets metrics owned by a specific team member.
        /// </summary>
        Task<List<Metric>> GetTeamMemberMetricsAsync(Guid teamMemberId);

        /// <summary>
        /// Gets child metrics of a composite parent metric.
        /// </summary>
        Task<List<Metric>> GetChildMetricsAsync(Guid parentMetricId);

        /// <summary>
        /// Gets team-visible metrics for a specific team member.
        /// </summary>
        Task<List<Metric>> GetTeamVisibleMetricsAsync(Guid teamMemberId);

        /// <summary>
        /// Gets the count of meetings where a specific metric was discussed.
        /// </summary>
        Task<int> GetMetricMeetingCountAsync(Guid metricId);

        /// <summary>
        /// Gets meeting counts for multiple metrics (batch operation).
        /// </summary>
        Task<Dictionary<Guid, int>> GetMetricMeetingCountsAsync(List<Guid> metricIds);

        /// <summary>
        /// Gets data sources for a metric.
        /// </summary>
        Task<List<MetricDataSource>> GetMetricDataSourcesAsync(Guid metricId);

        /// <summary>
        /// Adds a data source to a metric.
        /// </summary>
        Task<bool> AddMetricDataSourceAsync(MetricDataSource dataSource);

        /// <summary>
        /// Removes a data source from a metric.
        /// </summary>
        Task<bool> RemoveMetricDataSourceAsync(Guid dataSourceId);

        /// <summary>
        /// Links a metric to a meeting discussion.
        /// </summary>
        Task<bool> LinkMetricToMeetingAsync(Guid meetingId, Guid metricId, string? discussionNotes = null);

        /// <summary>
        /// Unlinks a metric from a meeting.
        /// </summary>
        Task<bool> UnlinkMetricFromMeetingAsync(Guid meetingId, Guid metricId);
    }
}
