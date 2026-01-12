using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Goal data access operations.
    /// Goals represent strategic objectives that can be Organizational, Team, or Personal.
    /// Progress is calculated from linked Targets.
    /// </summary>
    public interface IGoalRepository
    {
        /// <summary>
        /// Gets all goals for the current user.
        /// Goals are sorted by type (Organizational first) then by end date.
        /// </summary>
        Task<List<Goal>> GetGoalsAsync();

        /// <summary>
        /// Gets a specific goal by ID.
        /// </summary>
        Task<Goal?> GetGoalByIdAsync(Guid id);

        /// <summary>
        /// Gets goals filtered by type.
        /// If type is null, returns all goals.
        /// </summary>
        Task<List<Goal>> GetGoalsByTypeAsync(GoalType? type);

        /// <summary>
        /// Gets all goals owned by a specific team member.
        /// </summary>
        Task<List<Goal>> GetGoalsByOwnerAsync(Guid ownerTeamMemberId);

        /// <summary>
        /// Adds a new goal.
        /// </summary>
        Task<Guid> AddGoalAsync(Goal goal);

        /// <summary>
        /// Updates an existing goal.
        /// </summary>
        Task<bool> UpdateGoalAsync(Goal goal);

        /// <summary>
        /// Deletes a goal by ID.
        /// </summary>
        Task<bool> DeleteGoalAsync(Guid id);

        /// <summary>
        /// Gets all targets linked to a specific goal.
        /// </summary>
        Task<List<Target>> GetGoalTargetsAsync(Guid goalId);
    }
}
