using System.IO;
using System.Text.Json;
using DeepEndControls.Theming;
using Tracker.Classes;
using Tracker.Logging;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages loading and saving of user settings.
    /// Settings are stored per-Supabase-user to ensure each user has their own configuration.
    /// </summary>
    public class UserSettingsManager
    {
        #region Fields

        private bool _initialized;
        private LocalUserSettings _settings = new();
        private string? _currentSupabaseUserId;
        private readonly ILogger _logger;
        
        private static readonly string SettingsFileName = "TrackerSettings.json";
        private static readonly string BaseTrackerFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker");

        #endregion

        #region Singleton Instance

        private static readonly Lazy<UserSettingsManager> _lazyInstance = 
            new(() => new UserSettingsManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of UserSettingsManager.
        /// </summary>
        public static UserSettingsManager Instance => _lazyInstance.Value;

        #endregion

        #region Constructor

        private UserSettingsManager()
        {
            _logger = LoggingManager.GetComponentLogger("UserSettings");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current user settings.
        /// </summary>
        public LocalUserSettings Settings => _settings;

        /// <summary>
        /// Gets the current Supabase user ID (if logged in).
        /// </summary>
        public string? CurrentSupabaseUserId => _currentSupabaseUserId;

        /// <summary>
        /// Gets the path to the current user's settings file.
        /// </summary>
        public string CurrentSettingsFilePath => GetSettingsFilePath(_currentSupabaseUserId);

        /// <summary>
        /// Gets or sets the current theme.
        /// </summary>
        public DeepEndTheme Theme
        {
            get => _settings.Theme;
            set
            {
                if (_settings.Theme != value)
                {
                    _settings.Theme = value;
                    ThemeManager.Instance.ApplyTheme(value);
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently logged in user username (runtime only, not persisted).
        /// Used for audit tracking in database operations.
        /// </summary>
        public string CurrentUser { get; set; } = Environment.UserName;

        /// <summary>
        /// Gets or sets the currently logged in user's database ID (runtime only, not persisted).
        /// This is the User.Id from the Users table.
        /// </summary>
        public int? CurrentUserId { get; set; }

        /// <summary>
        /// Gets or sets the reminder settings.
        /// </summary>
        public ReminderSettings ReminderSettings
        {
            get => _settings.ReminderSettings ?? new ReminderSettings();
            set
            {
                _settings.ReminderSettings = value;
                SaveSettings();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the settings manager with default (anonymous) settings.
        /// Call SwitchToUser() after successful login to load user-specific settings.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            
            // Load anonymous/default settings initially
            // These will be replaced when user logs in via SwitchToUser()
            LoadSettings();
            _initialized = true;
            
            _logger.Info("UserSettingsManager initialized (anonymous mode)");
        }

        /// <summary>
        /// Switches to user-specific settings after successful Supabase login.
        /// Creates new default settings if this is the user's first login.
        /// </summary>
        /// <param name="supabaseUserId">The Supabase user ID (UUID)</param>
        /// <param name="isNewAccount">True if this is a newly created account (use fresh defaults)</param>
        public void SwitchToUser(string supabaseUserId, bool isNewAccount = false)
        {
            if (string.IsNullOrWhiteSpace(supabaseUserId))
            {
                _logger.Warn("SwitchToUser called with empty user ID");
                return;
            }

            _logger.Info("Switching settings to user: {0} (new account: {1})", supabaseUserId, isNewAccount);
            
            // Save current settings before switching (if we have a user)
            if (!string.IsNullOrEmpty(_currentSupabaseUserId))
            {
                SaveSettings();
            }

            _currentSupabaseUserId = supabaseUserId;
            
            var userSettingsPath = GetSettingsFilePath(supabaseUserId);
            
            if (isNewAccount || !File.Exists(userSettingsPath))
            {
                // New account: fresh defaults
                // First login (no user settings file): migrate from anonymous settings if they exist
                _logger.Info("Creating new settings for user: {0} (isNewAccount: {1})", supabaseUserId, isNewAccount);
                
                if (isNewAccount)
                {
                    // Brand new account - start fresh
                    _settings = new LocalUserSettings();
                    _settings.Database = new DatabaseSettings();
                    _settings.Database.SetupCompleted = false;
                }
                else
                {
                    // Existing user logging in for first time with user-specific settings
                    // Migrate settings from anonymous file if it exists
                    var anonymousPath = GetSettingsFilePath(null);
                    if (File.Exists(anonymousPath))
                    {
                        _logger.Info("Migrating settings from anonymous file for user: {0}", supabaseUserId);
                        try
                        {
                            var json = File.ReadAllText(anonymousPath);
                            _settings = JsonSerializer.Deserialize<LocalUserSettings>(json) ?? new LocalUserSettings();
                            _logger.Info("Migrated database settings - CustomSqlitePath: '{0}', SetupCompleted: {1}", 
                                _settings.Database?.CustomSqlitePath ?? "(null)", 
                                _settings.Database?.SetupCompleted ?? false);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn("Failed to migrate anonymous settings: {0}", ex.Message);
                            _settings = new LocalUserSettings();
                            _settings.Database = new DatabaseSettings();
                            _settings.Database.SetupCompleted = false;
                        }
                    }
                    else
                    {
                        _settings = new LocalUserSettings();
                        _settings.Database = new DatabaseSettings();
                        _settings.Database.SetupCompleted = false;
                    }
                }
                
                SaveSettings();
            }
            else
            {
                // Existing user - load their settings
                LoadSettings();
            }
            
            _logger.Info("Settings loaded for user {0}. DB path: {1}", 
                supabaseUserId, 
                string.IsNullOrEmpty(_settings.Database.CustomSqlitePath) 
                    ? "(default)" 
                    : _settings.Database.CustomSqlitePath);
        }

        /// <summary>
        /// Resets to anonymous mode (for logout).
        /// Clears user-specific settings and reloads defaults.
        /// </summary>
        public void ResetToAnonymous()
        {
            _logger.Info("Resetting to anonymous settings (logout)");
            
            // Save current user's settings before clearing
            if (!string.IsNullOrEmpty(_currentSupabaseUserId))
            {
                SaveSettings();
            }
            
            _currentSupabaseUserId = null;
            CurrentUserId = null;
            CurrentUser = Environment.UserName;
            
            // Load anonymous/default settings
            _settings = new LocalUserSettings();
        }

        public void Shutdown()
        {
            SaveSettings();
        }

        /// <summary>
        /// Saves the current settings to disk.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var filePath = GetSettingsFilePath(_currentSupabaseUserId);
                var directory = Path.GetDirectoryName(filePath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(filePath, json);
                
                _logger.Debug("Settings saved to: {0}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to save settings: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Gets the default database path for a specific user.
        /// Each Supabase user gets their own database folder.
        /// </summary>
        public string GetUserDatabasePath(string? supabaseUserId = null)
        {
            var userId = supabaseUserId ?? _currentSupabaseUserId;
            
            if (string.IsNullOrEmpty(userId))
            {
                // Anonymous/default path
                return Path.Combine(BaseTrackerFolder, "tracker.db");
            }
            
            // User-specific database path
            var userFolder = Path.Combine(BaseTrackerFolder, "Users", userId);
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }
            
            return Path.Combine(userFolder, "tracker.db");
        }

        /// <summary>
        /// Saves Remember Me credentials to the anonymous settings file.
        /// This must be called separately because RememberMe needs to be available
        /// before user login (when we're still in anonymous mode).
        /// </summary>
        /// <param name="rememberMe">Whether Remember Me is enabled</param>
        /// <param name="email">Email address to save (if RememberMe is true)</param>
        public void SaveRememberMeToAnonymousSettings(bool rememberMe, string? email)
        {
            try
            {
                var anonymousPath = GetSettingsFilePath(null);
                LocalUserSettings anonymousSettings;

                // Load existing anonymous settings if they exist
                if (File.Exists(anonymousPath))
                {
                    var json = File.ReadAllText(anonymousPath);
                    anonymousSettings = JsonSerializer.Deserialize<LocalUserSettings>(json) ?? new LocalUserSettings();
                }
                else
                {
                    anonymousSettings = new LocalUserSettings();
                }

                // Update RememberMe settings
                anonymousSettings.Authentication ??= new AuthenticationSettings();
                anonymousSettings.Authentication.RememberMe = rememberMe;
                anonymousSettings.Authentication.SavedEmail = rememberMe ? email : null;

                // Save back to anonymous path
                var dir = Path.GetDirectoryName(anonymousPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(anonymousSettings, options);
                File.WriteAllText(anonymousPath, updatedJson);

                _logger.Info("Saved RememberMe={0} to anonymous settings", rememberMe);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to save RememberMe to anonymous settings");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the settings file path for a specific user.
        /// </summary>
        private string GetSettingsFilePath(string? supabaseUserId)
        {
            if (string.IsNullOrEmpty(supabaseUserId))
            {
                // Anonymous/default settings path (pre-login)
                return Path.Combine(BaseTrackerFolder, SettingsFileName);
            }
            
            // User-specific settings path
            var userFolder = Path.Combine(BaseTrackerFolder, "Users", supabaseUserId);
            return Path.Combine(userFolder, SettingsFileName);
        }

        private void LoadSettings()
        {
            try
            {
                var filePath = GetSettingsFilePath(_currentSupabaseUserId);
                
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var loaded = JsonSerializer.Deserialize<LocalUserSettings>(json);
                    if (loaded != null)
                    {
                        _settings = loaded;
                        _logger.Debug("Settings loaded from: {0}", filePath);
                        return;
                    }
                }
                
                _logger.Debug("No settings file found at {0}, using defaults", filePath);
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to load settings: {0}", ex.Message);
            }
            
            // If loading fails or file doesn't exist, use defaults
            _settings = new LocalUserSettings();
        }

        #endregion
    }
}
