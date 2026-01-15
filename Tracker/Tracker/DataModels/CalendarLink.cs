using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a user's connection to an external calendar provider (Google, Microsoft, Apple, etc.).
    /// Stores authentication tokens and sync preferences for a specific calendar account.
    /// Maps to: calendar_links (20 columns)
    /// </summary>
    [Table("calendar_links")]
    public class CalendarLink
    {
        /// <summary>
        /// Unique identifier (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// FK to the user who owns this calendar account connection.
        /// Maps to: user_id UUID NOT NULL
        /// </summary>
        [Column("user_id")]
        public Guid UserId { get; set; }

        /// <summary>
        /// The calendar provider (stored as string for PostgreSQL enum).
        /// Maps to: provider calendar_provider (enum) NOT NULL
        /// </summary>
        [Column("provider")]
        [MaxLength(50)]
        public string ProviderString { get; set; } = "google";

        /// <summary>
        /// The calendar provider as enum.
        /// </summary>
        [NotMapped]
        public CalendarProviderType Provider
        {
            get => ProviderString switch
            {
                "google" => CalendarProviderType.Google,
                "microsoft" => CalendarProviderType.Microsoft,
                "apple" => CalendarProviderType.Apple,
                _ => CalendarProviderType.Other
            };
            set => ProviderString = value switch
            {
                CalendarProviderType.Google => "google",
                CalendarProviderType.Microsoft => "microsoft",
                CalendarProviderType.Apple => "apple",
                _ => "other"
            };
        }

        /// <summary>
        /// The email address of the calendar account.
        /// Maps to: account_email VARCHAR(255) NULL
        /// </summary>
        [Column("account_email")]
        [MaxLength(255)]
        public string? AccountEmail { get; set; }

        /// <summary>
        /// Display name for this calendar account.
        /// Maps to: account_name VARCHAR(200) NULL
        /// </summary>
        [Column("account_name")]
        [MaxLength(200)]
        public string? AccountName { get; set; }

        #region Authentication

        /// <summary>
        /// OAuth access token for this calendar provider.
        /// Maps to: access_token TEXT NULL
        /// </summary>
        [Column("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// OAuth refresh token for obtaining new access tokens.
        /// Maps to: refresh_token TEXT NULL
        /// </summary>
        [Column("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// When the current access token expires.
        /// Maps to: token_expires_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("token_expires_at")]
        public DateTime? TokenExpiresAt { get; set; }

        /// <summary>
        /// Delta sync token from the provider for incremental synchronization.
        /// Google Calendar: syncToken from Events.list response
        /// Outlook: deltaLink from delta query
        /// Maps to: sync_token TEXT NULL (added via ALTER)
        /// </summary>
        [Column("sync_token")]
        public string? SyncToken { get; set; }

        #endregion

        #region Sync Settings

        /// <summary>
        /// Whether this calendar link is currently active/enabled.
        /// Maps to: is_active BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether syncing is enabled for this provider.
        /// Maps to: sync_enabled BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("sync_enabled")]
        public bool SyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether to sync Tracker meetings to this provider's calendar.
        /// Maps to: sync_meetings_to_calendar BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("sync_meetings_to_calendar")]
        public bool SyncMeetingsToCalendar { get; set; } = true;

        /// <summary>
        /// Whether to sync Tracker tasks to this provider's calendar.
        /// Maps to: sync_tasks_to_calendar BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("sync_tasks_to_calendar")]
        public bool SyncTasksToCalendar { get; set; } = false;

        /// <summary>
        /// Whether to auto-create Tracker meetings from calendar events.
        /// Maps to: create_meeting_from_calendar BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("create_meeting_from_calendar")]
        public bool CreateMeetingFromCalendar { get; set; } = false;

        #endregion

        #region Calendar Selection

        /// <summary>
        /// The default calendar ID in this provider to sync to.
        /// Maps to: default_calendar_id VARCHAR(255) NULL
        /// </summary>
        [Column("default_calendar_id")]
        [MaxLength(255)]
        public string? DefaultCalendarId { get; set; }

        /// <summary>
        /// Display name of the default calendar.
        /// Maps to: default_calendar_name VARCHAR(200) NULL
        /// </summary>
        [Column("default_calendar_name")]
        [MaxLength(200)]
        public string? DefaultCalendarName { get; set; }

        #endregion

        #region Sync State

        /// <summary>
        /// When this calendar link was last synchronized.
        /// Maps to: last_sync_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("last_sync_at")]
        public DateTime? LastSyncAt { get; set; }

        /// <summary>
        /// Sync status (stored as string for PostgreSQL enum).
        /// Maps to: last_sync_status calendar_sync_status (enum) NULL
        /// </summary>
        [Column("last_sync_status")]
        [MaxLength(50)]
        public string? LastSyncStatusString { get; set; }

        /// <summary>
        /// Sync status as enum.
        /// </summary>
        [NotMapped]
        public CalendarSyncStatusType? LastSyncStatus
        {
            get => LastSyncStatusString switch
            {
                "pending" => CalendarSyncStatusType.Pending,
                "synced" => CalendarSyncStatusType.Synced,
                "failed" => CalendarSyncStatusType.Failed,
                "cancelled" => CalendarSyncStatusType.Cancelled,
                _ => null
            };
            set => LastSyncStatusString = value switch
            {
                CalendarSyncStatusType.Pending => "pending",
                CalendarSyncStatusType.Synced => "synced",
                CalendarSyncStatusType.Failed => "failed",
                CalendarSyncStatusType.Cancelled => "cancelled",
                _ => null
            };
        }

        /// <summary>
        /// Error message from the last failed sync attempt.
        /// Maps to: last_sync_error TEXT NULL
        /// </summary>
        [Column("last_sync_error")]
        public string? LastSyncError { get; set; }

        #endregion

        #region Timestamps

        /// <summary>
        /// When this record was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Navigation to the user.
        /// </summary>
        [NotMapped]
        public User? User { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether the access token is expired or about to expire (within 5 minutes).
        /// </summary>
        [NotMapped]
        public bool IsTokenExpired => TokenExpiresAt == null || TokenExpiresAt <= DateTime.UtcNow.AddMinutes(5);

        /// <summary>
        /// Whether this link is ready to sync (active, enabled, and auth valid).
        /// </summary>
        [NotMapped]
        public bool IsReadyToSync => IsActive && SyncEnabled && !IsTokenExpired && !string.IsNullOrEmpty(AccessToken);

        /// <summary>
        /// Whether the last sync was successful.
        /// </summary>
        [NotMapped]
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
    /// Sync status for a calendar provider connection.
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
