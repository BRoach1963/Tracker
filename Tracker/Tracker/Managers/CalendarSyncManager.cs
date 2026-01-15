using System;
using System.Threading.Tasks;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Google;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages synchronization of meetings with calendar providers (Google Calendar, Outlook).
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
        /// Syncs a meeting to Google Calendar if enabled.
        /// </summary>
        public async Task<bool> SyncToGoogleCalendarAsync(Meeting meeting, bool createGoogleMeet = false)
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

                // Check if already synced to a different provider
                if (!string.IsNullOrEmpty(meeting.CalendarEventId) && meeting.CalendarProviderString != "google")
                {
                    _logger.Warn("Meeting {0} is already synced to {1}, cannot sync to Google", meeting.Id, meeting.CalendarProviderString);
                    return false;
                }

                // Create or update event
                if (string.IsNullOrEmpty(meeting.CalendarEventId))
                {
                    // Create new event
                    var createdEvent = await GoogleCalendarService.Instance.CreateEventAsync(meeting, createGoogleMeet);
                    if (createdEvent != null)
                    {
                        meeting.CalendarEventId = createdEvent.Id;
                        meeting.CalendarProviderString = "google";
                        meeting.LastSyncedAt = DateTime.UtcNow;
                        meeting.CalendarSyncStatus = "synced";
                        
                        // Extract Google Meet URL if created
                        var meetUrl = GoogleCalendarService.GetGoogleMeetUrl(createdEvent);
                        if (!string.IsNullOrEmpty(meetUrl))
                        {
                            meeting.VideoConferenceUrl = meetUrl;
                            meeting.VideoConferenceProviderString = "google_meet";
                        }
                        
                        var meetingRepository = CreateMeetingRepository();
                        if (meetingRepository != null)
                        {
                            await meetingRepository.UpdateMeetingAsync(meeting);
                        }
                        
                        _logger.Info("Synced meeting {0} to Google Calendar", meeting.Id);
                        return true;
                    }
                }
                else
                {
                    // Update existing event
                    var updatedEvent = await GoogleCalendarService.Instance.UpdateEventAsync(meeting.CalendarEventId, meeting);
                    if (updatedEvent != null)
                    {
                        meeting.LastSyncedAt = DateTime.UtcNow;
                        meeting.CalendarSyncStatus = "synced";
                        var meetingRepository = CreateMeetingRepository();
                        if (meetingRepository != null)
                        {
                            await meetingRepository.UpdateMeetingAsync(meeting);
                        }
                        
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
        /// Removes a meeting from Google Calendar.
        /// </summary>
        public async Task<bool> UnsyncFromGoogleCalendarAsync(Meeting meeting)
        {
            // Only unsync if it's synced to Google
            if (string.IsNullOrEmpty(meeting.CalendarEventId) || meeting.CalendarProviderString != "google")
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

                var success = await GoogleCalendarService.Instance.DeleteEventAsync(meeting.CalendarEventId);
                
                if (success)
                {
                    meeting.CalendarEventId = null;
                    meeting.CalendarProviderString = null;
                    meeting.CalendarEtag = null;
                    meeting.CalendarSyncStatus = "not_synced";
                    
                    // Clear video conference if it was Google Meet
                    if (meeting.VideoConferenceProviderString == "google_meet")
                    {
                        meeting.VideoConferenceUrl = null;
                        meeting.VideoConferenceProviderString = null;
                        meeting.VideoConferenceId = null;
                    }

                    var meetingRepository = CreateMeetingRepository();
                    if (meetingRepository != null)
                    {
                        await meetingRepository.UpdateMeetingAsync(meeting);
                    }
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
        public async Task<bool> RefreshTimeFromCalendarAsync(Meeting meeting)
        {
            if (string.IsNullOrEmpty(meeting.CalendarEventId))
            {
                return false;
            }

            // Route to appropriate provider
            return meeting.CalendarProviderString switch
            {
                "microsoft" => await Services.Microsoft365.CalendarSyncService.Instance.RefreshTimeFromCalendarAsync(meeting),
                "google" => await RefreshTimeFromGoogleCalendarAsync(meeting),
                _ => false
            };
        }

        /// <summary>
        /// Fetches the latest Google Calendar event and updates the meeting's time fields.
        /// </summary>
        private async Task<bool> RefreshTimeFromGoogleCalendarAsync(Meeting meeting)
        {
            var settings = UserSettingsManager.Instance.Settings.Google;
            if (!settings.IsConnected || string.IsNullOrEmpty(meeting.CalendarEventId) || meeting.CalendarProviderString != "google")
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

                var calEvent = await GoogleCalendarService.Instance.GetEventAsync(meeting.CalendarEventId);
                if (calEvent == null)
                {
                    _logger.Warn("Google Calendar event not found for meeting {0}, may have been deleted", meeting.Id);
                    return false;
                }
                // Update scheduling fields from calendar (calendar is authoritative for time)
                bool timeChanged = false;

                DateTime? startDateTime = calEvent.Start?.DateTime;
                DateTime? endDateTime = calEvent.End?.DateTime;

                if (startDateTime.HasValue)
                {
                    var startLocal = startDateTime.Value;
                    if (meeting.ScheduledAt != startLocal)
                    {
                        meeting.ScheduledAt = startLocal;
                        timeChanged = true;
                    }
                }

                if (endDateTime.HasValue && startDateTime.HasValue)
                {
                    var durationMinutes = (int)Math.Round((endDateTime.Value - startDateTime.Value).TotalMinutes);
                    if (meeting.DurationMinutes != durationMinutes)
                    {
                        meeting.DurationMinutes = durationMinutes;
                        timeChanged = true;
                    }
                }

                meeting.LastSyncedAt = DateTime.UtcNow;

                if (timeChanged)
                {
                    _logger.Info("Updated meeting {0} time from Google Calendar: {1:g} ({2} min)", 
                        meeting.Id, meeting.ScheduledAt, meeting.DurationMinutes);
                    
                    var meetingRepository = CreateMeetingRepository();
                    if (meetingRepository != null)
                    {
                        await meetingRepository.UpdateMeetingAsync(meeting);
                    }
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
        /// Syncs a meeting to all enabled calendar providers.
        /// </summary>
        public async Task SyncToAllCalendarsAsync(Meeting meeting, bool createMeetingLinks = false)
        {
            var googleSuccess = await SyncToGoogleCalendarAsync(meeting, createMeetingLinks);
            
            // Future: Add Outlook sync here
            // var outlookSuccess = await SyncToOutlookCalendarAsync(meeting, createMeetingLinks);
            
            if (googleSuccess)
            {
                NotificationManager.Instance.ShowSuccess("Calendar Sync", "Meeting synced to Google Calendar");
            }
        }

        private static MeetingRepository? CreateMeetingRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new MeetingRepository(context, userId.Value, () => contextFactory.CreateContext());
        }
    }
}
