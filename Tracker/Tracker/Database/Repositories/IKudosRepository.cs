using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for Kudos operations.
    /// Handles all data access for recognition/kudos between team members.
    /// </summary>
    public interface IKudosRepository
    {
        /// <summary>
        /// Retrieves all kudos for the current user's organization.
        /// </summary>
        Task<List<Kudos>> GetKudosAsync();

        /// <summary>
        /// Retrieves a specific kudos by ID.
        /// </summary>
        Task<Kudos?> GetKudosByIdAsync(Guid id);

        /// <summary>
        /// Adds new kudos.
        /// </summary>
        Task<Guid> AddKudosAsync(Kudos kudos);

        /// <summary>
        /// Updates existing kudos.
        /// </summary>
        Task<bool> UpdateKudosAsync(Kudos kudos);

        /// <summary>
        /// Deletes kudos by ID.
        /// </summary>
        Task<bool> DeleteKudosAsync(Guid id);

        /// <summary>
        /// Gets kudos given by a specific team member.
        /// </summary>
        Task<List<Kudos>> GetKudosFromAsync(Guid fromTeamMemberId);

        /// <summary>
        /// Gets kudos received by a specific team member.
        /// </summary>
        Task<List<Kudos>> GetKudosToAsync(Guid toTeamMemberId);

        /// <summary>
        /// Gets kudos between two specific team members.
        /// </summary>
        Task<List<Kudos>> GetKudosBetweenAsync(Guid fromTeamMemberId, Guid toTeamMemberId);

        /// <summary>
        /// Gets kudos with a specific badge type.
        /// If badgeType is null, retrieves all kudos.
        /// </summary>
        Task<List<Kudos>> GetKudosByBadgeTypeAsync(string? badgeType);

        /// <summary>
        /// Gets public kudos only.
        /// </summary>
        Task<List<Kudos>> GetPublicKudosAsync();

        /// <summary>
        /// Gets recent kudos within a date range.
        /// </summary>
        Task<List<Kudos>> GetRecentKudosAsync(DateTime startDate, DateTime endDate);
    }
}
