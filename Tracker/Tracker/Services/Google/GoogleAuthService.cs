using System.IO;
using System.Net.Http;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.Services.Google
{
    /// <summary>
    /// Handles Google OAuth 2.0 authentication for desktop apps.
    /// </summary>
    public class GoogleAuthService
    {
        #region Singleton

        private static GoogleAuthService? _instance;
        private static readonly object _lock = new();

        public static GoogleAuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GoogleAuthService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private UserCredential? _credential;
        private readonly string _credentialPath;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the user is authenticated with Google.
        /// </summary>
        public bool IsAuthenticated => _credential != null && !string.IsNullOrEmpty(_credential.Token?.AccessToken);

        /// <summary>
        /// The authenticated user's email address.
        /// </summary>
        public string? UserEmail { get; private set; }

        /// <summary>
        /// The authenticated user's display name.
        /// </summary>
        public string? UserDisplayName { get; private set; }

        /// <summary>
        /// Gets the current credential for use by Google API services.
        /// </summary>
        public UserCredential? Credential => _credential;

        #endregion

        #region Constructor

        private GoogleAuthService()
        {
            _logger = LoggingManager.GetComponentLogger("GoogleAuth");
            _credentialPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tracker",
                "GoogleCredentials"
            );

            // Ensure credential directory exists
            Directory.CreateDirectory(_credentialPath);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Signs in the user interactively using Google OAuth.
        /// </summary>
        public async Task<bool> SignInAsync()
        {
            try
            {
                _logger.Info("Starting Google sign-in...");

                // Check if we have stored credentials
                var storedToken = LoadStoredToken();

                var clientSecrets = new ClientSecrets
                {
                    ClientId = GoogleConfig.ClientId,
                    ClientSecret = GoogleConfig.ClientSecret
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = GoogleConfig.Scopes,
                    DataStore = new FileDataStore(_credentialPath, true)
                });

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    GoogleConfig.Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(_credentialPath, true)
                );

                if (_credential != null)
                {
                    // Refresh token if needed
                    if (_credential.Token.IsStale)
                    {
                        await _credential.RefreshTokenAsync(CancellationToken.None);
                    }

                    // Get user info
                    await LoadUserInfoAsync();

                    // Save settings
                    UserSettingsManager.Instance.Settings.Google.IsConnected = true;
                    UserSettingsManager.Instance.Settings.Google.UserEmail = UserEmail;
                    UserSettingsManager.Instance.Settings.Google.UserDisplayName = UserDisplayName;
                    UserSettingsManager.Instance.SaveSettings();

                    _logger.Info($"Google sign-in successful: {UserEmail}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Google sign-in failed");
                return false;
            }
        }

        /// <summary>
        /// Tries to sign in silently using stored credentials.
        /// </summary>
        public async Task<bool> TrySilentSignInAsync()
        {
            try
            {
                var clientSecrets = new ClientSecrets
                {
                    ClientId = GoogleConfig.ClientId,
                    ClientSecret = GoogleConfig.ClientSecret
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = GoogleConfig.Scopes,
                    DataStore = new FileDataStore(_credentialPath, true)
                });

                // Try to load existing token
                var tokenResponse = await flow.LoadTokenAsync("user", CancellationToken.None);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.RefreshToken))
                {
                    _credential = new UserCredential(flow, "user", tokenResponse);

                    // Refresh if needed
                    if (_credential.Token.IsStale)
                    {
                        var success = await _credential.RefreshTokenAsync(CancellationToken.None);
                        if (!success)
                        {
                            _credential = null;
                            return false;
                        }
                    }

                    // Get user info
                    await LoadUserInfoAsync();

                    _logger.Info($"Google silent sign-in successful: {UserEmail}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Silent sign-in not available: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Signs out and clears stored credentials.
        /// </summary>
        public async Task SignOutAsync()
        {
            try
            {
                if (_credential != null)
                {
                    await _credential.RevokeTokenAsync(CancellationToken.None);
                }

                // Clear stored credentials
                var dataStore = new FileDataStore(_credentialPath, true);
                await dataStore.ClearAsync();

                _credential = null;
                UserEmail = null;
                UserDisplayName = null;

                // Clear settings
                UserSettingsManager.Instance.Settings.Google.IsConnected = false;
                UserSettingsManager.Instance.Settings.Google.UserEmail = null;
                UserSettingsManager.Instance.Settings.Google.UserDisplayName = null;
                UserSettingsManager.Instance.Settings.Google.LastCalendarSync = null;
                UserSettingsManager.Instance.Settings.Google.CalendarSyncEnabled = false;
                UserSettingsManager.Instance.SaveSettings();

                _logger.Info("Google sign-out successful");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Google sign-out error");
            }
        }

        /// <summary>
        /// Gets the access token for API calls.
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            if (_credential == null)
                return null;

            try
            {
                if (_credential.Token.IsStale)
                {
                    var success = await _credential.RefreshTokenAsync(CancellationToken.None);
                    if (!success)
                        return null;
                }

                return _credential.Token.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the base client service initializer for Google APIs.
        /// </summary>
        public BaseClientService.Initializer GetServiceInitializer()
        {
            return new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = GoogleConfig.ApplicationName
            };
        }

        #endregion

        #region Private Methods

        private async Task LoadUserInfoAsync()
        {
            if (_credential == null) return;

            try
            {
                // Use the People API to get user info
                using var httpClient = new HttpClient();
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token)) return;

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetStringAsync(
                    "https://www.googleapis.com/oauth2/v2/userinfo");

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                UserEmail = root.TryGetProperty("email", out var email) ? email.GetString() : null;
                UserDisplayName = root.TryGetProperty("name", out var name) ? name.GetString() : null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load user info: {ex.Message}");
            }
        }

        private TokenResponse? LoadStoredToken()
        {
            try
            {
                var tokenFile = Path.Combine(_credentialPath, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");
                if (File.Exists(tokenFile))
                {
                    var json = File.ReadAllText(tokenFile);
                    return JsonSerializer.Deserialize<TokenResponse>(json);
                }
            }
            catch
            {
                // Ignore errors loading stored token
            }
            return null;
        }

        #endregion
    }
}

