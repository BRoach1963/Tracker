using System;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Google;

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
        public async Task<bool> SyncToGoogleCalendarAsync(OneOnOne meeting, bool createGoogleMeet = false)
        {
            var settings = UserSettingsManager.Instance.Settings.Google;
            
            if (!settings.IsConnected || !settings.CalendarSyncEnabled)
            {
                return false;
            }

            try
            {
                // Ensure authenticated
                if (!GoogleAuthService.Instance.IsAuthenticated)
                {
                    var success = await GoogleAuthService.Instance.TrySilentSignInAsync();
                    if (!success)
                    {
                        _logger.Error("Unable to authenticate with Google");
                        return false;
                    }
                }

                // Create or update event
                if (string.IsNullOrEmpty(meeting.GoogleCalendarEventId))
                {
                    // Create new event
                    var createdEvent = await GoogleCalendarService.Instance.CreateEventAsync(meeting, createGoogleMeet);
                    if (createdEvent != null)
                    {
                        meeting.GoogleCalendarEventId = createdEvent.Id;
                        meeting.IsSyncedToGoogle = true;
                        
                        // Extract Google Meet URL if created
                        var meetUrl = GoogleCalendarService.GetGoogleMeetUrl(createdEvent);
                        if (!string.IsNullOrEmpty(meetUrl))
                        {
                            meeting.GoogleMeetUrl = meetUrl;
                        }
                        
                        await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                        _logger.Info("Synced meeting {0} to Google Calendar", meeting.Id);
                        return true;
                    }
                }
                else
                {
                    // Update existing event
                    var updatedEvent = await GoogleCalendarService.Instance.UpdateEventAsync(meeting.GoogleCalendarEventId, meeting);
                    if (updatedEvent != null)
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

            var settings = UserSettingsManager.Instance.Settings.Google;
            if (!settings.IsConnected)
            {
                return false;
            }

            try
            {
                // Ensure authenticated
                if (!GoogleAuthService.Instance.IsAuthenticated)
                {
                    var authSuccess = await GoogleAuthService.Instance.TrySilentSignInAsync();
                    if (!authSuccess)
                    {
                        _logger.Error("Unable to authenticate with Google for unsync");
                        return false;
                    }
                }

                var success = await GoogleCalendarService.Instance.DeleteEventAsync(meeting.GoogleCalendarEventId);
                
                if (success)
                {
                    meeting.GoogleCalendarEventId = null;
                    meeting.IsSyncedToGoogle = false;
                    meeting.GoogleMeetUrl = null;
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
        public async Task SyncToAllCalendarsAsync(OneOnOne meeting, bool createMeetingLinks = false)
        {
            var googleSuccess = await SyncToGoogleCalendarAsync(meeting, createMeetingLinks);
            
            // Future: Add Outlook sync here
            // var outlookSuccess = await SyncToOutlookCalendarAsync(meeting, createMeetingLinks);
            
            if (googleSuccess)
            {
                NotificationManager.Instance.ShowSuccess("Calendar Sync", "Meeting synced to Google Calendar");
            }
        }
    }
}
