using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for ReviewTemplate data access operations.
    /// Handles performance review templates with sections and questions.
    /// </summary>
    public class ReviewTemplateRepository : IReviewTemplateRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of ReviewTemplateRepository.
        /// </summary>
        public ReviewTemplateRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(ReviewTemplateRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all review templates for the current user.
        /// Includes sections and questions ordered by sort order.
        /// </summary>
        public async Task<List<ReviewTemplate>> GetReviewTemplatesAsync()
        {
            if (_context == null) return new List<ReviewTemplate>();

            try
            {
                return await _context.ReviewTemplates
                    .AsNoTracking()
                    .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                        .ThenInclude(s => s.Questions.OrderBy(q => q.SortOrder))
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review templates");
                return new List<ReviewTemplate>();
            }
        }

        /// <summary>
        /// Gets a specific review template by ID.
        /// Includes sections and questions.
        /// </summary>
        public async Task<ReviewTemplate?> GetReviewTemplateByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.ReviewTemplates
                    .AsNoTracking()
                    .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                        .ThenInclude(s => s.Questions.OrderBy(q => q.SortOrder))
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving review template ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new review template with sections and questions.
        /// </summary>
        public async Task<Guid> AddReviewTemplateAsync(ReviewTemplate template)
        {
            if (_context == null)
            {
                _logger.Error("AddReviewTemplateAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.ReviewTemplates.Add(template);
                await _context.SaveChangesAsync();
                _logger.Info("Added review template ID: {0} - {1}", template.Id, template.Name);
                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding review template");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing review template.
        /// </summary>
        public async Task<bool> UpdateReviewTemplateAsync(ReviewTemplate template)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.ReviewTemplates.FindAsync(template.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateReviewTemplateAsync: Template ID {0} not found", template.Id);
                    return false;
                }

                existing.Name = template.Name;
                existing.Description = template.Description;
                existing.ReviewType = template.ReviewType;
                existing.IsDefault = template.IsDefault;
                existing.IsActive = template.IsActive;

                await _context.SaveChangesAsync();
                _logger.Info("Updated review template ID: {0}", template.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating review template ID: {0}", template.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a review template by ID.
        /// </summary>
        public async Task<bool> DeleteReviewTemplateAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var template = await _context.ReviewTemplates.FindAsync(id);
                if (template != null)
                {
                    _context.ReviewTemplates.Remove(template);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted review template ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting review template ID: {0}", id);
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
