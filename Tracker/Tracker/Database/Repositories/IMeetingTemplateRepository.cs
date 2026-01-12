using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for MeetingTemplate data access operations.
    /// Handles meeting agenda templates with configurable items.
    /// </summary>
    public interface IMeetingTemplateRepository
    {
        /// <summary>
        /// Gets all meeting templates for the current user.
        /// </summary>
        Task<List<MeetingTemplate>> GetMeetingTemplatesAsync();

        /// <summary>
        /// Gets a specific meeting template by ID with items.
        /// </summary>
        Task<MeetingTemplate?> GetMeetingTemplateByIdAsync(int id);

        /// <summary>
        /// Adds a new meeting template.
        /// </summary>
        Task<int> AddMeetingTemplateAsync(MeetingTemplate template);

        /// <summary>
        /// Updates an existing meeting template.
        /// </summary>
        Task<bool> UpdateMeetingTemplateAsync(MeetingTemplate template);

        /// <summary>
        /// Deletes a meeting template.
        /// </summary>
        Task<bool> DeleteMeetingTemplateAsync(int id);

        /// <summary>
        /// Gets templates filtered by type.
        /// </summary>
        Task<List<MeetingTemplate>> GetTemplatesByTypeAsync(string templateType);
    }
}
