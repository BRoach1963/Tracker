using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Logging;

namespace Tracker.Services.Slack
{
    /// <summary>
    /// Provides Slack API operations for messaging, presence, and user lookups.
    /// </summary>
    public class SlackService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, SlackUser> _userCache = new();
        private readonly Dictionary<string, string> _emailToUserIdCache = new();
        private DateTime _cacheExpiry = DateTime.MinValue;

        #endregion

        #region Singleton

        private static readonly Lazy<SlackService> _instance =
            new(() => new SlackService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SlackService Instance => _instance.Value;

        #endregion

        #region Constructor

        private SlackService()
        {
            _logger = LoggingManager.GetComponentLogger("SlackService");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", SlackConfig.BotToken);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether the Slack service is available.
        /// </summary>
        public bool IsAvailable => SlackAuthService.Instance.IsConnected;

        #endregion

        #region Public Methods - Messaging

        /// <summary>
        /// Sends a direct message to a user by email.
        /// </summary>
        public async Task<bool> SendDirectMessageByEmailAsync(string email, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetUserIdByEmailAsync(email, cancellationToken);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.Warn("Could not find Slack user for email: {0}", email);
                    return false;
                }

                return await SendDirectMessageAsync(userId, message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending DM by email");
                return false;
            }
        }

        /// <summary>
        /// Sends a direct message to a user by Slack user ID.
        /// </summary>
        public async Task<bool> SendDirectMessageAsync(string userId, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                // First, open a DM channel
                var channelId = await OpenDirectMessageChannelAsync(userId, cancellationToken);
                if (string.IsNullOrEmpty(channelId))
                {
                    return false;
                }

                // Send the message
                var payload = new
                {
                    channel = channelId,
                    text = message,
                    unfurl_links = false,
                    unfurl_media = false
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{SlackConfig.ApiBaseUrl}/chat.postMessage", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(responseJson, _jsonOptions);

                if (result?.Ok == true)
                {
                    _logger.Info("Message sent to Slack user {0}", userId);
                    return true;
                }

                _logger.Warn("Failed to send message: {0}", result?.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending direct message");
                return false;
            }
        }

        /// <summary>
        /// Sends a rich message with blocks (for formatted content).
        /// </summary>
        public async Task<bool> SendRichMessageByEmailAsync(string email, string fallbackText, object[] blocks, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetUserIdByEmailAsync(email, cancellationToken);
                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                var channelId = await OpenDirectMessageChannelAsync(userId, cancellationToken);
                if (string.IsNullOrEmpty(channelId))
                {
                    return false;
                }

                var payload = new
                {
                    channel = channelId,
                    text = fallbackText,
                    blocks = blocks
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{SlackConfig.ApiBaseUrl}/chat.postMessage", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(responseJson, _jsonOptions);

                return result?.Ok == true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending rich message");
                return false;
            }
        }

        #endregion

        #region Public Methods - User Lookup

        /// <summary>
        /// Gets a Slack user ID by email address.
        /// </summary>
        public async Task<string?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check cache first
                if (_emailToUserIdCache.TryGetValue(email.ToLowerInvariant(), out var cachedId))
                {
                    return cachedId;
                }

                var response = await _httpClient.GetAsync(
                    $"{SlackConfig.ApiBaseUrl}/users.lookupByEmail?email={Uri.EscapeDataString(email)}", 
                    cancellationToken);
                
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackUserLookupResponse>(json, _jsonOptions);

                if (result?.Ok == true && result.User != null)
                {
                    _emailToUserIdCache[email.ToLowerInvariant()] = result.User.Id!;
                    _userCache[result.User.Id!] = result.User;
                    return result.User.Id;
                }

                _logger.Debug("User not found for email {0}: {1}", email, result?.Error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error looking up user by email");
                return null;
            }
        }

        /// <summary>
        /// Gets user information by Slack user ID.
        /// </summary>
        public async Task<SlackUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_userCache.TryGetValue(userId, out var cached))
                {
                    return cached;
                }

                var response = await _httpClient.GetAsync(
                    $"{SlackConfig.ApiBaseUrl}/users.info?user={userId}",
                    cancellationToken);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackUserInfoResponse>(json, _jsonOptions);

                if (result?.Ok == true && result.User != null)
                {
                    _userCache[userId] = result.User;
                    return result.User;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting user info");
                return null;
            }
        }

        #endregion

        #region Public Methods - Presence

        /// <summary>
        /// Gets the presence status for a user by email.
        /// </summary>
        public async Task<SlackPresence> GetPresenceByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetUserIdByEmailAsync(email, cancellationToken);
                if (string.IsNullOrEmpty(userId))
                {
                    return SlackPresence.Unknown;
                }

                return await GetPresenceAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting presence by email");
                return SlackPresence.Unknown;
            }
        }

        /// <summary>
        /// Gets the presence status for a user by Slack user ID.
        /// </summary>
        public async Task<SlackPresence> GetPresenceAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{SlackConfig.ApiBaseUrl}/users.getPresence?user={userId}",
                    cancellationToken);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackPresenceResponse>(json, _jsonOptions);

                if (result?.Ok == true)
                {
                    return result.Presence?.ToLowerInvariant() switch
                    {
                        "active" => SlackPresence.Active,
                        "away" => SlackPresence.Away,
                        _ => SlackPresence.Unknown
                    };
                }

                return SlackPresence.Unknown;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting presence");
                return SlackPresence.Unknown;
            }
        }

        /// <summary>
        /// Gets presence for multiple users by email.
        /// </summary>
        public async Task<Dictionary<string, SlackPresence>> GetPresenceBatchAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, SlackPresence>();

            // Slack doesn't have a batch presence API, so we need to call individually
            // Use parallel calls for efficiency
            var tasks = emails.Select(async email =>
            {
                var presence = await GetPresenceByEmailAsync(email, cancellationToken);
                return (email, presence);
            });

            var presenceResults = await Task.WhenAll(tasks);

            foreach (var (email, presence) in presenceResults)
            {
                results[email] = presence;
            }

            return results;
        }

        #endregion

        #region Public Methods - Profile Photos

        /// <summary>
        /// Gets the profile photo URL for a user by email.
        /// </summary>
        public async Task<string?> GetProfilePhotoUrlByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = await GetUserIdByEmailAsync(email, cancellationToken);
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var user = await GetUserAsync(userId, cancellationToken);
                return user?.Profile?.Image192 ?? user?.Profile?.Image72;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting profile photo URL");
                return null;
            }
        }

        /// <summary>
        /// Downloads a profile photo for a user by email.
        /// </summary>
        public async Task<byte[]?> GetProfilePhotoByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var photoUrl = await GetProfilePhotoUrlByEmailAsync(email, cancellationToken);
                if (string.IsNullOrEmpty(photoUrl))
                {
                    return null;
                }

                // Download the image (doesn't need auth header)
                using var client = new HttpClient();
                return await client.GetByteArrayAsync(photoUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error downloading profile photo");
                return null;
            }
        }

        #endregion

        #region Private Methods

        private async Task<string?> OpenDirectMessageChannelAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                var payload = new { users = userId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{SlackConfig.ApiBaseUrl}/conversations.open", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackConversationOpenResponse>(responseJson, _jsonOptions);

                if (result?.Ok == true)
                {
                    return result.Channel?.Id;
                }

                _logger.Warn("Failed to open DM channel: {0}", result?.Error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error opening DM channel");
                return null;
            }
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Response Models

        private class SlackApiResponse
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
        }

        private class SlackUserLookupResponse : SlackApiResponse
        {
            public SlackUser? User { get; set; }
        }

        private class SlackUserInfoResponse : SlackApiResponse
        {
            public SlackUser? User { get; set; }
        }

        private class SlackPresenceResponse : SlackApiResponse
        {
            public string? Presence { get; set; }
        }

        private class SlackConversationOpenResponse : SlackApiResponse
        {
            public SlackChannel? Channel { get; set; }
        }

        private class SlackChannel
        {
            public string? Id { get; set; }
        }

        #endregion
    }

    #region Public Models

    /// <summary>
    /// Represents a Slack user.
    /// </summary>
    public class SlackUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("real_name")]
        public string? RealName { get; set; }

        [JsonPropertyName("profile")]
        public SlackProfile? Profile { get; set; }

        [JsonPropertyName("is_bot")]
        public bool IsBot { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// Represents a Slack user's profile.
    /// </summary>
    public class SlackProfile
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("real_name")]
        public string? RealName { get; set; }

        [JsonPropertyName("status_text")]
        public string? StatusText { get; set; }

        [JsonPropertyName("status_emoji")]
        public string? StatusEmoji { get; set; }

        [JsonPropertyName("image_24")]
        public string? Image24 { get; set; }

        [JsonPropertyName("image_72")]
        public string? Image72 { get; set; }

        [JsonPropertyName("image_192")]
        public string? Image192 { get; set; }

        [JsonPropertyName("image_512")]
        public string? Image512 { get; set; }
    }

    /// <summary>
    /// Slack presence status.
    /// </summary>
    public enum SlackPresence
    {
        Unknown,
        Active,
        Away
    }

    #endregion
}

