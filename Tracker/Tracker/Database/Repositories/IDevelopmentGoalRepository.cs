using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for DevelopmentGoal data access operations.
    /// Handles career/skill development goals for individual team members.
    /// </summary>
    public interface IDevelopmentGoalRepository
    {
        /// <summary>
        /// Gets all development goals for a specific team member.
        /// </summary>
        Task<List<DevelopmentGoal>> GetDevelopmentGoalsForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Gets all development goals for all team members.
        /// Useful for reports and dashboards.
        /// </summary>
        Task<List<DevelopmentGoal>> GetAllDevelopmentGoalsAsync();

        /// <summary>
        /// Gets a specific development goal by ID.
        /// </summary>
        Task<DevelopmentGoal?> GetDevelopmentGoalByIdAsync(Guid id);

        /// <summary>
        /// Adds a new development goal.
        /// </summary>
        Task<Guid> AddDevelopmentGoalAsync(DevelopmentGoal goal);

        /// <summary>
        /// Updates an existing development goal.
        /// </summary>
        Task<bool> UpdateDevelopmentGoalAsync(DevelopmentGoal goal);

        /// <summary>
        /// Deletes a development goal by ID.
        /// </summary>
        Task<bool> DeleteDevelopmentGoalAsync(Guid id);

        /// <summary>
        /// Updates a development goal's progress percentage (0-100).
        /// Automatically sets status to Completed if progress reaches 100%.
        /// </summary>
        Task<bool> UpdateDevelopmentGoalProgressAsync(Guid goalId, int progressPercent);

        /// <summary>
        /// Toggles a development goal milestone's completion status.
        /// </summary>
        Task<bool> ToggleDevelopmentGoalMilestoneAsync(Guid milestoneId);
    }
}
