using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for Target entity.
    /// Provides data access for all target-related operations.
    /// 
    /// This is the ONLY place that queries the 'targets' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Targets (formerly Key Results) are measurable outcomes attached to Goals.
    /// Progress is calculated: (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
    /// </summary>
    public interface ITargetRepository : IRepository<Target>
    {
        /// <summary>
        /// Get all targets for a specific goal.
        /// </summary>
        Task<IEnumerable<Target>> GetByGoalAsync(Guid goalId);

        /// <summary>
        /// Get active (not deleted) targets for a goal.
        /// </summary>
        Task<IEnumerable<Target>> GetActiveByGoalAsync(Guid goalId);

        /// <summary>
        /// Get targets by status (on_track, at_risk, off_track, etc.).
        /// </summary>
        Task<IEnumerable<Target>> GetByStatusAsync(Guid goalId, string status);

        /// <summary>
        /// Update target's current value (for progress tracking).
        /// </summary>
        Task<bool> UpdateCurrentValueAsync(Guid id, decimal currentValue);

        /// <summary>
        /// Count active targets for a goal.
        /// </summary>
        Task<int> CountActiveByGoalAsync(Guid goalId);

        /// <summary>
        /// Bulk update sort order for targets.
        /// </summary>
        Task<bool> UpdateSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates);
    }

    public class TargetRepository : BaseRepository<Target>, ITargetRepository
    {
        public TargetRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<TargetRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "targets";
        }

        public async Task<IEnumerable<Target>> GetByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM targets
                    WHERE goal_id = @GoalId
                    ORDER BY sort_order, created_at";

                return await connection.QueryAsync<Target>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting targets for goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<IEnumerable<Target>> GetActiveByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM targets
                    WHERE goal_id = @GoalId AND is_deleted = false
                    ORDER BY sort_order, created_at";

                return await connection.QueryAsync<Target>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active targets for goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<IEnumerable<Target>> GetByStatusAsync(Guid goalId, string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM targets
                    WHERE goal_id = @GoalId AND status = @Status::goal_status AND is_deleted = false
                    ORDER BY sort_order, created_at";

                return await connection.QueryAsync<Target>(sql, new { GoalId = goalId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting targets by status for goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<bool> UpdateCurrentValueAsync(Guid id, decimal currentValue)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE targets
                    SET current_value = @CurrentValue, updated_at = now()
                    WHERE id = @Id AND is_deleted = false";

                var rows = await connection.ExecuteAsync(sql, new { Id = id, CurrentValue = currentValue });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current value for target {Id}", id);
                throw;
            }
        }

        public async Task<int> CountActiveByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM targets
                    WHERE goal_id = @GoalId AND is_deleted = false";

                return await connection.ExecuteScalarAsync<int>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active targets for goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<bool> UpdateSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE targets
                    SET sort_order = @SortOrder, updated_at = now()
                    WHERE id = @Id";

                foreach (var (id, sortOrder) in updates)
                {
                    await connection.ExecuteAsync(sql, new { Id = id, SortOrder = sortOrder });
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sort order for targets");
                throw;
            }
        }
    }
}
