using System;
using System.IO;
using System.Text.Json;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing local application settings.
/// Settings are stored in a JSON file in the user's local app data folder.
/// </summary>
public class LocalSettingsService
{
    #region Singleton

    private static LocalSettingsService? _instance;
    private static readonly object _lock = new();

    public static LocalSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalSettingsService();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Fields

    private readonly string _settingsFilePath;
    private LocalSettings _settings;

    #endregion

    #region Constructor

    private LocalSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ProCohere");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _settings = LoadSettings();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether dark theme is enabled.
    /// </summary>
    public bool IsDarkTheme
    {
        get => _settings.IsDarkTheme;
        set
        {
            if (_settings.IsDarkTheme != value)
            {
                _settings.IsDarkTheme = value;
                SaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets the remembered email for login.
    /// </summary>
    public string? RememberedEmail
    {
        get => _settings.RememberedEmail;
        set
        {
            if (_settings.RememberedEmail != value)
            {
                _settings.RememberedEmail = value;
                SaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to remember the user's email.
    /// </summary>
    public bool RememberEmail
    {
        get => _settings.RememberEmail;
        set
        {
            if (_settings.RememberEmail != value)
            {
                _settings.RememberEmail = value;
                SaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the user wants to stay signed in.
    /// </summary>
    public bool StaySignedIn
    {
        get => _settings.StaySignedIn;
        set
        {
            if (_settings.StaySignedIn != value)
            {
                _settings.StaySignedIn = value;
                SaveSettings();
            }
        }
    }

    #endregion

    #region Load/Save

    private LocalSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        return new LocalSettings();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    #endregion

    #region Settings Class

    private class LocalSettings
    {
        public bool IsDarkTheme { get; set; } = false;
        public string? RememberedEmail { get; set; }
        public bool RememberEmail { get; set; } = true;
        public bool StaySignedIn { get; set; } = false;
    }

    #endregion
}
