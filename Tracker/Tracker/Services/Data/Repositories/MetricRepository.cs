using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Metric entity.
    /// Provides data access for all metric-related operations.
    /// 
    /// This is the ONLY place that queries the 'metrics' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Metrics are quantifiable measures linked to goals and OKRs.
    /// They track progress over time with history tracking.
    /// </summary>
    public interface IMetricRepository : IRepository<Metric>
    {
        /// <summary>
        /// Get all metrics for a specific owner.
        /// </summary>
        Task<IEnumerable<Metric>> GetByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Get all active metrics for a owner.
        /// </summary>
        Task<IEnumerable<Metric>> GetActiveByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Get all metrics in an organization.
        /// </summary>
        Task<IEnumerable<Metric>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get all metrics linked to a specific goal.
        /// </summary>
        Task<IEnumerable<Metric>> GetByGoalAsync(Guid goalId);

        /// <summary>
        /// Get metrics by status (active, archived, paused).
        /// </summary>
        Task<IEnumerable<Metric>> GetByStatusAsync(string status);

        /// <summary>
        /// Get metrics updated in a date range (for progress tracking).
        /// </summary>
        Task<IEnumerable<Metric>> GetUpdatedInRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get metric history records for a specific metric.
        /// </summary>
        Task<IEnumerable<MetricHistory>> GetHistoryAsync(Guid metricId);

        /// <summary>
        /// Get metric history in a date range (for trend analysis).
        /// </summary>
        Task<IEnumerable<MetricHistory>> GetHistoryInRangeAsync(Guid metricId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Count active metrics for an owner.
        /// </summary>
        Task<int> CountActiveByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Count metrics by status in organization.
        /// </summary>
        Task<int> CountByStatusInOrganizationAsync(Guid organizationId, string status);
    }

    public class MetricRepository : BaseRepository<Metric>, IMetricRepository
    {
        public MetricRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<MetricRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "metrics";
        }

        public async Task<IEnumerable<Metric>> GetByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metrics
                    WHERE owner_id = @OwnerId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Metric>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics by owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetActiveByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metrics
                    WHERE owner_id = @OwnerId 
                      AND status = 'active'
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Metric>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active metrics by owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT m.* FROM metrics m
                    INNER JOIN users u ON m.owner_id = u.id
                    WHERE u.organization_id = @OrgId AND m.is_deleted = false
                    ORDER BY m.created_at DESC";

                return await connection.QueryAsync<Metric>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metrics
                    WHERE goal_id = @GoalId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Metric>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics by goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metrics
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Metric>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Metric>> GetUpdatedInRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metrics
                    WHERE updated_at >= @StartDate 
                      AND updated_at <= @EndDate
                      AND is_deleted = false
                    ORDER BY updated_at DESC";

                return await connection.QueryAsync<Metric>(sql, 
                    new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics updated in date range");
                throw;
            }
        }

        public async Task<IEnumerable<MetricHistory>> GetHistoryAsync(Guid metricId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metric_history
                    WHERE metric_id = @MetricId
                    ORDER BY recorded_at DESC";

                return await connection.QueryAsync<MetricHistory>(sql, new { MetricId = metricId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for metric {MetricId}", metricId);
                throw;
            }
        }

        public async Task<IEnumerable<MetricHistory>> GetHistoryInRangeAsync(Guid metricId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM metric_history
                    WHERE metric_id = @MetricId
                      AND recorded_at >= @StartDate
                      AND recorded_at <= @EndDate
                    ORDER BY recorded_at DESC";

                return await connection.QueryAsync<MetricHistory>(sql, 
                    new { MetricId = metricId, StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metric history in date range for metric {MetricId}", metricId);
                throw;
            }
        }

        public async Task<int> CountActiveByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM metrics
                    WHERE owner_id = @OwnerId 
                      AND status = 'active'
                      AND is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active metrics for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<int> CountByStatusInOrganizationAsync(Guid organizationId, string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM metrics m
                    INNER JOIN users u ON m.owner_id = u.id
                    WHERE u.organization_id = @OrgId 
                      AND m.status = @Status
                      AND m.is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, 
                    new { OrgId = organizationId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting metrics by status in organization {OrgId}", organizationId);
                throw;
            }
        }
    }
}
