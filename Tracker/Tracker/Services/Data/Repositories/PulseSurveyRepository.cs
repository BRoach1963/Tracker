using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for PulseSurvey entity.
    /// Provides data access for all pulse survey-related operations.
    /// 
    /// This is the ONLY place that queries the 'pulse_surveys' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Pulse surveys are lightweight, frequent engagement/satisfaction surveys.
    /// </summary>
    public interface IPulseSurveyRepository : IRepository<PulseSurvey>
    {
        /// <summary>
        /// Get all pulse surveys in an organization.
        /// </summary>
        Task<IEnumerable<PulseSurvey>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get active pulse surveys in an organization.
        /// </summary>
        Task<IEnumerable<PulseSurvey>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get pulse surveys created by a specific user.
        /// </summary>
        Task<IEnumerable<PulseSurvey>> GetByCreatorAsync(Guid creatorId);

        /// <summary>
        /// Get pulse surveys by status (draft, active, closed, etc.).
        /// </summary>
        Task<IEnumerable<PulseSurvey>> GetByStatusAsync(string status);
    }

    public class PulseSurveyRepository : BaseRepository<PulseSurvey>, IPulseSurveyRepository
    {
        public PulseSurveyRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<PulseSurveyRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "pulse_surveys";
        }

        public async Task<IEnumerable<PulseSurvey>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM pulse_surveys
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<PulseSurvey>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pulse surveys by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<PulseSurvey>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM pulse_surveys
                    WHERE organization_id = @OrgId 
                      AND status = 'active'
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<PulseSurvey>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active pulse surveys by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<PulseSurvey>> GetByCreatorAsync(Guid creatorId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM pulse_surveys
                    WHERE created_by = @CreatorId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<PulseSurvey>(sql, new { CreatorId = creatorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pulse surveys by creator {CreatorId}", creatorId);
                throw;
            }
        }

        public async Task<IEnumerable<PulseSurvey>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM pulse_surveys
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<PulseSurvey>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pulse surveys by status {Status}", status);
                throw;
            }
        }
    }
}
