using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Coordinator service that routes messages to the configured provider (Slack or Teams).
/// Singleton that auto-detects which messaging service is configured.
/// </summary>
public class MessageService
{
    private static MessageService? _instance;

    public static MessageService Instance => _instance ??= new MessageService();

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "message_service.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    private MessageService()
    {
        Log("MessageService initialized");
    }

    /// <summary>
    /// Gets the currently configured messaging provider.
    /// Returns "None" if no provider is configured.
    /// </summary>
    public string CurrentProvider
    {
        get
        {
            var settings = AppSettingsService.Instance.GetMessagingSettings();
            var provider = settings.Provider;
            return string.IsNullOrEmpty(provider) ? "None" : provider;
        }
    }

    /// <summary>
    /// Whether messaging is configured and available.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        var provider = await GetActiveProviderAsync();
        var available = provider != null;
        
        Log($"IsAvailable: {available} (Provider: {CurrentProvider})");
        return available;
    }

    /// <summary>
    /// Sends a message to a recipient using the configured provider.
    /// </summary>
    /// <param name="recipientEmail">Email address of recipient</param>
    /// <param name="message">Message content</param>
    /// <returns>True if sent successfully</returns>
    public async Task<bool> SendMessageAsync(string recipientEmail, string message)
    {
        try
        {
            Log($"SendMessageAsync called for {recipientEmail}, Provider: {CurrentProvider}");

            var provider = await GetActiveProviderAsync();
            if (provider == null)
            {
                Log("ERROR: No messaging provider configured");
                return false;
            }

            var result = await provider.SendMessageAsync(recipientEmail, message);
            Log($"SendMessageAsync result: {result}");
            
            return result;
        }
        catch (Exception ex)
        {
            Log($"ERROR in SendMessageAsync: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the active messaging provider based on configuration.
    /// Returns null if no provider is configured or ready.
    /// </summary>
    private async Task<IMessageService?> GetActiveProviderAsync()
    {
        var providerName = CurrentProvider;

        IMessageService? service = providerName switch
        {
            "Slack" => SlackService.Instance,
            "Teams" => TeamsService.Instance,
            _ => null
        };

        if (service == null)
        {
            Log($"No service found for provider: {providerName}");
            return null;
        }

        var isConfigured = await service.IsConfiguredAsync();
        if (!isConfigured)
        {
            Log($"{providerName} service exists but is not configured");
            return null;
        }

        return service;
    }
}
