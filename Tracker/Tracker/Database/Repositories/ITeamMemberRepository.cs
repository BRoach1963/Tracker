using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for TeamMember data access operations.
    /// Handles all operations for team members including CRUD and filtering.
    /// </summary>
    public interface ITeamMemberRepository
    {
        /// <summary>
        /// Gets all team members for the current user.
        /// Includes runtime statistics (last/next 1:1, open tasks, active goals).
        /// </summary>
        Task<List<TeamMember>> GetTeamMembersAsync();

        /// <summary>
        /// Gets a specific team member by ID.
        /// </summary>
        Task<TeamMember?> GetTeamMemberByIdAsync(Guid id);

        /// <summary>
        /// Adds a new team member.
        /// </summary>
        Task<Guid> AddTeamMemberAsync(TeamMember teamMember);

        /// <summary>
        /// Updates an existing team member.
        /// </summary>
        Task<bool> UpdateTeamMemberAsync(TeamMember teamMember);

        /// <summary>
        /// Deletes a team member by ID.
        /// </summary>
        Task<bool> DeleteTeamMemberAsync(Guid id);

        /// <summary>
        /// Finds a team member by display name (case-insensitive).
        /// Matches on full name, first name, or last name.
        /// </summary>
        Task<TeamMember?> FindTeamMemberByNameAsync(string displayName);

        /// <summary>
        /// Gets team members who haven't had a 1:1 meeting in the specified number of weeks.
        /// Useful for identifying team members needing attention.
        /// </summary>
        Task<List<TeamMember>> GetTeamMembersWithoutRecentOneOnOneAsync(int weeks);
    }
}
