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
        /// Defaults to Light theme for professional appearance.
        /// </summary>
        public DeepEndTheme Theme { get; set; } = DeepEndTheme.Light;

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

        /// <summary>
        /// Kudos/Recognition integration settings.
        /// </summary>
        public KudosSettings Kudos { get; set; } = new();

        /// <summary>
        /// Proactive AI Insights settings.
        /// </summary>
        public InsightSettings Insights { get; set; } = new();

        /// <summary>
        /// Predictive Analytics settings.
        /// </summary>
        public PredictiveAnalyticsSettings PredictiveAnalytics { get; set; } = new();

        /// <summary>
        /// Meeting Prep auto-generation settings.
        /// </summary>
        public MeetingPrepSettings MeetingPrep { get; set; } = new();
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

        /// <summary>
        /// The selected AI provider (Gemini, OpenAI, Anthropic).
        /// Stored as string for JSON serialization.
        /// </summary>
        public string SelectedProvider { get; set; } = "Gemini";
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
        /// Bot token obtained via OAuth for this workspace.
        /// Each customer gets their own token when they connect.
        /// </summary>
        public string? BotToken { get; set; }

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

    /// <summary>
    /// Kudos/Recognition integration settings.
    /// </summary>
    public class KudosSettings
    {
        /// <summary>
        /// Microsoft Teams incoming webhook URL for kudos delivery.
        /// </summary>
        public string? TeamsWebhookUrl { get; set; }

        /// <summary>
        /// Optional Slack channel ID for public kudos.
        /// </summary>
        public string? SlackKudosChannelId { get; set; }

        /// <summary>
        /// Whether to include kudos in meeting prep materials by default.
        /// </summary>
        public bool MentionInMeetingPrepByDefault { get; set; } = true;
    }

    /// <summary>
    /// Proactive AI Insights settings.
    /// </summary>
    public class InsightSettings
    {
        /// <summary>
        /// Whether proactive insights are enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether to show daily briefing on app startup.
        /// </summary>
        public bool ShowDailyBriefingOnStartup { get; set; } = true;

        /// <summary>
        /// Days without a 1:1 before generating a warning.
        /// </summary>
        public int MeetingGapWarningDays { get; set; } = 14;

        /// <summary>
        /// Days without a 1:1 before generating a critical alert.
        /// </summary>
        public int MeetingGapCriticalDays { get; set; } = 21;

        /// <summary>
        /// Days before an action item is considered stale.
        /// </summary>
        public int ActionItemStaleDays { get; set; } = 14;

        /// <summary>
        /// Days ahead to look for upcoming birthdays.
        /// </summary>
        public int BirthdayLookAheadDays { get; set; } = 7;

        /// <summary>
        /// Days ahead to look for upcoming work anniversaries.
        /// </summary>
        public int AnniversaryLookAheadDays { get; set; } = 7;

        /// <summary>
        /// Survey rating threshold (at or below) to generate an alert.
        /// </summary>
        public int LowSurveyRatingThreshold { get; set; } = 3;

        /// <summary>
        /// Whether to generate AI-powered summary (costs API credits).
        /// </summary>
        public bool EnableAiSummary { get; set; } = false;

        /// <summary>
        /// Hours between automatic analysis runs.
        /// </summary>
        public int AnalysisIntervalHours { get; set; } = 4;
    }

    /// <summary>
    /// Settings for Predictive Analytics features.
    /// </summary>
    public class PredictiveAnalyticsSettings
    {
        /// <summary>
        /// Whether predictive analytics features are enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether to show trajectory charts in detail views.
        /// </summary>
        public bool ShowTrajectoryCharts { get; set; } = true;

        /// <summary>
        /// Whether to show confidence intervals in predictions.
        /// </summary>
        public bool ShowConfidenceIntervals { get; set; } = true;

        /// <summary>
        /// Whether to enable what-if scenario simulations.
        /// </summary>
        public bool EnableWhatIfScenarios { get; set; } = true;

        /// <summary>
        /// Whether to show AI-generated recommendations.
        /// </summary>
        public bool ShowRecommendations { get; set; } = true;

        /// <summary>
        /// Minimum data points required before showing predictions.
        /// </summary>
        public int MinDataPointsForPrediction { get; set; } = 5;

        /// <summary>
        /// How many days of history to retain for analytics.
        /// </summary>
        public int HistoryRetentionDays { get; set; } = 365;

        /// <summary>
        /// How often to capture progress snapshots.
        /// </summary>
        public SnapshotFrequency SnapshotFrequency { get; set; } = SnapshotFrequency.Daily;
    }

    /// <summary>
    /// Frequency for capturing progress snapshots.
    /// </summary>
    public enum SnapshotFrequency
    {
        Daily,
        Weekly,
        Manual
    }

    /// <summary>
    /// Settings for automatic meeting prep generation.
    /// </summary>
    public class MeetingPrepSettings
    {
        /// <summary>
        /// Whether meeting prep auto-generation is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether to automatically show prep when opening a meeting.
        /// </summary>
        public bool AutoShowOnMeetingOpen { get; set; } = true;

        /// <summary>
        /// Whether to enable AI-generated agenda suggestions.
        /// </summary>
        public bool EnableAiSuggestions { get; set; } = false;

        /// <summary>
        /// Maximum days to look back for overdue tasks.
        /// </summary>
        public int ShowOverdueTasksMaxDays { get; set; } = 30;

        /// <summary>
        /// Whether to show completed action items from last meeting.
        /// </summary>
        public bool ShowCompletedActionItems { get; set; } = false;

        /// <summary>
        /// Whether to include survey responses in prep.
        /// </summary>
        public bool IncludeSurveyResponses { get; set; } = true;

        /// <summary>
        /// Days to look back for survey responses.
        /// </summary>
        public int SurveyLookbackDays { get; set; } = 30;

        /// <summary>
        /// Maximum items to show per section.
        /// </summary>
        public int MaxItemsPerSection { get; set; } = 5;

        /// <summary>
        /// Days ahead to check for birthdays.
        /// </summary>
        public int BirthdayLookAheadDays { get; set; } = 7;

        /// <summary>
        /// Days ahead to check for work anniversaries.
        /// </summary>
        public int AnniversaryLookAheadDays { get; set; } = 7;

        /// <summary>
        /// Days to look back for recent feedback.
        /// </summary>
        public int FeedbackLookbackDays { get; set; } = 30;
    }
}
