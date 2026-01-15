using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Microsoft365;

namespace Tracker.Services.Kudos
{
    /// <summary>
    /// Configuration for Teams webhook integration (optional for channel posts).
    /// </summary>
    public static class TeamsWebhookConfig
    {
        /// <summary>
        /// Gets the configured webhook URL from user settings.
        /// </summary>
        public static string? WebhookUrl => UserSettingsManager.Instance?.Settings.Kudos.TeamsWebhookUrl;

        /// <summary>
        /// Sets the webhook URL in user settings.
        /// </summary>
        public static void SetWebhookUrl(string url)
        {
            if (UserSettingsManager.Instance != null)
            {
                UserSettingsManager.Instance.Settings.Kudos.TeamsWebhookUrl = url;
                UserSettingsManager.Instance.SaveSettings();
            }
        }
    }

    /// <summary>
    /// Delivers kudos via Microsoft Teams.
    /// 
    /// Primary Method (Automatic - No Setup Required):
    /// Uses Microsoft Graph API via QuickMessageService to send direct 1:1 messages
    /// when the user is connected to M365.
    /// 
    /// Fallback Method (Optional - Manual Setup):
    /// Uses Incoming Webhook to post to a Teams channel if configured.
    /// </summary>
    public class TeamsDeliveryProvider : IKudosDeliveryProvider
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        #endregion

        #region Singleton

        private static readonly Lazy<TeamsDeliveryProvider> _instance =
            new(() => new TeamsDeliveryProvider(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TeamsDeliveryProvider Instance => _instance.Value;

        #endregion

        #region Constructor

        private TeamsDeliveryProvider()
        {
            _logger = LoggingManager.GetComponentLogger("TeamsKudosProvider");
            _httpClient = new HttpClient();
        }

        #endregion

        #region IKudosDeliveryProvider Implementation

        /// <inheritdoc/>
        public DeliveryChannel Channel => DeliveryChannel.MicrosoftTeams;

        /// <inheritdoc/>
        public bool IsAvailable => QuickMessageService.Instance.TeamsAvailable || 
                                   !string.IsNullOrWhiteSpace(TeamsWebhookConfig.WebhookUrl);

        /// <inheritdoc/>
        public string DisplayName => "Microsoft Teams";

        /// <inheritdoc/>
        public async Task<KudosDeliveryResult> SendKudosAsync(
            DataModels.Kudos kudos,
            TeamMember teamMember,
            string senderName,
            CancellationToken cancellationToken = default)
        {
            // Primary: Use Graph API for direct 1:1 messages (no setup required)
            if (QuickMessageService.Instance.TeamsAvailable && !string.IsNullOrEmpty(teamMember.Email))
            {
                return await SendViaGraphAsync(kudos, teamMember, senderName, cancellationToken);
            }

            // Fallback: Use webhook for channel posts
            if (!string.IsNullOrWhiteSpace(TeamsWebhookConfig.WebhookUrl))
            {
                return await SendViaWebhookAsync(kudos, teamMember, senderName, cancellationToken);
            }

            return KudosDeliveryResult.Failed(
                "Teams is not available. Please connect to Microsoft 365 in Settings, " +
                "or configure a Teams webhook for channel posts.");
        }

        /// <summary>
        /// Sends kudos via Microsoft Graph API (direct 1:1 message).
        /// </summary>
        private async Task<KudosDeliveryResult> SendViaGraphAsync(
            DataModels.Kudos kudos,
            TeamMember teamMember,
            string senderName,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info("Sending kudos to {0} via Teams Graph API", teamMember.FullName);

                var message = FormatKudosMessageHtml(kudos, teamMember, senderName);
                var (success, error) = await QuickMessageService.Instance.SendTeamsMessageAsync(
                    teamMember.Email!, message);

                if (success)
                {
                    _logger.Info("Kudos delivered to {0} via Teams direct message", teamMember.FullName);
                    return KudosDeliveryResult.Succeeded();
                }
                else
                {
                    _logger.Warn("Teams Graph API failed: {0}", error);
                    
                    // Try webhook fallback if available
                    if (!string.IsNullOrWhiteSpace(TeamsWebhookConfig.WebhookUrl))
                    {
                        _logger.Info("Attempting webhook fallback...");
                        return await SendViaWebhookAsync(kudos, teamMember, senderName, cancellationToken);
                    }
                    
                    return KudosDeliveryResult.Failed(error ?? "Unknown error sending Teams message");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending kudos via Teams Graph API");
                return KudosDeliveryResult.Failed($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends kudos via Teams Incoming Webhook (channel post).
        /// </summary>
        private async Task<KudosDeliveryResult> SendViaWebhookAsync(
            DataModels.Kudos kudos,
            TeamMember teamMember,
            string senderName,
            CancellationToken cancellationToken)
        {
            try
            {
                var webhookUrl = TeamsWebhookConfig.WebhookUrl;
                if (string.IsNullOrWhiteSpace(webhookUrl))
                {
                    return KudosDeliveryResult.Failed("Teams webhook URL is not configured.");
                }

                var card = BuildAdaptiveCard(kudos, teamMember, senderName);
                var payload = new
                {
                    type = "message",
                    attachments = new[]
                    {
                        new
                        {
                            contentType = "application/vnd.microsoft.card.adaptive",
                            contentUrl = (string?)null,
                            content = card
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.Info("Sending kudos to Teams channel for {0}", teamMember.FullName);
                var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.Info("Kudos delivered to Teams channel successfully");
                    return KudosDeliveryResult.Succeeded();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.Warn("Teams webhook returned {0}: {1}", response.StatusCode, error);
                    return KudosDeliveryResult.Failed($"Teams returned {response.StatusCode}: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending kudos to Teams webhook");
                return KudosDeliveryResult.Failed($"Error: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            // Test Graph API first
            if (QuickMessageService.Instance.TeamsAvailable)
            {
                return true; // Graph API is available
            }

            // Test webhook if configured
            if (!string.IsNullOrWhiteSpace(TeamsWebhookConfig.WebhookUrl))
            {
                return await TestWebhookAsync(cancellationToken);
            }

            return false;
        }

        private async Task<bool> TestWebhookAsync(CancellationToken cancellationToken)
        {
            try
            {
                var webhookUrl = TeamsWebhookConfig.WebhookUrl;
                if (string.IsNullOrWhiteSpace(webhookUrl))
                    return false;

                var testPayload = new
                {
                    type = "message",
                    attachments = new[]
                    {
                        new
                        {
                            contentType = "application/vnd.microsoft.card.adaptive",
                            content = new
                            {
                                type = "AdaptiveCard",
                                version = "1.4",
                                body = new[]
                                {
                                    new
                                    {
                                        type = "TextBlock",
                                        text = "✅ Tracker Kudos connection test successful!",
                                        wrap = true
                                    }
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(testPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Teams webhook test failed");
                return false;
            }
        }

        /// <inheritdoc/>
        public string GetSetupInstructions()
        {
            if (QuickMessageService.Instance.TeamsAvailable)
            {
                return """
                    ✅ Teams is ready to use!
                    
                    You're connected to Microsoft 365, so kudos will be sent directly 
                    to team members as private Teams messages.
                    
                    No additional setup required!
                    
                    Optional: Configure a webhook below to also post kudos to a Teams channel.
                    """;
            }
            
            return """
                📋 Microsoft Teams Setup Options:
                
                RECOMMENDED - Connect to Microsoft 365:
                Go to Settings > Integrations > Microsoft 365 and sign in.
                This enables sending kudos as direct messages - no webhook needed!
                
                ALTERNATIVE - Use a Webhook (for channel posts only):
                1. Open Microsoft Teams and go to the channel where you want kudos posted
                2. Click the "..." menu next to the channel name
                3. Select "Connectors" (or "Manage channel" > "Connectors")
                4. Search for "Incoming Webhook" and click "Configure"
                5. Give it a name like "Tracker Kudos" and optionally upload an icon
                6. Click "Create" and copy the webhook URL
                7. Paste the URL in the field below
                """;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats kudos as an HTML message for Teams direct messages.
        /// </summary>
        private string FormatKudosMessageHtml(DataModels.Kudos kudos, TeamMember teamMember, string senderName)
        {
            var categoryEmoji = GetBadgeEmoji(kudos.BadgeType);
            var title = string.IsNullOrWhiteSpace(kudos.Title)
                ? $"{categoryEmoji} You received kudos!"
                : $"{categoryEmoji} {kudos.Title}";

            return $"""
                <div style="font-family: Segoe UI, sans-serif;">
                    <h2 style="color: #6264A7; margin-bottom: 8px;">{title}</h2>
                    <p style="font-size: 14px; margin: 12px 0;">{kudos.Message}</p>
                    <hr style="border: none; border-top: 1px solid #ddd; margin: 12px 0;" />
                    <p style="font-size: 12px; color: #666;">
                        <strong>Badge:</strong> {kudos.BadgeType ?? "Recognition"}<br/>
                        <strong>From:</strong> {senderName}
                    </p>
                </div>
                """;
        }

        /// <summary>
        /// Gets the emoji for a kudos badge type.
        /// </summary>
        private static string GetBadgeEmoji(string? badgeType) => badgeType?.ToLower() switch
        {
            "team_player" => "🤝",
            "innovator" => "💡",
            "leader" or "leadership" => "👑",
            "customer_focus" => "🎯",
            "mentor" => "🚀",
            "problem_solver" => "🔧",
            "learner" => "📚",
            "reliable" => "⏰",
            "communicator" => "💬",
            _ => "⭐"
        };

        /// <summary>
        /// Builds an Adaptive Card for the kudos message (for webhook delivery).
        /// </summary>
        private object BuildAdaptiveCard(DataModels.Kudos kudos, TeamMember teamMember, string senderName)
        {
            var categoryEmoji = GetBadgeEmoji(kudos.BadgeType);

            var title = string.IsNullOrWhiteSpace(kudos.Title)
                ? $"{categoryEmoji} Kudos to {teamMember.FullName}!"
                : $"{categoryEmoji} {kudos.Title}";

            return new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new object[]
                {
                    new
                    {
                        type = "TextBlock",
                        text = title,
                        weight = "bolder",
                        size = "large",
                        wrap = true
                    },
                    new
                    {
                        type = "TextBlock",
                        text = kudos.Message,
                        wrap = true,
                        spacing = "medium"
                    },
                    new
                    {
                        type = "FactSet",
                        facts = new[]
                        {
                            new { title = "To", value = teamMember.FullName },
                            new { title = "From", value = senderName },
                            new { title = "Badge", value = kudos.BadgeType ?? "Recognition" }
                        },
                        spacing = "medium"
                    }
                },
                msteams = new
                {
                    width = "Full"
                }
            };
        }

        #endregion
    }
}
