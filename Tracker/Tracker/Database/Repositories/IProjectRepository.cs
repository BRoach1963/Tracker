using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Project data access operations.
    /// Handles all operations for projects including tasks, milestones, risks, and dependencies.
    /// </summary>
    public interface IProjectRepository
    {
        /// <summary>
        /// Gets all projects for the current user.
        /// Includes all related entities (tasks, milestones, team members).
        /// </summary>
        Task<List<Project>> GetProjectsAsync();

        /// <summary>
        /// Gets a specific project by ID.
        /// </summary>
        Task<Project?> GetProjectByIdAsync(Guid id);

        /// <summary>
        /// Adds a new project and returns its Guid identifier.
        /// </summary>
        Task<Guid> AddProjectAsync(Project project);

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        Task<bool> UpdateProjectAsync(Project project);

        /// <summary>
        /// Deletes a project by ID.
        /// </summary>
        Task<bool> DeleteProjectAsync(Guid id);
    }
}
