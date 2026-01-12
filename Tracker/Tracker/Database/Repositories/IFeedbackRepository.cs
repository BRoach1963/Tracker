using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Feedback data access operations.
    /// Handles all feedback records given to team members.
    /// </summary>
    public interface IFeedbackRepository
    {
        /// <summary>
        /// Gets all feedback for a specific team member.
        /// </summary>
        Task<List<Feedback>> GetFeedbackForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Gets all feedback for all team members.
        /// Useful for reports and dashboards.
        /// </summary>
        Task<List<Feedback>> GetAllFeedbackAsync();

        /// <summary>
        /// Gets a specific feedback record by ID.
        /// </summary>
        Task<Feedback?> GetFeedbackByIdAsync(Guid id);

        /// <summary>
        /// Adds new feedback and returns its Guid identifier.
        /// </summary>
        Task<Guid> AddFeedbackAsync(Feedback feedback);

        /// <summary>
        /// Updates existing feedback.
        /// </summary>
        Task<bool> UpdateFeedbackAsync(Feedback feedback);

        /// <summary>
        /// Deletes feedback by ID.
        /// </summary>
        Task<bool> DeleteFeedbackAsync(Guid id);
    }
}
