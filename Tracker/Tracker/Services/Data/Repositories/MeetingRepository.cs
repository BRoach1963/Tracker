using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for Meeting entity.
    /// Provides data access for all meeting-related operations.
    /// 
    /// This is the ONLY place that queries the 'meetings' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Meetings are one-on-ones, team meetings, and syncs between users.
    /// They have date range queries, status filtering, and attendee lookups.
    /// </summary>
    public interface IMeetingRepository : IRepository<Meeting>
    {
        /// <summary>
        /// Get all meetings for a specific user (as organizer or participant).
        /// </summary>
        Task<IEnumerable<Meeting>> GetByUserAsync(Guid userId);

        /// <summary>
        /// Get meetings in a specific date range.
        /// </summary>
        Task<IEnumerable<Meeting>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get upcoming meetings for a user (not deleted, scheduled in future).
        /// </summary>
        Task<IEnumerable<Meeting>> GetUpcomingByUserAsync(Guid userId, DateTime fromDate);

        /// <summary>
        /// Get past meetings for a user (completed or date has passed).
        /// </summary>
        Task<IEnumerable<Meeting>> GetPastByUserAsync(Guid userId, DateTime upToDate);

        /// <summary>
        /// Get meetings by status (scheduled, completed, cancelled, etc.).
        /// </summary>
        Task<IEnumerable<Meeting>> GetByStatusAsync(string status);

        /// <summary>
        /// Get meetings organized by a specific user.
        /// </summary>
        Task<IEnumerable<Meeting>> GetByOrganizerAsync(Guid organizerId);

        /// <summary>
        /// Get meetings for a specific one-on-one relationship.
        /// </summary>
        Task<IEnumerable<Meeting>> GetByOneOnOneAsync(Guid oneOnOneId);

        /// <summary>
        /// Get all attendees of a meeting (via meeting_attendees junction table).
        /// </summary>
        Task<IEnumerable<Guid>> GetAttendeeIdsAsync(Guid meetingId);

        /// <summary>
        /// Count meetings in organization for a date range (useful for analytics).
        /// </summary>
        Task<int> CountByOrganizationInDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate);
    }

    public class MeetingRepository : BaseRepository<Meeting>, IMeetingRepository
    {
        public MeetingRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<MeetingRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "meetings";
        }

        public async Task<IEnumerable<Meeting>> GetByUserAsync(Guid userId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT DISTINCT m.* FROM meetings m
                    LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
                    WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
                      AND m.is_deleted = false
                    ORDER BY m.scheduled_at DESC";

                return await connection.QueryAsync<Meeting>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings by user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meetings
                    WHERE scheduled_at >= @StartDate 
                      AND scheduled_at <= @EndDate
                      AND is_deleted = false
                    ORDER BY scheduled_at";

                return await connection.QueryAsync<Meeting>(sql, 
                    new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings by date range");
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetUpcomingByUserAsync(Guid userId, DateTime fromDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT DISTINCT m.* FROM meetings m
                    LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
                    WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
                      AND m.is_deleted = false
                      AND m.scheduled_at >= @FromDate
                    ORDER BY m.scheduled_at ASC";

                return await connection.QueryAsync<Meeting>(sql, 
                    new { UserId = userId, FromDate = fromDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming meetings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetPastByUserAsync(Guid userId, DateTime upToDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT DISTINCT m.* FROM meetings m
                    LEFT JOIN meeting_attendees ma ON m.id = ma.meeting_id
                    WHERE (m.organizer_id = @UserId OR ma.user_id = @UserId)
                      AND m.is_deleted = false
                      AND m.scheduled_at <= @UpToDate
                    ORDER BY m.scheduled_at DESC";

                return await connection.QueryAsync<Meeting>(sql, 
                    new { UserId = userId, UpToDate = upToDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting past meetings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetByStatusAsync(string status)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meetings
                    WHERE status = @Status AND is_deleted = false
                    ORDER BY scheduled_at DESC";

                return await connection.QueryAsync<Meeting>(sql, new { Status = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetByOrganizerAsync(Guid organizerId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meetings
                    WHERE organizer_id = @OrganizerId AND is_deleted = false
                    ORDER BY scheduled_at DESC";

                return await connection.QueryAsync<Meeting>(sql, new { OrganizerId = organizerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings by organizer {OrganizerId}", organizerId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetByOneOnOneAsync(Guid oneOnOneId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM meetings
                    WHERE one_on_one_id = @OneOnOneId AND is_deleted = false
                    ORDER BY scheduled_at DESC";

                return await connection.QueryAsync<Meeting>(sql, new { OneOnOneId = oneOnOneId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings by one-on-one {OneOnOneId}", oneOnOneId);
                throw;
            }
        }

        public async Task<IEnumerable<Guid>> GetAttendeeIdsAsync(Guid meetingId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT user_id FROM meeting_attendees
                    WHERE meeting_id = @MeetingId";

                return await connection.QueryAsync<Guid>(sql, new { MeetingId = meetingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendees for meeting {MeetingId}", meetingId);
                throw;
            }
        }

        public async Task<int> CountByOrganizationInDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM meetings m
                    INNER JOIN users u ON m.organizer_id = u.id
                    WHERE u.organization_id = @OrgId
                      AND m.scheduled_at >= @StartDate
                      AND m.scheduled_at <= @EndDate
                      AND m.is_deleted = false";

                return await connection.QueryFirstAsync<int>(sql, 
                    new { OrgId = organizationId, StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting meetings in organization {OrgId}", organizationId);
                throw;
            }
        }
    }
}
