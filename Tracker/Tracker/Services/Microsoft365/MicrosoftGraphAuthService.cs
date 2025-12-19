using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.Identity.Client;
using Tracker.Logging;

namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Handles Microsoft Graph authentication using MSAL (Microsoft Authentication Library).
    /// Manages OAuth 2.0 flow with PKCE for desktop applications.
    /// </summary>
    public class MicrosoftGraphAuthService : IDisposable
    {
        #region Singleton

        private static MicrosoftGraphAuthService? _instance;
        private static readonly object _lock = new();

        public static MicrosoftGraphAuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new MicrosoftGraphAuthService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private IPublicClientApplication? _msalClient;
        private IAccount? _currentAccount;
        private readonly string _tokenCachePath;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the user is currently authenticated with Microsoft.
        /// </summary>
        public bool IsAuthenticated => _currentAccount != null;

        /// <summary>
        /// The authenticated user's email address.
        /// </summary>
        public string? UserEmail => _currentAccount?.Username;

        /// <summary>
        /// The authenticated user's display name (if available).
        /// </summary>
        public string? UserDisplayName { get; private set; }

        /// <summary>
        /// Whether Teams is available for this user (detected at runtime).
        /// </summary>
        public bool TeamsAvailable { get; private set; }

        /// <summary>
        /// Whether Calendar is available for this user (detected at runtime).
        /// </summary>
        public bool CalendarAvailable { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when authentication state changes.
        /// </summary>
        public event Action<bool>? AuthenticationStateChanged;

        #endregion

        #region Constructor

        private MicrosoftGraphAuthService()
        {
            _logger = LoggingManager.GetComponentLogger("GraphAuth");

            var tokenDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tracker", "auth");
            Directory.CreateDirectory(tokenDir);
            _tokenCachePath = Path.Combine(tokenDir, MicrosoftGraphConfig.TokenCacheFileName);

            InitializeMsalClient();
        }

        #endregion

        #region Initialization

        private void InitializeMsalClient()
        {
            try
            {
                _msalClient = PublicClientApplicationBuilder
                    .Create(MicrosoftGraphConfig.ClientId)
                    .WithAuthority(MicrosoftGraphConfig.Authority)
                    .WithRedirectUri(MicrosoftGraphConfig.RedirectUri)
                    .WithDefaultRedirectUri()
                    .Build();

                // Enable token caching
                EnableTokenCache(_msalClient.UserTokenCache);

                _logger.Info("MSAL client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize MSAL client");
            }
        }

        private void EnableTokenCache(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(args =>
            {
                try
                {
                    if (File.Exists(_tokenCachePath))
                    {
                        var encryptedData = File.ReadAllBytes(_tokenCachePath);
                        var plainData = ProtectedData.Unprotect(
                            encryptedData, null, DataProtectionScope.CurrentUser);
                        args.TokenCache.DeserializeMsalV3(plainData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to load token cache: {ex.Message}");
                }
            });

            tokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    try
                    {
                        var plainData = args.TokenCache.SerializeMsalV3();
                        var encryptedData = ProtectedData.Protect(
                            plainData, null, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(_tokenCachePath, encryptedData);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to save token cache: {ex.Message}");
                    }
                }
            });
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Attempts to sign in silently using cached tokens.
        /// Call this on app startup to restore previous session.
        /// </summary>
        /// <returns>True if silent authentication succeeded.</returns>
        public async Task<bool> TrySignInSilentlyAsync()
        {
            if (_msalClient == null)
                return false;

            try
            {
                var accounts = await _msalClient.GetAccountsAsync();
                var account = accounts.FirstOrDefault();

                if (account == null)
                {
                    _logger.Info("No cached Microsoft account found");
                    return false;
                }

                var result = await _msalClient
                    .AcquireTokenSilent(MicrosoftGraphConfig.Scopes, account)
                    .ExecuteAsync();

                _currentAccount = result.Account;
                UserDisplayName = ExtractDisplayName(result);
                
                _logger.Info($"Silent sign-in successful for {result.Account.Username}");
                
                await DetectAvailableServicesAsync(result.AccessToken);
                AuthenticationStateChanged?.Invoke(true);
                
                return true;
            }
            catch (MsalUiRequiredException)
            {
                _logger.Info("Silent sign-in requires user interaction");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Silent sign-in failed");
                return false;
            }
        }

        /// <summary>
        /// Signs in interactively, showing the Microsoft login dialog.
        /// </summary>
        /// <param name="parentWindowHandle">Handle to parent window for modal dialog.</param>
        /// <returns>True if sign-in succeeded.</returns>
        public async Task<bool> SignInInteractiveAsync(IntPtr? parentWindowHandle = null)
        {
            if (_msalClient == null)
            {
                _logger.Error("MSAL client not initialized");
                return false;
            }

            try
            {
                var builder = _msalClient
                    .AcquireTokenInteractive(MicrosoftGraphConfig.Scopes)
                    .WithPrompt(Prompt.SelectAccount);

                if (parentWindowHandle.HasValue && parentWindowHandle.Value != IntPtr.Zero)
                {
                    builder = builder.WithParentActivityOrWindow(parentWindowHandle.Value);
                }

                var result = await builder.ExecuteAsync();

                _currentAccount = result.Account;
                UserDisplayName = ExtractDisplayName(result);
                
                _logger.Info($"Interactive sign-in successful for {result.Account.Username}");
                
                await DetectAvailableServicesAsync(result.AccessToken);
                AuthenticationStateChanged?.Invoke(true);
                
                return true;
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
            {
                _logger.Info("User cancelled Microsoft sign-in");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Interactive sign-in failed");
                return false;
            }
        }

        /// <summary>
        /// Signs out the current user and clears cached tokens.
        /// </summary>
        public async Task SignOutAsync()
        {
            if (_msalClient == null || _currentAccount == null)
                return;

            try
            {
                await _msalClient.RemoveAsync(_currentAccount);
                
                // Clear cache file
                if (File.Exists(_tokenCachePath))
                    File.Delete(_tokenCachePath);

                _currentAccount = null;
                UserDisplayName = null;
                CalendarAvailable = false;
                TeamsAvailable = false;

                _logger.Info("Microsoft sign-out successful");
                AuthenticationStateChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Sign-out failed");
            }
        }

        /// <summary>
        /// Gets a valid access token for Microsoft Graph API calls.
        /// Automatically refreshes if expired.
        /// </summary>
        /// <param name="scopes">Optional custom scopes. Uses default if not specified.</param>
        /// <returns>Access token string, or null if not authenticated.</returns>
        public async Task<string?> GetAccessTokenAsync(string[]? scopes = null)
        {
            if (_msalClient == null || _currentAccount == null)
                return null;

            try
            {
                var result = await _msalClient
                    .AcquireTokenSilent(scopes ?? MicrosoftGraphConfig.Scopes, _currentAccount)
                    .ExecuteAsync();

                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                _logger.Warn("Token expired and refresh failed - user interaction required");
                AuthenticationStateChanged?.Invoke(false);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to acquire access token");
                return null;
            }
        }

        #endregion

        #region Service Detection

        /// <summary>
        /// Detects which Microsoft 365 services are available for this user.
        /// </summary>
        private async Task DetectAvailableServicesAsync(string accessToken)
        {
            CalendarAvailable = false;
            TeamsAvailable = false;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Check Calendar availability
            try
            {
                var calResponse = await httpClient.GetAsync(
                    "https://graph.microsoft.com/v1.0/me/calendar");
                CalendarAvailable = calResponse.IsSuccessStatusCode;
                _logger.Info($"Calendar available: {CalendarAvailable}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Calendar detection failed: {ex.Message}");
            }

            // Check Teams availability (will fail if user doesn't have Teams license)
            try
            {
                var teamsResponse = await httpClient.GetAsync(
                    "https://graph.microsoft.com/v1.0/me/joinedTeams");
                TeamsAvailable = teamsResponse.IsSuccessStatusCode;
                _logger.Info($"Teams available: {TeamsAvailable}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Teams detection failed: {ex.Message}");
            }
        }

        #endregion

        #region Helpers

        private string ExtractDisplayName(AuthenticationResult result)
        {
            // Try to extract from claims if available
            if (result.ClaimsPrincipal != null)
            {
                var nameClaim = result.ClaimsPrincipal.FindFirst("name")
                    ?? result.ClaimsPrincipal.FindFirst("preferred_username");
                if (nameClaim != null)
                    return nameClaim.Value;
            }
            
            // Fall back to username
            return result.Account.Username;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _msalClient = null;
            _currentAccount = null;
        }

        #endregion
    }
}

