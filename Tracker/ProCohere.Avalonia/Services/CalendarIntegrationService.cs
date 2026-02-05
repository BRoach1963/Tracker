using System;
using System.IO;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing calendar integrations in Supabase.
/// Handles CRUD for calendar_integrations table.
/// </summary>
public class CalendarIntegrationService
{
    #region Singleton

    private static readonly Lazy<CalendarIntegrationService> _instance =
        new(() => new CalendarIntegrationService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static CalendarIntegrationService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "calendar_integration.log");

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

    public string? LastError { get; private set; }

    private CalendarIntegrationService() { }

    /// <summary>
    /// Gets the active Google Calendar integration for the current user.
    /// </summary>
    public async Task<CalendarIntegration?> GetGoogleIntegrationAsync()
    {
        return await GetIntegrationAsync("google");
    }

    /// <summary>
    /// Gets the active Microsoft Calendar integration for the current user.
    /// </summary>
    public async Task<CalendarIntegration?> GetMicrosoftIntegrationAsync()
    {
        return await GetIntegrationAsync("microsoft");
    }

    /// <summary>
    /// Gets the active calendar integration for the specified provider.
    /// </summary>
    private async Task<CalendarIntegration?> GetIntegrationAsync(string provider)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var integration = await client
                .From<CalendarIntegration>()
                .Filter("team_member_id", Operator.Equals, session.TeamMember.Id)
                .Filter("provider", Operator.Equals, provider)
                .Filter("is_deleted", Operator.Equals, false)
                .Single();

            return integration;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error getting {provider} integration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates or updates a calendar integration.
    /// </summary>
    public async Task<CalendarIntegration?> SaveIntegrationAsync(CalendarIntegration integration)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            // Set required fields
            integration.OrganizationId = session.TeamMember.OrganizationId;
            integration.TeamMemberId = session.TeamMember.Id;
            integration.UpdatedAt = DateTime.UtcNow;

            // Check if integration already exists
            var existing = await GetGoogleIntegrationAsync();

            if (existing != null)
            {
                // Update existing
                integration.Id = existing.Id;
                integration.CreatedAt = existing.CreatedAt;

                await client
                    .From<CalendarIntegration>()
                    .Filter("id", Operator.Equals, integration.Id)
                    .Update(integration);

                Log($"Updated calendar integration: {integration.Provider}");
            }
            else
            {
                // Create new
                integration.Id = Guid.NewGuid();
                integration.CreatedAt = DateTime.UtcNow;
                integration.IsDeleted = false;

                await client
                    .From<CalendarIntegration>()
                    .Insert(integration);

                Log($"Created calendar integration: {integration.Provider}");
            }

            return integration;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error saving integration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Disconnects (soft deletes) the Google Calendar integration.
    /// </summary>
    public async Task<bool> DisconnectGoogleAsync()
    {
        return await DisconnectAsync("google");
    }

    /// <summary>
    /// Disconnects (soft deletes) the Microsoft Calendar integration.
    /// </summary>
    public async Task<bool> DisconnectMicrosoftAsync()
    {
        return await DisconnectAsync("microsoft");
    }

    /// <summary>
    /// Disconnects (soft deletes) the calendar integration for the specified provider.
    /// </summary>
    private async Task<bool> DisconnectAsync(string provider)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var session = AuthService.Instance.CurrentSession_ProCohere;

        if (client == null || session?.TeamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            var integration = await GetIntegrationAsync(provider);
            if (integration == null)
            {
                return true; // Already disconnected
            }

            await client
                .From<CalendarIntegration>()
                .Filter("id", Operator.Equals, integration.Id)
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt!, DateTime.UtcNow)
                .Set(x => x.DeletedBy!, session.TeamMember.Id)
                .Update();

            Log($"Disconnected {provider} Calendar");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Error disconnecting {provider}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Connects to Google Calendar via OAuth and stores the integration.
    /// </summary>
    public async Task<bool> ConnectGoogleAsync()
    {
        LastError = null;

        try
        {
            Log("Starting Google Calendar connection");

            // Initiate OAuth flow
            var integration = await GoogleCalendarService.Instance.AuthenticateAsync();
            if (integration == null)
            {
                LastError = GoogleCalendarService.Instance.LastError ?? "OAuth failed";
                return false;
            }

            // Save to database
            var saved = await SaveIntegrationAsync(integration);
            if (saved == null)
            {
                return false;
            }

            Log("Google Calendar connected successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Connection error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Connects to Microsoft Calendar via OAuth and stores the integration.
    /// </summary>
    public async Task<bool> ConnectMicrosoftAsync()
    {
        LastError = null;

        try
        {
            Log("Starting Microsoft Calendar connection");

            // Initiate OAuth flow
            var integration = await MicrosoftCalendarService.Instance.AuthenticateAsync();
            if (integration == null)
            {
                LastError = MicrosoftCalendarService.Instance.LastError ?? "OAuth failed";
                return false;
            }

            // Save to database
            var saved = await SaveIntegrationAsync(integration);
            if (saved == null)
            {
                return false;
            }

            Log("Microsoft Calendar connected successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Connection error: {ex.Message}");
            return false;
        }
    }
}
