using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for PerformanceReviewCycle data access operations.
    /// Handles performance review cycles (evaluation periods).
    /// </summary>
    public interface IReviewCycleRepository
    {
        /// <summary>
        /// Gets all review cycles for the current user.
        /// </summary>
        Task<List<PerformanceReviewCycle>> GetReviewCyclesAsync();

        /// <summary>
        /// Gets a review cycle by ID with all related data (reviews, sections, answers).
        /// </summary>
        Task<PerformanceReviewCycle?> GetReviewCycleByIdAsync(Guid id);

        /// <summary>
        /// Adds a new review cycle.
        /// </summary>
        Task<Guid> AddReviewCycleAsync(PerformanceReviewCycle cycle);

        /// <summary>
        /// Updates an existing review cycle.
        /// </summary>
        Task<bool> UpdateReviewCycleAsync(PerformanceReviewCycle cycle);

        /// <summary>
        /// Deletes a review cycle.
        /// </summary>
        Task<bool> DeleteReviewCycleAsync(Guid id);

        /// <summary>
        /// Gets the active (current) review cycle.
        /// </summary>
        Task<PerformanceReviewCycle?> GetActiveReviewCycleAsync();
    }
}
