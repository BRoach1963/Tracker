using System;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages synchronization of 1:1 meetings with calendar providers (Google Calendar, Outlook).
    /// </summary>
    public class CalendarSyncManager
    {
        private static readonly Lazy<CalendarSyncManager> _instance = new(() => new CalendarSyncManager());
        public static CalendarSyncManager Instance => _instance.Value;

        private readonly LoggingManager.Logger _logger = new("CalendarSync", "CalendarSync");

        private CalendarSyncManager()
        {
        }

        /// <summary>
        /// Syncs a 1:1 meeting to Google Calendar if enabled.
        /// </summary>
        public async Task<bool> SyncToGoogleCalendarAsync(OneOnOne meeting)
        {
            var settings = UserSettingsManager.Instance.Settings.Calendar;
            
            if (!settings.GoogleCalendarEnabled || string.IsNullOrEmpty(settings.GoogleAccessToken))
            {
                return false;
            }

            try
            {
                // Get valid access token (refresh if needed)
                var accessToken = await GoogleAuthService.Instance.GetValidAccessTokenAsync(settings);
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.Error("Unable to get valid Google access token");
                    return false;
                }

                // Initialize calendar service
                GoogleCalendarService.Instance.Initialize(accessToken);

                // Create or update event
                string? eventId;
                if (string.IsNullOrEmpty(meeting.GoogleCalendarEventId))
                {
                    // Create new event
                    eventId = await GoogleCalendarService.Instance.CreateEventAsync(meeting);
                    if (eventId != null)
                    {
                        meeting.GoogleCalendarEventId = eventId;
                        meeting.IsSyncedToGoogle = true;
                        await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                        _logger.Info("Synced meeting {0} to Google Calendar", meeting.Id);
                        return true;
                    }
                }
                else
                {
                    // Update existing event
                    var success = await GoogleCalendarService.Instance.UpdateEventAsync(meeting.GoogleCalendarEventId, meeting);
                    if (success)
                    {
                        meeting.IsSyncedToGoogle = true;
                        await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                        _logger.Info("Updated Google Calendar event for meeting {0}", meeting.Id);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error syncing meeting {0} to Google Calendar", meeting.Id);
                return false;
            }
        }

        /// <summary>
        /// Removes a 1:1 meeting from Google Calendar.
        /// </summary>
        public async Task<bool> UnsyncFromGoogleCalendarAsync(OneOnOne meeting)
        {
            if (string.IsNullOrEmpty(meeting.GoogleCalendarEventId))
            {
                return true; // Nothing to unsync
            }

            var settings = UserSettingsManager.Instance.Settings.Calendar;
            if (!settings.GoogleCalendarEnabled || string.IsNullOrEmpty(settings.GoogleAccessToken))
            {
                return false;
            }

            try
            {
                var accessToken = await GoogleAuthService.Instance.GetValidAccessTokenAsync(settings);
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.Error("Unable to get valid Google access token for unsync");
                    return false;
                }

                GoogleCalendarService.Instance.Initialize(accessToken);
                var success = await GoogleCalendarService.Instance.DeleteEventAsync(meeting.GoogleCalendarEventId);
                
                if (success)
                {
                    meeting.GoogleCalendarEventId = null;
                    meeting.IsSyncedToGoogle = false;
                    await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                    _logger.Info("Removed meeting {0} from Google Calendar", meeting.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unsyncing meeting {0} from Google Calendar", meeting.Id);
                return false;
            }
        }

        /// <summary>
        /// Syncs a 1:1 meeting to all enabled calendar providers.
        /// </summary>
        public async Task SyncToAllCalendarsAsync(OneOnOne meeting)
        {
            var googleSuccess = await SyncToGoogleCalendarAsync(meeting);
            
            // Future: Add Outlook sync here
            
            if (googleSuccess)
            {
                NotificationManager.Instance.ShowSuccess("Calendar Sync", "Meeting synced to Google Calendar");
            }
        }
    }
}

