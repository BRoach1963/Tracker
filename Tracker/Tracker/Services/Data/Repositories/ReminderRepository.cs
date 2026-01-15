using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Reminder entity.
    /// Provides data access for all reminder-related operations.
    /// 
    /// This is the ONLY place that queries the 'reminders' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Reminders are notifications that alert users about upcoming events/tasks.
    /// Uses polymorphic entity references: entity_type + entity_id.
    /// </summary>
    public interface IReminderRepository : IRepository<Reminder>
    {
        /// <summary>
        /// Get all reminders for a user.
        /// </summary>
        Task<IEnumerable<Reminder>> GetByUserAsync(Guid userId);

        /// <summary>
        /// Get pending (scheduled) reminders for a user.
        /// </summary>
        Task<IEnumerable<Reminder>> GetPendingByUserAsync(Guid userId);

        /// <summary>
        /// Get reminders due within a time window.
        /// </summary>
        Task<IEnumerable<Reminder>> GetDueRemindersAsync(DateTime from, DateTime to);

        /// <summary>
        /// Get reminders for a specific entity (meeting, task, goal, etc.).
        /// </summary>
        Task<IEnumerable<Reminder>> GetByEntityAsync(string entityType, Guid entityId);

        /// <summary>
        /// Get reminders for a specific team member.
        /// </summary>
        Task<IEnumerable<Reminder>> GetByTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Mark reminder as sent.
        /// </summary>
        Task<bool> MarkAsSentAsync(Guid id);

        /// <summary>
        /// Mark reminder as dismissed.
        /// </summary>
        Task<bool> DismissAsync(Guid id);

        /// <summary>
        /// Snooze reminder until a later time.
        /// </summary>
        Task<bool> SnoozeAsync(Guid id, DateTime snoozeUntil);

        /// <summary>
        /// Delete reminders for an entity (when entity is deleted).
        /// </summary>
        Task<int> DeleteByEntityAsync(string entityType, Guid entityId);
    }

    public class ReminderRepository : BaseRepository<Reminder>, IReminderRepository
    {
        public ReminderRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<ReminderRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "reminders";
        }

        public async Task<IEnumerable<Reminder>> GetByUserAsync(Guid userId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM reminders
                    WHERE user_id = @UserId
                    ORDER BY remind_at";

                return await connection.QueryAsync<Reminder>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Reminder>> GetPendingByUserAsync(Guid userId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM reminders
                    WHERE user_id = @UserId AND status = 'scheduled'
                    ORDER BY remind_at";

                return await connection.QueryAsync<Reminder>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending reminders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Reminder>> GetDueRemindersAsync(DateTime from, DateTime to)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM reminders
                    WHERE status = 'scheduled' 
                      AND remind_at >= @From AND remind_at <= @To
                    ORDER BY remind_at";

                return await connection.QueryAsync<Reminder>(sql, new { From = from, To = to });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting due reminders from {From} to {To}", from, to);
                throw;
            }
        }

        public async Task<IEnumerable<Reminder>> GetByEntityAsync(string entityType, Guid entityId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM reminders
                    WHERE entity_type = @EntityType AND entity_id = @EntityId
                    ORDER BY remind_at";

                return await connection.QueryAsync<Reminder>(sql, new { EntityType = entityType, EntityId = entityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for entity {EntityType}:{EntityId}", entityType, entityId);
                throw;
            }
        }

        public async Task<IEnumerable<Reminder>> GetByTeamMemberAsync(Guid teamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM reminders
                    WHERE team_member_id = @TeamMemberId
                    ORDER BY remind_at";

                return await connection.QueryAsync<Reminder>(sql, new { TeamMemberId = teamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reminders for team member {TeamMemberId}", teamMemberId);
                throw;
            }
        }

        public async Task<bool> MarkAsSentAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE reminders
                    SET status = 'sent', sent_at = now(), updated_at = now()
                    WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, new { Id = id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reminder {Id} as sent", id);
                throw;
            }
        }

        public async Task<bool> DismissAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE reminders
                    SET status = 'dismissed', dismissed_at = now(), updated_at = now()
                    WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, new { Id = id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing reminder {Id}", id);
                throw;
            }
        }

        public async Task<bool> SnoozeAsync(Guid id, DateTime snoozeUntil)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE reminders
                    SET status = 'snoozed', snoozed_until = @SnoozeUntil, updated_at = now()
                    WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, new { Id = id, SnoozeUntil = snoozeUntil });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error snoozing reminder {Id} until {SnoozeUntil}", id, snoozeUntil);
                throw;
            }
        }

        public async Task<int> DeleteByEntityAsync(string entityType, Guid entityId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    DELETE FROM reminders
                    WHERE entity_type = @EntityType AND entity_id = @EntityId";

                return await connection.ExecuteAsync(sql, new { EntityType = entityType, EntityId = entityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reminders for entity {EntityType}:{EntityId}", entityType, entityId);
                throw;
            }
        }
    }
}
