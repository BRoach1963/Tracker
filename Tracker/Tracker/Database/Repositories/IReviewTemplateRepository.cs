using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for ReviewTemplate data access operations.
    /// Handles performance review templates with sections and questions.
    /// </summary>
    public interface IReviewTemplateRepository
    {
        /// <summary>
        /// Gets all review templates for the current user.
        /// Includes sections and questions ordered by sort order.
        /// </summary>
        Task<List<ReviewTemplate>> GetReviewTemplatesAsync();

        /// <summary>
        /// Gets a specific review template by ID.
        /// Includes sections and questions.
        /// </summary>
        Task<ReviewTemplate?> GetReviewTemplateByIdAsync(Guid id);

        /// <summary>
        /// Adds a new review template with sections and questions.
        /// </summary>
        Task<Guid> AddReviewTemplateAsync(ReviewTemplate template);

        /// <summary>
        /// Updates an existing review template.
        /// </summary>
        Task<bool> UpdateReviewTemplateAsync(ReviewTemplate template);

        /// <summary>
        /// Deletes a review template by ID.
        /// </summary>
        Task<bool> DeleteReviewTemplateAsync(Guid id);
    }
}
