using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.Services.Slack
{
    /// <summary>
    /// Handles Slack OAuth 2.0 authentication flow.
    /// </summary>
    public class SlackAuthService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private HttpListener? _callbackListener;
        private string? _userAccessToken;
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
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the user is connected to Slack.
        /// </summary>
        public bool IsConnected => !string.IsNullOrEmpty(_userAccessToken) || !string.IsNullOrEmpty(SlackConfig.BotToken);

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
        /// The bot token for API calls.
        /// </summary>
        public string BotToken => SlackConfig.BotToken;

        /// <summary>
        /// User access token (if user-level OAuth was performed).
        /// </summary>
        public string? UserAccessToken => _userAccessToken;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates the OAuth flow to connect the user's Slack account.
        /// This is optional - the bot token already allows most operations.
        /// </summary>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Starting Slack OAuth flow...");

                // Build authorization URL
                var state = Guid.NewGuid().ToString("N");
                var scopes = string.Join(",", SlackConfig.UserScopes);
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
                    _logger.Warn("OAuth flow timed out");
                    return false;
                }

                var context = await contextTask;
                var code = context.Request.QueryString["code"];
                var returnedState = context.Request.QueryString["state"];

                // Send response to browser
                var responseHtml = "<html><body><h2>Slack Connected!</h2><p>You can close this window.</p></body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, cancellationToken);
                context.Response.Close();

                // Validate state
                if (returnedState != state)
                {
                    _logger.Error("OAuth state mismatch");
                    return false;
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.Error("No authorization code received");
                    return false;
                }

                // Exchange code for token
                return await ExchangeCodeForTokenAsync(code, cancellationToken);
            }
            catch (Exception ex)
            {
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
        /// Validates the bot token by making a test API call.
        /// </summary>
        public async Task<bool> ValidateBotTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackConfig.ApiBaseUrl}/auth.test");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SlackConfig.BotToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(json);

                if (result?.Ok == true)
                {
                    _teamId = result.TeamId;
                    _teamName = result.Team;
                    _userId = result.UserId;
                    _logger.Info("Bot token validated. Team: {0}", _teamName);
                    return true;
                }

                _logger.Warn("Bot token validation failed: {0}", result?.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error validating bot token");
                return false;
            }
        }

        /// <summary>
        /// Attempts to restore connection using stored tokens.
        /// </summary>
        public async Task<bool> TryRestoreConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to load user token from secure storage
                var savedToken = SecureTokenStorage.GetSlackUserToken();
                if (!string.IsNullOrEmpty(savedToken))
                {
                    _userAccessToken = savedToken;
                }

                // Validate bot token
                return await ValidateBotTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error restoring Slack connection");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from Slack.
        /// </summary>
        public void Disconnect()
        {
            _userAccessToken = null;
            _userId = null;
            _teamId = null;
            _teamName = null;
            SecureTokenStorage.ClearSlackToken();
            _logger.Info("Disconnected from Slack");
        }

        #endregion

        #region Private Methods

        private async Task<bool> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
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
                var result = JsonSerializer.Deserialize<SlackOAuthResponse>(json);

                if (result?.Ok != true)
                {
                    _logger.Error("Token exchange failed: {0}", result?.Error);
                    return false;
                }

                _userAccessToken = result.AccessToken;
                _teamId = result.Team?.Id;
                _teamName = result.Team?.Name;
                _userId = result.AuthedUser?.Id;

                // Save token securely
                if (!string.IsNullOrEmpty(_userAccessToken))
                {
                    SecureTokenStorage.SaveSlackUserToken(_userAccessToken);
                }

                _logger.Info("Successfully connected to Slack workspace: {0}", _teamName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error exchanging code for token");
                return false;
            }
        }

        #endregion

        #region Response Models

        private class SlackApiResponse
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public string? Team { get; set; }
            public string? TeamId { get; set; }
            public string? UserId { get; set; }
        }

        private class SlackOAuthResponse
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public string? AccessToken { get; set; }
            public string? TokenType { get; set; }
            public string? Scope { get; set; }
            public SlackTeam? Team { get; set; }
            public SlackAuthedUser? AuthedUser { get; set; }
        }

        private class SlackTeam
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
        }

        private class SlackAuthedUser
        {
            public string? Id { get; set; }
        }

        #endregion
    }
}

