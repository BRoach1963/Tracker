using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for QuickNote entity.
    /// Provides data access for all quick note-related operations.
    /// 
    /// This is the ONLY place that queries the 'quick_notes' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Quick notes are brief notes captured during or after meetings.
    /// </summary>
    public interface IQuickNoteRepository : IRepository<QuickNote>
    {
        /// <summary>
        /// Get all quick notes created by a user.
        /// </summary>
        Task<IEnumerable<QuickNote>> GetByCreatorAsync(Guid creatorId);

        /// <summary>
        /// Get quick notes for a specific meeting.
        /// </summary>
        Task<IEnumerable<QuickNote>> GetByMeetingAsync(Guid meetingId);

        /// <summary>
        /// Get quick notes by date range.
        /// </summary>
        Task<IEnumerable<QuickNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }

    public class QuickNoteRepository : BaseRepository<QuickNote>, IQuickNoteRepository
    {
        public QuickNoteRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<QuickNoteRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "quick_notes";
        }

        public async Task<IEnumerable<QuickNote>> GetByCreatorAsync(Guid creatorId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM quick_notes
                    WHERE created_by = @CreatorId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<QuickNote>(sql, new { CreatorId = creatorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick notes by creator {CreatorId}", creatorId);
                throw;
            }
        }

        public async Task<IEnumerable<QuickNote>> GetByMeetingAsync(Guid meetingId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM quick_notes
                    WHERE meeting_id = @MeetingId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<QuickNote>(sql, new { MeetingId = meetingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick notes by meeting {MeetingId}", meetingId);
                throw;
            }
        }

        public async Task<IEnumerable<QuickNote>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM quick_notes
                    WHERE created_at >= @StartDate 
                      AND created_at <= @EndDate
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<QuickNote>(sql, 
                    new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quick notes by date range");
                throw;
            }
        }
    }
}
