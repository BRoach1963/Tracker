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

        /// <summary>
        /// Add new kudos/recognition.
        /// </summary>
        Task<Guid> AddKudosAsync(DataModels.Kudos kudos);

        /// <summary>
        /// Update existing kudos/recognition.
        /// </summary>
        Task UpdateKudosAsync(DataModels.Kudos kudos);

        /// <summary>
        /// Delete kudos (soft delete).
        /// </summary>
        Task DeleteKudosAsync(Guid kudosId);

        /// <summary>
        /// Get kudos given to a team member.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetKudosToAsync(Guid teamMemberId);

        /// <summary>
        /// Get kudos given from a team member.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetKudosFromAsync(Guid teamMemberId);

        /// <summary>
        /// Get all kudos.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetKudosAsync();

        /// <summary>
        /// Get all public kudos.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetPublicKudosAsync();

        /// <summary>
        /// Get recent kudos.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetRecentKudosAsync(int days = 30);

        /// <summary>
        /// Get kudos by badge type.
        /// </summary>
        Task<IEnumerable<DataModels.Kudos>> GetKudosByBadgeTypeAsync(string badgeType);
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

        public async Task<Guid> AddKudosAsync(DataModels.Kudos kudos)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO recognition (id, from_team_member_id, to_team_member_id, message, badge_type, 
                        is_public, related_project_id, related_goal_id, organization_id, created_at)
                    VALUES (@Id, @FromTeamMemberId, @ToTeamMemberId, @Message, @BadgeType,
                        @IsPublic, @RelatedProjectId, @RelatedGoalId, @OrganizationId, NOW())
                    RETURNING id";

                if (kudos.Id == Guid.Empty)
                    kudos.Id = Guid.NewGuid();

                return await connection.QueryFirstAsync<Guid>(sql, kudos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding kudos");
                throw;
            }
        }

        public async Task UpdateKudosAsync(DataModels.Kudos kudos)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE recognition SET
                        message = @Message,
                        badge_type = @BadgeType,
                        is_public = @IsPublic,
                        updated_at = NOW()
                    WHERE id = @Id AND is_deleted = false";

                await connection.ExecuteAsync(sql, kudos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating kudos {KudosId}", kudos.Id);
                throw;
            }
        }

        public async Task DeleteKudosAsync(Guid kudosId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE recognition SET
                        is_deleted = true,
                        deleted_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = kudosId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting kudos {KudosId}", kudosId);
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetKudosToAsync(Guid teamMemberId)
        {
            return await GetReceivedByAsync(teamMemberId);
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetKudosFromAsync(Guid teamMemberId)
        {
            return await GetGivenByAsync(teamMemberId);
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetKudosAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all kudos");
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetPublicKudosAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE is_public = true AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public kudos");
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetRecentKudosAsync(int days = 30)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE is_deleted = false
                      AND created_at >= NOW() - INTERVAL '@Days days'
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { Days = days });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent kudos");
                throw;
            }
        }

        public async Task<IEnumerable<DataModels.Kudos>> GetKudosByBadgeTypeAsync(string badgeType)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM recognition
                    WHERE badge_type = @BadgeType AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<DataModels.Kudos>(sql, new { BadgeType = badgeType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kudos by badge type {BadgeType}", badgeType);
                throw;
            }
        }
    }
}
