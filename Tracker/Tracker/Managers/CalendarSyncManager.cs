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
                        meeting.LastSyncedAt = DateTime.Now;
                        meeting.SyncStatus = "Synced";
                        
                        // Extract Google Meet URL if created
                        var meetUrl = GoogleCalendarService.GetGoogleMeetUrl(createdEvent);
                        if (!string.IsNullOrEmpty(meetUrl))
                        {
                            meeting.GoogleMeetUrl = meetUrl;
                        }
                        
                        await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                        
                        // Also create CalendarLink for tracking
                        var link = new CalendarLink
                        {
                            OneOnOneId = meeting.Id,
                            ProviderId = "google",
                            ExternalEventId = createdEvent.Id,
                            ETag = createdEvent.ETag,
                            LastSyncedAt = DateTime.Now,
                            LastSyncDirection = SyncDirection.Push,
                            Status = CalendarLinkStatus.Synced
                        };
                        await TrackerDbManager.Instance.SaveCalendarLinkAsync(link);
                        
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
                        meeting.LastSyncedAt = DateTime.Now;
                        meeting.SyncStatus = "Synced";
                        await TrackerDbManager.Instance!.UpdateOneOnOneAsync(meeting);
                        
                        // Update CalendarLink
                        var link = new CalendarLink
                        {
                            OneOnOneId = meeting.Id,
                            ProviderId = "google",
                            ExternalEventId = meeting.GoogleCalendarEventId,
                            ETag = updatedEvent.ETag,
                            LastSyncedAt = DateTime.Now,
                            LastSyncDirection = SyncDirection.Push,
                            Status = CalendarLinkStatus.Synced
                        };
                        await TrackerDbManager.Instance.SaveCalendarLinkAsync(link);
                        
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
                    // Delete CalendarLink record
                    await TrackerDbManager.Instance.DeleteCalendarLinkAsync(meeting.Id, "google");
                    
                    meeting.GoogleCalendarEventId = null;
                    meeting.IsSyncedToGoogle = false;
                    meeting.GoogleMeetUrl = null;
                    meeting.SyncStatus = "NotSynced";
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
        /// Fetches the latest calendar event and updates the meeting's time fields.
        /// Call this before opening a meeting for edit to ensure time is current.
        /// Checks both Google and Outlook based on what's connected.
        /// </summary>
        /// <param name="meeting">The meeting to refresh from calendar.</param>
        /// <returns>True if calendar was fetched and time was updated.</returns>
        public async Task<bool> RefreshTimeFromCalendarAsync(OneOnOne meeting)
        {
            bool timeUpdated = false;

            // Try Outlook first (via CalendarSyncService)
            if (!string.IsNullOrEmpty(meeting.CalendarEventId))
            {
                timeUpdated = await Services.Microsoft365.CalendarSyncService.Instance.RefreshTimeFromCalendarAsync(meeting);
                if (timeUpdated) return true;
            }

            // Try Google Calendar
            if (!string.IsNullOrEmpty(meeting.GoogleCalendarEventId))
            {
                timeUpdated = await RefreshTimeFromGoogleCalendarAsync(meeting);
            }

            return timeUpdated;
        }

        /// <summary>
        /// Fetches the latest Google Calendar event and updates the meeting's time fields.
        /// </summary>
        private async Task<bool> RefreshTimeFromGoogleCalendarAsync(OneOnOne meeting)
        {
            var settings = UserSettingsManager.Instance.Settings.Google;
            if (!settings.IsConnected || string.IsNullOrEmpty(meeting.GoogleCalendarEventId))
            {
                return false;
            }

            try
            {
                // Ensure authenticated
                if (!GoogleAuthService.Instance.IsAuthenticated)
                {
                    var success = await GoogleAuthService.Instance.TrySilentSignInAsync();
                    if (!success) return false;
                }

                var calEvent = await GoogleCalendarService.Instance.GetEventAsync(meeting.GoogleCalendarEventId);
                if (calEvent == null)
                {
                    _logger.Warn("Google Calendar event not found for meeting {0}, may have been deleted", meeting.Id);
                    return false;
                }

                // Check if event has changed (different ETag)
                // Note: Google uses ETag for versioning
                if (calEvent.ETag == meeting.CalendarEventEtag)
                {
                    _logger.Debug("Google Calendar event unchanged for meeting {0}", meeting.Id);
                    return false;
                }

                // Update time fields from calendar
                bool timeChanged = false;

                if (calEvent.Start?.DateTime != null)
                {
                    var startDateTime = calEvent.Start.DateTime.Value;
                    if (meeting.Date != startDateTime.Date || meeting.StartTime != startDateTime.TimeOfDay)
                    {
                        meeting.Date = startDateTime.Date;
                        meeting.StartTime = startDateTime.TimeOfDay;
                        timeChanged = true;
                    }
                }

                if (calEvent.End?.DateTime != null)
                {
                    var endDateTime = calEvent.End.DateTime.Value;
                    if (meeting.EndTime != endDateTime.TimeOfDay)
                    {
                        meeting.EndTime = endDateTime.TimeOfDay;
                        timeChanged = true;
                    }
                }

                // Update ETag and sync timestamp
                meeting.CalendarEventEtag = calEvent.ETag;
                meeting.LastSyncedAt = DateTime.Now;

                if (timeChanged)
                {
                    _logger.Info("Updated meeting {0} time from Google Calendar: {1:d} {2:hh\\:mm}-{3:hh\\:mm}", 
                        meeting.Id, meeting.Date, meeting.StartTime, meeting.EndTime);
                    
                    await TrackerDbManager.Instance.UpdateOneOnOneAsync(meeting);
                }

                return timeChanged;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to refresh time from Google Calendar for meeting {0}", meeting.Id);
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
