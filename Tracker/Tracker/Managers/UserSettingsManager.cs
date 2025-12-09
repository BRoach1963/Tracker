using System.IO;
using System.Text.Json;
using DeepEndControls.Theming;
using Tracker.Classes;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages loading and saving of user settings.
    /// </summary>
    public class UserSettingsManager
    {
        #region Fields

        private bool _initialized;
        private LocalUserSettings _settings = new();
        private static readonly string SettingsFileName = "TrackerSettings.json";
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker",
            SettingsFileName);

        #endregion

        #region Singleton Instance

        private static UserSettingsManager? _instance;
        private static readonly object SyncRoot = new();

        public static UserSettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new UserSettingsManager();
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current user settings.
        /// </summary>
        public LocalUserSettings Settings => _settings;

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
        /// Gets or sets the currently logged in user (runtime only, not persisted).
        /// Used for audit tracking in database operations.
        /// </summary>
        public string CurrentUser { get; set; } = Environment.UserName;

        #endregion

        #region Public Methods

        public void Initialize()
        {
            if (_initialized) return;
            LoadSettings();
            _initialized = true;
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
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail - settings are not critical
            }
        }

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<LocalUserSettings>(json);
                    if (loaded != null)
                    {
                        _settings = loaded;
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, use defaults
                _settings = new LocalUserSettings();
            }
        }

        #endregion
    }
}
