using System;

namespace Tracker.Classes
{
    /// <summary>
    /// Settings for calendar and email integrations.
    /// </summary>
    public class CalendarSettings
    {
        #region Google Calendar

        /// <summary>
        /// Whether Google Calendar integration is enabled.
        /// </summary>
        public bool GoogleCalendarEnabled { get; set; } = false;

        /// <summary>
        /// Google OAuth2 access token (encrypted).
        /// </summary>
        public string? GoogleAccessToken { get; set; }

        /// <summary>
        /// Google OAuth2 refresh token (encrypted).
        /// </summary>
        public string? GoogleRefreshToken { get; set; }

        /// <summary>
        /// When the Google access token expires.
        /// </summary>
        public DateTime? GoogleTokenExpiry { get; set; }

        /// <summary>
        /// Google user email address.
        /// </summary>
        public string? GoogleUserEmail { get; set; }

        #endregion

        #region Outlook Calendar

        /// <summary>
        /// Whether Outlook Calendar integration is enabled.
        /// </summary>
        public bool OutlookCalendarEnabled { get; set; } = false;

        /// <summary>
        /// Outlook/Microsoft access token (encrypted).
        /// </summary>
        public string? OutlookAccessToken { get; set; }

        /// <summary>
        /// Outlook/Microsoft refresh token (encrypted).
        /// </summary>
        public string? OutlookRefreshToken { get; set; }

        /// <summary>
        /// When the Outlook access token expires.
        /// </summary>
        public DateTime? OutlookTokenExpiry { get; set; }

        /// <summary>
        /// Outlook user email address.
        /// </summary>
        public string? OutlookUserEmail { get; set; }

        #endregion

        #region Sync Options

        /// <summary>
        /// Automatically sync 1:1 meetings to calendars when created/updated.
        /// </summary>
        public bool AutoSyncOnSave { get; set; } = true;

        /// <summary>
        /// Send meeting invitations via email when syncing.
        /// </summary>
        public bool SyncMeetingInvitations { get; set; } = true;

        /// <summary>
        /// Send meeting summaries via email after meetings.
        /// </summary>
        public bool SyncMeetingSummaries { get; set; } = false;

        #endregion
    }
}

