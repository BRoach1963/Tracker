using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Requests;
using Tracker.Classes;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Common.Enums;

namespace Tracker.Services
{
    /// <summary>
    /// Service for handling Google OAuth2 authentication.
    /// </summary>
    public class GoogleAuthService
    {
        private static readonly Lazy<GoogleAuthService> _instance = new(() => new GoogleAuthService());
        public static GoogleAuthService Instance => _instance.Value;

        private readonly LoggingManager.Logger _logger = new("GoogleAuth", "GoogleAuth");
        private const string ClientId = "639487192956-03cmcvtdr8n7a11kru120amd4eecint0.apps.googleusercontent.com";
        private const string ClientSecret = "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";
        private const string ApplicationName = "Tracker";
        private static readonly string[] Scopes = { "https://www.googleapis.com/auth/calendar", "https://www.googleapis.com/auth/userinfo.email" };

        private GoogleAuthService()
        {
        }

        /// <summary>
        /// Initiates the OAuth2 authentication flow and returns authorization URL.
        /// </summary>
        /// <param name="redirectUri">The redirect URI (default: http://localhost:8080/).</param>
        public string GetAuthorizationUrl(string redirectUri = "http://localhost:8080/")
        {
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = Scopes
                });

                var request = flow.CreateAuthorizationCodeRequest(redirectUri);
                return request.Build().ToString();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error generating Google authorization URL");
                throw;
            }
        }

        /// <summary>
        /// Completes the OAuth2 flow by exchanging an authorization code for tokens.
        /// </summary>
        /// <param name="authorizationCode">The authorization code from the callback.</param>
        /// <param name="redirectUri">The redirect URI used in the authorization request.</param>
        /// <returns>True if authentication was successful, false otherwise.</returns>
        public async Task<bool> CompleteAuthenticationAsync(string authorizationCode, string redirectUri = "http://localhost:8080/")
        {
            try
            {
                var tokenResponse = await ExchangeCodeForTokensAsync(authorizationCode, redirectUri);
                if (tokenResponse == null)
                {
                    return false;
                }

                // Store encrypted tokens
                var settings = UserSettingsManager.Instance.Settings.Calendar;
                var encryptionService = TokenEncryptionService.Instance;

                settings.GoogleAccessToken = encryptionService.Encrypt(tokenResponse.AccessToken);
                settings.GoogleRefreshToken = encryptionService.Encrypt(tokenResponse.RefreshToken);
                settings.GoogleTokenExpiry = tokenResponse.ExpiresInSeconds.HasValue
                    ? DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds.Value)
                    : DateTime.UtcNow.AddHours(1);
                settings.GoogleCalendarEnabled = true;

                // Try to get user email (optional)
                try
                {
                    // We'll get this from the calendar service after initialization
                    settings.GoogleUserEmail = "Connected"; // Placeholder
                }
                catch
                {
                    // Non-critical, continue
                }

                UserSettingsManager.Instance.SaveSettings();
                _logger.Info("Google Calendar authentication completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error completing Google authentication");
                return false;
            }
        }

        /// <summary>
        /// Exchanges an authorization code for access and refresh tokens.
        /// </summary>
        public async Task<TokenResponse?> ExchangeCodeForTokensAsync(string authorizationCode, string redirectUri = "http://localhost:8080/")
        {
            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = Scopes
                });

                var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    "user",
                    authorizationCode,
                    redirectUri,
                    CancellationToken.None);

                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error exchanging authorization code for tokens");
                return null;
            }
        }

        /// <summary>
        /// Refreshes an expired access token using the refresh token.
        /// </summary>
        public async Task<TokenResponse?> RefreshTokenAsync(string encryptedRefreshToken)
        {
            try
            {
                // Decrypt refresh token
                var encryptionService = TokenEncryptionService.Instance;
                var refreshToken = encryptionService.Decrypt(encryptedRefreshToken);
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.Error("Failed to decrypt refresh token");
                    return null;
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = Scopes
                });

                var token = new TokenResponse { RefreshToken = refreshToken };
                var credential = new UserCredential(flow, "user", token);

                if (await credential.RefreshTokenAsync(CancellationToken.None))
                {
                    return credential.Token;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error refreshing Google access token");
                return null;
            }
        }

        /// <summary>
        /// Gets a valid access token, refreshing if necessary.
        /// </summary>
        public async Task<string?> GetValidAccessTokenAsync(CalendarSettings settings)
        {
            if (string.IsNullOrEmpty(settings.GoogleAccessToken))
                return null;

            var encryptionService = TokenEncryptionService.Instance;

            // Decrypt access token
            var accessToken = encryptionService.Decrypt(settings.GoogleAccessToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.Error("Failed to decrypt Google access token");
                return null;
            }

            // Check if token is expired or about to expire (within 5 minutes)
            if (settings.GoogleTokenExpiry.HasValue && 
                settings.GoogleTokenExpiry.Value <= DateTime.UtcNow.AddMinutes(5))
            {
                // Token expired, refresh it
                if (!string.IsNullOrEmpty(settings.GoogleRefreshToken))
                {
                    var refreshedToken = await RefreshTokenAsync(settings.GoogleRefreshToken);
                    if (refreshedToken != null)
                    {
                        // Encrypt and store new tokens
                        settings.GoogleAccessToken = encryptionService.Encrypt(refreshedToken.AccessToken);
                        settings.GoogleTokenExpiry = refreshedToken.ExpiresInSeconds.HasValue
                            ? DateTime.UtcNow.AddSeconds(refreshedToken.ExpiresInSeconds.Value)
                            : DateTime.UtcNow.AddHours(1);
                        
                        if (!string.IsNullOrEmpty(refreshedToken.RefreshToken))
                        {
                            settings.GoogleRefreshToken = encryptionService.Encrypt(refreshedToken.RefreshToken);
                        }

                        // Save updated settings
                        UserSettingsManager.Instance.SaveSettings();
                        return refreshedToken.AccessToken;
                    }
                }

                return null;
            }

            return accessToken;
        }

        /// <summary>
        /// Disconnects Google Calendar by clearing tokens.
        /// </summary>
        public void Disconnect(CalendarSettings settings)
        {
            settings.GoogleCalendarEnabled = false;
            settings.GoogleAccessToken = null;
            settings.GoogleRefreshToken = null;
            settings.GoogleTokenExpiry = null;
            settings.GoogleUserEmail = null;
            UserSettingsManager.Instance.SaveSettings();
            _logger.Info("Google Calendar disconnected");
        }
    }
}

