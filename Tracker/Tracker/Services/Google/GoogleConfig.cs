namespace Tracker.Services.Google
{
    /// <summary>
    /// Configuration for Google Workspace integration.
    /// Get credentials from: https://console.cloud.google.com/
    /// </summary>
    internal static class GoogleConfig
    {
        /// <summary>
        /// Google Cloud Console Project Client ID.
        /// Go to: APIs & Services → Credentials → Create OAuth 2.0 Client ID (Desktop app)
        /// </summary>
        internal const string ClientId = "798933418018-qk901a3tbjvetpdqu2lils04h8kna4vi.apps.googleusercontent.com";

        /// <summary>
        /// Client Secret (for desktop apps, this is embedded but still required by Google)
        /// </summary>
        internal const string ClientSecret = "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";

        /// <summary>
        /// OAuth 2.0 scopes required for integration.
        /// These are requested at runtime - no console configuration needed.
        /// </summary>
        internal static readonly string[] Scopes = new[]
        {
            // Calendar - read/write events, create Meet links
            "https://www.googleapis.com/auth/calendar",
            "https://www.googleapis.com/auth/calendar.events",
            
            // Gmail - send emails
            "https://www.googleapis.com/auth/gmail.send",
            
            // User info - basic profile (always available, no extra API needed)
            "https://www.googleapis.com/auth/userinfo.email",
            "https://www.googleapis.com/auth/userinfo.profile"
        };

        /// <summary>
        /// Redirect URI for desktop OAuth flow.
        /// </summary>
        internal const string RedirectUri = "http://localhost";

        /// <summary>
        /// Application name shown in Google consent screen.
        /// </summary>
        internal const string ApplicationName = "Tracker";

        /// <summary>
        /// File to store user credentials (encrypted via DPAPI).
        /// </summary>
        internal const string CredentialFileName = "google_credentials.dat";
    }
}

