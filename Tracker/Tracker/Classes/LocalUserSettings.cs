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
        public DeepEndTheme Theme { get; set; } = DeepEndTheme.Default;

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
    }
}
