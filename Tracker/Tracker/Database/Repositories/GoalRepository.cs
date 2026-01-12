using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Goal data access operations.
    /// Goals represent strategic objectives (Organizational, Team, or Personal) with linked Targets.
    /// </summary>
    public class GoalRepository : IGoalRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of GoalRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public GoalRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(GoalRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all goals for the current user.
        /// Goals are sorted by type (Organizational first) then by end date.
        /// </summary>
        public async Task<List<Goal>> GetGoalsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetGoalsAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetGoalsAsync: No context ===");
                return new List<Goal>();
            }

            try
            {
                var result = await context.Goals
                    .AsNoTracking()
                    .Where(g => !g.IsDeleted)
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.Owner)
                    .OrderByDescending(g => g.Type == GoalType.Organizational)
                    .ThenBy(g => g.EndDate)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetGoalsAsync: Query succeeded, got {result.Count} goals ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetGoalsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving goals from database");
                return new List<Goal>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets a specific goal by ID.
        /// </summary>
        public async Task<Goal?> GetGoalByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Goals
                    .Where(g => !g.IsDeleted)
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.Owner)
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving goal with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Gets goals filtered by type.
        /// If type is null, returns all goals.
        /// </summary>
        public async Task<List<Goal>> GetGoalsByTypeAsync(GoalType? type)
        {
            if (_context == null) return new List<Goal>();

            try
            {
                var query = _context.Goals
                    .Where(g => !g.IsDeleted)
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.Owner)
                    .AsQueryable();

                if (type.HasValue)
                {
                    query = query.Where(g => g.Type == type.Value);
                }

                return await query
                    .OrderByDescending(g => g.Type == GoalType.Organizational)
                    .ThenBy(g => g.EndDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving goals by type {0}", type);
                return new List<Goal>();
            }
        }

        /// <summary>
        /// Gets all goals owned by a specific team member.
        /// </summary>
        public async Task<List<Goal>> GetGoalsByOwnerAsync(Guid ownerTeamMemberId)
        {
            if (_context == null) return new List<Goal>();

            try
            {
                return await _context.Goals
                    .Where(g => !g.IsDeleted && g.OwnerTeamMemberId == ownerTeamMemberId)
                    .Include(g => g.Targets)
                        .ThenInclude(t => t.Measurables)
                    .Include(g => g.Owner)
                    .OrderByDescending(g => g.Type == GoalType.Organizational)
                    .ThenBy(g => g.EndDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving goals for team member {0}", ownerTeamMemberId);
                return new List<Goal>();
            }
        }

        /// <summary>
        /// Adds a new goal.
        /// </summary>
        public async Task<Guid> AddGoalAsync(Goal goal)
        {
            if (_context == null)
            {
                _logger.Error("AddGoalAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                goal.CreatedByUserId = _userId;
                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();
                _logger.Info("Added goal ID: {0} - {1}", goal.Id, goal.Title);
                return goal.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding goal");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing goal.
        /// </summary>
        public async Task<bool> UpdateGoalAsync(Goal goal)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Goals.FindAsync(goal.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateGoalAsync: Goal ID {0} not found", goal.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(goal);
                await _context.SaveChangesAsync();
                _logger.Info("Updated goal ID: {0}", goal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating goal");
                return false;
            }
        }

        /// <summary>
        /// Deletes a goal by ID.
        /// </summary>
        public async Task<bool> DeleteGoalAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.Goals.FindAsync(id);
                if (goal != null)
                {
                    _context.Goals.Remove(goal);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted goal ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting goal ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets all targets linked to a specific goal.
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
