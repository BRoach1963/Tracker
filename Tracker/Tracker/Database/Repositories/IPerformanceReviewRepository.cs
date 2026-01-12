using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for PerformanceReview data access operations.
    /// Handles individual performance reviews within review cycles.
    /// </summary>
    public interface IPerformanceReviewRepository
    {
        /// <summary>
        /// Gets all performance reviews for a team member.
        /// </summary>
        Task<List<PerformanceReview>> GetReviewsForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Gets a performance review by ID with all related data.
        /// </summary>
        Task<PerformanceReview?> GetPerformanceReviewByIdAsync(Guid id);

        /// <summary>
        /// Adds a new performance review.
        /// </summary>
        Task<Guid> AddPerformanceReviewAsync(PerformanceReview review);

        /// <summary>
        /// Updates an existing performance review including sections and answers.
        /// </summary>
        Task<bool> UpdatePerformanceReviewAsync(PerformanceReview review);

        /// <summary>
        /// Deletes a performance review.
        /// </summary>
        Task<bool> DeletePerformanceReviewAsync(Guid id);

        /// <summary>
        /// Gets all reviews for a specific review cycle.
        /// </summary>
        Task<List<PerformanceReview>> GetReviewsForCycleAsync(Guid cycleId);
    }
}
