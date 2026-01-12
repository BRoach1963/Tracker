using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Goal entity.
    /// Provides data access for all goal-related operations.
    /// 
    /// This is the ONLY place that queries the 'goals' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Goals are strategic/personal objectives with measurables and key results.
    /// They support filtering by owner, organization, status, and time periods.
    /// </summary>
    public interface IGoalRepository : IRepository<Goal>
    {
        /// <summary>
        /// Get all goals for a specific owner (person who set the goal).
        /// </summary>
        Task<IEnumerable<Goal>> GetByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Get active (not completed/archived) goals for an owner.
        /// </summary>
        Task<IEnumerable<Goal>> GetActiveByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Get goals for an entire organization.
        /// </summary>
        Task<IEnumerable<Goal>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get active goals for an organization.
        /// </summary>
        Task<IEnumerable<Goal>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get goals by status (in_progress, completed, abandoned, etc.).
        /// </summary>
        Task<IEnumerable<Goal>> GetByStatusAsync(string status);

        /// <summary>
        /// Get goals set in a specific quarter/period.
        /// </summary>
        Task<IEnumerable<Goal>> GetByQuarterAsync(int year, int quarter);

        /// <summary>
        /// Get goals that are part of a specific OKR set.
        /// </summary>
        Task<IEnumerable<Goal>> GetByOkrSetAsync(Guid okrSetId);

        /// <summary>
        /// Get child goals (goals that support a parent goal).
        /// </summary>
        Task<IEnumerable<Goal>> GetChildrenAsync(Guid parentGoalId);

        /// <summary>
        /// Count active goals for an owner (used for UI summaries).
        /// </summary>
        Task<int> CountActiveByOwnerAsync(Guid ownerId);

        /// <summary>
        /// Count goals by status in an organization (for analytics).
        /// </summary>
        Task<int> CountByStatusInOrganizationAsync(Guid organizationId, string status);
    }

    public class GoalRepository : BaseRepository<Goal>, IGoalRepository
    {
        public GoalRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<GoalRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "goals";
        }

        public async Task<IEnumerable<Goal>> GetByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE owner_id = @OwnerId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals by owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetActiveByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE owner_id = @OwnerId 
                      AND status != 'completed'
                      AND status != 'abandoned'
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active goals by owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT g.* FROM goals g
                    INNER JOIN users u ON g.owner_id = u.id
                    WHERE u.organization_id = @OrgId AND g.is_deleted = false
                    ORDER BY g.created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT g.* FROM goals g
                    INNER JOIN users u ON g.owner_id = u.id
                    WHERE u.organization_id = @OrgId 
                      AND g.status != 'completed'
                      AND g.status != 'abandoned'
                      AND g.is_deleted = false
                    ORDER BY g.created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active goals by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetByQuarterAsync(int year, int quarter)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE goal_year = @Year AND goal_quarter = @Quarter AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { Year = year, Quarter = quarter });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals by quarter {Year}Q{Quarter}", year, quarter);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetByOkrSetAsync(Guid okrSetId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE okr_set_id = @OkrSetId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { OkrSetId = okrSetId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting goals by OKR set {OkrSetId}", okrSetId);
                throw;
            }
        }

        public async Task<IEnumerable<Goal>> GetChildrenAsync(Guid parentGoalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM goals
                    WHERE parent_goal_id = @ParentGoalId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Goal>(sql, new { ParentGoalId = parentGoalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting children for goal {ParentGoalId}", parentGoalId);
                throw;
            }
        }

        public async Task<int> CountActiveByOwnerAsync(Guid ownerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM goals
                    WHERE owner_id = @OwnerId 
                      AND status != 'completed'
                      AND status != 'abandoned'
                      AND is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, new { OwnerId = ownerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active goals for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<int> CountByStatusInOrganizationAsync(Guid organizationId, string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM goals g
                    INNER JOIN users u ON g.owner_id = u.id
                    WHERE u.organization_id = @OrgId 
                      AND g.status = @Status
                      AND g.is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, 
                    new { OrgId = organizationId, Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting goals by status in organization {OrgId}", organizationId);
                throw;
            }
        }
    }
}
