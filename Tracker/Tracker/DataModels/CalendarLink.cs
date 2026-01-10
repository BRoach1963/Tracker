namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a user's connection to an external calendar provider (Google, Microsoft, Apple, etc.).
    /// Stores authentication tokens and sync preferences for a specific calendar account.
    /// Maps to calendar_links table in Supabase.
    /// </summary>
    public class CalendarLink : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for this calendar account link (UUID).
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// FK to the user who owns this calendar account connection.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The calendar provider for this link (google, microsoft, apple, other).
        /// </summary>
        public CalendarProviderType Provider { get; set; } = CalendarProviderType.Google;

        /// <summary>
        /// The email address of the calendar account (e.g., user@gmail.com or user@outlook.com).
        /// Helps identify which account this link represents.
        /// </summary>
        public string? AccountEmail { get; set; }

        /// <summary>
        /// Display name for this calendar account (e.g., "John Doe" or "Work Calendar").
        /// </summary>
        public string? AccountName { get; set; }

        #region Authentication

        /// <summary>
        /// OAuth access token for this calendar provider.
        /// Should be encrypted at rest in the database.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// OAuth refresh token for obtaining new access tokens.
        /// Should be encrypted at rest in the database.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// When the current access token expires.
        /// Used to determine if token refresh is needed.
        /// </summary>
        public DateTime? TokenExpiresAt { get; set; }

        /// <summary>
        /// Delta sync token from the provider for incremental synchronization.
        /// For Google Calendar: syncToken from Events.list response
        /// For Outlook: deltaLink from delta query
        /// Enables fetching only NEW/CHANGED events instead of all events.
        /// Null if not yet synced or if provider doesn't support delta sync.
        /// </summary>
        public string? SyncToken { get; set; }

        #endregion

        #region Sync Settings

        /// <summary>
        /// Whether this calendar link is currently active/enabled.
        /// Inactive links won't sync events.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether syncing is enabled for this provider.
        /// Can be disabled to pause sync without deactivating the link.
        /// </summary>
        public bool SyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether to sync Tracker meetings to this provider's calendar.
        /// </summary>
        public bool SyncMeetingsToCalendar { get; set; } = true;

        /// <summary>
        /// Whether to sync Tracker tasks to this provider's calendar.
        /// </summary>
        public bool SyncTasksToCalendar { get; set; } = false;

        /// <summary>
        /// Whether to auto-create Tracker meetings from calendar events.
        /// </summary>
        public bool CreateMeetingFromCalendar { get; set; } = false;

        #endregion

        #region Calendar Selection

        /// <summary>
        /// The default calendar ID in this provider to sync to.
        /// Null if using the provider's default calendar.
        /// </summary>
        public string? DefaultCalendarId { get; set; }

        /// <summary>
        /// Display name of the default calendar.
        /// </summary>
        public string? DefaultCalendarName { get; set; }

        #endregion

        #region Sync State

        /// <summary>
        /// When this calendar link was last synchronized.
        /// Null if never synced.
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Overall sync status for this calendar provider connection.
        /// pending = Initial sync not yet attempted
        /// synced = Successfully synced
        /// failed = Last sync failed
        /// cancelled = Sync was cancelled
        /// </summary>
        public CalendarSyncStatusType? LastSyncStatus { get; set; }

        /// <summary>
        /// Error message from the last failed sync attempt.
        /// Null if last sync was successful.
        /// </summary>
        public string? LastSyncError { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether the access token is expired or about to expire (within 5 minutes).
        /// </summary>
        public bool IsTokenExpired => TokenExpiresAt == null || TokenExpiresAt <= DateTime.UtcNow.AddMinutes(5);

        /// <summary>
        /// Whether this link is ready to sync (active, enabled, and auth valid).
        /// </summary>
        public bool IsReadyToSync => IsActive && SyncEnabled && !IsTokenExpired && !string.IsNullOrEmpty(AccessToken);

        /// <summary>
        /// Whether the last sync was successful.
        /// </summary>
        public bool LastSyncSuccessful => LastSyncStatus == CalendarSyncStatusType.Synced;

        #endregion
    }

    /// <summary>
    /// Calendar provider types.
    /// </summary>
    public enum CalendarProviderType
    {
        /// <summary>Google Calendar</summary>
        Google,
        /// <summary>Microsoft Outlook / Microsoft 365</summary>
        Microsoft,
        /// <summary>Apple Calendar</summary>
        Apple,
        /// <summary>Other provider</summary>
        Other
    }

    /// <summary>
    /// Overall sync status for a calendar provider connection.
    /// Note: Individual meeting sync status is tracked on Meeting.calendar_sync_status.
    /// </summary>
    public enum CalendarSyncStatusType
    {
        /// <summary>Sync pending - not yet synced</summary>
        Pending,
        /// <summary>Successfully synced</summary>
        Synced,
        /// <summary>Sync failed</summary>
        Failed,
        /// <summary>Sync was cancelled</summary>
        Cancelled
    }
}
