using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using static Tracker.Common.Enums.MeetingType;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Meeting data access operations.
    /// Handles all CRUD operations for meetings of all types (1:1s, team meetings, all-hands, interviews, etc).
    /// </summary>
    public class MeetingRepository : IMeetingRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of MeetingRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public MeetingRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(MeetingRepository), "DatabaseLog");
        }

        /// <summary>
        /// Retrieves all meetings for the current user.
        /// </summary>
        public async Task<List<Meeting>> GetMeetingsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetMeetingsAsync: Starting ===");
            
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsAsync: No context ===");
                return new List<Meeting>();
            }

            try
            {
                var query = context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks)
                    .Include(m => m.AgendaItems)
                    .AsQueryable();

                var results = await query
                    .OrderByDescending(m => m.ScheduledAt)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var meeting in results)
                {
                    if (meeting.Tasks != null)
                        meeting.Tasks = meeting.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (meeting.AgendaItems != null)
                        meeting.AgendaItems = meeting.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsAsync: Query succeeded, got {results.Count} meetings ===");
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving meetings from database");
                return new List<Meeting>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves meetings of a specific type for the current user.
        /// If type is null, retrieves all meetings.
        /// </summary>
        public async Task<List<Meeting>> GetMeetingsByTypeAsync(MeetingType? type)
        {
            System.Diagnostics.Debug.WriteLine($"=== GetMeetingsByTypeAsync: Starting with type {type} ===");
            
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsByTypeAsync: No context ===");
                return new List<Meeting>();
            }

            try
            {
                var query = context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks)
                    .Include(m => m.AgendaItems)
                    .AsQueryable();

                // Filter by type if specified
                if (type.HasValue)
                {
                    query = query.Where(m => m.Type == type.Value);
                }

                var results = await query
                    .OrderByDescending(m => m.ScheduledAt)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var meeting in results)
                {
                    if (meeting.Tasks != null)
                        meeting.Tasks = meeting.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (meeting.AgendaItems != null)
                        meeting.AgendaItems = meeting.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsByTypeAsync: Query succeeded, got {results.Count} meetings of type {type} ===");
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMeetingsByTypeAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving meetings of type {0} from database", type);
                return new List<Meeting>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves a specific meeting by ID.
        /// </summary>
        public async Task<Meeting?> GetMeetingByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks.Where(t => !t.IsDeleted))
                    .Include(m => m.AgendaItems.Where(a => !a.IsDeleted))
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new meeting.
        /// </summary>
        public async Task<Guid> AddMeetingAsync(Meeting meeting, Guid? teamMemberId = null)
        {
            if (_context == null)
            {
                _logger.Error("AddMeetingAsync: _context is null");
                return Guid.Empty;
            }

            _logger.Info("AddMeetingAsync: Starting with UserId={0}, TeamMemberId={1}", _userId, teamMemberId);

            try
            {
                // Ensure creator is set to current user
                meeting.CreatedByUserId = _userId;
                
                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();
                _logger.Info("Added meeting ID: {0}", meeting.Id);
                return meeting.Id;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $" Inner: {ex.InnerException.Message}" : "";
                _logger.Exception(ex, "Error adding meeting.{0}", innerMsg);
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing meeting.
        /// </summary>
        public async Task<bool> UpdateMeetingAsync(Meeting meeting)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Meetings.FindAsync(meeting.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateMeetingAsync: Meeting ID {0} not found", meeting.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(meeting);
                await _context.SaveChangesAsync();
                _logger.Info("Updated meeting ID: {0}", meeting.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating meeting ID: {0}", meeting.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a meeting by ID.
        /// </summary>
        public async Task<bool> DeleteMeetingAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var meeting = await _context.Meetings.FindAsync(id);
                if (meeting != null)
                {
                    _context.Meetings.Remove(meeting);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted meeting ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting meeting ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets the most recent meeting for a specific team member (excluding the current meeting if provided).
        /// Used to show previous meeting summary and rollover uncompleted items.
        /// </summary>
        public async Task<Meeting?> GetPreviousMeetingAsync(Guid teamMemberId, Guid? excludeMeetingId = null)
        {
            if (_context == null) return null;

            try
            {
                var query = _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.ReportTeamMemberId == teamMemberId && m.Type == MeetingType.OneOnOne);

                if (excludeMeetingId.HasValue)
                {
                    query = query.Where(m => m.Id != excludeMeetingId.Value);
                }

                return await query
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks.Where(t => !t.IsDeleted))
                    .Include(m => m.AgendaItems.Where(a => !a.IsDeleted))
                    .OrderByDescending(m => m.ScheduledAt)
                    .ThenByDescending(m => m.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving previous meeting for team member {0}", teamMemberId);
                return null;
            }
        }

        /// <summary>
        /// Gets all meetings for a specific team member.
        /// Used to show meeting history in the team member view.
        /// </summary>
        public async Task<List<Meeting>> GetMeetingsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Meeting>();

            try
            {
                var results = await _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.ReportTeamMemberId == teamMemberId && m.Type == MeetingType.OneOnOne)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks)
                    .Include(m => m.AgendaItems)
                    .OrderByDescending(m => m.ScheduledAt)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var meeting in results)
                {
                    if (meeting.Tasks != null)
                        meeting.Tasks = meeting.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (meeting.AgendaItems != null)
                        meeting.AgendaItems = meeting.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meetings for team member {0}", teamMemberId);
                return new List<Meeting>();
            }
        }

        /// <summary>
        /// Gets all meetings within a date range.
        /// </summary>
        public async Task<List<Meeting>> GetMeetingsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (_context == null) return new List<Meeting>();

            try
            {
                var results = await _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId &&
                                m.ScheduledAt >= startDate && m.ScheduledAt <= endDate)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks)
                    .Include(m => m.AgendaItems)
                    .OrderBy(m => m.ScheduledAt)
                    .ToListAsync();
                
                // Filter deleted items in memory to avoid SQL translation issues
                foreach (var meeting in results)
                {
                    if (meeting.Tasks != null)
                        meeting.Tasks = meeting.Tasks.Where(t => !t.IsDeleted).ToList();
                    if (meeting.AgendaItems != null)
                        meeting.AgendaItems = meeting.AgendaItems.Where(a => !a.IsDeleted).ToList();
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meetings in range {0} to {1}", startDate, endDate);
                return new List<Meeting>();
            }
        }

        /// <summary>
        /// Gets all completed meetings for a specific team member.
        /// </summary>
        public async Task<List<Meeting>> GetCompletedMeetingsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Meeting>();

            try
            {
                var results = await _context.Meetings
                    .Where(m => !m.IsDeleted && 
                                m.CreatedByUserId == _userId &&
                                m.ReportTeamMemberId == teamMemberId &&
                                m.Status == MeetingStatus.Completed)
                    .Include(m => m.Manager)
                    .Include(m => m.Report)
                    .Include(m => m.Tasks)
                    .OrderByDescending(m => m.ScheduledAt)
                    .ToListAsync();
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving completed meetings for team member {0}", teamMemberId);
                return new List<Meeting>();
            }
        }

        /// <summary>
        /// Saves calendar link data for a meeting.
        /// </summary>
        public async Task SaveCalendarLinkAsync(CalendarLink link)
        {
            if (_context == null || link == null) return;

            try
            {
                _context.CalendarLinks.Add(link);
                await _context.SaveChangesAsync();
                _logger.Info("Saved calendar link for meeting");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving calendar link");
            }
        }

        /// <summary>
        /// Deletes the calendar link for a meeting.
        /// </summary>
        public async Task DeleteCalendarLinkAsync(Guid meetingId, string provider)
        {
            if (_context == null) return;

            try
            {
                // CalendarLink no longer links individual meetings; method retained for backwards compatibility.
                _logger.Debug("DeleteCalendarLinkAsync called for meeting {0}, provider {1} - no-op with new CalendarLink model", meetingId, provider);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting calendar link for meeting {0}", meetingId);
            }
        }

        /// <summary>
        /// Finds a meeting by external calendar event ID.
        /// </summary>
        public async Task<Meeting?> FindMeetingByCalendarEventIdAsync(string provider, string externalEventId)
        {
            if (_context == null || string.IsNullOrEmpty(externalEventId)) return null;

            try
            {
                // With the new schema, meetings track their own external IDs
                // (GoogleCalendarEventId / OutlookCalendarEventId). CalendarLink
                // no longer stores per-meeting mappings, so this lookup is not
                // supported. Callers should migrate to querying Meeting directly.
                _logger.Debug("FindMeetingByCalendarEventIdAsync is not supported with the new calendar schema (provider={0})", provider);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error finding meeting by calendar event ID");
                return null;
            }
        }

        /// <summary>
        /// Updates meeting sync data from external calendar.
        /// </summary>
        public async Task UpdateMeetingSyncDataAsync(Guid meetingId, string? externalEventId, string? externalEtag, string? syncStatus)
        {
            if (_context == null) return;

            try
            {
                var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId);
                if (meeting != null)
                {
                    // For Outlook, store the external event id on the meeting
                    meeting.OutlookCalendarEventId = externalEventId;
                    meeting.SyncStatus = syncStatus ?? meeting.SyncStatus;
                    meeting.LastSyncedAt = DateTime.UtcNow;
                    
                    _context.Meetings.Update(meeting);
                    await _context.SaveChangesAsync();
                    _logger.Info("Updated sync data for meeting {0}", meetingId);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating meeting sync data for meeting {0}", meetingId);
            }
        }

        /// <summary>
        /// Links a tracker task to a meeting.
        /// </summary>
        public async Task<bool> LinkTaskToMeetingAsync(Guid meetingId, Guid taskId)
        {
            if (_context == null) return false;

            try
            {
                // Verify Meeting belongs to current user
                var meeting = await _context.Meetings
                    .Where(m => m.Id == meetingId && m.CreatedByUserId == _userId)
                    .FirstOrDefaultAsync();
                
                if (meeting == null)
                {
                    _logger.Warn("Cannot link task {0} to meeting {1} - meeting not found or doesn't belong to current user", taskId, meetingId);
                    return false;
                }

                // Add task to meeting's tasks collection
                var task = await _context.TrackerTasks.FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                {
                    _logger.Warn("Cannot link task {0} - task not found", taskId);
                    return false;
                }

                // Check if task is already in meeting
                if (meeting.Tasks == null)
                    meeting.Tasks = new List<TrackerTask>();
                
                if (!meeting.Tasks.Any(t => t.Id == taskId))
                {
                    meeting.Tasks.Add(task);
                    await _context.SaveChangesAsync();
                    _logger.Info("Linked task {0} to meeting {1}", taskId, meetingId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking task {0} to meeting {1}", taskId, meetingId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks a tracker task from a meeting.
        /// </summary>
        public async Task<bool> UnlinkTaskFromMeetingAsync(Guid meetingId, Guid taskId)
        {
            if (_context == null) return false;

            try
            {
                var meeting = await _context.Meetings
                    .Include(m => m.Tasks)
                    .FirstOrDefaultAsync(m => m.Id == meetingId && m.CreatedByUserId == _userId);

                if (meeting?.Tasks != null)
                {
                    var task = meeting.Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task != null)
                    {
                        meeting.Tasks.Remove(task);
                        await _context.SaveChangesAsync();
                        _logger.Info("Unlinked task {0} from meeting {1}", taskId, meetingId);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking task {0} from meeting {1}", taskId, meetingId);
                return false;
            }
        }

        /// <summary>
        /// Gets the count of meetings associated with a tracker task.
        /// </summary>
        public async Task<int> GetTaskMeetingCountAsync(Guid taskId)
        {
            if (_context == null) return 0;

            try
            {
                return await _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId &&
                                m.Tasks.Any(t => t.Id == taskId))
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for task {0}", taskId);
                return 0;
            }
        }

        /// <summary>
        /// Disposes the context if it was created by the factory.
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext context)
        {
            // Only dispose if it came from the factory and not the primary context
            if (context != _context && context is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
