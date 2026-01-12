using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.Google
{
    /// <summary>
    /// Handles Google Calendar operations including event management and Google Meet links.
    /// </summary>
    public class GoogleCalendarService
    {
        #region Singleton

        private static GoogleCalendarService? _instance;
        private static readonly object _lock = new();

        public static GoogleCalendarService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GoogleCalendarService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private CalendarService? _service;

        #endregion

        #region Constructor

        private GoogleCalendarService()
        {
            _logger = LoggingManager.GetComponentLogger("GoogleCalendar");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the calendar service with the current credentials.
        /// </summary>
        private async Task<bool> EnsureServiceAsync()
        {
            if (_service != null) return true;

            if (!GoogleAuthService.Instance.IsAuthenticated)
            {
                var success = await GoogleAuthService.Instance.TrySilentSignInAsync();
                if (!success) return false;
            }

            _service = new CalendarService(GoogleAuthService.Instance.GetServiceInitializer());
            return true;
        }

        #endregion

        #region Calendar Events

        /// <summary>
        /// Gets calendar events within a date range.
        /// </summary>
        public async Task<List<Event>?> GetEventsAsync(DateTime startDate, DateTime endDate, string? syncToken = null)
        {
            if (!await EnsureServiceAsync()) return null;

            try
            {
                var request = _service!.Events.List("primary");
                request.TimeMin = startDate;
                request.TimeMax = endDate;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                request.MaxResults = 250;

                if (!string.IsNullOrEmpty(syncToken))
                {
                    request.SyncToken = syncToken;
                }

                var events = new List<Event>();
                string? pageToken = null;

                do
                {
                    request.PageToken = pageToken;
                    var response = await request.ExecuteAsync();
                    
                    if (response.Items != null)
                    {
                        events.AddRange(response.Items);
                    }

                    pageToken = response.NextPageToken;

                    // Save sync token for incremental updates
                    if (string.IsNullOrEmpty(response.NextPageToken) && !string.IsNullOrEmpty(response.NextSyncToken))
                    {
                        UserSettingsManager.Instance.Settings.Google.CalendarSyncToken = response.NextSyncToken;
                        UserSettingsManager.Instance.SaveSettings();
                    }

                } while (!string.IsNullOrEmpty(pageToken));

                _logger.Info($"Retrieved {events.Count} calendar events");
                return events;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get calendar events");
                return null;
            }
        }

        /// <summary>
        /// Creates a calendar event for a 1:1 meeting.
        /// </summary>
        public async Task<Event?> CreateEventAsync(Meeting meeting, bool createGoogleMeet = false)
        {
            if (!await EnsureServiceAsync()) return null;
            if (meeting.Report == null) return null;

            try
            {
                var startDateTime = meeting.ScheduledAt;
                var endDateTime = meeting.ScheduledAt.AddMinutes(meeting.DurationMinutes ?? 60);

                var newEvent = new Event
                {
                    Summary = $"1:1 with {meeting.Report.FullName}",
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
                        new EventAttendee
                        {
                            Email = meeting.Report.Email,
                            DisplayName = meeting.Report.FullName,
                            ResponseStatus = "needsAction"
                        }
                    },
                    Reminders = new Event.RemindersData
                    {
                        UseDefault = false,
                        Overrides = new List<EventReminder>
                        {
                            new EventReminder { Method = "popup", Minutes = 15 },
                            new EventReminder { Method = "email", Minutes = 60 }
                        }
                    },
                    // Custom properties to identify Tracker events
                    ExtendedProperties = new Event.ExtendedPropertiesData
                    {
                        Private__ = new Dictionary<string, string>
                        {
                            { "tracker_meeting_id", meeting.Id.ToString() },
                            { "tracker_app", "true" }
                        }
                    }
                };

                // Add Google Meet if requested
                if (createGoogleMeet)
                {
                    newEvent.ConferenceData = new ConferenceData
                    {
                        CreateRequest = new CreateConferenceRequest
                        {
                            RequestId = Guid.NewGuid().ToString(),
                            ConferenceSolutionKey = new ConferenceSolutionKey
                            {
                                Type = "hangoutsMeet"
                            }
                        }
                    };
                }

                var request = _service!.Events.Insert(newEvent, "primary");
                
                if (createGoogleMeet)
                {
                    request.ConferenceDataVersion = 1;
                }

                var createdEvent = await request.ExecuteAsync();

                _logger.Info($"Created calendar event: {createdEvent.Id}");
                return createdEvent;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to create calendar event");
                return null;
            }
        }

        /// <summary>
        /// Updates an existing calendar event.
        /// </summary>
        public async Task<Event?> UpdateEventAsync(string eventId, Meeting meeting)
        {
            if (!await EnsureServiceAsync()) return null;
            if (meeting.Report == null || string.IsNullOrEmpty(eventId)) return null;

            try
            {
                // Get existing event to preserve some properties
                var existingEvent = await _service!.Events.Get("primary", eventId).ExecuteAsync();
                if (existingEvent == null) return null;

                var startDateTime = meeting.ScheduledAt;
                var endDateTime = meeting.ScheduledAt.AddMinutes(meeting.DurationMinutes ?? 60);

                existingEvent.Summary = $"1:1 with {meeting.Report.FullName}";
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

                var updatedEvent = await _service.Events.Update(existingEvent, "primary", eventId).ExecuteAsync();

                _logger.Info($"Updated calendar event: {eventId}");
                return updatedEvent;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to update calendar event: {eventId}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a calendar event.
        /// </summary>
        public async Task<bool> DeleteEventAsync(string eventId)
        {
            if (!await EnsureServiceAsync()) return false;
            if (string.IsNullOrEmpty(eventId)) return false;

            try
            {
                await _service!.Events.Delete("primary", eventId).ExecuteAsync();
                _logger.Info($"Deleted calendar event: {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to delete calendar event: {eventId}");
                return false;
            }
        }

        /// <summary>
        /// Gets a specific calendar event by ID.
        /// </summary>
        /// <param name="eventId">Event ID from Google Calendar.</param>
        /// <returns>Event details, or null if not found.</returns>
        public async Task<Event?> GetEventAsync(string eventId)
        {
            if (!await EnsureServiceAsync()) return null;
            if (string.IsNullOrEmpty(eventId)) return null;

            try
            {
                var calEvent = await _service!.Events.Get("primary", eventId).ExecuteAsync();
                _logger.Info($"Retrieved calendar event: {eventId}");
                return calEvent;
            }
            catch (Exception ex) when (ex.Message?.Contains("404") == true || ex.Message?.Contains("Not Found") == true)
            {
                _logger.Warn($"Calendar event not found: {eventId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to get calendar event: {eventId}");
                return null;
            }
        }

        /// <summary>
        /// Gets free/busy information for a user.
        /// </summary>
        public async Task<List<TimePeriod>?> GetFreeBusyAsync(string email, DateTime startDate, DateTime endDate)
        {
            if (!await EnsureServiceAsync()) return null;

            try
            {
                var request = new FreeBusyRequest
                {
                    TimeMin = startDate,
                    TimeMax = endDate,
                    Items = new List<FreeBusyRequestItem>
                    {
                        new FreeBusyRequestItem { Id = email }
                    }
                };

                var response = await _service!.Freebusy.Query(request).ExecuteAsync();

                if (response.Calendars.TryGetValue(email, out var calendar))
                {
                    return calendar.Busy.ToList();
                }

                return new List<TimePeriod>();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to get free/busy for: {email}");
                return null;
            }
        }

        /// <summary>
        /// Extracts Google Meet URL from an event.
        /// </summary>
        public static string? GetGoogleMeetUrl(Event calendarEvent)
        {
            // Check hangout link first (older format)
            if (!string.IsNullOrEmpty(calendarEvent.HangoutLink))
            {
                return calendarEvent.HangoutLink;
            }

            // Check conference data
            if (calendarEvent.ConferenceData?.EntryPoints != null)
            {
                var videoEntry = calendarEvent.ConferenceData.EntryPoints
                    .FirstOrDefault(ep => ep.EntryPointType == "video");
                if (videoEntry != null)
                {
                    return videoEntry.Uri;
                }
            }

            return null;
        }

        #endregion

        #region Helper Methods

        private string BuildEventDescription(Meeting meeting)
        {
            var description = new System.Text.StringBuilder();
            description.AppendLine($"1:1 Meeting - {meeting.Report?.FullName}");
            description.AppendLine();

            if (!string.IsNullOrEmpty(meeting.Description))
            {
                description.AppendLine($"Description: {meeting.Description}");
                description.AppendLine();
            }

            if (meeting.AgendaItems?.Any() == true)
            {
                description.AppendLine("Agenda:");
                foreach (var item in meeting.AgendaItems.Where(a => !a.IsDeleted))
                {
                    description.AppendLine($"• {item.Title}");
                }
                description.AppendLine();
            }

            description.AppendLine("---");
            description.AppendLine("Managed by Tracker");

            return description.ToString();
        }

        #endregion
    }
}

