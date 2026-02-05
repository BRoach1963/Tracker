using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for Google Calendar OAuth and API operations.
/// Handles authentication, token management, and calendar event CRUD.
/// </summary>
public class GoogleCalendarService
{
    #region Singleton

    private static readonly Lazy<GoogleCalendarService> _instance =
        new(() => new GoogleCalendarService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static GoogleCalendarService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "google_calendar.log");

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        catch { /* Logging should never throw */ }
    }

    #endregion

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private CalendarService? _service;
    private UserCredential? _credential;

    private GoogleCalendarService() { }

    #region OAuth Flow

    /// <summary>
    /// Initiates OAuth flow and returns the calendar integration record for storage.
    /// Opens browser for user consent.
    /// </summary>
    public async Task<CalendarIntegration?> AuthenticateAsync()
    {
        LastError = null;

        try
        {
            Log("Starting Google OAuth flow");

            // Load OAuth credentials from appsettings.json
            var (clientId, clientSecret) = AppSettingsService.Instance.GetGoogleCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Google Calendar OAuth credentials not configured in appsettings.json";
                Log(LastError);
                return null;
            }

            var secrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Request calendar access
            string[] scopes = { CalendarService.Scope.Calendar };

            // Launch OAuth browser flow
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                scopes,
                "user",
                System.Threading.CancellationToken.None);

            Log($"OAuth successful. Token expires: {_credential.Token.ExpiresInSeconds}s");

            // Create calendar service
            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "ProCohere"
            });

            // Get user's email from calendar
            var calendarList = await _service.CalendarList.List().ExecuteAsync();
            var primaryCalendar = calendarList.Items?.FirstOrDefault(c => c.Primary == true);
            var userEmail = primaryCalendar?.Id ?? "unknown";

            Log($"Connected as: {userEmail}");

            // Create integration record for storage
            var integration = new CalendarIntegration
            {
                Provider = "google",
                ExternalAccountId = userEmail,
                AccessToken = _credential.Token.AccessToken,
                RefreshToken = _credential.Token.RefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(_credential.Token.ExpiresInSeconds ?? 3600),
                SyncEnabled = true,
                LastSyncedAt = DateTime.UtcNow
            };

            return integration;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"OAuth error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Initializes service with existing credentials from database.
    /// </summary>
    public async Task<bool> InitializeFromIntegrationAsync(CalendarIntegration integration)
    {
        LastError = null;

        if (integration.Provider != "google")
        {
            LastError = "Integration is not for Google Calendar";
            return false;
        }

        if (string.IsNullOrEmpty(integration.AccessToken))
        {
            LastError = "No access token";
            return false;
        }

        try
        {
            Log("Initializing from existing integration");

            // Load OAuth credentials from appsettings.json
            var (clientId, clientSecret) = AppSettingsService.Instance.GetGoogleCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Google Calendar OAuth credentials not configured in appsettings.json";
                Log(LastError);
                return false;
            }

            // Create token from stored values
            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                AccessToken = integration.AccessToken,
                RefreshToken = integration.RefreshToken,
                ExpiresInSeconds = (long?)(integration.TokenExpiresAt - DateTime.UtcNow)?.TotalSeconds
            };

            // Recreate credential
            _credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = clientId,
                            ClientSecret = clientSecret
                        }
                    }),
                "user",
                token);

            // Create service
            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "ProCohere"
            });

            Log("Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Initialization error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Calendar Events

    /// <summary>
    /// Creates a calendar event from a ProCohere meeting.
    /// Returns the Google Calendar event ID.
    /// </summary>
    public async Task<string?> CreateEventAsync(MeetingDetail meeting)
    {
        LastError = null;

        if (_service == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating calendar event: {meeting.Title}");

            var calendarEvent = new Event
            {
                Summary = meeting.Title,
                Description = meeting.Description,
                Location = meeting.Location,
                Start = new EventDateTime
                {
                    DateTimeDateTimeOffset = meeting.ScheduledAt.HasValue ? new DateTimeOffset(meeting.ScheduledAt.Value) : null,
                    TimeZone = TimeZoneInfo.Local.Id
                },
                End = new EventDateTime
                {
                    DateTimeDateTimeOffset = meeting.ScheduledAt.HasValue ? new DateTimeOffset(meeting.ScheduledAt.Value.AddMinutes(meeting.DurationMinutes ?? 30)) : null,
                    TimeZone = TimeZoneInfo.Local.Id
                }
            };

            // Add video link as conferencing if present
            if (!string.IsNullOrEmpty(meeting.VideoLink))
            {
                calendarEvent.ConferenceData = new ConferenceData
                {
                    EntryPoints = new[]
                    {
                        new EntryPoint
                        {
                            EntryPointType = "video",
                            Uri = meeting.VideoLink
                        }
                    }
                };
            }

            // Create event
            var request = _service.Events.Insert(calendarEvent, "primary");
            var createdEvent = await request.ExecuteAsync();

            Log($"Event created: {createdEvent.Id}");
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Create event error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing calendar event.
    /// </summary>
    public async Task<bool> UpdateEventAsync(string eventId, MeetingDetail meeting)
    {
        LastError = null;

        if (_service == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating calendar event: {eventId}");

            // Fetch existing event
            var existingEvent = await _service.Events.Get("primary", eventId).ExecuteAsync();

            // Update fields
            existingEvent.Summary = meeting.Title;
            existingEvent.Description = meeting.Description;
            existingEvent.Location = meeting.Location;
            existingEvent.Start = new EventDateTime
            {
                DateTimeDateTimeOffset = meeting.ScheduledAt.HasValue ? new DateTimeOffset(meeting.ScheduledAt.Value) : null,
                TimeZone = TimeZoneInfo.Local.Id
            };
            existingEvent.End = new EventDateTime
            {
                DateTimeDateTimeOffset = meeting.ScheduledAt.HasValue ? new DateTimeOffset(meeting.ScheduledAt.Value.AddMinutes(meeting.DurationMinutes ?? 30)) : null,
                TimeZone = TimeZoneInfo.Local.Id
            };

            // Update event
            var request = _service.Events.Update(existingEvent, "primary", eventId);
            await request.ExecuteAsync();

            Log($"Event updated: {eventId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Update event error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    public async Task<bool> DeleteEventAsync(string eventId)
    {
        LastError = null;

        if (_service == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting calendar event: {eventId}");

            await _service.Events.Delete("primary", eventId).ExecuteAsync();

            Log($"Event deleted: {eventId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Delete event error: {ex.Message}");
            return false;
        }
    }

    #endregion
}
