using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for Feedback entity.
    /// Provides data access for all feedback-related operations.
    /// 
    /// This is the ONLY place that queries the 'feedback' table.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// Feedback represents structured feedback given during 1:1s, reviews, or surveys.
    /// </summary>
    public interface IFeedbackRepository : IRepository<Feedback>
    {
        /// <summary>
        /// Get all feedback given to a specific person.
        /// </summary>
        Task<IEnumerable<Feedback>> GetByRecipientAsync(Guid recipientId);

        /// <summary>
        /// Get all feedback given by a specific person.
        /// </summary>
        Task<IEnumerable<Feedback>> GetByGiverAsync(Guid giverId);

        /// <summary>
        /// Get feedback for a specific meeting.
        /// </summary>
        Task<IEnumerable<Feedback>> GetByMeetingAsync(Guid meetingId);

        /// <summary>
        /// Get feedback in a date range.
        /// </summary>
        Task<IEnumerable<Feedback>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get feedback for a specific team member.
        /// </summary>
        Task<IEnumerable<Feedback>> GetFeedbackForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Delete feedback (soft delete).
        /// </summary>
        Task DeleteFeedbackAsync(Guid feedbackId);
    }

    public class FeedbackRepository : BaseRepository<Feedback>, IFeedbackRepository
    {
        public FeedbackRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<FeedbackRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "feedback";
        }

        public async Task<IEnumerable<Feedback>> GetByRecipientAsync(Guid recipientId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM feedback
                    WHERE recipient_id = @RecipientId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Feedback>(sql, new { RecipientId = recipientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback by recipient {RecipientId}", recipientId);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetByGiverAsync(Guid giverId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM feedback
                    WHERE giver_id = @GiverId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Feedback>(sql, new { GiverId = giverId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback by giver {GiverId}", giverId);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetByMeetingAsync(Guid meetingId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM feedback
                    WHERE meeting_id = @MeetingId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Feedback>(sql, new { MeetingId = meetingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback by meeting {MeetingId}", meetingId);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM feedback
                    WHERE created_at >= @StartDate 
                      AND created_at <= @EndDate
                      AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Feedback>(sql, 
                    new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback by date range");
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackForTeamMemberAsync(Guid teamMemberId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM feedback
                    WHERE team_member_id = @TeamMemberId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<Feedback>(sql, new { TeamMemberId = teamMemberId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback for team member {TeamMemberId}", teamMemberId);
                throw;
            }
        }

        public async Task DeleteFeedbackAsync(Guid feedbackId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE feedback SET
                        is_deleted = true,
                        deleted_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = feedbackId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback {FeedbackId}", feedbackId);
                throw;
            }
        }
    }
}
