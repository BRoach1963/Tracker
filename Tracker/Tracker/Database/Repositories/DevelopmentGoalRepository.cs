using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for DevelopmentGoal data access operations.
    /// Handles career/skill development goals including progress tracking and milestones.
    /// </summary>
    public class DevelopmentGoalRepository : IDevelopmentGoalRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of DevelopmentGoalRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public DevelopmentGoalRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(DevelopmentGoalRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all development goals for a specific team member.
        /// </summary>
        public async Task<List<DevelopmentGoal>> GetDevelopmentGoalsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<DevelopmentGoal>();

            try
            {
                return await _context.DevelopmentGoals
                    .AsNoTracking()
                    .Where(g => !g.IsDeleted && g.TeamMemberId == teamMemberId)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == DevelopmentGoalStatus.Active)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving development goals for team member {0}", teamMemberId);
                return new List<DevelopmentGoal>();
            }
        }

        /// <summary>
        /// Gets all development goals for all team members.
        /// </summary>
        public async Task<List<DevelopmentGoal>> GetAllDevelopmentGoalsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetAllDevelopmentGoalsAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetAllDevelopmentGoalsAsync: No context ===");
                return new List<DevelopmentGoal>();
            }

            try
            {
                var result = await context.DevelopmentGoals
                    .AsNoTracking()
                    .Where(g => !g.IsDeleted)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .OrderByDescending(g => g.Status == DevelopmentGoalStatus.Active)
                    .ThenBy(g => g.TargetDate)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetAllDevelopmentGoalsAsync: Query succeeded, got {result.Count} goals ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetAllDevelopmentGoalsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving all development goals");
                return new List<DevelopmentGoal>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets a specific development goal by ID.
        /// </summary>
        public async Task<DevelopmentGoal?> GetDevelopmentGoalByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.DevelopmentGoals
                    .AsNoTracking()
                    .Where(g => !g.IsDeleted)
                    .Include(g => g.TeamMember)
                    .Include(g => g.Milestones.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving development goal with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new development goal.
        /// </summary>
        public async Task<Guid> AddDevelopmentGoalAsync(DevelopmentGoal goal)
        {
            if (_context == null)
            {
                _logger.Error("AddDevelopmentGoalAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.DevelopmentGoals.Add(goal);

                await _context.SaveChangesAsync();
                _logger.Info("Added development goal ID: {0} - {1}", goal.Id, goal.Title);
                return goal.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding development goal");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing development goal.
        /// </summary>
        public async Task<bool> UpdateDevelopmentGoalAsync(DevelopmentGoal goal)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.DevelopmentGoals.FindAsync(goal.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateDevelopmentGoalAsync: Goal ID {0} not found", goal.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(goal);

                // Handle milestones - add new ones
                foreach (var milestone in goal.Milestones.Where(m => m.Id == Guid.Empty))
                {
                    milestone.OrganizationId = goal.OrganizationId;
                    _context.DevelopmentGoalMilestones.Add(milestone);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Updated development goal ID: {0}", goal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating development goal ID: {0}", goal.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a development goal by ID.
        /// </summary>
        public async Task<bool> DeleteDevelopmentGoalAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.DevelopmentGoals.FindAsync(id);
                if (goal != null)
                {
                    _context.DevelopmentGoals.Remove(goal);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted development goal ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting development goal ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Updates a development goal's progress percentage (0-100).
        /// Automatically sets status to Completed if progress reaches 100%.
        /// </summary>
        public async Task<bool> UpdateDevelopmentGoalProgressAsync(Guid goalId, int progressPercent)
        {
            if (_context == null) return false;

            try
            {
                var goal = await _context.DevelopmentGoals.FindAsync(goalId);
                if (goal != null)
                {
                    goal.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
                    if (goal.ProgressPercent == 100 && goal.Status != DevelopmentGoalStatus.Completed)
                    {
                        goal.Status = DevelopmentGoalStatus.Completed;
                        goal.CompletedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                    _logger.Info("Updated progress for development goal ID: {0} to {1}%", goalId, progressPercent);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating development goal progress");
                return false;
            }
        }

        /// <summary>
        /// Toggles a development goal milestone's completion status.
        /// </summary>
        public async Task<bool> ToggleDevelopmentGoalMilestoneAsync(Guid milestoneId)
        {
            if (_context == null) return false;

            try
            {
                var milestone = await _context.DevelopmentGoalMilestones.FindAsync(milestoneId);
                if (milestone != null)
                {
                    milestone.Status = milestone.Status == "completed" 
                        ? "not_started" 
                        : "completed";
                    milestone.CompletedAt = milestone.Status == "completed" 
                        ? DateTime.UtcNow 
                        : null;
                    await _context.SaveChangesAsync();
                    _logger.Info("Toggled milestone ID: {0} to {1}", milestoneId, milestone.Status);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error toggling development goal milestone");
                return false;
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
