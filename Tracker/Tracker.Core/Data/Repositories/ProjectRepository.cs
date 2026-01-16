using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for Project entity.
    /// Provides data access for all project-related operations.
    /// 
    /// This is the ONLY place that queries the 'projects' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Projects organize work with teams, timelines, and deliverables.
    /// </summary>
    public interface IProjectRepository : IRepository<Project>
    {
        /// <summary>
        /// Get all projects in an organization.
        /// </summary>
        Task<IEnumerable<Project>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get active projects in an organization.
        /// </summary>
        Task<IEnumerable<Project>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get projects led by a specific person.
        /// </summary>
        Task<IEnumerable<Project>> GetByLeadAsync(Guid leadId);

        /// <summary>
        /// Get projects by status.
        /// </summary>
        Task<IEnumerable<Project>> GetByStatusAsync(string status);
    }

    public class ProjectRepository : BaseRepository<Project>, IProjectRepository
    {
        public ProjectRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<ProjectRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "projects";
        }

        public async Task<IEnumerable<Project>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM projects
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Project>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM projects
                    WHERE organization_id = @OrgId 
                      AND status != 'completed'
                      AND status != 'cancelled'
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Project>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active projects by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetByLeadAsync(Guid leadId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM projects
                    WHERE lead_id = @LeadId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Project>(sql, new { LeadId = leadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by lead {LeadId}", leadId);
                throw;
            }
        }

        public async Task<IEnumerable<Project>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM projects
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Project>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects by status {Status}", status);
                throw;
            }
        }
    }
}
