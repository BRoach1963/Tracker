using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for MeetingTemplate entity.
    /// Provides data access for all meeting template-related operations.
    /// 
    /// This is the ONLY place that queries the 'meeting_templates' and 'meeting_template_items' tables.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Meeting templates are reusable meeting structures with pre-defined agenda items.
    /// </summary>
    public interface IMeetingTemplateRepository : IRepository<MeetingTemplate>
    {
        /// <summary>
        /// Get all templates for an organization.
        /// </summary>
        Task<IEnumerable<MeetingTemplate>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get active templates for an organization.
        /// </summary>
        Task<IEnumerable<MeetingTemplate>> GetActiveByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get templates by meeting type.
        /// </summary>
        Task<IEnumerable<MeetingTemplate>> GetByMeetingTypeAsync(Guid organizationId, string meetingType);

        /// <summary>
        /// Get a template with its items loaded.
        /// </summary>
        Task<MeetingTemplate?> GetWithItemsAsync(Guid id);

        /// <summary>
        /// Get template items for a template.
        /// </summary>
        Task<IEnumerable<MeetingTemplateItem>> GetTemplateItemsAsync(Guid templateId);

        /// <summary>
        /// Add an item to a template.
        /// </summary>
        Task<MeetingTemplateItem?> AddTemplateItemAsync(MeetingTemplateItem item);

        /// <summary>
        /// Update a template item.
        /// </summary>
        Task<bool> UpdateTemplateItemAsync(MeetingTemplateItem item);

        /// <summary>
        /// Delete a template item.
        /// </summary>
        Task<bool> DeleteTemplateItemAsync(Guid itemId);

        /// <summary>
        /// Update sort order for template items.
        /// </summary>
        Task<bool> UpdateItemSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates);
    }

    public class MeetingTemplateRepository : BaseRepository<MeetingTemplate>, IMeetingTemplateRepository
    {
        public MeetingTemplateRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<MeetingTemplateRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "meeting_templates";
        }

        public async Task<IEnumerable<MeetingTemplate>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meeting_templates
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY sort_order, name";

                return await connection.QueryAsync<MeetingTemplate>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting templates for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<MeetingTemplate>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meeting_templates
                    WHERE organization_id = @OrgId AND is_active = true AND is_deleted = false
                    ORDER BY sort_order, name";

                return await connection.QueryAsync<MeetingTemplate>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active meeting templates for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<MeetingTemplate>> GetByMeetingTypeAsync(Guid organizationId, string meetingType)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meeting_templates
                    WHERE organization_id = @OrgId 
                      AND meeting_type = @MeetingType 
                      AND is_active = true 
                      AND is_deleted = false
                    ORDER BY sort_order, name";

                return await connection.QueryAsync<MeetingTemplate>(sql, new { OrgId = organizationId, MeetingType = meetingType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting templates by type for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<MeetingTemplate?> GetWithItemsAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                
                // Get the template
                const string templateSql = @"
                    SELECT * FROM meeting_templates
                    WHERE id = @Id AND is_deleted = false";
                    
                var template = await connection.QueryFirstOrDefaultAsync<MeetingTemplate>(templateSql, new { Id = id });
                
                if (template != null)
                {
                    // Get the items
                    var items = await GetTemplateItemsAsync(id);
                    template.Items = items.ToList();
                }
                
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting template with items {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<MeetingTemplateItem>> GetTemplateItemsAsync(Guid templateId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meeting_template_items
                    WHERE template_id = @TemplateId
                    ORDER BY sort_order, created_at";

                return await connection.QueryAsync<MeetingTemplateItem>(sql, new { TemplateId = templateId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template items for template {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<MeetingTemplateItem?> AddTemplateItemAsync(MeetingTemplateItem item)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO meeting_template_items 
                        (template_id, title, notes, time_estimate_minutes, sort_order)
                    VALUES 
                        (@TemplateId, @Title, @Notes, @TimeEstimateMinutes, @SortOrder)
                    RETURNING *";

                return await connection.QueryFirstOrDefaultAsync<MeetingTemplateItem>(sql, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding template item to template {TemplateId}", item.TemplateId);
                throw;
            }
        }

        public async Task<bool> UpdateTemplateItemAsync(MeetingTemplateItem item)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE meeting_template_items
                    SET title = @Title, 
                        notes = @Notes, 
                        time_estimate_minutes = @TimeEstimateMinutes,
                        sort_order = @SortOrder,
                        updated_at = now()
                    WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, item);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template item {Id}", item.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateItemAsync(Guid itemId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "DELETE FROM meeting_template_items WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, new { Id = itemId });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template item {ItemId}", itemId);
                throw;
            }
        }

        public async Task<bool> UpdateItemSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE meeting_template_items
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
                _logger.LogError(ex, "Error updating sort order for template items");
                throw;
            }
        }
    }
}
