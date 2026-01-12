using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for TrackerTask data access operations.
    /// Handles all CRUD operations for tasks, action items, and subtasks.
    /// </summary>
    public class TrackerTaskRepository : ITrackerTaskRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of TrackerTaskRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public TrackerTaskRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(TrackerTaskRepository), "DatabaseLog");
        }

        /// <summary>
        /// Retrieves all tasks for the current user.
        /// </summary>
        public async Task<List<TrackerTask>> GetTasksAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: No context ===");
                return new List<TrackerTask>();
            }

            try
            {
                var result = await context.TrackerTasks
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId)
                    .Include(t => t.Owner)
                    .Include(t => t.Project)
                    .Include(t => t.Goal)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync: Query succeeded, got {result.Count} tasks ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetTasksAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving tasks from database");
                return new List<TrackerTask>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves a specific task by ID.
        /// </summary>
        public async Task<TrackerTask?> GetTaskByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId)
                    .Include(t => t.Owner)
                    .Include(t => t.Project)
                    .Include(t => t.Goal)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving task with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new task.
        /// </summary>
        public async Task<Guid> AddTaskAsync(TrackerTask task)
        {
            if (_context == null)
            {
                _logger.Error("AddTaskAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                task.CreatedByUserId = _userId;
                _context.TrackerTasks.Add(task);
                await _context.SaveChangesAsync();
                return task.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding task");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        public async Task<bool> UpdateTaskAsync(TrackerTask task)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.TrackerTasks.FindAsync(task.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateTaskAsync: Task ID {0} not found", task.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(task);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating task");
                return false;
            }
        }

        /// <summary>
        /// Deletes a task by ID.
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var task = await _context.TrackerTasks.FindAsync(id);
                if (task != null)
                {
                    _context.TrackerTasks.Remove(task);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted task ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting task ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets tasks for a specific project.
        /// </summary>
        public async Task<List<TrackerTask>> GetProjectTasksAsync(Guid projectId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.ProjectId == projectId)
                    .Include(t => t.Owner)
                    .Include(t => t.Project)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving tasks for project {0}", projectId);
                return new List<TrackerTask>();
            }
        }

        /// <summary>
        /// Gets tasks for a specific goal.
        /// </summary>
        public async Task<List<TrackerTask>> GetGoalTasksAsync(Guid goalId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.GoalId == goalId)
                    .Include(t => t.Owner)
                    .Include(t => t.Goal)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving tasks for goal {0}", goalId);
                return new List<TrackerTask>();
            }
        }

        /// <summary>
        /// Gets tasks from a specific meeting (action items).
        /// </summary>
        public async Task<List<TrackerTask>> GetMeetingActionItemsAsync(Guid meetingId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.MeetingId == meetingId)
                    .Include(t => t.Owner)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving action items for meeting {0}", meetingId);
                return new List<TrackerTask>();
            }
        }

        /// <summary>
        /// Gets uncompleted tasks for a specific team member.
        /// </summary>
        public async Task<List<TrackerTask>> GetUncompletedTasksAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.OwnerTeamMemberId == teamMemberId && !t.IsCompleted)
                    .Include(t => t.Owner)
                    .OrderByDescending(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving uncompleted tasks for team member {0}", teamMemberId);
                return new List<TrackerTask>();
            }
        }

        /// <summary>
        /// Gets tasks assigned to a specific team member.
        /// </summary>
        public async Task<List<TrackerTask>> GetAssignedTasksAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.OwnerTeamMemberId == teamMemberId)
                    .Include(t => t.Owner)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving assigned tasks for team member {0}", teamMemberId);
                return new List<TrackerTask>();
            }
        }

        /// <summary>
        /// Gets the count of meetings where a specific task was discussed.
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
        /// Gets meeting counts for multiple tasks (batch operation).
        /// Prevents N+1 query problem when loading meeting counts for multiple tasks.
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetTaskMeetingCountsAsync(List<Guid> taskIds)
        {
            if (_context == null || taskIds == null || taskIds.Count == 0)
                return new Dictionary<Guid, int>();

            try
            {
                var counts = await _context.Meetings
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .SelectMany(m => m.Tasks.Where(t => taskIds.Contains(t.Id)), (m, t) => t.Id)
                    .GroupBy(taskId => taskId)
                    .Select(g => new { TaskId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TaskId, x => x.Count);

                return counts;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for tasks");
                return new Dictionary<Guid, int>();
            }
        }

        /// <summary>
        /// Gets subtasks of a parent task.
        /// </summary>
        public async Task<List<TrackerTask>> GetSubtasksAsync(Guid parentTaskId)
        {
            if (_context == null) return new List<TrackerTask>();

            try
            {
                return await _context.TrackerTasks
                    .Where(t => !t.IsDeleted && t.CreatedByUserId == _userId && t.ParentTaskId == parentTaskId)
                    .Include(t => t.Owner)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving subtasks for parent task {0}", parentTaskId);
                return new List<TrackerTask>();
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
