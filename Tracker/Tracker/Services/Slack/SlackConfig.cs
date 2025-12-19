namespace Tracker.Services.Slack
{
    /// <summary>
    /// Configuration for Slack API integration.
    /// </summary>
    internal static class SlackConfig
    {
        /// <summary>
        /// Slack App Client ID.
        /// </summary>
        internal const string ClientId = "10131370740996.10112350860071";

        /// <summary>
        /// Slack App Client Secret.
        /// </summary>
        internal const string ClientSecret = "71b62f13f9f9d89d7cb448b71125550a";

        /// <summary>
        /// Slack App Signing Secret (for verifying requests from Slack).
        /// </summary>
        internal const string SigningSecret = "e268c17a9c8f3df97bd95782beeeb92e";

        /// <summary>
        /// Bot User OAuth Token (for API calls).
        /// This token is workspace-specific and allows the bot to act in the workspace.
        /// </summary>
        internal const string BotToken = "xoxb-10131370740996-10156144519904-uDUz4r0wmLSoSxIvv5ZbfErl";

        /// <summary>
        /// Local redirect URI for OAuth flow.
        /// </summary>
        internal const string RedirectUri = "http://localhost:8891/slack/callback";

        /// <summary>
        /// OAuth scopes requested for user tokens.
        /// </summary>
        internal static readonly string[] UserScopes = new[]
        {
            "users:read",
            "dnd:read"
        };

        /// <summary>
        /// OAuth scopes for bot tokens (already configured in Slack app).
        /// </summary>
        internal static readonly string[] BotScopes = new[]
        {
            "chat:write",
            "im:write",
            "users:read",
            "users:read.email",
            "users.profile:read",
            "team:read",
            "channels:read",
            "groups:read",
            "im:read"
        };

        /// <summary>
        /// Slack API base URL.
        /// </summary>
        internal const string ApiBaseUrl = "https://slack.com/api";

        /// <summary>
        /// Slack OAuth authorize URL.
        /// </summary>
        internal const string AuthorizeUrl = "https://slack.com/oauth/v2/authorize";

        /// <summary>
        /// Slack OAuth token URL.
        /// </summary>
        internal const string TokenUrl = "https://slack.com/api/oauth.v2.access";
    }
}

