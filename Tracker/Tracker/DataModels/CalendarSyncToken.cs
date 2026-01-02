namespace Tracker.DataModels
{
    /// <summary>
    /// Stores sync tokens for calendar providers to enable delta synchronization.
    /// Each provider tracks its own token per user for incremental sync.
    /// </summary>
    public class CalendarSyncToken
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The calendar provider identifier: "google", "outlook".
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// The sync token or delta link from the provider.
        /// For Google: syncToken from Events.list response.
        /// For Outlook: deltaLink from delta query.
        /// </summary>
        public string SyncToken { get; set; } = string.Empty;

        /// <summary>
        /// When this token was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user this sync token belongs to (shadow property set via EF).
        /// </summary>
        /// <remarks>
        /// This is a shadow property - not stored on the entity but tracked by EF.
        /// </remarks>
    }
}
