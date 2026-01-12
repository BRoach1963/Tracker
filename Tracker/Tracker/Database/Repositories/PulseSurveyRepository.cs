using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for PulseSurvey data access operations.
    /// Handles employee pulse/engagement surveys.
    /// </summary>
    public class PulseSurveyRepository : IPulseSurveyRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of PulseSurveyRepository.
        /// </summary>
        public PulseSurveyRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(PulseSurveyRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all pulse surveys for the current user.
        /// </summary>
        public async Task<List<PulseSurvey>> GetPulseSurveysAsync()
        {
            if (_context == null) return new List<PulseSurvey>();

            try
            {
                return await _context.PulseSurveys
                    .AsNoTracking()
                    .Include(s => s.Questions.OrderBy(q => q.SortOrder))
                    .Include(s => s.Responses)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pulse surveys");
                return new List<PulseSurvey>();
            }
        }

        /// <summary>
        /// Gets a pulse survey by ID with all related data.
        /// </summary>
        public async Task<PulseSurvey?> GetPulseSurveyByIdAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.PulseSurveys
                    .AsNoTracking()
                    .Include(s => s.Questions.OrderBy(q => q.SortOrder))
                    .Include(s => s.Responses)
                        .ThenInclude(r => r.Answers)
                    .Include(s => s.Responses)
                        .ThenInclude(r => r.TeamMember)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving pulse survey ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new pulse survey.
        /// </summary>
        public async Task<int> AddPulseSurveyAsync(PulseSurvey survey)
        {
            if (_context == null)
            {
                _logger.Error("AddPulseSurveyAsync: _context is null");
                return 0;
            }

            try
            {
                _context.PulseSurveys.Add(survey);
                await _context.SaveChangesAsync();
                _logger.Info("Added pulse survey ID: {0}", survey.Id);
                return survey.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding pulse survey");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing pulse survey.
        /// </summary>
        public async Task<bool> UpdatePulseSurveyAsync(PulseSurvey survey)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.PulseSurveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == survey.Id);

                if (existing == null)
                {
                    _logger.Error("UpdatePulseSurveyAsync: Survey ID {0} not found", survey.Id);
                    return false;
                }

                // Update basic properties
                existing.Title = survey.Title;
                existing.Description = survey.Description;
                existing.Status = survey.Status;
                existing.SentDate = survey.SentDate;
                existing.DueDate = survey.DueDate;
                existing.ClosedDate = survey.ClosedDate;
                existing.IsAnonymous = survey.IsAnonymous;

                await _context.SaveChangesAsync();
                _logger.Info("Updated pulse survey ID: {0}", survey.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating pulse survey ID: {0}", survey.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a pulse survey.
        /// </summary>
        public async Task<bool> DeletePulseSurveyAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var survey = await _context.PulseSurveys.FindAsync(id);
                if (survey != null)
                {
                    _context.PulseSurveys.Remove(survey);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted pulse survey ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting pulse survey ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets active pulse surveys (sent but not closed).
        /// </summary>
        public async Task<List<PulseSurvey>> GetActivePulseSurveysAsync()
        {
            if (_context == null) return new List<PulseSurvey>();

            try
            {
                var now = DateTime.Now;
                return await _context.PulseSurveys
                    .AsNoTracking()
                    .Include(s => s.Questions.OrderBy(q => q.SortOrder))
                    .Where(s => s.Status == SurveyStatus.Active &&
                               s.SentDate <= now &&
                               (s.ClosedDate == null || s.ClosedDate > now))
                    .OrderByDescending(s => s.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving active pulse surveys");
                return new List<PulseSurvey>();
            }
        }

        /// <summary>
        /// Adds a survey response from a team member.
        /// </summary>
        public async Task<int> AddSurveyResponseAsync(PulseSurveyResponse response)
        {
            if (_context == null)
            {
                _logger.Error("AddSurveyResponseAsync: _context is null");
                return 0;
            }

            try
            {
                _context.PulseSurveyResponses.Add(response);
                await _context.SaveChangesAsync();
                _logger.Info("Added survey response ID: {0}", response.Id);
                return response.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding survey response");
                return 0;
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
