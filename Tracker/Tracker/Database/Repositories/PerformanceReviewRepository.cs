using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for PerformanceReview data access operations.
    /// Handles individual performance reviews within review cycles.
    /// </summary>
    public class PerformanceReviewRepository : IPerformanceReviewRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of PerformanceReviewRepository.
        /// </summary>
        public PerformanceReviewRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(PerformanceReviewRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all performance reviews for a team member.
        /// </summary>
        public async Task<List<PerformanceReview>> GetReviewsForTeamMemberAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<PerformanceReview>();

            try
            {
                return await _context.PerformanceReviews
                    .AsNoTracking()
                    .Include(r => r.PerformanceReviewCycle)
                    .Where(r => r.TeamMemberId == teamMemberId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving reviews for team member ID: {0}", teamMemberId);
                return new List<PerformanceReview>();
            }
        }

        /// <summary>
        /// Gets a performance review by ID with all related data.
        /// </summary>
        public async Task<PerformanceReview?> GetPerformanceReviewByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PerformanceReviews
                    .AsNoTracking()
                    .Include(r => r.TeamMember)
                    .Include(r => r.PerformanceReviewCycle)
                        .ThenInclude(c => c.ReviewTemplate)
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.Answers)
                            .ThenInclude(a => a.ReviewTemplateQuestion)
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.ReviewTemplateSection)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving performance review ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new performance review.
        /// </summary>
        public async Task<Guid> AddPerformanceReviewAsync(PerformanceReview review)
        {
            if (_context == null)
            {
                _logger.Error("AddPerformanceReviewAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.PerformanceReviews.Add(review);
                await _context.SaveChangesAsync();
                _logger.Info("Added performance review ID: {0}", review.Id);
                return review.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding performance review");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing performance review including sections and answers.
        /// </summary>
        public async Task<bool> UpdatePerformanceReviewAsync(PerformanceReview review)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PerformanceReviews
                    .Include(r => r.Sections)
                        .ThenInclude(s => s.Answers)
                    .FirstOrDefaultAsync(r => r.Id == review.Id);

                if (existing == null)
                {
                    _logger.Error("UpdatePerformanceReviewAsync: Review ID {0} not found", review.Id);
                    return false;
                }

                // Update review properties
                existing.Status = review.Status;
                existing.OverallRating = review.OverallRating;
                existing.ManagerSummary = review.ManagerSummary;
                existing.SelfAssessmentSummary = review.SelfAssessmentSummary;
                existing.SelfReviewSubmittedAt = review.SelfReviewSubmittedAt;
                existing.ManagerReviewSubmittedAt = review.ManagerReviewSubmittedAt;
                existing.SharedAt = review.SharedAt;
                existing.DiscussionDate = review.DiscussionDate;
                existing.MeetingId = review.MeetingId;

                // Update answers
                foreach (var section in review.Sections)
                {
                    var existingSection = existing.Sections.FirstOrDefault(s => s.Id == section.Id);
                    if (existingSection != null)
                    {
                        foreach (var answer in section.Answers)
                        {
                            var existingAnswer = existingSection.Answers.FirstOrDefault(a => a.Id == answer.Id);
                            if (existingAnswer != null)
                            {
                                existingAnswer.TextValue = answer.TextValue;
                                existingAnswer.RatingValue = answer.RatingValue;
                                existingAnswer.IsSelfAssessment = answer.IsSelfAssessment;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.Info("Updated performance review ID: {0}", review.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating performance review ID: {0}", review.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a performance review.
        /// </summary>
        public async Task<bool> DeletePerformanceReviewAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var review = await _context.PerformanceReviews.FindAsync(id);
                if (review != null)
                {
                    _context.PerformanceReviews.Remove(review);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted performance review ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting performance review ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets all reviews for a specific review cycle.
        /// </summary>
        public async Task<List<PerformanceReview>> GetReviewsForCycleAsync(Guid cycleId)
        {
            if (_context == null) return new List<PerformanceReview>();

            try
            {
                return await _context.PerformanceReviews
                    .AsNoTracking()
                    .Include(r => r.TeamMember)
                    .Where(r => r.PerformanceReviewCycleId == cycleId)
                    .OrderBy(r => r.TeamMember.FirstName)
                    .ThenBy(r => r.TeamMember.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving reviews for cycle ID: {0}", cycleId);
                return new List<PerformanceReview>();
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
