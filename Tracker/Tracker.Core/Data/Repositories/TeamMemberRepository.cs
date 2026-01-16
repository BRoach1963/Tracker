using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for TeamMember entity.
    /// Provides data access for all team member-related operations.
    /// 
    /// This is the ONLY place that queries the 'team_members' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// TeamMembers represent the association of users to organizations/teams.
    /// A single User can be a TeamMember in multiple organizations.
    /// </summary>
    public interface ITeamMemberRepository : IRepository<TeamMember>
    {
        /// <summary>
        /// Get all active team members (convenience method).
        /// </summary>
        Task<IEnumerable<TeamMember>> GetTeamMembersAsync();

        /// <summary>
        /// Get all team members in an organization.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get all active (not deleted) team members in an organization.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get team members managed by a specific person.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetByManagerAsync(Guid managerId);

        /// <summary>
        /// Get all team memberships for a specific user.
        /// Returns every organization/team the user belongs to.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Get team member by user ID and organization ID (unique constraint).
        /// </summary>
        Task<TeamMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId);

        /// <summary>
        /// Get all team members in a specific team.
        /// </summary>
        Task<IEnumerable<TeamMember>> GetByTeamAsync(Guid teamId);

        /// <summary>
        /// Check if user is active in organization.
        /// </summary>
        Task<bool> IsUserActiveInOrganizationAsync(Guid userId, Guid organizationId);

        /// <summary>
        /// Count active team members in organization.
        /// </summary>
        Task<int> CountActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get team members who haven't had a 1:1 meeting in the specified weeks.
        /// </summary>
        Task<List<TeamMember>> GetTeamMembersWithoutRecentOneOnOneAsync(int weeks);

        /// <summary>
        /// Find a team member by name (first + last).
        /// </summary>
        Task<TeamMember?> FindTeamMemberByNameAsync(string name);
    }

    public class TeamMemberRepository : BaseRepository<TeamMember>, ITeamMemberRepository
    {
        public TeamMemberRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<TeamMemberRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "team_members";
        }

        public async Task<IEnumerable<TeamMember>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE organization_id = @OrgId
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<TeamMember>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<TeamMember>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active team members by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetByManagerAsync(Guid managerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT tm.* FROM team_members tm
                    INNER JOIN team_members manager ON tm.organization_id = manager.organization_id
                    WHERE manager.user_id = @ManagerId 
                      AND tm.user_id != @ManagerId
                      AND tm.is_deleted = false
                      AND manager.is_deleted = false
                    ORDER BY tm.created_at DESC";

                return await connection.QueryAsync<TeamMember>(sql, new { ManagerId = managerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members by manager {ManagerId}", managerId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE user_id = @UserId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<TeamMember>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members by user {UserId}", userId);
                throw;
            }
        }

        public async Task<TeamMember?> GetByUserAndOrganizationAsync(Guid userId, Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE user_id = @UserId AND organization_id = @OrgId AND is_deleted = false
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<TeamMember>(sql, 
                    new { UserId = userId, OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team member by user {UserId} and org {OrgId}", userId, organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetByTeamAsync(Guid teamId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE team_id = @TeamId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<TeamMember>(sql, new { TeamId = teamId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members by team {TeamId}", teamId);
                throw;
            }
        }

        public async Task<bool> IsUserActiveInOrganizationAsync(Guid userId, Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM team_members
                    WHERE user_id = @UserId AND organization_id = @OrgId AND is_deleted = false";

                var count = await connection.QueryFirstAsync<int>(sql, 
                    new { UserId = userId, OrgId = organizationId });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is active in organization");
                throw;
            }
        }

        public async Task<int> CountActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM team_members
                    WHERE organization_id = @OrgId AND is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active team members in organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<TeamMember>> GetTeamMembersAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE is_deleted = false
                    ORDER BY first_name, last_name";

                return await connection.QueryAsync<TeamMember>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all team members");
                throw;
            }
        }

        public async Task<List<TeamMember>> GetTeamMembersWithoutRecentOneOnOneAsync(int weeks)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var cutoffDate = DateTime.UtcNow.AddDays(-7 * weeks);
                
                const string sql = @"
                    SELECT tm.* FROM team_members tm
                    WHERE tm.is_deleted = false
                    AND NOT EXISTS (
                        SELECT 1 FROM meetings m
                        WHERE m.team_member_id = tm.id
                        AND m.is_deleted = false
                        AND m.meeting_type = 'OneOnOne'
                        AND m.scheduled_date >= @CutoffDate
                    )
                    ORDER BY tm.first_name, tm.last_name";

                var result = await connection.QueryAsync<TeamMember>(sql, new { CutoffDate = cutoffDate });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team members without recent 1:1");
                throw;
            }
        }

        public async Task<TeamMember?> FindTeamMemberByNameAsync(string name)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var nameParts = name.Trim().Split(' ', 2);
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                
                const string sql = @"
                    SELECT * FROM team_members
                    WHERE is_deleted = false
                    AND (
                        (first_name ILIKE @FirstName AND last_name ILIKE @LastName)
                        OR (first_name || ' ' || last_name ILIKE @FullName)
                    )
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<TeamMember>(sql, 
                    new { FirstName = $"%{firstName}%", LastName = $"%{lastName}%", FullName = $"%{name}%" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding team member by name {Name}", name);
                throw;
            }
        }
    }
}
