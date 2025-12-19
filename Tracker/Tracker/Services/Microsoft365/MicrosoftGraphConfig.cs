namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Configuration constants for Microsoft Graph API integration.
    /// These are embedded in the binary - safe to include as they are public identifiers.
    /// </summary>
    internal static class MicrosoftGraphConfig
    {
        /// <summary>
        /// Azure AD Application (client) ID from App Registration.
        /// This is a public identifier, not a secret.
        /// </summary>
        internal const string ClientId = "54e29071-3d9f-4e95-a93d-149d843d67c5";

        /// <summary>
        /// Authority URL for multi-tenant authentication.
        /// "common" allows both work/school and personal Microsoft accounts.
        /// "organizations" would restrict to work/school only.
        /// </summary>
        internal const string Authority = "https://login.microsoftonline.com/common";

        /// <summary>
        /// Redirect URI for desktop applications using MSAL.
        /// MSAL handles this automatically for public client flows.
        /// </summary>
        internal const string RedirectUri = "http://localhost";

        /// <summary>
        /// Permission scopes requested from Microsoft Graph.
        /// These match what's configured in Azure App Registration.
        /// </summary>
        internal static readonly string[] Scopes = new[]
        {
            "User.Read",              // Basic user profile
            "User.Read.All",          // Read other users' profiles (photos)
            "Calendars.ReadWrite",    // Read/write calendar events
            "Chat.ReadWrite",         // Send Teams messages (1:1 chats)
            "Mail.Send",              // Send emails
            "OnlineMeetings.ReadWrite", // Create Teams meeting links
            "Presence.Read.All",      // Read availability status
            "offline_access"          // Refresh tokens for persistent access
        };

        /// <summary>
        /// Additional scopes for advanced Teams integration.
        /// Only requested when user enables Teams channel features.
        /// </summary>
        internal static readonly string[] TeamsAdvancedScopes = new[]
        {
            "ChannelMessage.Send",  // Post to Teams channels
            "Team.ReadBasic.All"    // List teams user belongs to
        };

        /// <summary>
        /// Cache file name for MSAL token cache.
        /// Stored in %LOCALAPPDATA%\Tracker\auth\
        /// </summary>
        internal const string TokenCacheFileName = "msal_cache.dat";
    }
}

