using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for Insight entity.
    /// Provides data access for AI-generated proactive insights.
    /// 
    /// This is the ONLY place that queries the 'ai_insights' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Replaces the old SQLite-based InsightStore.cs.
    /// </summary>
    public interface IInsightRepository : IRepository<Insight>
    {
        /// <summary>
        /// Get all active (non-dismissed, within validity period) insights for an organization.
        /// </summary>
        Task<IEnumerable<Insight>> GetActiveInsightsAsync(Guid organizationId);

        /// <summary>
        /// Get active insights targeting a specific team.
        /// </summary>
        Task<IEnumerable<Insight>> GetActiveInsightsForTeamAsync(Guid teamId);

        /// <summary>
        /// Get active insights targeting a specific team member.
        /// </summary>
        Task<IEnumerable<Insight>> GetActiveInsightsForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Get insights by type (e.g., risk_alert, opportunity, trend, recommendation).
        /// </summary>
        Task<IEnumerable<Insight>> GetByTypeAsync(Guid organizationId, string insightType);

        /// <summary>
        /// Get insights by category (e.g., engagement, performance, retention).
        /// </summary>
        Task<IEnumerable<Insight>> GetByCategoryAsync(Guid organizationId, string category);

        /// <summary>
        /// Get count of unread insights for an organization.
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid organizationId);

        /// <summary>
        /// Mark an insight as read.
        /// </summary>
        Task MarkAsReadAsync(Guid insightId);

        /// <summary>
        /// Mark all insights as read for an organization.
        /// </summary>
        Task MarkAllAsReadAsync(Guid organizationId);

        /// <summary>
        /// Dismiss an insight with optional reason.
        /// </summary>
        Task DismissInsightAsync(Guid insightId, Guid dismissedBy, string? reason = null);

        /// <summary>
        /// Mark an insight as acted upon with optional notes.
        /// </summary>
        Task MarkAsActionedAsync(Guid insightId, string? actionNotes = null);

        /// <summary>
        /// Check if an insight with the given unique key exists and is active.
        /// Used for deduplication.
        /// </summary>
        Task<bool> ExistsActiveByUniqueKeyAsync(string uniqueKey);

        /// <summary>
        /// Get an insight by its unique key.
        /// </summary>
        Task<Insight?> GetByUniqueKeyAsync(string uniqueKey);

        /// <summary>
        /// Delete old dismissed insights (cleanup/maintenance).
        /// </summary>
        Task<int> CleanupOldInsightsAsync(int olderThanDays = 30);
    }

    /// <summary>
    /// Dapper implementation of IInsightRepository.
    /// Uses Supabase PostgreSQL ai_insights table.
    /// </summary>
    public class InsightRepository : BaseRepository<Insight>, IInsightRepository
    {
        public InsightRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<InsightRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "ai_insights";
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Insight>> GetActiveInsightsAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE organization_id = @OrganizationId
                      AND is_dismissed = false
                      AND is_deleted = false
                      AND valid_from <= NOW()
                      AND (valid_until IS NULL OR valid_until > NOW())
                    ORDER BY 
                        CASE priority 
                            WHEN 'critical' THEN 0 
                            WHEN 'high' THEN 1 
                            WHEN 'medium' THEN 2 
                            ELSE 3 
                        END,
                        created_at DESC";

                return await connection.QueryAsync<Insight>(sql, new { OrganizationId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active insights for organization {OrgId}", organizationId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Insight>> GetActiveInsightsForTeamAsync(Guid teamId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE target_team_id = @TeamId
                      AND is_dismissed = false
                      AND is_deleted = false
                      AND valid_from <= NOW()
                      AND (valid_until IS NULL OR valid_until > NOW())
                    ORDER BY 
                        CASE priority 
                            WHEN 'critical' THEN 0 
                            WHEN 'high' THEN 1 
                            WHEN 'medium' THEN 2 
                            ELSE 3 
                        END,
                        created_at DESC";

                return await connection.QueryAsync<Insight>(sql, new { TeamId = teamId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active insights for team {TeamId}", teamId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Insight>> GetActiveInsightsForTeamMemberAsync(Guid teamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE target_team_member_id = @TeamMemberId
                      AND is_dismissed = false
                      AND is_deleted = false
                      AND valid_from <= NOW()
                      AND (valid_until IS NULL OR valid_until > NOW())
                    ORDER BY 
                        CASE priority 
                            WHEN 'critical' THEN 0 
                            WHEN 'high' THEN 1 
                            WHEN 'medium' THEN 2 
                            ELSE 3 
                        END,
                        created_at DESC";

                return await connection.QueryAsync<Insight>(sql, new { TeamMemberId = teamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active insights for team member {MemberId}", teamMemberId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Insight>> GetByTypeAsync(Guid organizationId, string insightType)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE organization_id = @OrganizationId
                      AND insight_type = @InsightType
                      AND is_dismissed = false
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Insight>(sql, new { OrganizationId = organizationId, InsightType = insightType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insights by type {Type} for org {OrgId}", insightType, organizationId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Insight>> GetByCategoryAsync(Guid organizationId, string category)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE organization_id = @OrganizationId
                      AND category = @Category
                      AND is_dismissed = false
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Insight>(sql, new { OrganizationId = organizationId, Category = category });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insights by category {Cat} for org {OrgId}", category, organizationId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> GetUnreadCountAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM ai_insights
                    WHERE organization_id = @OrganizationId
                      AND is_dismissed = false
                      AND is_deleted = false
                      AND is_read = false
                      AND valid_from <= NOW()
                      AND (valid_until IS NULL OR valid_until > NOW())";

                return await connection.ExecuteScalarAsync<int>(sql, new { OrganizationId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for organization {OrgId}", organizationId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task MarkAsReadAsync(Guid insightId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE ai_insights 
                    SET is_read = true, updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = insightId });
                _logger.LogDebug("Marked insight {Id} as read", insightId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking insight {Id} as read", insightId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task MarkAllAsReadAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE ai_insights 
                    SET is_read = true, updated_at = NOW()
                    WHERE organization_id = @OrganizationId
                      AND is_read = false
                      AND is_dismissed = false
                      AND is_deleted = false";

                var affected = await connection.ExecuteAsync(sql, new { OrganizationId = organizationId });
                _logger.LogInformation("Marked {Count} insights as read for organization {OrgId}", affected, organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all insights as read for organization {OrgId}", organizationId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DismissInsightAsync(Guid insightId, Guid dismissedBy, string? reason = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE ai_insights 
                    SET is_dismissed = true, 
                        dismissed_at = NOW(), 
                        dismissed_by = @DismissedBy,
                        dismiss_reason = @Reason,
                        updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = insightId, DismissedBy = dismissedBy, Reason = reason });
                _logger.LogInformation("Dismissed insight {Id} by user {UserId}", insightId, dismissedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing insight {Id}", insightId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task MarkAsActionedAsync(Guid insightId, string? actionNotes = null)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE ai_insights 
                    SET is_actioned = true, 
                        actioned_at = NOW(), 
                        action_notes = @ActionNotes,
                        is_read = true,
                        updated_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = insightId, ActionNotes = actionNotes });
                _logger.LogInformation("Marked insight {Id} as actioned", insightId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking insight {Id} as actioned", insightId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsActiveByUniqueKeyAsync(string uniqueKey)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM ai_insights
                    WHERE unique_key = @UniqueKey
                      AND is_dismissed = false
                      AND is_deleted = false
                      AND valid_from <= NOW()
                      AND (valid_until IS NULL OR valid_until > NOW())";

                var count = await connection.ExecuteScalarAsync<int>(sql, new { UniqueKey = uniqueKey });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if insight exists by unique key {Key}", uniqueKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Insight?> GetByUniqueKeyAsync(string uniqueKey)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM ai_insights
                    WHERE unique_key = @UniqueKey
                      AND is_deleted = false
                    ORDER BY created_at DESC
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<Insight>(sql, new { UniqueKey = uniqueKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insight by unique key {Key}", uniqueKey);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> CleanupOldInsightsAsync(int olderThanDays = 30)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                // Soft delete old dismissed insights
                const string sql = @"
                    UPDATE ai_insights 
                    SET is_deleted = true, 
                        deleted_at = NOW(),
                        updated_at = NOW()
                    WHERE is_dismissed = true 
                      AND dismissed_at < NOW() - @Days * INTERVAL '1 day'
                      AND is_deleted = false";

                var affected = await connection.ExecuteAsync(sql, new { Days = olderThanDays });
                
                if (affected > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old insights (dismissed > {Days} days ago)", affected, olderThanDays);
                }
                
                return affected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old insights");
                throw;
            }
        }

        /// <summary>
        /// Override CreateAsync to handle JSONB fields properly.
        /// </summary>
        public override async Task<Insight> CreateAsync(Insight entity)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO ai_insights (
                        id, organization_id, target_team_id, target_team_member_id,
                        insight_type, category, title, summary, unique_key,
                        details, priority, recommended_actions, source_entities,
                        valid_from, valid_until, is_dismissed, is_actioned, is_read,
                        created_at, updated_at
                    ) VALUES (
                        COALESCE(@Id, gen_random_uuid()), @OrganizationId, @TargetTeamId, @TargetTeamMemberId,
                        @InsightType, @Category, @Title, @Summary, @UniqueKey,
                        @Details::jsonb, @Priority, @RecommendedActions::jsonb, @SourceEntities::jsonb,
                        @ValidFrom, @ValidUntil, false, false, false,
                        NOW(), NOW()
                    ) RETURNING *";

                var result = await connection.QueryFirstOrDefaultAsync<Insight>(sql, new
                {
                    entity.Id,
                    entity.OrganizationId,
                    entity.TargetTeamId,
                    entity.TargetTeamMemberId,
                    entity.InsightType,
                    entity.Category,
                    entity.Title,
                    entity.Summary,
                    entity.UniqueKey,
                    Details = entity.Details != null ? System.Text.Json.JsonSerializer.Serialize(entity.Details) : null,
                    entity.Priority,
                    RecommendedActions = entity.RecommendedActions != null ? System.Text.Json.JsonSerializer.Serialize(entity.RecommendedActions) : null,
                    SourceEntities = entity.SourceEntities != null ? System.Text.Json.JsonSerializer.Serialize(entity.SourceEntities) : null,
                    entity.ValidFrom,
                    entity.ValidUntil
                });

                _logger.LogInformation("Created insight {Id}: {Title}", result!.Id, result.Title);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating insight: {Title}", entity.Title);
                throw;
            }
        }
    }
}
