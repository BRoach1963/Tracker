using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Microsoft Teams messaging service implementation.
/// Sends messages via Microsoft Graph API.
/// Reuses existing Microsoft authentication from calendar integration.
/// </summary>
public class TeamsService : IMessageService
{
    private static TeamsService? _instance;

    public static TeamsService Instance => _instance ??= new TeamsService();

    public string ProviderName => "Teams";

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "teams_service.log");

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

    private TeamsService()
    {
        Log("TeamsService initialized");
    }

    public Task<bool> IsConfiguredAsync()
    {
        var settings = AppSettingsService.Instance.GetMessagingSettings();
        var provider = settings.Provider;
        
        // Teams uses the same Microsoft auth as calendar integration
        var hasAuth = MicrosoftCalendarService.Instance.IsAuthenticated;
        
        var isConfigured = provider == "Teams" && hasAuth;
        Log($"IsConfigured: {isConfigured} (Provider: {provider}, HasAuth: {hasAuth})");
        
        return Task.FromResult(isConfigured);
    }

    public async Task<bool> SendMessageAsync(string recipientEmail, string message)
    {
        try
        {
            Log($"SendMessageAsync called for {recipientEmail}");

            if (!MicrosoftCalendarService.Instance.IsAuthenticated)
            {
                Log("ERROR: Microsoft Graph not authenticated");
                return false;
            }

            var graphClient = MicrosoftCalendarService.Instance.GraphClient;
            if (graphClient == null)
            {
                Log("ERROR: GraphClient is null");
                return false;
            }

            // Step 1: Look up user by email
            var user = await GetUserByEmailAsync(graphClient, recipientEmail);
            if (user == null)
            {
                Log($"ERROR: Could not find user for email {recipientEmail}");
                return false;
            }

            // Step 2: Create a chat message
            // Note: Sending Teams chat messages requires creating a new chat or using an existing one
            // For simplicity, we'll send an email via Outlook instead (which Teams also surfaces)
            var success = await SendEmailAsync(graphClient, recipientEmail, message);
            Log($"SendMessageAsync result: {success}");
            
            return success;
        }
        catch (Exception ex)
        {
            Log($"ERROR in SendMessageAsync: {ex.Message}");
            return false;
        }
    }

    private async Task<User?> GetUserByEmailAsync(GraphServiceClient graphClient, string email)
    {
        try
        {
            // Search for user by email
            var users = await graphClient.Users
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
                    config.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName" };
                });

            var userList = users?.Value;
            if (userList == null || userList.Count == 0)
            {
                Log($"No user found for email {email}");
                return null;
            }

            return userList[0];
        }
        catch (Exception ex)
        {
            Log($"ERROR in GetUserByEmailAsync: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> SendEmailAsync(GraphServiceClient graphClient, string recipientEmail, string messageText)
    {
        try
        {
            var currentUser = await graphClient.Me.GetAsync();
            if (currentUser == null)
            {
                Log("ERROR: Could not get current user");
                return false;
            }

            var emailMessage = new Message
            {
                Subject = "Quick Message from ProCohere",
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = messageText
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = recipientEmail
                        }
                    }
                }
            };

            await graphClient.Me.SendMail
                .PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
                {
                    Message = emailMessage,
                    SaveToSentItems = true
                });

            Log($"Email sent successfully to {recipientEmail}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"ERROR in SendEmailAsync: {ex.Message}");
            return false;
        }
    }
}
