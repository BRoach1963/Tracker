using System;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Common.Enums;

namespace Tracker.Services
{
    /// <summary>
    /// Service for interacting with Google Calendar API.
    /// </summary>
    public class GoogleCalendarService
    {
        private static readonly Lazy<GoogleCalendarService> _instance = new(() => new GoogleCalendarService());
        public static GoogleCalendarService Instance => _instance.Value;

        private readonly LoggingManager.Logger _logger = new("GoogleCalendar", "GoogleCalendar");
        private CalendarService? _calendarService;

        private GoogleCalendarService()
        {
        }

        /// <summary>
        /// Initializes the Google Calendar service with an access token.
        /// </summary>
        public void Initialize(string accessToken)
        {
            try
            {
                // Decrypt token if needed (for backward compatibility)
                var encryptionService = TokenEncryptionService.Instance;
                var decryptedToken = encryptionService.IsEncrypted(accessToken) 
                    ? encryptionService.Decrypt(accessToken) 
                    : accessToken;

                if (string.IsNullOrEmpty(decryptedToken))
                {
                    _logger.Error("Failed to decrypt or access token is empty");
                    throw new InvalidOperationException("Invalid access token");
                }

                var initializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = Google.Apis.Auth.OAuth2.GoogleCredential.FromAccessToken(decryptedToken),
                    ApplicationName = "Tracker"
                };

                _calendarService = new CalendarService(initializer);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error initializing Google Calendar service");
                throw;
            }
        }

        /// <summary>
        /// Creates a calendar event from a 1:1 meeting.
        /// </summary>
        public async Task<string?> CreateEventAsync(OneOnOne meeting)
        {
            if (_calendarService == null)
            {
                _logger.Error("Google Calendar service not initialized");
                return null;
            }

            try
            {
                var startDateTime = meeting.Date.Date.Add(meeting.StartTime);
                var endDateTime = meeting.Date.Date.Add(meeting.EndTime);

                var eventItem = new Event
                {
                    Summary = $"1:1 with {meeting.TeamMemberName}",
                    Description = BuildEventDescription(meeting),
                    Start = new EventDateTime
                    {
                        DateTime = startDateTime,
                        TimeZone = TimeZoneInfo.Local.Id
                    },
                    End = new EventDateTime
                    {
                        DateTime = endDateTime,
                        TimeZone = TimeZoneInfo.Local.Id
                    },
                    Attendees = new List<EventAttendee>
                    {
                        new EventAttendee { Email = meeting.TeamMember.Email }
                    },
                    Reminders = new Event.RemindersData
                    {
                        UseDefault = false,
                        Overrides = new List<EventReminder>
                        {
                            new EventReminder { Method = "email", Minutes = 15 },
                            new EventReminder { Method = "popup", Minutes = 15 }
                        }
                    }
                };

                var request = _calendarService.Events.Insert(eventItem, "primary");
                var createdEvent = await request.ExecuteAsync();

                _logger.Info("Created Google Calendar event: {0}", createdEvent.Id);
                return createdEvent.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error creating Google Calendar event for meeting {0}", meeting.Id);
                return null;
            }
        }

        /// <summary>
        /// Updates an existing calendar event.
        /// </summary>
        public async Task<bool> UpdateEventAsync(string eventId, OneOnOne meeting)
        {
            if (_calendarService == null)
            {
                _logger.Error("Google Calendar service not initialized");
                return false;
            }

            try
            {
                var existingEvent = await _calendarService.Events.Get("primary", eventId).ExecuteAsync();
                if (existingEvent == null)
                {
                    _logger.Warn("Google Calendar event {0} not found", eventId);
                    return false;
                }

                var startDateTime = meeting.Date.Date.Add(meeting.StartTime);
                var endDateTime = meeting.Date.Date.Add(meeting.EndTime);

                existingEvent.Summary = $"1:1 with {meeting.TeamMemberName}";
                existingEvent.Description = BuildEventDescription(meeting);
                existingEvent.Start = new EventDateTime
                {
                    DateTime = startDateTime,
                    TimeZone = TimeZoneInfo.Local.Id
                };
                existingEvent.End = new EventDateTime
                {
                    DateTime = endDateTime,
                    TimeZone = TimeZoneInfo.Local.Id
                };

                // Update attendees
                existingEvent.Attendees = new List<EventAttendee>
                {
                    new EventAttendee { Email = meeting.TeamMember.Email }
                };

                var request = _calendarService.Events.Update(existingEvent, "primary", eventId);
                await request.ExecuteAsync();

                _logger.Info("Updated Google Calendar event: {0}", eventId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating Google Calendar event {0}", eventId);
                return false;
            }
        }

        /// <summary>
        /// Deletes a calendar event.
        /// </summary>
        public async Task<bool> DeleteEventAsync(string eventId)
        {
            if (_calendarService == null)
            {
                _logger.Error("Google Calendar service not initialized");
                return false;
            }

            try
            {
                await _calendarService.Events.Delete("primary", eventId).ExecuteAsync();
                _logger.Info("Deleted Google Calendar event: {0}", eventId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting Google Calendar event {0}", eventId);
                return false;
            }
        }

        /// <summary>
        /// Builds a description for the calendar event from meeting data.
        /// </summary>
        private string BuildEventDescription(OneOnOne meeting)
        {
            var description = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(meeting.Description))
            {
                description.AppendLine(meeting.Description);
                description.AppendLine();
            }

            if (!string.IsNullOrEmpty(meeting.Agenda))
            {
                description.AppendLine("Agenda:");
                description.AppendLine(meeting.Agenda);
                description.AppendLine();
            }

            if (meeting.LinkedTasks?.Count > 0)
            {
                description.AppendLine($"Linked Tasks: {meeting.LinkedTasks.Count}");
            }

            if (meeting.LinkedOkrs?.Count > 0)
            {
                description.AppendLine($"Linked OKRs: {meeting.LinkedOkrs.Count}");
            }

            if (meeting.LinkedKpis?.Count > 0)
            {
                description.AppendLine($"Linked KPIs: {meeting.LinkedKpis.Count}");
            }

            return description.ToString().TrimEnd();
        }
    }
}

