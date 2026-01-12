using Tracker.DataModels;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for QuickNote data access operations.
    /// Handles quick notes attached to team members.
    /// </summary>
    public interface IQuickNoteRepository
    {
        /// <summary>
        /// Gets all quick notes for the current user (excluding archived by default).
        /// </summary>
        Task<List<QuickNote>> GetQuickNotesAsync(bool includeArchived = false);

        /// <summary>
        /// Gets quick notes for a specific team member.
        /// </summary>
        Task<List<QuickNote>> GetQuickNotesForTeamMemberAsync(Guid teamMemberId);

        /// <summary>
        /// Gets a specific quick note by ID.
        /// </summary>
        Task<QuickNote?> GetQuickNoteByIdAsync(int id);

        /// <summary>
        /// Adds a new quick note.
        /// </summary>
        Task<int> AddQuickNoteAsync(QuickNote note);

        /// <summary>
        /// Updates an existing quick note.
        /// </summary>
        Task<bool> UpdateQuickNoteAsync(QuickNote note);

        /// <summary>
        /// Deletes a quick note.
        /// </summary>
        Task<bool> DeleteQuickNoteAsync(int id);

        /// <summary>
        /// Toggles the pinned status of a note.
        /// </summary>
        Task<bool> ToggleNotePinnedAsync(int id);

        /// <summary>
        /// Archives a note (soft delete with IsArchived flag).
        /// </summary>
        Task<bool> ArchiveNoteAsync(int id);
    }
}
