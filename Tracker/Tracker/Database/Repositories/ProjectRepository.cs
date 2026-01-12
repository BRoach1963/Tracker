using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Project data access operations.
    /// Handles all operations including CRUD and related entity loading.
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of ProjectRepository.
        /// </summary>
        public ProjectRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(ProjectRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all projects for the current user.
        /// Includes all related entities (tasks, milestones, risks, dependencies, team members).
        /// </summary>
        public async Task<List<Project>> GetProjectsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: No context ===");
                return new List<Project>();
            }

            try
            {
                var result = await context.Projects
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.Owner)
                    .Include(p => p.TeamMembers.Where(tm => !tm.IsDeleted))
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync: Query succeeded, got {result.Count} projects ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetProjectsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving projects from database");
                return new List<Project>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Gets a specific project by ID.
        /// </summary>
        public async Task<Project?> GetProjectByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Projects
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.Owner)
                    .Include(p => p.TeamMembers.Where(tm => !tm.IsDeleted))
                    .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving project with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new project.
        /// </summary>
        public async Task<Guid> AddProjectAsync(Project project)
        {
            if (_context == null)
            {
                _logger.Error("AddProjectAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                _logger.Info("Added project: {0} (ID: {1})", project.Name, project.Id);
                return project.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding project");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        public async Task<bool> UpdateProjectAsync(Project project)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Projects.FindAsync(project.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateProjectAsync: Project ID {0} not found", project.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(project);
                await _context.SaveChangesAsync();
                _logger.Info("Updated project ID: {0}", project.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating project ID: {0}", project.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a project by ID.
        /// </summary>
        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project != null)
                {
                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted project ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting project ID: {0}", id);
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
