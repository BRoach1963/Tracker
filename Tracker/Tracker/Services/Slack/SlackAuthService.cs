using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.Slack
{
    /// <summary>
    /// Handles Slack OAuth 2.0 authentication flow.
    /// Users connect their own Slack workspace via "Add to Slack" OAuth.
    /// Each user/organization gets their own bot token stored in settings.
    /// </summary>
    public class SlackAuthService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private HttpListener? _callbackListener;
        private string? _botToken;
        private string? _userId;
        private string? _teamId;
        private string? _teamName;

        #endregion

        #region Singleton

        private static readonly Lazy<SlackAuthService> _instance =
            new(() => new SlackAuthService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SlackAuthService Instance => _instance.Value;

        #endregion

        #region Constructor

        private SlackAuthService()
        {
            _logger = LoggingManager.GetComponentLogger("SlackAuth");
            _httpClient = new HttpClient();
            
            // Try to restore from settings
            RestoreFromSettings();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the user has connected a Slack workspace.
        /// </summary>
        public bool IsConnected => !string.IsNullOrEmpty(_botToken);

        /// <summary>
        /// The connected user's Slack ID.
        /// </summary>
        public string? UserId => _userId;

        /// <summary>
        /// The connected workspace team ID.
        /// </summary>
        public string? TeamId => _teamId;

        /// <summary>
        /// The connected workspace name.
        /// </summary>
        public string? TeamName => _teamName;

        /// <summary>
        /// The bot token for API calls (from the user's connected workspace).
        /// </summary>
        public string? BotToken => _botToken;

        /// <summary>
        /// Last error message from Slack API (for diagnostics).
        /// </summary>
        public string? LastError { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates the "Add to Slack" OAuth flow to connect the user's workspace.
        /// This grants our app a bot token specific to their workspace.
        /// </summary>
        public async Task<bool> ConnectWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Starting Slack 'Add to Slack' OAuth flow...");

                // Build authorization URL requesting bot token scopes
                var state = Guid.NewGuid().ToString("N");
                var scopes = string.Join(",", SlackConfig.BotScopes);
                
                // Use scope parameter for bot scopes (v2 OAuth)
                var authUrl = $"{SlackConfig.AuthorizeUrl}?" +
                    $"client_id={SlackConfig.ClientId}&" +
                    $"scope={HttpUtility.UrlEncode(scopes)}&" +
                    $"redirect_uri={HttpUtility.UrlEncode(SlackConfig.RedirectUri)}&" +
                    $"state={state}";

                // Start local callback listener
                _callbackListener = new HttpListener();
                _callbackListener.Prefixes.Add("http://localhost:8891/slack/");
                _callbackListener.Start();

                // Open browser for user authorization
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                _logger.Info("Waiting for OAuth callback...");

                // Wait for callback
                var contextTask = _callbackListener.GetContextAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                var completedTask = await Task.WhenAny(contextTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    LastError = "OAuth flow timed out";
                    _logger.Warn(LastError);
                    return false;
                }

                var context = await contextTask;
                var code = context.Request.QueryString["code"];
                var returnedState = context.Request.QueryString["state"];
                var error = context.Request.QueryString["error"];

                // Check for error from Slack
                if (!string.IsNullOrEmpty(error))
                {
                    LastError = $"Slack authorization denied: {error}";
                    _logger.Warn(LastError);
                    
                    var errorHtml = $"<html><body><h2>Authorization Failed</h2><p>{error}</p><p>You can close this window.</p></body></html>";
                    var errorBuffer = System.Text.Encoding.UTF8.GetBytes(errorHtml);
                    context.Response.ContentLength64 = errorBuffer.Length;
                    await context.Response.OutputStream.WriteAsync(errorBuffer, cancellationToken);
                    context.Response.Close();
                    return false;
                }

                // Send success response to browser
                var responseHtml = "<html><body style='font-family: sans-serif; text-align: center; padding-top: 50px;'>" +
                    "<h2>✅ Slack Connected!</h2>" +
                    "<p>Your Slack workspace has been connected to Team Tracker.</p>" +
                    "<p>You can close this window and return to the app.</p></body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
                context.Response.Close();

                // Validate state
                if (returnedState != state)
                {
                    LastError = "OAuth state mismatch - possible CSRF attack";
                    _logger.Error(LastError);
                    return false;
                }

                if (string.IsNullOrEmpty(code))
                {
                    LastError = "No authorization code received from Slack";
                    _logger.Error(LastError);
                    return false;
                }

                // Exchange code for bot token
                return await ExchangeCodeForBotTokenAsync(code, cancellationToken);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.Exception(ex, "Error during Slack OAuth flow");
                return false;
            }
            finally
            {
                _callbackListener?.Stop();
                _callbackListener?.Close();
                _callbackListener = null;
            }
        }

        /// <summary>
        /// Validates the stored bot token by making a test API call.
        /// </summary>
        public async Task<bool> ValidateBotTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_botToken))
                {
                    LastError = "No bot token available - please connect your Slack workspace";
                    return false;
                }

                LastError = null;
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackConfig.ApiBaseUrl}/auth.test");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _botToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(json);

                if (result?.Ok == true)
                {
                    _teamId = result.TeamId;
                    _teamName = result.Team;
                    _userId = result.UserId;
                    LastError = null;
                    _logger.Info("Bot token validated. Team: {0}", _teamName);
                    return true;
                }

                LastError = result?.Error ?? "Unknown error";
                _logger.Warn("Bot token validation failed: {0}", result?.Error);
                
                // If token is invalid, clear it
                if (result?.Error == "invalid_auth" || result?.Error == "token_revoked")
                {
                    _logger.Warn("Token is invalid/revoked, clearing stored token");
                    Disconnect();
                }
                
                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.Exception(ex, "Error validating bot token");
                return false;
            }
        }

        /// <summary>
        /// Attempts to restore and validate the connection from stored settings.
        /// </summary>
        public async Task<bool> TryRestoreConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                RestoreFromSettings();
                
                if (!IsConnected)
                {
                    _logger.Info("No Slack connection to restore");
                    return false;
                }

                // Validate the stored token
                return await ValidateBotTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error restoring Slack connection");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from Slack and clears stored tokens.
        /// </summary>
        public void Disconnect()
        {
            _botToken = null;
            _userId = null;
            _teamId = null;
            _teamName = null;
            
            // Clear from settings
            var settings = UserSettingsManager.Instance?.Settings?.Slack;
            if (settings != null)
            {
                settings.IsConnected = false;
                settings.BotToken = null;
                settings.WorkspaceName = null;
                settings.WorkspaceId = null;
                settings.UserId = null;
                UserSettingsManager.Instance.SaveSettings();
            }
            
            _logger.Info("Disconnected from Slack");
        }

        #endregion

        #region Private Methods

        private void RestoreFromSettings()
        {
            var settings = UserSettingsManager.Instance?.Settings?.Slack;
            if (settings != null && settings.IsConnected && !string.IsNullOrEmpty(settings.BotToken))
            {
                _botToken = settings.BotToken;
                _teamId = settings.WorkspaceId;
                _teamName = settings.WorkspaceName;
                _userId = settings.UserId;
                _logger.Info("Restored Slack connection from settings: {0}", _teamName);
            }
        }

        private async Task<bool> ExchangeCodeForBotTokenAsync(string code, CancellationToken cancellationToken)
        {
            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = SlackConfig.ClientId,
                    ["client_secret"] = SlackConfig.ClientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = SlackConfig.RedirectUri
                });

                var response = await _httpClient.PostAsync(SlackConfig.TokenUrl, content, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.Debug("OAuth response: {0}", json);
                
                var result = JsonSerializer.Deserialize<SlackOAuthV2Response>(json);

                if (result?.Ok != true)
                {
                    LastError = result?.Error ?? "Token exchange failed";
                    _logger.Error("Token exchange failed: {0}", LastError);
                    return false;
                }

                // Store the bot token (this is what we need for API calls)
                _botToken = result.AccessToken;
                _teamId = result.Team?.Id;
                _teamName = result.Team?.Name;
                _userId = result.BotUserId;

                // Save to settings
                var settings = UserSettingsManager.Instance?.Settings?.Slack;
                if (settings != null)
                {
                    settings.IsConnected = true;
                    settings.BotToken = _botToken;
                    settings.WorkspaceName = _teamName;
                    settings.WorkspaceId = _teamId;
                    settings.UserId = _userId;
                    UserSettingsManager.Instance.SaveSettings();
                }

                _logger.Info("Successfully connected to Slack workspace: {0}", _teamName);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.Exception(ex, "Error exchanging code for token");
                return false;
            }
        }

        #endregion

        #region Response Models

        private class SlackApiResponse
        {
            [JsonPropertyName("ok")]
            public bool Ok { get; set; }
            
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            
            [JsonPropertyName("team")]
            public string? Team { get; set; }
            
            [JsonPropertyName("team_id")]
            public string? TeamId { get; set; }
            
            [JsonPropertyName("user_id")]
            public string? UserId { get; set; }
        }

        /// <summary>
        /// Response from Slack OAuth v2 token exchange.
        /// When requesting bot scopes, we get a bot access token.
        /// </summary>
        private class SlackOAuthV2Response
        {
            [JsonPropertyName("ok")]
            public bool Ok { get; set; }
            
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
            
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
            
            [JsonPropertyName("scope")]
            public string? Scope { get; set; }
            
            [JsonPropertyName("bot_user_id")]
            public string? BotUserId { get; set; }
            
            [JsonPropertyName("app_id")]
            public string? AppId { get; set; }
            
            [JsonPropertyName("team")]
            public SlackTeamInfo? Team { get; set; }
            
            [JsonPropertyName("authed_user")]
            public SlackAuthedUser? AuthedUser { get; set; }
        }

        private class SlackTeamInfo
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        private class SlackAuthedUser
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        #endregion
    }
}

