using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Meeting operations (formerly OneOnOne).
    /// Handles all data access for meetings with team members.
    /// </summary>
    public interface IMeetingRepository
    {
        /// <summary>
        /// Retrieves all meetings for the current user.
        /// </summary>
        Task<List<Meeting>> GetMeetingsAsync();

        /// <summary>
        /// Retrieves meetings of a specific type for the current user.
        /// If type is null, retrieves all meetings.
        /// </summary>
        Task<List<Meeting>> GetMeetingsByTypeAsync(MeetingType? type);

        /// <summary>
        /// Retrieves a specific meeting by ID.
        /// </summary>
        Task<Meeting?> GetMeetingByIdAsync(Guid id);

        /// <summary>
        /// Adds a new meeting.
        /// </summary>
        Task<Guid> AddMeetingAsync(Meeting meeting, Guid? teamMemberId = null);

        /// <summary>
        /// Updates an existing meeting.
        /// </summary>
        Task<bool> UpdateMeetingAsync(Meeting meeting);

        /// <summary>
        /// Deletes a meeting by ID.
        /// </summary>
        Task<bool> DeleteMeetingAsync(Guid id);

        /// <summary>
        /// Retrieves all meetings for a specific team member.
        /// </summary>
        Task<List<Meeting>> GetMeetingsForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Links a tracker task to a meeting.
        /// </summary>
        Task<bool> LinkTaskToMeetingAsync(Guid meetingId, Guid taskId);

        /// <summary>
        /// Unlinks a tracker task from a meeting.
        /// </summary>
        Task<bool> UnlinkTaskFromMeetingAsync(Guid meetingId, Guid taskId);

        /// <summary>
        /// Gets the count of meetings associated with a tracker task.
        /// </summary>
        Task<int> GetTaskMeetingCountAsync(Guid taskId);
    }
}
