using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using ProCohere.Avalonia.Models;
using MsalPrompt = Microsoft.Identity.Client.Prompt;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for Microsoft Calendar integration via Microsoft Graph API.
/// Handles OAuth authentication and calendar event CRUD operations.
/// </summary>
public class MicrosoftCalendarService
{
    #region Singleton

    private static readonly Lazy<MicrosoftCalendarService> _instance =
        new(() => new MicrosoftCalendarService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static MicrosoftCalendarService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "microsoft_calendar.log");

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

    /// <summary>
    /// Gets whether the service is currently authenticated with Microsoft Graph.
    /// </summary>
    public bool IsAuthenticated => _graphClient != null;

    /// <summary>
    /// Gets the authenticated GraphServiceClient. Returns null if not authenticated.
    /// </summary>
    public GraphServiceClient? GraphClient => _graphClient;

    private GraphServiceClient? _graphClient;
    private IPublicClientApplication? _publicClientApp;

    private MicrosoftCalendarService() { }

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
            Log("Starting Microsoft OAuth flow");

            // Load OAuth credentials from appsettings.json
            var (clientId, clientSecret) = AppSettingsService.Instance.GetMicrosoftCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Microsoft Calendar OAuth credentials not configured in appsettings.json";
                Log(LastError);
                return null;
            }

            var scopes = new[] { "User.Read", "Calendars.ReadWrite", "offline_access" };

            // Create public client application
            _publicClientApp = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                .WithRedirectUri("http://localhost")
                .Build();

            // Interactive login - opens browser
            var authResult = await _publicClientApp
                .AcquireTokenInteractive(scopes)
                .WithPrompt(MsalPrompt.SelectAccount)
                .ExecuteAsync();

            // Create Graph client using HttpClient with bearer token
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            _graphClient = new GraphServiceClient(httpClient);

            // Get user info
            var user = await _graphClient.Me.GetAsync();

            Log($"OAuth successful for user: {user?.Mail ?? user?.UserPrincipalName}");

            // Return integration record for storage
            return new CalendarIntegration
            {
                Id = Guid.NewGuid(),
                Provider = "microsoft",
                ExternalAccountId = user?.Mail ?? user?.UserPrincipalName,
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.Account.HomeAccountId.Identifier, // Store account identifier
                TokenExpiresAt = authResult.ExpiresOn.UtcDateTime,
                SyncEnabled = true,
                LastSyncedAt = DateTime.UtcNow
            };
        }
        catch (MsalException ex)
        {
            LastError = $"OAuth failed: {ex.Message}";
            Log(LastError);
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"Unexpected error: {ex.Message}";
            Log($"AuthenticateAsync failed: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Initialize service from stored integration (for subsequent uses).
    /// </summary>
    public async Task<bool> InitializeFromIntegrationAsync(CalendarIntegration integration)
    {
        if (integration == null)
        {
            LastError = "Integration is null";
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
            var (clientId, clientSecret) = AppSettingsService.Instance.GetMicrosoftCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Microsoft Calendar OAuth credentials not configured in appsettings.json";
                Log(LastError);
                return false;
            }

            // Create Graph client with stored token
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);
            _graphClient = new GraphServiceClient(httpClient);

            // Test the token
            try
            {
                await _graphClient.Me.GetAsync();
                Log("Token is valid");
                return true;
            }
            catch
            {
                // Token expired or invalid - would need to refresh
                LastError = "Token expired or invalid - re-authentication required";
                Log(LastError);
                return false;
            }
        }
        catch (Exception ex)
        {
            LastError = $"Initialization failed: {ex.Message}";
            Log($"InitializeFromIntegrationAsync failed: {ex}");
            return false;
        }
    }

    #endregion

    #region Calendar Operations

    /// <summary>
    /// Create a calendar event from a meeting.
    /// </summary>
    public async Task<string?> CreateEventAsync(MeetingDetail meeting)
    {
        if (_graphClient == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Creating calendar event for meeting: {meeting.Title}");

            var calendarEvent = new Event
            {
                Subject = meeting.Title,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = meeting.Notes ?? string.Empty
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = meeting.ScheduledAt.HasValue ? meeting.ScheduledAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = meeting.ScheduledAt.HasValue 
                        ? meeting.ScheduledAt.Value.AddMinutes(meeting.DurationMinutes ?? 30).ToString("yyyy-MM-ddTHH:mm:ss") 
                        : DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                Location = string.IsNullOrEmpty(meeting.Location) ? null : new Location
                {
                    DisplayName = meeting.Location
                }
            };

            var createdEvent = await _graphClient.Me.Calendar.Events.PostAsync(calendarEvent);

            if (createdEvent?.Id != null)
            {
                Log($"Event created successfully: {createdEvent.Id}");
                return createdEvent.Id;
            }

            LastError = "Event creation returned null";
            return null;
        }
        catch (Exception ex)
        {
            LastError = $"Create event failed: {ex.Message}";
            Log($"CreateEventAsync failed: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Update an existing calendar event.
    /// </summary>
    public async Task<bool> UpdateEventAsync(string eventId, MeetingDetail meeting)
    {
        if (_graphClient == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating calendar event: {eventId}");

            var updatedEvent = new Event
            {
                Subject = meeting.Title,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = meeting.Notes ?? string.Empty
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = meeting.ScheduledAt.HasValue ? meeting.ScheduledAt.Value.ToString("yyyy-MM-ddTHH:mm:ss") : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = meeting.ScheduledAt.HasValue 
                        ? meeting.ScheduledAt.Value.AddMinutes(meeting.DurationMinutes ?? 30).ToString("yyyy-MM-ddTHH:mm:ss") 
                        : DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                Location = string.IsNullOrEmpty(meeting.Location) ? null : new Location
                {
                    DisplayName = meeting.Location
                }
            };

            await _graphClient.Me.Calendar.Events[eventId].PatchAsync(updatedEvent);
            Log("Event updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Update event failed: {ex.Message}";
            Log($"UpdateEventAsync failed: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Delete a calendar event.
    /// </summary>
    public async Task<bool> DeleteEventAsync(string eventId)
    {
        if (_graphClient == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting calendar event: {eventId}");
            await _graphClient.Me.Calendar.Events[eventId].DeleteAsync();
            Log("Event deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"Delete event failed: {ex.Message}";
            Log($"DeleteEventAsync failed: {ex}");
            return false;
        }
    }

    #endregion
}
