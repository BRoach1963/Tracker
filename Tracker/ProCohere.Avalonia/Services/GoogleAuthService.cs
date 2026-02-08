using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Centralized Google OAuth service for all Google API integrations.
/// Manages authentication state, token refresh, and provides initialized services.
/// </summary>
public class GoogleAuthService
{
    #region Singleton

    private static readonly Lazy<GoogleAuthService> _instance =
        new(() => new GoogleAuthService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static GoogleAuthService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "google_auth.log");

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

    #region Fields

    private UserCredential? _credential;
    private CalendarService? _calendarService;
    private GmailService? _gmailService;
    private PeopleServiceService? _peopleService;

    #endregion

    #region Properties

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Whether the user is authenticated with Google.
    /// </summary>
    public bool IsAuthenticated => _credential != null && 
        !string.IsNullOrEmpty(_credential.Token?.AccessToken);

    /// <summary>
    /// The authenticated user's email address.
    /// </summary>
    public string? UserEmail { get; private set; }

    /// <summary>
    /// The authenticated user's display name.
    /// </summary>
    public string? UserDisplayName { get; private set; }

    #endregion

    private GoogleAuthService() { }

    #region OAuth Flow

    /// <summary>
    /// Initiates OAuth flow with full Google API scopes (Calendar, Gmail, People).
    /// Opens browser for user consent.
    /// </summary>
    public async Task<CalendarIntegration?> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        LastError = null;

        try
        {
            Log("Starting Google OAuth flow with full scopes");

            var (clientId, clientSecret) = AppSettingsService.Instance.GetGoogleCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Google OAuth credentials not configured in appsettings.json";
                Log(LastError);
                return null;
            }

            var secrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Request all needed scopes
            string[] scopes = 
            {
                CalendarService.Scope.Calendar,
                GmailService.Scope.GmailSend,
                GmailService.Scope.GmailReadonly,
                PeopleServiceService.Scope.ContactsReadonly
            };

            // Launch OAuth browser flow
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                scopes,
                "user",
                cancellationToken);

            Log($"OAuth successful. Token expires: {_credential.Token.ExpiresInSeconds}s");

            // Initialize services
            InitializeServices();

            // Get user info
            await LoadUserInfoAsync();

            // Create integration record for storage
            var integration = new CalendarIntegration
            {
                Provider = "google",
                ExternalAccountId = UserEmail ?? "unknown",
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
    /// Initializes from existing integration stored in database.
    /// </summary>
    public async Task<bool> InitializeFromIntegrationAsync(
        CalendarIntegration integration, 
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        if (integration.Provider != "google")
        {
            LastError = "Integration is not for Google";
            return false;
        }

        if (string.IsNullOrEmpty(integration.AccessToken))
        {
            LastError = "No access token";
            return false;
        }

        try
        {
            var (clientId, clientSecret) = AppSettingsService.Instance.GetGoogleCalendarCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                LastError = "Google OAuth credentials not configured";
                return false;
            }

            // Reconstruct credential from stored tokens
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[]
                {
                    CalendarService.Scope.Calendar,
                    GmailService.Scope.GmailSend,
                    GmailService.Scope.GmailReadonly,
                    PeopleServiceService.Scope.ContactsReadonly
                }
            });

            var token = new TokenResponse
            {
                AccessToken = integration.AccessToken,
                RefreshToken = integration.RefreshToken,
                ExpiresInSeconds = integration.TokenExpiresAt.HasValue
                    ? (long)(integration.TokenExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                    : 3600
            };

            _credential = new UserCredential(flow, "user", token);

            // Check if token needs refresh
            if (await _credential.RefreshTokenAsync(cancellationToken))
            {
                Log("Token refreshed successfully");
            }

            InitializeServices();
            UserEmail = integration.ExternalAccountId;

            Log($"Initialized from integration: {UserEmail}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Init error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Service Access

    /// <summary>
    /// Get the Calendar service. Returns null if not authenticated.
    /// </summary>
    public CalendarService? GetCalendarService()
    {
        if (!IsAuthenticated) return null;
        if (_calendarService == null) InitializeServices();
        return _calendarService;
    }

    /// <summary>
    /// Get the Gmail service. Returns null if not authenticated.
    /// </summary>
    public GmailService? GetGmailService()
    {
        if (!IsAuthenticated) return null;
        if (_gmailService == null) InitializeServices();
        return _gmailService;
    }

    /// <summary>
    /// Get the People service. Returns null if not authenticated.
    /// </summary>
    public PeopleServiceService? GetPeopleService()
    {
        if (!IsAuthenticated) return null;
        if (_peopleService == null) InitializeServices();
        return _peopleService;
    }

    /// <summary>
    /// Get service initializer for creating custom services.
    /// </summary>
    public BaseClientService.Initializer? GetServiceInitializer()
    {
        if (_credential == null) return null;
        
        return new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "ProCohere"
        };
    }

    #endregion

    #region Private Methods

    private void InitializeServices()
    {
        if (_credential == null) return;

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "ProCohere"
        };

        _calendarService = new CalendarService(initializer);
        _gmailService = new GmailService(initializer);
        _peopleService = new PeopleServiceService(initializer);
    }

    private async Task LoadUserInfoAsync()
    {
        if (_calendarService == null) return;

        try
        {
            var calendarList = await _calendarService.CalendarList.List().ExecuteAsync();
            var primaryCalendar = calendarList.Items?.FirstOrDefault(c => c.Primary == true);
            UserEmail = primaryCalendar?.Id ?? "unknown";
            UserDisplayName = primaryCalendar?.Summary ?? UserEmail;
            Log($"Loaded user info: {UserEmail}");
        }
        catch (Exception ex)
        {
            Log($"Failed to load user info: {ex.Message}");
        }
    }

    /// <summary>
    /// Sign out and clear all credentials.
    /// </summary>
    public void SignOut()
    {
        _credential = null;
        _calendarService = null;
        _gmailService = null;
        _peopleService = null;
        UserEmail = null;
        UserDisplayName = null;
        Log("Signed out");
    }

    #endregion
}
