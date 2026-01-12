using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for QuickNote data access operations.
    /// Handles quick notes attached to team members.
    /// </summary>
    public class QuickNoteRepository : IQuickNoteRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of QuickNoteRepository.
        /// </summary>
        public QuickNoteRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(QuickNoteRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all quick notes for the current user (excluding archived by default).
        /// </summary>
        public async Task<List<QuickNote>> GetQuickNotesAsync(bool includeArchived = false)
        {
            if (_context == null) return new List<QuickNote>();

            try
            {
                var baseQuery = _context.QuickNotes
                    .Where(n => !n.IsDeleted);

                if (!includeArchived)
                {
                    baseQuery = baseQuery.Where(n => !n.IsArchived);
                }

                return await baseQuery
                    .AsNoTracking()
                    .Include(n => n.TeamMember)
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving quick notes");
                return new List<QuickNote>();
            }
        }

        /// <summary>
        /// Gets quick notes for a specific team member.
        /// </summary>
        public async Task<List<QuickNote>> GetQuickNotesForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<QuickNote>();

            try
            {
                return await _context.QuickNotes
                    .AsNoTracking()
                    .Where(n => !n.IsDeleted &&
                                !n.IsArchived &&
                                n.TeamMemberId == teamMemberId)
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving quick notes for team member");
                return new List<QuickNote>();
            }
        }

        /// <summary>
        /// Gets a specific quick note by ID.
        /// </summary>
        public async Task<QuickNote?> GetQuickNoteByIdAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.QuickNotes
                    .AsNoTracking()
                    .Where(n => !n.IsDeleted)
                    .Include(n => n.TeamMember)
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving quick note ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new quick note.
        /// </summary>
        public async Task<int> AddQuickNoteAsync(QuickNote note)
        {
            if (_context == null)
            {
                _logger.Error("AddQuickNoteAsync: _context is null");
                return 0;
            }

            try
            {
                _context.QuickNotes.Add(note);
                await _context.SaveChangesAsync();
                _logger.Info("Added quick note ID: {0}", note.Id);
                return note.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding quick note");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing quick note.
        /// </summary>
        public async Task<bool> UpdateQuickNoteAsync(QuickNote note)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.QuickNotes.FindAsync(note.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateQuickNoteAsync: QuickNote ID {0} not found", note.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(note);
                await _context.SaveChangesAsync();
                _logger.Info("Updated quick note ID: {0}", note.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating quick note ID: {0}", note.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a quick note.
        /// </summary>
        public async Task<bool> DeleteQuickNoteAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    _context.QuickNotes.Remove(note);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted quick note ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting quick note ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Toggles the pinned status of a note.
        /// </summary>
        public async Task<bool> ToggleNotePinnedAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    note.IsPinned = !note.IsPinned;
                    await _context.SaveChangesAsync();
                    _logger.Info("Toggled pin status for quick note ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error toggling note pin status");
                return false;
            }
        }

        /// <summary>
        /// Archives a note (soft delete with IsArchived flag).
        /// </summary>
        public async Task<bool> ArchiveNoteAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var note = await _context.QuickNotes.FindAsync(id);
                if (note != null)
                {
                    note.IsArchived = true;
                    await _context.SaveChangesAsync();
                    _logger.Info("Archived quick note ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error archiving quick note");
                return false;
            }
        }

        /// <summary>
        /// Disposes the context if it was created by the factory.
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext context)
        {
            if (context != _context && context is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
