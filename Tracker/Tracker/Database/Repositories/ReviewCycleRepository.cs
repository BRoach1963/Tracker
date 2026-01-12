using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for PerformanceReviewCycle data access operations.
    /// Handles performance review cycles (evaluation periods).
    /// </summary>
    public class ReviewCycleRepository : IReviewCycleRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of ReviewCycleRepository.
        /// </summary>
        public ReviewCycleRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(ReviewCycleRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all review cycles for the current user.
        /// </summary>
        public async Task<List<PerformanceReviewCycle>> GetReviewCyclesAsync()
        {
            if (_context == null) return new List<PerformanceReviewCycle>();

            try
            {
                return await _context.PerformanceReviewCycles
                    .AsNoTracking()
                    .Include(c => c.ReviewTemplate)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.TeamMember)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review cycles");
                return new List<PerformanceReviewCycle>();
            }
        }

        /// <summary>
        /// Gets a review cycle by ID with all related data.
        /// </summary>
        public async Task<PerformanceReviewCycle?> GetReviewCycleByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PerformanceReviewCycles
                    .AsNoTracking()
                    .Include(c => c.ReviewTemplate)
                        .ThenInclude(t => t.Sections)
                            .ThenInclude(s => s.Questions)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.TeamMember)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.Sections)
                            .ThenInclude(s => s.Answers)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review cycle ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new review cycle.
        /// </summary>
        public async Task<Guid> AddReviewCycleAsync(PerformanceReviewCycle cycle)
        {
            if (_context == null)
            {
                _logger.Error("AddReviewCycleAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.PerformanceReviewCycles.Add(cycle);
                await _context.SaveChangesAsync();
                _logger.Info("Added review cycle ID: {0}", cycle.Id);
                return cycle.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding review cycle");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing review cycle.
        /// </summary>
        public async Task<bool> UpdateReviewCycleAsync(PerformanceReviewCycle cycle)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PerformanceReviewCycles.FindAsync(cycle.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReviewCycleAsync: Cycle ID {0} not found", cycle.Id);
                    return false;
                }

                existing.Name = cycle.Name;
                existing.Description = cycle.Description;
                existing.Status = cycle.Status;
                existing.SelfReviewStartDate = cycle.SelfReviewStartDate;
                existing.SelfReviewDueDate = cycle.SelfReviewDueDate;
                existing.ManagerReviewStartDate = cycle.ManagerReviewStartDate;
                existing.ManagerReviewDueDate = cycle.ManagerReviewDueDate;
                existing.CalibrationDate = cycle.CalibrationDate;
                existing.ShareDate = cycle.ShareDate;

                await _context.SaveChangesAsync();
                _logger.Info("Updated review cycle ID: {0}", cycle.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating review cycle ID: {0}", cycle.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a review cycle.
        /// </summary>
        public async Task<bool> DeleteReviewCycleAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var cycle = await _context.PerformanceReviewCycles.FindAsync(id);
                if (cycle != null)
                {
                    _context.PerformanceReviewCycles.Remove(cycle);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted review cycle ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting review cycle ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets the active (current) review cycle.
        /// </summary>
        public async Task<PerformanceReviewCycle?> GetActiveReviewCycleAsync()
        {
            if (_context == null) return null;

            try
            {
                var now = DateTime.Now;
                return await _context.PerformanceReviewCycles
                    .AsNoTracking()
                    .Include(c => c.ReviewTemplate)
                    .Include(c => c.Reviews)
                        .ThenInclude(r => r.TeamMember)
                    .FirstOrDefaultAsync(c => (c.Status == ReviewCycleStatus.SelfReviewInProgress || c.Status == ReviewCycleStatus.ManagerReviewInProgress) &&
                                             c.SelfReviewStartDate <= now &&
                                             now <= c.ShareDate);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving active review cycle");
                return null;
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
