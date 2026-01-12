using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for User entity.
    /// Provides data access for all user-related operations.
    /// 
    /// This is the ONLY place that queries the 'users' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Pattern:
    /// - Inherit from BaseRepository&lt;User&gt;
    /// - Set TableName in constructor
    /// - Override base methods for custom implementations
    /// - Add entity-specific query methods
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Get user by Supabase auth ID.
        /// </summary>
        Task<User?> GetBySupabaseIdAsync(Guid supabaseId);

        /// <summary>
        /// Get user by email address.
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get all users in an organization.
        /// </summary>
        Task<IEnumerable<User>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get all active (not deleted) users in an organization.
        /// </summary>
        Task<IEnumerable<User>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Check if email is already in use.
        /// </summary>
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<UserRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "users";
        }

        public async Task<User?> GetBySupabaseIdAsync(Guid supabaseId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM users 
                    WHERE supabase_id = @SupabaseId AND is_deleted = false 
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { SupabaseId = supabaseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by Supabase ID {SupabaseId}", supabaseId);
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM users 
                    WHERE email = @Email AND is_deleted = false 
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT u.* FROM users u
                    INNER JOIN team_members tm ON u.id = tm.user_id
                    WHERE tm.organization_id = @OrgId AND u.is_deleted = false
                    ORDER BY u.created_at DESC";

                return await connection.QueryAsync<User>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT u.* FROM users u
                    INNER JOIN team_members tm ON u.id = tm.user_id
                    WHERE tm.organization_id = @OrgId 
                      AND u.is_deleted = false 
                      AND tm.is_deleted = false
                    ORDER BY u.created_at DESC";

                return await connection.QueryAsync<User>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM users 
                    WHERE email = @Email AND is_deleted = false";

                var count = await connection.QueryFirstAsync<int>(sql, new { Email = email });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence {Email}", email);
                throw;
            }
        }
    }
}
