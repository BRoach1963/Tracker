using System.IO;
using System.Net.Http;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MimeKit;
using Tracker.Logging;
using Tracker.Managers;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace Tracker.Services.Google
{
    /// <summary>
    /// Handles Gmail operations for sending emails.
    /// </summary>
    public class GoogleGmailService
    {
        #region Singleton

        private static GoogleGmailService? _instance;
        private static readonly object _lock = new();

        public static GoogleGmailService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GoogleGmailService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private GmailService? _service;

        #endregion

        #region Constructor

        private GoogleGmailService()
        {
            _logger = LoggingManager.GetComponentLogger("GoogleGmail");
        }

        #endregion

        #region Initialization

        private async Task<bool> EnsureServiceAsync()
        {
            if (_service != null) return true;

            if (!GoogleAuthService.Instance.IsAuthenticated)
            {
                var success = await GoogleAuthService.Instance.TrySilentSignInAsync();
                if (!success) return false;
            }

            _service = new GmailService(GoogleAuthService.Instance.GetServiceInitializer());
            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="bodyHtml">Email body (HTML)</param>
        /// <param name="bodyPlain">Email body (plain text fallback)</param>
        /// <returns>True if sent successfully</returns>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string bodyHtml, string? bodyPlain = null)
        {
            if (!await EnsureServiceAsync()) return false;

            try
            {
                var message = CreateMessage(toEmail, subject, bodyHtml, bodyPlain);
                var gmailMessage = new GmailMessage
                {
                    Raw = Base64UrlEncode(message)
                };

                await _service!.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
                
                _logger.Info($"Email sent to: {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, $"Failed to send email to: {toEmail}");
                return false;
            }
        }

        /// <summary>
        /// Sends a meeting summary email.
        /// </summary>
        public async Task<bool> SendMeetingSummaryAsync(DataModels.OneOnOne meeting)
        {
            if (meeting.TeamMember == null || string.IsNullOrEmpty(meeting.TeamMember.Email))
            {
                _logger.Warn("Cannot send meeting summary: no team member email");
                return false;
            }

            var subject = $"1:1 Meeting Summary - {meeting.Date:MMMM d, yyyy}";
            var bodyHtml = BuildMeetingSummaryHtml(meeting);
            var bodyPlain = BuildMeetingSummaryPlain(meeting);

            return await SendEmailAsync(meeting.TeamMember.Email, subject, bodyHtml, bodyPlain);
        }

        /// <summary>
        /// Sends a meeting reminder email.
        /// </summary>
        public async Task<bool> SendMeetingReminderAsync(DataModels.OneOnOne meeting)
        {
            if (meeting.TeamMember == null || string.IsNullOrEmpty(meeting.TeamMember.Email))
            {
                _logger.Warn("Cannot send meeting reminder: no team member email");
                return false;
            }

            var subject = $"Reminder: 1:1 Meeting - {meeting.Date:MMMM d, yyyy} at {meeting.StartTime:hh\\:mm tt}";
            var bodyHtml = BuildMeetingReminderHtml(meeting);
            var bodyPlain = BuildMeetingReminderPlain(meeting);

            return await SendEmailAsync(meeting.TeamMember.Email, subject, bodyHtml, bodyPlain);
        }

        #endregion

        #region Private Methods

        private string CreateMessage(string toEmail, string subject, string bodyHtml, string? bodyPlain)
        {
            var message = new MimeMessage();
            
            message.From.Add(new MailboxAddress(
                GoogleAuthService.Instance.UserDisplayName ?? "Tracker",
                GoogleAuthService.Instance.UserEmail ?? "noreply@tracker.app"
            ));
            
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = bodyHtml;
            
            if (!string.IsNullOrEmpty(bodyPlain))
            {
                builder.TextBody = bodyPlain;
            }
            
            message.Body = builder.ToMessageBody();

            using var stream = new MemoryStream();
            message.WriteTo(stream);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string Base64UrlEncode(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        private string BuildMeetingSummaryHtml(DataModels.OneOnOne meeting)
        {
            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><style>");
            html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }");
            html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            html.AppendLine("h1 { color: #D4AF37; border-bottom: 2px solid #D4AF37; padding-bottom: 10px; }");
            html.AppendLine("h2 { color: #444; margin-top: 20px; }");
            html.AppendLine(".section { background: #f9f9f9; padding: 15px; border-radius: 8px; margin: 15px 0; }");
            html.AppendLine(".agenda-item { padding: 8px 0; border-bottom: 1px solid #eee; }");
            html.AppendLine(".task-item { padding: 8px 0; }");
            html.AppendLine(".completed { color: #28a745; }");
            html.AppendLine(".pending { color: #ffc107; }");
            html.AppendLine(".footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }");
            html.AppendLine("</style></head><body>");
            html.AppendLine("<div class='container'>");
            
            html.AppendLine($"<h1>1:1 Meeting Summary</h1>");
            html.AppendLine($"<p><strong>Date:</strong> {meeting.Date:dddd, MMMM d, yyyy}</p>");
            html.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm tt} - {meeting.EndTime:hh\\:mm tt}</p>");

            // Agenda Items
            if (meeting.AgendaItems?.Any(a => !a.IsDeleted) == true)
            {
                html.AppendLine("<h2>Agenda</h2>");
                html.AppendLine("<div class='section'>");
                foreach (var item in meeting.AgendaItems.Where(a => !a.IsDeleted))
                {
                    var status = item.IsCompleted ? "✅" : "⬜";
                    html.AppendLine($"<div class='agenda-item'>{status} {System.Web.HttpUtility.HtmlEncode(item.Description)}</div>");
                }
                html.AppendLine("</div>");
            }

            // Notes
            if (!string.IsNullOrEmpty(meeting.Notes))
            {
                html.AppendLine("<h2>Notes</h2>");
                html.AppendLine("<div class='section'>");
                html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(meeting.Notes).Replace("\n", "<br/>")}</p>");
                html.AppendLine("</div>");
            }

            // Tasks
            if (meeting.Tasks?.Any(t => !t.IsDeleted) == true)
            {
                html.AppendLine("<h2>Action Items</h2>");
                html.AppendLine("<div class='section'>");
                foreach (var task in meeting.Tasks.Where(t => !t.IsDeleted))
                {
                    var status = task.IsCompleted ? "<span class='completed'>✅ Completed</span>" : "<span class='pending'>⏳ Pending</span>";
                    html.AppendLine($"<div class='task-item'><strong>{System.Web.HttpUtility.HtmlEncode(task.Description)}</strong> - {status}</div>");
                }
                html.AppendLine("</div>");
            }

            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>This summary was generated by Tracker.</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div></body></html>");

            return html.ToString();
        }

        private string BuildMeetingSummaryPlain(DataModels.OneOnOne meeting)
        {
            var text = new System.Text.StringBuilder();
            text.AppendLine("1:1 MEETING SUMMARY");
            text.AppendLine("===================");
            text.AppendLine();
            text.AppendLine($"Date: {meeting.Date:dddd, MMMM d, yyyy}");
            text.AppendLine($"Time: {meeting.StartTime:hh\\:mm tt} - {meeting.EndTime:hh\\:mm tt}");
            text.AppendLine();

            if (meeting.AgendaItems?.Any(a => !a.IsDeleted) == true)
            {
                text.AppendLine("AGENDA");
                text.AppendLine("------");
                foreach (var item in meeting.AgendaItems.Where(a => !a.IsDeleted))
                {
                    var status = item.IsCompleted ? "[X]" : "[ ]";
                    text.AppendLine($"{status} {item.Description}");
                }
                text.AppendLine();
            }

            if (!string.IsNullOrEmpty(meeting.Notes))
            {
                text.AppendLine("NOTES");
                text.AppendLine("-----");
                text.AppendLine(meeting.Notes);
                text.AppendLine();
            }

            if (meeting.Tasks?.Any(t => !t.IsDeleted) == true)
            {
                text.AppendLine("ACTION ITEMS");
                text.AppendLine("------------");
                foreach (var task in meeting.Tasks.Where(t => !t.IsDeleted))
                {
                    var status = task.IsCompleted ? "(Completed)" : "(Pending)";
                    text.AppendLine($"- {task.Description} {status}");
                }
                text.AppendLine();
            }

            text.AppendLine("---");
            text.AppendLine("Generated by Tracker");

            return text.ToString();
        }

        private string BuildMeetingReminderHtml(DataModels.OneOnOne meeting)
        {
            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><style>");
            html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }");
            html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            html.AppendLine("h1 { color: #D4AF37; }");
            html.AppendLine(".meeting-details { background: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; }");
            html.AppendLine(".agenda-item { padding: 5px 0; }");
            html.AppendLine(".footer { margin-top: 30px; font-size: 12px; color: #666; }");
            html.AppendLine("</style></head><body>");
            html.AppendLine("<div class='container'>");
            
            html.AppendLine($"<h1>📅 Upcoming 1:1 Reminder</h1>");
            html.AppendLine("<div class='meeting-details'>");
            html.AppendLine($"<p><strong>Date:</strong> {meeting.Date:dddd, MMMM d, yyyy}</p>");
            html.AppendLine($"<p><strong>Time:</strong> {meeting.StartTime:hh\\:mm tt} - {meeting.EndTime:hh\\:mm tt}</p>");

            if (!string.IsNullOrEmpty(meeting.GoogleMeetUrl))
            {
                html.AppendLine($"<p><strong>Join Meeting:</strong> <a href='{meeting.GoogleMeetUrl}'>Google Meet Link</a></p>");
            }

            html.AppendLine("</div>");

            if (meeting.AgendaItems?.Any(a => !a.IsDeleted) == true)
            {
                html.AppendLine("<h2>Agenda for Discussion</h2>");
                foreach (var item in meeting.AgendaItems.Where(a => !a.IsDeleted).Take(5))
                {
                    html.AppendLine($"<div class='agenda-item'>• {System.Web.HttpUtility.HtmlEncode(item.Description)}</div>");
                }
            }

            html.AppendLine("<div class='footer'>");
            html.AppendLine("<p>This reminder was sent by Tracker.</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div></body></html>");

            return html.ToString();
        }

        private string BuildMeetingReminderPlain(DataModels.OneOnOne meeting)
        {
            var text = new System.Text.StringBuilder();
            text.AppendLine("UPCOMING 1:1 REMINDER");
            text.AppendLine("=====================");
            text.AppendLine();
            text.AppendLine($"Date: {meeting.Date:dddd, MMMM d, yyyy}");
            text.AppendLine($"Time: {meeting.StartTime:hh\\:mm tt} - {meeting.EndTime:hh\\:mm tt}");

            if (!string.IsNullOrEmpty(meeting.GoogleMeetUrl))
            {
                text.AppendLine($"Join Meeting: {meeting.GoogleMeetUrl}");
            }

            text.AppendLine();

            if (meeting.AgendaItems?.Any(a => !a.IsDeleted) == true)
            {
                text.AppendLine("AGENDA FOR DISCUSSION");
                text.AppendLine("---------------------");
                foreach (var item in meeting.AgendaItems.Where(a => !a.IsDeleted).Take(5))
                {
                    text.AppendLine($"• {item.Description}");
                }
            }

            text.AppendLine();
            text.AppendLine("---");
            text.AppendLine("This reminder was sent by Tracker.");

            return text.ToString();
        }

        #endregion
    }
}

