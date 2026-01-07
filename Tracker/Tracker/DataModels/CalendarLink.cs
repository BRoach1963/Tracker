namespace Tracker.DataModels
{
    /// <summary>
    /// Links a Tracker OneOnOne meeting to an external calendar event.
    /// Supports multiple calendar providers (Google, Outlook) per meeting.
    /// </summary>
    public class CalendarLink : AuditableEntity
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The organization this calendar link belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// The Tracker meeting this link belongs to.
        /// </summary>
        public int OneOnOneId { get; set; }

        /// <summary>
        /// Navigation property to the linked meeting.
        /// </summary>
        public OneOnOne? OneOnOne { get; set; }

        /// <summary>
        /// The calendar provider identifier: "google", "outlook", "ics".
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// The event ID in the external calendar system.
        /// For Google: event ID string.
        /// For Outlook: Microsoft Graph event ID.
        /// </summary>
        public string ExternalEventId { get; set; } = string.Empty;

        /// <summary>
        /// ETag or change key for conflict detection.
        /// Used to detect if the event was modified externally.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// When this link was last synchronized.
        /// </summary>
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Direction of the last sync operation.
        /// </summary>
        public SyncDirection LastSyncDirection { get; set; } = SyncDirection.Push;

        /// <summary>
        /// Current sync status for this link.
        /// </summary>
        public CalendarLinkStatus Status { get; set; } = CalendarLinkStatus.Synced;

        /// <summary>
        /// Error message if last sync failed.
        /// </summary>
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Direction of calendar sync operations.
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>Tracker → Calendar</summary>
        Push,
        /// <summary>Calendar → Tracker</summary>
        Pull
    }

    /// <summary>
    /// Status of a calendar link.
    /// </summary>
    public enum CalendarLinkStatus
    {
        /// <summary>Successfully synced</summary>
        Synced,
        /// <summary>Pending sync (queued)</summary>
        Pending,
        /// <summary>Sync failed</summary>
        Error,
        /// <summary>External event was deleted</summary>
        Orphaned
    }
}
