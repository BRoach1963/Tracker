using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Services.Subscription;

namespace Tracker.Services.Microsoft365
{
    /// <summary>
    /// Handles sending quick messages via Teams and Email from within Tracker.
    /// Designed for contextual, human-triggered messages only.
    /// </summary>
    public class QuickMessageService : IDisposable
    {
        #region Singleton

        private static QuickMessageService? _instance;
        private static readonly object _lock = new();

        public static QuickMessageService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new QuickMessageService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        #endregion

        #region Properties

        /// <summary>
        /// Whether Teams messaging is available.
        /// </summary>
        public bool TeamsAvailable => MicrosoftGraphAuthService.Instance.TeamsAvailable;

        /// <summary>
        /// Whether email sending is available (requires M365 connection).
        /// </summary>
        public bool EmailAvailable => MicrosoftGraphAuthService.Instance.IsAuthenticated;

        #endregion

        #region Constructor

        private QuickMessageService()
        {
            _logger = LoggingManager.GetComponentLogger("QuickMessage");
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(GraphBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #endregion

        #region Teams Messaging

        /// <summary>
        /// Sends a Teams message to a team member.
        /// Creates a 1:1 chat if one doesn't exist.
        /// </summary>
        /// <param name="recipientEmail">The recipient's email address.</param>
        /// <param name="message">The message content (plain text or HTML).</param>
        /// <returns>True if sent successfully.</returns>
        public async Task<(bool Success, string? Error)> SendTeamsMessageAsync(
            string recipientEmail, string message)
        {
            if (!TeamsAvailable)
                return (false, "Teams is not available. Please check your Microsoft 365 subscription.");

            if (!SubscriptionService.Instance.HasFeature("teams_integration"))
                return (false, "Teams integration requires a Pro subscription.");

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return (false, "Not authenticated with Microsoft 365.");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // First, create or get the 1:1 chat
                var chatId = await GetOrCreateChatAsync(recipientEmail);
                if (string.IsNullOrEmpty(chatId))
                    return (false, "Could not create chat with recipient.");

                // Send the message
                var messagePayload = new
                {
                    body = new
                    {
                        content = message,
                        contentType = "html"
                    }
                };

                var json = JsonSerializer.Serialize(messagePayload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/chats/{chatId}/messages", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.Info($"Teams message sent to {recipientEmail}");
                    return (true, null);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.Error($"Teams message failed: {response.StatusCode} - {error}");
                return (false, $"Failed to send message: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Teams message send failed");
                return (false, ex.Message);
            }
        }

        private async Task<string?> GetOrCreateChatAsync(string recipientEmail)
        {
            try
            {
                // Create a 1:1 chat (or get existing one)
                // Use explicit object type to handle different member properties
                var members = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["@odata.type"] = "#microsoft.graph.aadUserConversationMember",
                        ["roles"] = new[] { "owner" },
                        ["user@odata.bind"] = "https://graph.microsoft.com/v1.0/me"
                    },
                    new Dictionary<string, object>
                    {
                        ["@odata.type"] = "#microsoft.graph.aadUserConversationMember",
                        ["roles"] = new[] { "owner" },
                        ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users/{recipientEmail}"
                    }
                };

                var chatPayload = new
                {
                    chatType = "oneOnOne",
                    members = members
                };

                // Use the special endpoint that creates or returns existing chat
                var json = JsonSerializer.Serialize(chatPayload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/chats", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    return doc.RootElement.GetProperty("id").GetString();
                }

                // If chat already exists, try to find it
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return await FindExistingChatAsync(recipientEmail);
                }

                _logger.Error($"Create chat failed: {response.StatusCode} - {responseBody}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "GetOrCreateChat failed");
                return null;
            }
        }

        private async Task<string?> FindExistingChatAsync(string recipientEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/me/chats?$filter=chatType eq 'oneOnOne'&$expand=members");

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);

                if (doc.RootElement.TryGetProperty("value", out var chats))
                {
                    foreach (var chat in chats.EnumerateArray())
                    {
                        if (chat.TryGetProperty("members", out var members))
                        {
                            foreach (var member in members.EnumerateArray())
                            {
                                if (member.TryGetProperty("email", out var email) &&
                                    email.GetString()?.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    return chat.GetProperty("id").GetString();
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "FindExistingChat failed");
                return null;
            }
        }

        #endregion

        #region Email

        /// <summary>
        /// Sends an email via Microsoft Graph.
        /// </summary>
        /// <param name="recipientEmail">The recipient's email address.</param>
        /// <param name="recipientName">The recipient's display name.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="bodyHtml">Email body in HTML format.</param>
        /// <returns>True if sent successfully.</returns>
        public async Task<(bool Success, string? Error)> SendEmailAsync(
            string recipientEmail, string recipientName, string subject, string bodyHtml)
        {
            if (!EmailAvailable)
                return (false, "Email requires Microsoft 365 connection.");

            if (!SubscriptionService.Instance.HasFeature("calendar_sync")) // Email is Standard+ feature
                return (false, "Email features require a Standard or Pro subscription.");

            try
            {
                var token = await MicrosoftGraphAuthService.Instance.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return (false, "Not authenticated with Microsoft 365.");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var emailPayload = new
                {
                    message = new
                    {
                        subject = subject,
                        body = new
                        {
                            contentType = "HTML",
                            content = bodyHtml
                        },
                        toRecipients = new[]
                        {
                            new
                            {
                                emailAddress = new
                                {
                                    address = recipientEmail,
                                    name = recipientName
                                }
                            }
                        }
                    },
                    saveToSentItems = true
                };

                var json = JsonSerializer.Serialize(emailPayload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/me/sendMail", content);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.Info($"Email sent to {recipientEmail}: {subject}");
                    return (true, null);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.Error($"Email send failed: {response.StatusCode} - {error}");
                return (false, $"Failed to send email: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Email send failed");
                return (false, ex.Message);
            }
        }

        #endregion

        #region Message Templates

        /// <summary>
        /// Gets a pre-meeting reminder message for Teams.
        /// </summary>
        public static string GetPreMeetingTeamsMessage(Meeting meeting, TeamMember member)
        {
            var time = meeting.ScheduledAt.ToString("dddd") + " at " + meeting.ScheduledAt.ToString(@"h\:mm tt");
            return $"Hey {member.FirstName}! 👋\n\n" +
                   $"Quick reminder - we have our 1:1 coming up on {time}.\n\n" +
                   $"Anything you'd like to add to the agenda? Let me know!";
        }

        /// <summary>
        /// Gets an action item reminder message for Teams.
        /// </summary>
        public static string GetActionItemTeamsMessage(TeamMember member, string taskDescription)
        {
            return $"Hey {member.FirstName}! 👋\n\n" +
                   $"Quick check-in on: **{taskDescription}**\n\n" +
                   $"How's it going? Any blockers I can help with?";
        }

        /// <summary>
        /// Gets a kudos/recognition message for Teams.
        /// </summary>
        public static string GetKudosTeamsMessage(TeamMember member, string feedbackText)
        {
            return $"Hey {member.FirstName}! 🎉\n\n" +
                   $"{feedbackText}\n\n" +
                   $"Great work - keep it up!";
        }

        /// <summary>
        /// Gets a meeting rescheduled message for Teams.
        /// </summary>
        public static string GetRescheduleTeamsMessage(TeamMember member, Meeting meeting)
        {
            var newTime = meeting.ScheduledAt.ToString("dddd, MMM d") + " at " + meeting.ScheduledAt.ToString(@"h\:mm tt");
            return $"Hey {member.FirstName}! 📅\n\n" +
                   $"Heads up - I've moved our 1:1 to **{newTime}**.\n\n" +
                   $"Let me know if that doesn't work for you!";
        }

        /// <summary>
        /// Gets a 1:1 summary email.
        /// </summary>
        public static (string Subject, string Body) GetSummaryEmail(
            Meeting meeting, TeamMember member, string managerName)
        {
            var subject = $"1:1 Summary - {meeting.ScheduledAt:MMMM d, yyyy}";

            var agendaHtml = "";
            if (meeting.AgendaItems?.Any() == true)
            {
                agendaHtml = "<ul>" + string.Join("", meeting.AgendaItems.Select(a => $"<li>{a.Title}</li>")) + "</ul>";
            }
            else
            {
                agendaHtml = "<p><em>No specific agenda items recorded.</em></p>";
            }

            var tasksHtml = "";
            if (meeting.Tasks?.Any() == true)
            {
                tasksHtml = "<ul>" + string.Join("", meeting.Tasks.Select(t => 
                    $"<li><strong>[{(t.IsCompleted ? "✓" : "○")}]</strong> {t.Description}</li>")) + "</ul>";
            }
            else
            {
                tasksHtml = "<p><em>No action items from this meeting.</em></p>";
            }

            var notesHtml = !string.IsNullOrWhiteSpace(meeting.Notes)
                ? $"<p>{meeting.Notes.Replace("\n", "<br/>")}</p>"
                : "<p><em>No additional notes.</em></p>";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background: #0078d4; color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .section {{ background: white; padding: 15px; margin: 10px 0; border-radius: 8px; border-left: 4px solid #0078d4; }}
        .section h3 {{ margin: 0 0 10px 0; color: #0078d4; }}
        .footer {{ padding: 15px; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2 style='margin:0;'>1:1 Summary</h2>
        <p style='margin:5px 0 0 0;'>{meeting.ScheduledAt:dddd, MMMM d, yyyy}</p>
    </div>
    <div class='content'>
        <p>Hi {member.FirstName},</p>
        <p>Thanks for our chat today! Here's a quick summary:</p>
        
        <div class='section'>
            <h3>📋 What We Discussed</h3>
            {agendaHtml}
        </div>
        
        <div class='section'>
            <h3>✅ Action Items</h3>
            {tasksHtml}
        </div>
        
        <div class='section'>
            <h3>📝 Notes</h3>
            {notesHtml}
        </div>
        
        <p>Let me know if I missed anything!</p>
        <p>Best,<br/>{managerName}</p>
    </div>
    <div class='footer'>
        <p>Sent via Tracker</p>
    </div>
</body>
</html>";

            return (subject, body);
        }

        /// <summary>
        /// Gets a pre-meeting prep request email.
        /// </summary>
        public static (string Subject, string Body) GetPrepRequestEmail(
            Meeting meeting, TeamMember member, string managerName)
        {
            var meetingTime = meeting.ScheduledAt.ToString("dddd, MMMM d") + " at " + meeting.ScheduledAt.ToString(@"h\:mm tt");
            var subject = $"Prep for our 1:1 on {meeting.ScheduledAt:MMM d}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .card {{ background: #f5f5f5; padding: 20px; border-radius: 8px; max-width: 500px; }}
        .highlight {{ background: #e3f2fd; padding: 15px; border-radius: 8px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='card'>
        <h2 style='color: #0078d4;'>📅 Upcoming 1:1</h2>
        <p>Hi {member.FirstName},</p>
        
        <div class='highlight'>
            <strong>{meetingTime}</strong>
        </div>
        
        <p>Looking forward to our chat! A few things to think about:</p>
        <ul>
            <li>What's going well?</li>
            <li>Any blockers or challenges?</li>
            <li>Anything you'd like to discuss?</li>
        </ul>
        
        <p>Feel free to reply with any topics you want to add to the agenda.</p>
        
        <p>See you soon!<br/>{managerName}</p>
    </div>
</body>
</html>";

            return (subject, body);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        #endregion
    }
}

