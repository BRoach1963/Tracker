using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Kudos (Recognition) entity.
    /// Provides data access for all recognition-related operations.
    /// 
    /// This is the ONLY place that queries the 'recognition' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Kudos/Recognition is public praise given from one team member to another.
    /// </summary>
    public interface IKudosRepository : IRepository<DataModels.Kudos>
    {
        /// <summary>
        /// Get all recognition in an organization.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get recognition given by a specific team member.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetGivenByAsync(Guid fromTeamMemberId);

        /// <summary>
        /// Get recognition received by a specific team member.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetReceivedByAsync(Guid toTeamMemberId);

        /// <summary>
        /// Get public recognition in an organization (for wall display).
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetPublicByOrganizationAsync(Guid organizationId, int limit = 50);

        /// <summary>
        /// Get recognition related to a specific project.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetByProjectAsync(Guid projectId);

        /// <summary>
        /// Get recognition related to a specific goal.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetByGoalAsync(Guid goalId);

        /// <summary>
        /// Count recognition received by a team member.
        /// </summary>
        Task<int> CountReceivedByAsync(Guid toTeamMemberId);
    }

    public class KudosRepository : BaseRepository<DataModels.Kudos>, IKudosRepository
    {
        public KudosRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<KudosRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "recognition";
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recognition for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetGivenByAsync(Guid fromTeamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE from_team_member_id = @FromId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { FromId = fromTeamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recognition given by {FromId}", fromTeamMemberId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetReceivedByAsync(Guid toTeamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE to_team_member_id = @ToId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { ToId = toTeamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recognition received by {ToId}", toTeamMemberId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetPublicByOrganizationAsync(Guid organizationId, int limit = 50)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE organization_id = @OrgId AND is_public = true AND is_deleted = false
                    ORDER BY created_at DESC
                    LIMIT @Limit";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { OrgId = organizationId, Limit = limit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public recognition for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetByProjectAsync(Guid projectId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE related_project_id = @ProjectId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { ProjectId = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recognition for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetByGoalAsync(Guid goalId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE related_goal_id = @GoalId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { GoalId = goalId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recognition for goal {GoalId}", goalId);
                throw;
            }
        }

        public async Task<int> CountReceivedByAsync(Guid toTeamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM recognition
                    WHERE to_team_member_id = @ToId AND is_deleted = false";

                return await connection.ExecuteScalarAsync<int>(sql, new { ToId = toTeamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting recognition for {ToId}", toTeamMemberId);
                throw;
            }
        }
    }
}
