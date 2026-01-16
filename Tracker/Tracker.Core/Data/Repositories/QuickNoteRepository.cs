using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
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

        /// <summary>
        /// Add a new quick note.
        /// </summary>
        Task<Guid> AddQuickNoteAsync(QuickNote note);

        /// <summary>
        /// Update an existing quick note.
        /// </summary>
        Task UpdateQuickNoteAsync(QuickNote note);

        /// <summary>
        /// Delete a quick note (soft delete).
        /// </summary>
        Task DeleteQuickNoteAsync(Guid noteId);

        /// <summary>
        /// Toggle the pinned status of a note.
        /// </summary>
        Task ToggleNotePinnedAsync(Guid noteId);

        /// <summary>
        /// Archive a note.
        /// </summary>
        Task ArchiveNoteAsync(Guid noteId);

        /// <summary>
        /// Get all quick notes.
        /// </summary>
        Task<IEnumerable<QuickNote>> GetQuickNotesAsync();
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

        public async Task<Guid> AddQuickNoteAsync(QuickNote note)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO quick_notes (id, title, content, meeting_id, team_member_id, 
                        is_pinned, is_archived, created_by, created_at)
                    VALUES (@Id, @Title, @Content, @MeetingId, @TeamMemberId,
                        @IsPinned, @IsArchived, @CreatedBy, NOW())
                    RETURNING id";

                if (note.Id == Guid.Empty)
                    note.Id = Guid.NewGuid();

                return await connection.QueryFirstAsync<Guid>(sql, note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding quick note");
                throw;
            }
        }

        public async Task UpdateQuickNoteAsync(QuickNote note)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE quick_notes SET
                        title = @Title,
                        content = @Content,
                        is_pinned = @IsPinned,
                        is_archived = @IsArchived,
                        updated_at = NOW()
                    WHERE id = @Id AND is_deleted = false";

                await connection.ExecuteAsync(sql, note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quick note {NoteId}", note.Id);
                throw;
            }
        }

        public async Task DeleteQuickNoteAsync(Guid noteId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE quick_notes SET
                        is_deleted = true,
                        deleted_at = NOW()
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, new { Id = noteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quick note {NoteId}", noteId);
                throw;
            }
        }

        public async Task ToggleNotePinnedAsync(Guid noteId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE quick_notes SET
                        is_pinned = NOT is_pinned,
                        updated_at = NOW()
                    WHERE id = @Id AND is_deleted = false";

                await connection.ExecuteAsync(sql, new { Id = noteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling pinned status for note {NoteId}", noteId);
                throw;
            }
        }

        public async Task ArchiveNoteAsync(Guid noteId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE quick_notes SET
                        is_archived = true,
                        updated_at = NOW()
                    WHERE id = @Id AND is_deleted = false";

                await connection.ExecuteAsync(sql, new { Id = noteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving note {NoteId}", noteId);
                throw;
            }
        }

        public async Task<IEnumerable<QuickNote>> GetQuickNotesAsync()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM quick_notes
                    WHERE is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<QuickNote>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all quick notes");
                throw;
            }
        }
    }
}
