using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Services.Slack;

namespace Tracker.Services.Kudos
{
    /// <summary>
    /// Delivers kudos via Slack Bot API.
    /// Uses the existing Slack integration for authentication and messaging.
    /// </summary>
    public class SlackDeliveryProvider : IKudosDeliveryProvider
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        #endregion

        #region Singleton

        private static readonly Lazy<SlackDeliveryProvider> _instance =
            new(() => new SlackDeliveryProvider(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SlackDeliveryProvider Instance => _instance.Value;

        #endregion

        #region Constructor

        private SlackDeliveryProvider()
        {
            _logger = LoggingManager.GetComponentLogger("SlackKudosProvider");
            _httpClient = new HttpClient();
            // Note: Don't set default auth header here - token is dynamic per user
        }

        #endregion

        #region IKudosDeliveryProvider Implementation

        /// <inheritdoc/>
        public DeliveryChannel Channel => DeliveryChannel.Slack;

        /// <inheritdoc/>
        public bool IsAvailable => SlackAuthService.Instance.IsConnected;

        /// <inheritdoc/>
        public string DisplayName => "Slack";

        /// <inheritdoc/>
        public async Task<KudosDeliveryResult> SendKudosAsync(
            DataModels.Kudos kudos,
            TeamMember teamMember,
            string senderName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if we have a valid token
                var botToken = SlackAuthService.Instance.BotToken;
                if (string.IsNullOrEmpty(botToken))
                {
                    return KudosDeliveryResult.Failed("Slack is not connected. Please connect your Slack workspace in Settings > Integrations.");
                }

                // First, find the Slack user by email
                var userId = await GetUserIdByEmailAsync(teamMember.Email, botToken, cancellationToken);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.Warn("Could not find Slack user for email: {0}", teamMember.Email);
                    return KudosDeliveryResult.Failed($"Could not find Slack user with email {teamMember.Email}");
                }

                // Open a DM channel
                var channelId = await OpenDmChannelAsync(userId, botToken, cancellationToken);
                if (string.IsNullOrEmpty(channelId))
                {
                    return KudosDeliveryResult.Failed("Could not open DM channel with user");
                }

                // Build and send the message with blocks
                var blocks = BuildSlackBlocks(kudos, teamMember, senderName);
                var fallbackText = $"🎉 Kudos from {senderName}: {kudos.Message}";

                var payload = new
                {
                    channel = channelId,
                    text = fallbackText,
                    blocks = blocks,
                    unfurl_links = false,
                    unfurl_media = false
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackConfig.ApiBaseUrl}/chat.postMessage");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.Info("Sending kudos to Slack for {0}", teamMember.FullName);
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(responseJson);

                if (result?.Ok == true)
                {
                    _logger.Info("Kudos delivered to Slack successfully");
                    return KudosDeliveryResult.Succeeded();
                }
                else
                {
                    var error = result?.Error ?? "Unknown error";
                    _logger.Warn("Slack API returned error: {0}", error);
                    return KudosDeliveryResult.Failed($"Slack error: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending kudos to Slack");
                return KudosDeliveryResult.Failed($"Error: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var botToken = SlackAuthService.Instance.BotToken;
                if (string.IsNullOrEmpty(botToken))
                    return false;
                    
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{SlackConfig.ApiBaseUrl}/auth.test");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<SlackApiResponse>(json);
                return result?.Ok == true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Slack connection test failed");
                return false;
            }
        }

        /// <inheritdoc/>
        public string GetSetupInstructions()
        {
            var status = IsAvailable ? "✅ Connected" : "❌ Not Connected";
            return $@"📋 Slack Integration Setup:

Tracker already has Slack integration built-in! To enable kudos delivery:

1. Make sure you've connected Tracker to your Slack workspace
   (Settings > Integrations > Slack > Connect)

2. Ensure the team member has an email address that matches their Slack account

3. The Slack bot must have the following permissions:
   - chat:write (to send messages)
   - im:write (to open DM channels)
   - users:read.email (to look up users by email)

Note: Kudos are sent as direct messages to the recipient.
If you want to post to a channel, enable ""Public kudos"" when sending.

Current Status: {status}";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Looks up a Slack user ID by email address.
        /// </summary>
        private async Task<string?> GetUserIdByEmailAsync(string email, string botToken, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{SlackConfig.ApiBaseUrl}/users.lookupByEmail?email={Uri.EscapeDataString(email)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetProperty("ok").GetBoolean())
                {
                    return root.GetProperty("user").GetProperty("id").GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Opens a DM channel with a user.
        /// </summary>
        private async Task<string?> OpenDmChannelAsync(string userId, string botToken, CancellationToken cancellationToken)
        {
            try
            {
                var payload = new { users = userId };
                var json = JsonSerializer.Serialize(payload);
                
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{SlackConfig.ApiBaseUrl}/conversations.open");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.GetProperty("ok").GetBoolean())
                {
                    return root.GetProperty("channel").GetProperty("id").GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds Slack Block Kit blocks for a rich kudos message.
        /// </summary>
        private object[] BuildSlackBlocks(DataModels.Kudos kudos, TeamMember teamMember, string senderName)
        {
            var categoryEmoji = kudos.Category switch
            {
                KudosCategory.TeamWork => "🤝",
                KudosCategory.Innovation => "💡",
                KudosCategory.Leadership => "👑",
                KudosCategory.CustomerFocus => "🎯",
                KudosCategory.GoingAboveBeyond => "🚀",
                KudosCategory.ProblemSolving => "🔧",
                KudosCategory.LearningGrowth => "📚",
                KudosCategory.Reliability => "⏰",
                KudosCategory.Communication => "💬",
                _ => "⭐"
            };

            var headerText = string.IsNullOrWhiteSpace(kudos.Title)
                ? $"{categoryEmoji} You've received kudos!"
                : $"{categoryEmoji} {kudos.Title}";

            var blocks = new List<object>
            {
                // Header
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = headerText,
                        emoji = true
                    }
                },
                // Message content
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = kudos.Message
                    }
                },
                // Divider
                new { type = "divider" },
                // Context (from, category)
                new
                {
                    type = "context",
                    elements = new object[]
                    {
                        new
                        {
                            type = "mrkdwn",
                            text = $"*From:* {senderName}  |  *Category:* {kudos.CategoryDisplayName}"
                        }
                    }
                }
            };

            return blocks.ToArray();
        }

        #endregion

        #region Helper Classes

        private class SlackApiResponse
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
        }

        #endregion
    }
}
