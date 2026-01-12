using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Common.Enums;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for MeetingTemplate data access operations.
    /// Handles meeting agenda templates with configurable items.
    /// </summary>
    public class MeetingTemplateRepository : IMeetingTemplateRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory;
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of MeetingTemplateRepository.
        /// </summary>
        public MeetingTemplateRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(MeetingTemplateRepository), "DatabaseLog");
        }

        /// <summary>
        /// Gets all meeting templates for the current user.
        /// </summary>
        public async Task<List<MeetingTemplate>> GetMeetingTemplatesAsync()
        {
            if (_context == null) return new List<MeetingTemplate>();

            try
            {
                return await _context.MeetingTemplates
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting templates");
                return new List<MeetingTemplate>();
            }
        }

        /// <summary>
        /// Gets a specific meeting template by ID with items.
        /// </summary>
        public async Task<MeetingTemplate?> GetMeetingTemplateByIdAsync(int id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.MeetingTemplates
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && t.Id == id)
                    .Include(t => t.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting template ID: {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new meeting template.
        /// </summary>
        public async Task<int> AddMeetingTemplateAsync(MeetingTemplate template)
        {
            if (_context == null)
            {
                _logger.Error("AddMeetingTemplateAsync: _context is null");
                return 0;
            }

            try
            {
                _context.MeetingTemplates.Add(template);
                await _context.SaveChangesAsync();
                _logger.Info("Added meeting template ID: {0}", template.Id);
                return template.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding meeting template");
                return 0;
            }
        }

        /// <summary>
        /// Updates an existing meeting template.
        /// </summary>
        public async Task<bool> UpdateMeetingTemplateAsync(MeetingTemplate template)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.MeetingTemplates.FindAsync(template.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateMeetingTemplateAsync: Template ID {0} not found", template.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(template);
                await _context.SaveChangesAsync();
                _logger.Info("Updated meeting template ID: {0}", template.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating meeting template ID: {0}", template.Id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a meeting template.
        /// </summary>
        public async Task<bool> DeleteMeetingTemplateAsync(int id)
        {
            if (_context == null) return false;

            try
            {
                var template = await _context.MeetingTemplates.FindAsync(id);
                if (template != null)
                {
                    _context.MeetingTemplates.Remove(template);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted meeting template ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting meeting template ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets templates filtered by name containing the search term.
        /// </summary>
        public async Task<List<MeetingTemplate>> GetTemplatesByTypeAsync(string searchTerm)
        {
            if (_context == null) return new List<MeetingTemplate>();

            try
            {
                return await _context.MeetingTemplates
                    .AsNoTracking()
                    .Where(t => !t.IsDeleted && t.Name.Contains(searchTerm))
                    .Include(t => t.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving meeting templates by search term: {0}", searchTerm);
                return new List<MeetingTemplate>();
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
