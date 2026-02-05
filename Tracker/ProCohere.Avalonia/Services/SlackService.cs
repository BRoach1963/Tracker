using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Slack messaging service implementation.
/// Sends messages via Slack Web API using bot token.
/// </summary>
public class SlackService : IMessageService
{
    private static SlackService? _instance;
    private readonly HttpClient _httpClient;

    public static SlackService Instance => _instance ??= new SlackService();

    public string ProviderName => "Slack";

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "slack_service.log");

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

    private SlackService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://slack.com/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        Log("SlackService initialized");
    }

    public Task<bool> IsConfiguredAsync()
    {
        var settings = AppSettingsService.Instance.GetMessagingSettings();
        var provider = settings.Provider;
        var botToken = settings.Slack?.BotToken;
        
        var isConfigured = provider == "Slack" && !string.IsNullOrEmpty(botToken);
        Log($"IsConfigured: {isConfigured} (Provider: {provider}, HasToken: {!string.IsNullOrEmpty(botToken)})");
        
        return Task.FromResult(isConfigured);
    }

    public async Task<bool> SendMessageAsync(string recipientEmail, string message)
    {
        try
        {
            Log($"SendMessageAsync called for {recipientEmail}");

            var settings = AppSettingsService.Instance.GetMessagingSettings();
            var botToken = settings.Slack?.BotToken;
            if (string.IsNullOrEmpty(botToken))
            {
                Log("ERROR: Slack bot token not configured");
                return false;
            }

            // Step 1: Look up user by email
            var userId = await GetUserIdByEmailAsync(recipientEmail, botToken);
            if (string.IsNullOrEmpty(userId))
            {
                Log($"ERROR: Could not find Slack user for email {recipientEmail}");
                return false;
            }

            // Step 2: Open a DM channel with the user
            var channelId = await OpenDirectMessageChannelAsync(userId, botToken);
            if (string.IsNullOrEmpty(channelId))
            {
                Log($"ERROR: Could not open DM channel for user {userId}");
                return false;
            }

            // Step 3: Send the message
            var success = await PostMessageAsync(channelId, message, botToken);
            Log($"SendMessageAsync result: {success}");
            
            return success;
        }
        catch (Exception ex)
        {
            Log($"ERROR in SendMessageAsync: {ex.Message}");
            return false;
        }
    }

    private async Task<string?> GetUserIdByEmailAsync(string email, string botToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botToken}");

            var response = await _httpClient.GetAsync($"users.lookupByEmail?email={Uri.EscapeDataString(email)}");
            var content = await response.Content.ReadAsStringAsync();
            
            Log($"users.lookupByEmail response: {content}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"ERROR: Slack API returned {response.StatusCode}");
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.GetProperty("ok").GetBoolean())
            {
                Log($"ERROR: Slack API returned ok=false");
                return null;
            }

            return doc.RootElement.GetProperty("user").GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            Log($"ERROR in GetUserIdByEmailAsync: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> OpenDirectMessageChannelAsync(string userId, string botToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botToken}");

            var payload = new { users = userId };
            var response = await _httpClient.PostAsJsonAsync("conversations.open", payload);
            var content = await response.Content.ReadAsStringAsync();
            
            Log($"conversations.open response: {content}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"ERROR: Slack API returned {response.StatusCode}");
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.GetProperty("ok").GetBoolean())
            {
                Log($"ERROR: Slack API returned ok=false");
                return null;
            }

            return doc.RootElement.GetProperty("channel").GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            Log($"ERROR in OpenDirectMessageChannelAsync: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> PostMessageAsync(string channelId, string messageText, string botToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {botToken}");

            var payload = new
            {
                channel = channelId,
                text = messageText
            };

            var response = await _httpClient.PostAsJsonAsync("chat.postMessage", payload);
            var content = await response.Content.ReadAsStringAsync();
            
            Log($"chat.postMessage response: {content}");

            if (!response.IsSuccessStatusCode)
            {
                Log($"ERROR: Slack API returned {response.StatusCode}");
                return false;
            }

            using var doc = JsonDocument.Parse(content);
            var ok = doc.RootElement.GetProperty("ok").GetBoolean();
            
            return ok;
        }
        catch (Exception ex)
        {
            Log($"ERROR in PostMessageAsync: {ex.Message}");
            return false;
        }
    }
}
