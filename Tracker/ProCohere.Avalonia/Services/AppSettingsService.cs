using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for loading configuration from appsettings.json
/// </summary>
public class AppSettingsService
{
    private static AppSettingsService? _instance;
    private static readonly object _lock = new();
    private AppSettings? _settings;
    private readonly string _settingsPath;

    private AppSettingsService()
    {
        // Look for appsettings.json in the application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _settingsPath = Path.Combine(appDirectory, "appsettings.json");
    }

    public static AppSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AppSettingsService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Load settings from appsettings.json. Call this at application startup.
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                Console.WriteLine($"Warning: appsettings.json not found at {_settingsPath}");
                _settings = new AppSettings(); // Use empty settings
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (_settings == null)
            {
                Console.WriteLine("Warning: Failed to deserialize appsettings.json");
                _settings = new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading appsettings.json: {ex.Message}");
            _settings = new AppSettings();
        }
    }

    /// <summary>
    /// Get the current settings. Returns empty settings if not loaded.
    /// </summary>
    public AppSettings GetSettings()
    {
        return _settings ?? new AppSettings();
    }

    /// <summary>
    /// Get Google Calendar OAuth credentials
    /// </summary>
    public (string? ClientId, string? ClientSecret) GetGoogleCalendarCredentials()
    {
        var settings = GetSettings();
        return (settings.GoogleCalendar?.ClientId, settings.GoogleCalendar?.ClientSecret);
    }

    /// <summary>
    /// Get Gemini API key (legacy path for backwards compatibility)
    /// </summary>
    public string? GetGeminiApiKey()
    {
        return GetSettings().AI?.GeminiApiKey;
    }

    /// <summary>
    /// Get Microsoft Calendar OAuth credentials
    /// </summary>
    public (string? ClientId, string? ClientSecret) GetMicrosoftCalendarCredentials()
    {
        var settings = GetSettings();
        return (settings.MicrosoftCalendar?.ClientId, settings.MicrosoftCalendar?.ClientSecret);
    }

    /// <summary>
    /// Get AI settings
    /// </summary>
    public AISettings GetAISettings()
    {
        return GetSettings().AI ?? new AISettings();
    }

    /// <summary>
    /// Get Messaging settings
    /// </summary>
    public MessagingSettings GetMessagingSettings()
    {
        return GetSettings().Messaging ?? new MessagingSettings();
    }
}

/// <summary>
/// Root settings model - only app-level credentials that are identical for all installations
/// </summary>
public class AppSettings
{
    public AISettings? AI { get; set; }
    public GoogleCalendarSettings? GoogleCalendar { get; set; }
    public MicrosoftCalendarSettings? MicrosoftCalendar { get; set; }
    public MessagingSettings? Messaging { get; set; }
}

/// <summary>
/// App-level AI configuration (API keys shared across all users)
/// </summary>
public class AISettings
{
    public bool IsEnabled { get; set; } = true;
    public string Provider { get; set; } = "Gemini";
    public string? GeminiApiKey { get; set; }
    public string? GroqApiKey { get; set; }
    public string GeminiModel { get; set; } = "gemini-2.5-flash";
    public int MaxResponseTokens { get; set; } = 1024;
}

public class GoogleCalendarSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class MicrosoftCalendarSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class MessagingSettings
{
    public string Provider { get; set; } = "None";
    public SlackSettings? Slack { get; set; }
    public TeamsSettings? Teams { get; set; }
}

public class SlackSettings
{
    public string? BotToken { get; set; }
    public string? WorkspaceId { get; set; }
}

public class TeamsSettings
{
    // Uses existing Microsoft Calendar credentials
}
