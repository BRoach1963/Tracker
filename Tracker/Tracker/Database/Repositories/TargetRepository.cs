using Microsoft.EntityFrameworkCore;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Target data access operations.
    /// Handles all CRUD operations for targets (Key Results), measurables, and progress tracking.
    /// </summary>
    public class TargetRepository : ITargetRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of TargetRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public TargetRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(TargetRepository), "DatabaseLog");
        }

        /// <summary>
        /// Retrieves all targets for the current user.
        /// </summary>
        public async Task<List<Target>> GetTargetsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetTargetsAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTargetsAsync: No context ===");
                return new List<Target>();
            }

            try
            {
                var result = await context.Targets
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Goal)
                    .Include(t => t.Measurables)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetTargetsAsync: Query succeeded, got {result.Count} targets ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTargetsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving targets from database");
                return new List<Target>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves a specific target by ID.
        /// </summary>
        public async Task<Target?> GetTargetByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Targets
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Goal)
                    .Include(t => t.Measurables)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving target with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new target.
        /// </summary>
        public async Task<Guid> AddTargetAsync(Target target)
        {
            if (_context == null)
            {
                _logger.Error("AddTargetAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.Targets.Add(target);
                await _context.SaveChangesAsync();
                return target.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding target");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing target.
        /// </summary>
        public async Task<bool> UpdateTargetAsync(Target target)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Targets.FindAsync(target.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateTargetAsync: Target ID {0} not found", target.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(target);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating target");
                return false;
            }
        }

        /// <summary>
        /// Deletes a target by ID.
        /// </summary>
        public async Task<bool> DeleteTargetAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var target = await _context.Targets.FindAsync(id);
                if (target != null)
                {
                    _context.Targets.Remove(target);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted target ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting target ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets targets for a specific goal.
        /// </summary>
        public async Task<List<Target>> GetGoalTargetsAsync(Guid goalId)
        {
            if (_context == null) return new List<Target>();

            try
            {
                return await _context.Targets
                    .Where(t => !t.IsDeleted && t.GoalId == goalId)
                    .Include(t => t.Goal)
                    .Include(t => t.Measurables)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving targets for goal {0}", goalId);
                return new List<Target>();
            }
        }

        /// <summary>
        /// Gets targets with a specific status.
        /// If status is null, retrieves all targets.
        /// </summary>
        public async Task<List<Target>> GetTargetsByStatusAsync(OkrStatus? status)
        {
            if (_context == null) return new List<Target>();

            try
            {
                var query = _context.Targets
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Goal)
                    .Include(t => t.Measurables)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(t => t.Status == status.Value);
                }

                return await query
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving targets with status {0}", status);
                return new List<Target>();
            }
        }

        /// <summary>
        /// Gets measurables (data sources) for a target.
        /// </summary>
        public async Task<List<TargetMeasurable>> GetTargetMeasurablesAsync(Guid targetId)
        {
            if (_context == null) return new List<TargetMeasurable>();

            try
            {
                return await _context.TargetMeasurables
                    .Where(tm => !tm.IsDeleted && tm.TargetId == targetId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving measurables for target {0}", targetId);
                return new List<TargetMeasurable>();
            }
        }

        /// <summary>
        /// Links a measurable (metric/task collection) to a target.
        /// </summary>
        public async Task<bool> LinkMeasurableToTargetAsync(Guid targetId, Guid measurableId, string measurableType, decimal weight = 1.0m)
        {
            if (_context == null) return false;

            try
            {
                // Verify target exists
                var target = await _context.Targets
                    .Where(t => t.Id == targetId)
                    .FirstOrDefaultAsync();
                
                if (target == null)
                {
                    _logger.Warn("Cannot link measurable {0} to target {1} - target not found", measurableId, targetId);
                    return false;
                }

                // Check if link already exists
                var existing = await _context.TargetMeasurables
                    .FirstOrDefaultAsync(tm => tm.TargetId == targetId && tm.MeasurableId == measurableId && !tm.IsDeleted);

                if (existing != null)
                {
                    // Update existing link - just mark as updated
                    _context.TargetMeasurables.Update(existing);
                }
                else
                {
                    // Create new link
                    var measurable = new TargetMeasurable
                    {
                        Id = Guid.NewGuid(),
                        TargetId = targetId,
                        MeasurableId = measurableId,
                        MeasurableType = measurableType
                    };
                    _context.TargetMeasurables.Add(measurable);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Linked measurable {0} to target {1}", measurableId, targetId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking measurable {0} to target {1}", measurableId, targetId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks a measurable from a target.
        /// </summary>
        public async Task<bool> UnlinkMeasurableFromTargetAsync(Guid targetMeasurableId)
        {
            if (_context == null) return false;

            try
            {
                var measurable = await _context.TargetMeasurables.FindAsync(targetMeasurableId);
                if (measurable != null)
                {
                    _context.TargetMeasurables.Remove(measurable);
                    await _context.SaveChangesAsync();
                    _logger.Info("Unlinked measurable from target");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking measurable {0}", targetMeasurableId);
                return false;
            }
        }

        /// <summary>
        /// Gets targets by progress status (on-track, at-risk, off-track).
        /// </summary>
        public async Task<List<Target>> GetTargetsByProgressAsync(string progressStatus)
        {
            if (_context == null || string.IsNullOrEmpty(progressStatus)) 
                return new List<Target>();

            try
            {
                var query = _context.Targets
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Goal)
                    .Include(t => t.Measurables)
                    .AsQueryable();

                // Filter by progress status based on progress percentage
                // This is a simplified implementation; adjust thresholds as needed
                query = progressStatus.ToLower() switch
                {
                    "on-track" => query.Where(t => t.Progress >= 80),
                    "at-risk" => query.Where(t => t.Progress >= 50 && t.Progress < 80),
                    "off-track" => query.Where(t => t.Progress < 50),
                    _ => query
                };

                return await query
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving targets by progress {0}", progressStatus);
                return new List<Target>();
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
