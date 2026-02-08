using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MimeKit;
using ProCohere.Avalonia.Models;
using GoogleGmail = Google.Apis.Gmail.v1.GmailService;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for Gmail operations including sending emails and reading messages.
/// </summary>
public class GmailEmailService
{
    #region Singleton

    private static readonly Lazy<GmailEmailService> _instance =
        new(() => new GmailEmailService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static GmailEmailService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "gmail.log");

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
        catch { /* Logging should never throw */ }
    }

    #endregion

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private GmailEmailService() { }

    #region Email Sending

    /// <summary>
    /// Send an email message.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="bodyHtml">Email body in HTML</param>
    /// <param name="bodyPlain">Optional plain text fallback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if sent successfully</returns>
    public async Task<bool> SendEmailAsync(
        string toEmail, 
        string subject, 
        string bodyHtml, 
        string? bodyPlain = null,
        CancellationToken cancellationToken = default)
    {
        LastError = null;

        var service = GoogleAuthService.Instance.GetGmailService();
        if (service == null)
        {
            LastError = "Not authenticated with Google";
            return false;
        }

        try
        {
            var rawMessage = CreateMimeMessage(toEmail, subject, bodyHtml, bodyPlain);
            var gmailMessage = new Message
            {
                Raw = Base64UrlEncode(rawMessage)
            };

            await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync(cancellationToken);

            Log($"Email sent to: {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Failed to send email to {toEmail}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a meeting summary email to the attendees.
    /// </summary>
    public async Task<bool> SendMeetingSummaryAsync(
        MeetingDetail meeting,
        CancellationToken cancellationToken = default)
    {
        if (meeting.Attendees == null || !meeting.Attendees.Any())
        {
            LastError = "No attendees with email addresses";
            return false;
        }

        // Get attendee emails
        var recipientEmails = meeting.Attendees
            .Where(a => !string.IsNullOrEmpty(a.Email))
            .Select(a => a.Email!)
            .ToList();

        if (!recipientEmails.Any())
        {
            LastError = "No attendees with email addresses";
            return false;
        }

        var scheduledDate = meeting.ScheduledAt ?? DateTime.Now;
        var subject = $"Meeting Summary - {meeting.Title} - {scheduledDate:MMMM d, yyyy}";
        var bodyHtml = BuildMeetingSummaryHtml(meeting);
        var bodyPlain = BuildMeetingSummaryPlain(meeting);

        // Send to each participant
        var allSuccess = true;
        foreach (var email in recipientEmails)
        {
            var success = await SendEmailAsync(email, subject, bodyHtml, bodyPlain, cancellationToken);
            if (!success) allSuccess = false;
        }

        return allSuccess;
    }

    /// <summary>
    /// Send a meeting reminder email.
    /// </summary>
    public async Task<bool> SendMeetingReminderAsync(
        MeetingDetail meeting,
        CancellationToken cancellationToken = default)
    {
        if (meeting.Attendees == null || !meeting.Attendees.Any())
        {
            LastError = "No attendees with email addresses";
            return false;
        }

        var recipientEmails = meeting.Attendees
            .Where(a => !string.IsNullOrEmpty(a.Email))
            .Select(a => a.Email!)
            .ToList();

        if (!recipientEmails.Any())
        {
            LastError = "No attendees with email addresses";
            return false;
        }

        var scheduledAt = meeting.ScheduledAt ?? DateTime.Now;
        var subject = $"Reminder: {meeting.Title} - {scheduledAt:MMMM d} at {scheduledAt:h:mm tt}";
        var bodyHtml = BuildMeetingReminderHtml(meeting);
        var bodyPlain = BuildMeetingReminderPlain(meeting);

        var allSuccess = true;
        foreach (var email in recipientEmails)
        {
            var success = await SendEmailAsync(email, subject, bodyHtml, bodyPlain, cancellationToken);
            if (!success) allSuccess = false;
        }

        return allSuccess;
    }

    /// <summary>
    /// Send feedback notification email.
    /// </summary>
    public async Task<bool> SendFeedbackNotificationAsync(
        FeedbackDetail feedback,
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(recipientEmail))
        {
            LastError = "No recipient email";
            return false;
        }

        var subject = $"New Feedback Received - {feedback.FeedbackType}";
        var bodyHtml = BuildFeedbackNotificationHtml(feedback, recipientName);
        var bodyPlain = BuildFeedbackNotificationPlain(feedback, recipientName);

        return await SendEmailAsync(recipientEmail, subject, bodyHtml, bodyPlain, cancellationToken);
    }

    #endregion

    #region Email Building

    private string CreateMimeMessage(string toEmail, string subject, string bodyHtml, string? bodyPlain)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            GoogleAuthService.Instance.UserDisplayName ?? "ProCohere",
            GoogleAuthService.Instance.UserEmail ?? "noreply@procohere.app"
        ));

        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = bodyHtml
        };

        if (!string.IsNullOrEmpty(bodyPlain))
        {
            builder.TextBody = bodyPlain;
        }

        message.Body = builder.ToMessageBody();

        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    private static string BuildMeetingSummaryHtml(MeetingDetail meeting)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine("h1 { color: #22C55E; border-bottom: 2px solid #22C55E; padding-bottom: 10px; }");
        html.AppendLine("h2 { color: #374151; margin-top: 24px; font-size: 18px; }");
        html.AppendLine(".section { background: #F9FAFB; padding: 16px; border-radius: 8px; margin: 12px 0; }");
        html.AppendLine(".item { padding: 8px 0; border-bottom: 1px solid #E5E7EB; }");
        html.AppendLine(".item:last-child { border-bottom: none; }");
        html.AppendLine(".completed { color: #22C55E; }");
        html.AppendLine(".pending { color: #F59E0B; }");
        html.AppendLine(".meta { color: #6B7280; font-size: 14px; }");
        html.AppendLine(".footer { margin-top: 32px; padding-top: 16px; border-top: 1px solid #E5E7EB; font-size: 12px; color: #9CA3AF; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine($"<h1>Meeting Summary</h1>");
        html.AppendLine($"<p class='meta'><strong>{HtmlEncode(meeting.Title)}</strong></p>");
        
        var scheduledAt = meeting.ScheduledAt ?? DateTime.Now;
        var duration = meeting.DurationMinutes ?? 30;
        var endTime = scheduledAt.AddMinutes(duration);
        html.AppendLine($"<p class='meta'>📅 {scheduledAt:dddd, MMMM d, yyyy} • {scheduledAt:h:mm tt} - {endTime:h:mm tt}</p>");

        // Agenda Items
        if (meeting.AgendaItems?.Any() == true)
        {
            html.AppendLine("<h2>📋 Agenda</h2>");
            html.AppendLine("<div class='section'>");
            foreach (var item in meeting.AgendaItems)
            {
                var status = item.IsCompleted ? "✅" : "⬜";
                html.AppendLine($"<div class='item'>{status} {HtmlEncode(item.Title)}</div>");
            }
            html.AppendLine("</div>");
        }

        // Notes
        if (!string.IsNullOrEmpty(meeting.Notes))
        {
            html.AppendLine("<h2>📝 Notes</h2>");
            html.AppendLine("<div class='section'>");
            html.AppendLine($"<p>{HtmlEncode(meeting.Notes).Replace("\n", "<br/>")}</p>");
            html.AppendLine("</div>");
        }

        // Attendees
        if (meeting.Attendees?.Any() == true)
        {
            html.AppendLine("<h2>👥 Attendees</h2>");
            html.AppendLine("<div class='section'>");
            foreach (var a in meeting.Attendees)
            {
                html.AppendLine($"<div class='item'>{HtmlEncode(a.Name ?? a.Email ?? "Unknown")}</div>");
            }
            html.AppendLine("</div>");
        }

        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>This summary was generated by ProCohere.</p>");
        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private static string BuildMeetingSummaryPlain(MeetingDetail meeting)
    {
        var text = new StringBuilder();
        text.AppendLine("MEETING SUMMARY");
        text.AppendLine("===============");
        text.AppendLine();
        text.AppendLine(meeting.Title);
        var scheduledAt = meeting.ScheduledAt ?? DateTime.Now;
        var duration = meeting.DurationMinutes ?? 30;
        var endTime = scheduledAt.AddMinutes(duration);
        text.AppendLine($"Date: {scheduledAt:dddd, MMMM d, yyyy}");
        text.AppendLine($"Time: {scheduledAt:h:mm tt} - {endTime:h:mm tt}");
        text.AppendLine();

        if (meeting.AgendaItems?.Any() == true)
        {
            text.AppendLine("AGENDA");
            text.AppendLine("------");
            foreach (var item in meeting.AgendaItems)
            {
                var status = item.IsCompleted ? "[X]" : "[ ]";
                text.AppendLine($"{status} {item.Title}");
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

        text.AppendLine("---");
        text.AppendLine("Generated by ProCohere");

        return text.ToString();
    }

    private static string BuildMeetingReminderHtml(MeetingDetail meeting)
    {
        var scheduledAt = meeting.ScheduledAt ?? DateTime.Now;
        
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: #22C55E; color: white; padding: 24px; border-radius: 8px; text-align: center; }");
        html.AppendLine(".header h1 { margin: 0; font-size: 24px; }");
        html.AppendLine(".content { padding: 24px 0; }");
        html.AppendLine(".time { font-size: 32px; font-weight: bold; color: #111827; }");
        html.AppendLine(".date { color: #6B7280; font-size: 16px; }");
        html.AppendLine(".section { background: #F9FAFB; padding: 16px; border-radius: 8px; margin: 12px 0; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>⏰ Meeting Reminder</h1>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='content'>");
        html.AppendLine($"<p class='time'>{scheduledAt:h:mm tt}</p>");
        html.AppendLine($"<p class='date'>{scheduledAt:dddd, MMMM d, yyyy}</p>");
        html.AppendLine($"<h2>{HtmlEncode(meeting.Title)}</h2>");

        if (!string.IsNullOrEmpty(meeting.Location))
        {
            html.AppendLine($"<p>📍 {HtmlEncode(meeting.Location)}</p>");
        }

        if (meeting.AgendaItems?.Any() == true)
        {
            html.AppendLine("<h3>Agenda Preview</h3>");
            html.AppendLine("<div class='section'>");
            foreach (var item in meeting.AgendaItems.Take(5))
            {
                html.AppendLine($"<div>• {HtmlEncode(item.Title)}</div>");
            }
            if (meeting.AgendaItems.Count > 5)
            {
                html.AppendLine($"<div>...and {meeting.AgendaItems.Count - 5} more items</div>");
            }
            html.AppendLine("</div>");
        }

        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private static string BuildMeetingReminderPlain(MeetingDetail meeting)
    {
        var scheduledAt = meeting.ScheduledAt ?? DateTime.Now;
        
        var text = new StringBuilder();
        text.AppendLine("MEETING REMINDER");
        text.AppendLine("================");
        text.AppendLine();
        text.AppendLine($"Time: {scheduledAt:h:mm tt}");
        text.AppendLine($"Date: {scheduledAt:dddd, MMMM d, yyyy}");
        text.AppendLine();
        text.AppendLine(meeting.Title);

        if (!string.IsNullOrEmpty(meeting.Location))
        {
            text.AppendLine($"Location: {meeting.Location}");
        }

        text.AppendLine();
        text.AppendLine("---");
        text.AppendLine("ProCohere");

        return text.ToString();
    }

    private static string BuildFeedbackNotificationHtml(FeedbackDetail feedback, string recipientName)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><style>");
        html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: #3B82F6; color: white; padding: 24px; border-radius: 8px; }");
        html.AppendLine(".content { padding: 24px 0; }");
        html.AppendLine(".feedback-box { background: #F9FAFB; padding: 20px; border-radius: 8px; border-left: 4px solid #3B82F6; }");
        html.AppendLine(".type-badge { display: inline-block; background: #EFF6FF; color: #3B82F6; padding: 4px 12px; border-radius: 16px; font-size: 12px; font-weight: 600; }");
        html.AppendLine("</style></head><body>");

        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>💬 New Feedback</h1>");
        html.AppendLine("</div>");

        html.AppendLine("<div class='content'>");
        html.AppendLine($"<p>Hi {HtmlEncode(recipientName)},</p>");
        html.AppendLine("<p>You've received new feedback!</p>");

        html.AppendLine($"<p><span class='type-badge'>{HtmlEncode(feedback.FeedbackType ?? "General")}</span></p>");

        html.AppendLine("<div class='feedback-box'>");
        html.AppendLine($"<p>{HtmlEncode(feedback.Content ?? "").Replace("\n", "<br/>")}</p>");
        html.AppendLine("</div>");

        html.AppendLine($"<p style='color: #6B7280; font-size: 14px;'>Received on {feedback.CreatedAt:MMMM d, yyyy}</p>");
        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private static string BuildFeedbackNotificationPlain(FeedbackDetail feedback, string recipientName)
    {
        var text = new StringBuilder();
        text.AppendLine("NEW FEEDBACK");
        text.AppendLine("============");
        text.AppendLine();
        text.AppendLine($"Hi {recipientName},");
        text.AppendLine();
        text.AppendLine("You've received new feedback!");
        text.AppendLine();
        text.AppendLine($"Type: {feedback.FeedbackType}");
        text.AppendLine();
        text.AppendLine(feedback.Content ?? "");
        text.AppendLine();
        text.AppendLine($"Received: {feedback.CreatedAt:MMMM d, yyyy}");
        text.AppendLine();
        text.AppendLine("---");
        text.AppendLine("ProCohere");

        return text.ToString();
    }

    private static string HtmlEncode(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }

    #endregion
}
