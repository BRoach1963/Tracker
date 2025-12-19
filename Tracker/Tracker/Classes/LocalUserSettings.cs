using DeepEndControls.Theming;

namespace Tracker.Classes
{
    /// <summary>
    /// Stores user preferences that persist across sessions.
    /// </summary>
    public class LocalUserSettings
    {
        /// <summary>
        /// The selected application theme.
        /// </summary>
        public DeepEndTheme Theme { get; set; } = DeepEndTheme.Tracker;

        /// <summary>
        /// Database connection settings.
        /// </summary>
        public DatabaseSettings Database { get; set; } = new();

        /// <summary>
        /// The current user's display name.
        /// </summary>
        public string CurrentUser { get; set; } = Environment.UserName;

        /// <summary>
        /// Whether to remember the last used database connection.
        /// </summary>
        public bool RememberConnection { get; set; } = true;

        /// <summary>
        /// Calendar and email integration settings.
        /// </summary>
        public CalendarSettings Calendar { get; set; } = new();

        /// <summary>
        /// Reminder and notification settings.
        /// </summary>
        public ReminderSettings ReminderSettings { get; set; } = new();

        /// <summary>
        /// Authentication settings.
        /// </summary>
        public AuthenticationSettings Authentication { get; set; } = new();

        /// <summary>
        /// AI Assistant settings.
        /// </summary>
        public AISettings AI { get; set; } = new();

        /// <summary>
        /// Microsoft 365 integration settings.
        /// </summary>
        public Microsoft365Settings Microsoft365 { get; set; } = new();

        /// <summary>
        /// Google Workspace integration settings.
        /// </summary>
        public GoogleSettings Google { get; set; } = new();

        /// <summary>
        /// Slack integration settings.
        /// </summary>
        public SlackSettings Slack { get; set; } = new();
    }

    /// <summary>
    /// Microsoft 365 (Outlook/Teams) integration settings.
    /// </summary>
    public class Microsoft365Settings
    {
        /// <summary>
        /// Whether Microsoft 365 integration is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Whether calendar sync is enabled.
        /// </summary>
        public bool CalendarSyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether Teams integration is enabled.
        /// </summary>
        public bool TeamsSyncEnabled { get; set; } = true;

        /// <summary>
        /// Delta link for incremental calendar sync.
        /// </summary>
        public string? CalendarDeltaLink { get; set; }

        /// <summary>
        /// When calendar was last synchronized.
        /// </summary>
        public DateTime? LastCalendarSync { get; set; }

        /// <summary>
        /// Sync interval in minutes.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Whether to sync on app window focus.
        /// </summary>
        public bool SyncOnFocus { get; set; } = true;

        /// <summary>
        /// Whether to show sync notifications.
        /// </summary>
        public bool ShowSyncNotifications { get; set; } = true;

        /// <summary>
        /// How many days back to sync calendar events.
        /// </summary>
        public int SyncDaysBack { get; set; } = 30;

        /// <summary>
        /// How many days forward to sync calendar events.
        /// </summary>
        public int SyncDaysForward { get; set; } = 90;

        /// <summary>
        /// Connected Microsoft account email (for display).
        /// </summary>
        public string? ConnectedAccountEmail { get; set; }

        /// <summary>
        /// Whether Calendar service is available (detected at runtime).
        /// </summary>
        public bool CalendarAvailable { get; set; } = false;

        /// <summary>
        /// Whether Teams service is available (detected at runtime).
        /// </summary>
        public bool TeamsAvailable { get; set; } = false;
    }

    /// <summary>
    /// AI Assistant configuration settings.
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Whether the AI Assistant is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The AI provider to use (Gemini, Groq, etc.)
        /// </summary>
        public string Provider { get; set; } = "Gemini";

        /// <summary>
        /// Google Gemini API Key.
        /// </summary>
        public string GeminiApiKey { get; set; } = "AIzaSyD-emfKOD1x5DDbSSzU11HlLZq-QjpX5fk";

        /// <summary>
        /// Groq API Key (alternative provider).
        /// </summary>
        public string GroqApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The model to use for Gemini (e.g., "gemini-2.5-pro").
        /// </summary>
        public string GeminiModel { get; set; } = "gemini-2.5-pro";

        /// <summary>
        /// Maximum tokens for AI responses.
        /// </summary>
        public int MaxResponseTokens { get; set; } = 1024;

        /// <summary>
        /// Monthly budget limit in dollars.
        /// </summary>
        public decimal MonthlyBudget { get; set; } = 100.00m;

        /// <summary>
        /// Percentage of budget at which to show warnings (0-100).
        /// </summary>
        public int BudgetWarningPercent { get; set; } = 80;

        /// <summary>
        /// Whether to enforce the budget limit (disable AI when exceeded).
        /// </summary>
        public bool EnforceBudgetLimit { get; set; } = true;
    }

    /// <summary>
    /// Authentication and login settings.
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// The database User ID associated with this authentication.
        /// Stored to quickly look up the user without querying by username.
        /// </summary>
        public int? StoredUserId { get; set; }

        /// <summary>
        /// Whether the user has completed initial account setup.
        /// </summary>
        public bool AccountSetupCompleted { get; set; } = false;

        /// <summary>
        /// Whether the user has linked a cloud account (Supabase).
        /// </summary>
        public bool CloudAccountLinked { get; set; } = false;

        /// <summary>
        /// The cloud user ID (Supabase auth.users.id).
        /// </summary>
        public string? CloudUserId { get; set; }

        /// <summary>
        /// The cloud user email for display purposes.
        /// </summary>
        public string? CloudUserEmail { get; set; }

        /// <summary>
        /// Whether "Remember Me" is enabled for auto-login.
        /// </summary>
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// The saved email address when "Remember Me" is enabled.
        /// Password is stored separately in encrypted secure storage.
        /// </summary>
        public string? SavedEmail { get; set; }
    }

    /// <summary>
    /// Google Workspace (Gmail, Calendar, Meet) integration settings.
    /// </summary>
    public class GoogleSettings
    {
        /// <summary>
        /// Whether Google integration is connected.
        /// </summary>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Whether calendar sync is enabled.
        /// </summary>
        public bool CalendarSyncEnabled { get; set; } = false;

        /// <summary>
        /// Whether Gmail integration is enabled.
        /// </summary>
        public bool GmailEnabled { get; set; } = false;

        /// <summary>
        /// Whether to auto-create Google Meet links for meetings.
        /// </summary>
        public bool AutoCreateMeetLinks { get; set; } = false;

        /// <summary>
        /// Connected Google account email.
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// Connected Google account display name.
        /// </summary>
        public string? UserDisplayName { get; set; }

        /// <summary>
        /// When calendar was last synchronized.
        /// </summary>
        public DateTime? LastCalendarSync { get; set; }

        /// <summary>
        /// Sync token for incremental calendar sync.
        /// </summary>
        public string? CalendarSyncToken { get; set; }

        /// <summary>
        /// Sync interval in minutes.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// How many days back to sync calendar events.
        /// </summary>
        public int SyncDaysBack { get; set; } = 30;

        /// <summary>
        /// How many days forward to sync calendar events.
        /// </summary>
        public int SyncDaysForward { get; set; } = 90;
    }

    /// <summary>
    /// Slack integration settings.
    /// </summary>
    public class SlackSettings
    {
        /// <summary>
        /// Whether Slack integration is connected.
        /// </summary>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Whether Slack messaging is enabled.
        /// </summary>
        public bool MessagingEnabled { get; set; } = true;

        /// <summary>
        /// Whether to sync presence/status from Slack.
        /// </summary>
        public bool PresenceSyncEnabled { get; set; } = true;

        /// <summary>
        /// Whether to use Slack profile photos.
        /// </summary>
        public bool UseSlackPhotos { get; set; } = true;

        /// <summary>
        /// Connected Slack workspace name.
        /// </summary>
        public string? WorkspaceName { get; set; }

        /// <summary>
        /// Connected Slack workspace ID.
        /// </summary>
        public string? WorkspaceId { get; set; }

        /// <summary>
        /// Connected user's Slack ID.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Connected user's email on Slack.
        /// </summary>
        public string? UserEmail { get; set; }
    }
}
