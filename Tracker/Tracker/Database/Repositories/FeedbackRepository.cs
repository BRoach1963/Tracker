using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Feedback data access operations.
    /// Handles all feedback records given to team members.
    /// </summary>
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of FeedbackRepository.
        /// </summary>
        public FeedbackRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(FeedbackRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all feedback for a specific team member.
        /// </summary>
        public async Task<List<Feedback>> GetFeedbackForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Feedback>();

            try
            {
                return await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => !f.IsDeleted && f.ToTeamMemberId == teamMemberId)
                    .Include(f => f.ToTeamMember)
                    .Include(f => f.FromTeamMember)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving feedback for team member {0}", teamMemberId);
                return new List<Feedback>();
            }
        }

        /// <summary>
        /// Gets all feedback for all team members.
        /// </summary>
        public async Task<List<Feedback>> GetAllFeedbackAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetAllFeedbackAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetAllFeedbackAsync: No context ===");
                return new List<Feedback>();
            }

            try
            {
                var result = await context.Feedbacks
                    .AsNoTracking()
                    .Where(f => !f.IsDeleted)
                    .Include(f => f.ToTeamMember)
                    .Include(f => f.FromTeamMember)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetAllFeedbackAsync: Query succeeded, got {result.Count} feedback records ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetAllFeedbackAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving all feedback");
                return new List<Feedback>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets a specific feedback record by ID.
        /// </summary>
        public async Task<Feedback?> GetFeedbackByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f => !f.IsDeleted)
                    .Include(f => f.ToTeamMember)
                    .Include(f => f.FromTeamMember)
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving feedback with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds new feedback.
        /// </summary>
        public async Task<Guid> AddFeedbackAsync(Feedback feedback)
        {
            if (_context == null)
            {
                _logger.Error("AddFeedbackAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                _logger.Info("Added feedback ID: {0}", feedback.Id);
                return feedback.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding feedback");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates existing feedback.
        /// </summary>
        public async Task<bool> UpdateFeedbackAsync(Feedback feedback)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Feedbacks.FindAsync(feedback.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateFeedbackAsync: Feedback ID {0} not found", feedback.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(feedback);
                await _context.SaveChangesAsync();
                _logger.Info("Updated feedback ID: {0}", feedback.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating feedback ID: {0}", feedback.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes feedback by ID.
        /// </summary>
        public async Task<bool> DeleteFeedbackAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback != null)
                {
                    _context.Feedbacks.Remove(feedback);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted feedback ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting feedback ID: {0}", id);
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
