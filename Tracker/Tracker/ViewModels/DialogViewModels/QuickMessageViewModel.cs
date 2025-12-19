using System.Windows.Input;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Microsoft365;
using Tracker.Services.Slack;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the Quick Message dialog.
    /// Allows managers to send contextual Teams messages or emails.
    /// </summary>
    public class QuickMessageViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger;
        private TeamMember? _recipient;
        private OneOnOne? _relatedMeeting;
        private string _messageText = string.Empty;
        private string _emailSubject = string.Empty;
        private bool _isTeamsMessage = true;
        private bool _isSlackMessage;
        private bool _isEmail;
        private bool _isSending;
        private string _statusMessage = string.Empty;
        private bool _hasError;
        private MessageTemplate _selectedTemplate = MessageTemplate.Custom;

        private ICommand? _sendCommand;
        private ICommand? _cancelCommand;
        private ICommand? _applyTemplateCommand;

        #endregion

        #region Constructor

        public QuickMessageViewModel(Action? callback) : base(callback)
        {
            _logger = LoggingManager.GetComponentLogger("QuickMessage");
        }

        /// <summary>
        /// Initialize with a recipient and optional related meeting.
        /// </summary>
        public void Initialize(TeamMember recipient, OneOnOne? relatedMeeting = null)
        {
            _recipient = recipient;
            _relatedMeeting = relatedMeeting;
            
            RaisePropertyChanged(nameof(Recipient));
            RaisePropertyChanged(nameof(RecipientName));
            RaisePropertyChanged(nameof(RecipientEmail));
            RaisePropertyChanged(nameof(HasTeams));
            RaisePropertyChanged(nameof(HasSlack));
            RaisePropertyChanged(nameof(HasEmail));
            RaisePropertyChanged(nameof(CanSendTeams));
            RaisePropertyChanged(nameof(CanSendSlack));
            RaisePropertyChanged(nameof(CanSendEmail));

            // Default to Teams if available, then Slack, otherwise email
            if (HasTeams)
            {
                IsTeamsMessage = true;
            }
            else if (HasSlack)
            {
                IsSlackMessage = true;
            }
            else if (HasEmail)
            {
                IsEmail = true;
            }
        }

        #endregion

        #region Properties

        public TeamMember? Recipient => _recipient;
        public string RecipientName => _recipient != null 
            ? $"{_recipient.FirstName} {_recipient.LastName}".Trim() 
            : "Unknown";
        public string RecipientEmail => _recipient?.Email ?? string.Empty;

        public bool HasTeams => QuickMessageService.Instance.TeamsAvailable;
        public bool HasSlack => SlackService.Instance.IsAvailable && UserSettingsManager.Instance.Settings.Slack.IsConnected;
        public bool HasEmail => QuickMessageService.Instance.EmailAvailable;
        
        public bool CanSendTeams => HasTeams && !string.IsNullOrEmpty(RecipientEmail);
        public bool CanSendSlack => HasSlack && !string.IsNullOrEmpty(RecipientEmail);
        public bool CanSendEmail => HasEmail && !string.IsNullOrEmpty(RecipientEmail);

        public bool IsTeamsMessage
        {
            get => _isTeamsMessage;
            set
            {
                if (_isTeamsMessage != value)
                {
                    _isTeamsMessage = value;
                    if (value)
                    {
                        _isSlackMessage = false;
                        _isEmail = false;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsSlackMessage));
                    RaisePropertyChanged(nameof(IsEmail));
                    RaisePropertyChanged(nameof(ShowEmailSubject));
                }
            }
        }

        public bool IsSlackMessage
        {
            get => _isSlackMessage;
            set
            {
                if (_isSlackMessage != value)
                {
                    _isSlackMessage = value;
                    if (value)
                    {
                        _isTeamsMessage = false;
                        _isEmail = false;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsTeamsMessage));
                    RaisePropertyChanged(nameof(IsEmail));
                    RaisePropertyChanged(nameof(ShowEmailSubject));
                }
            }
        }

        public bool IsEmail
        {
            get => _isEmail;
            set
            {
                if (_isEmail != value)
                {
                    _isEmail = value;
                    if (value)
                    {
                        _isTeamsMessage = false;
                        _isSlackMessage = false;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsTeamsMessage));
                    RaisePropertyChanged(nameof(IsSlackMessage));
                    RaisePropertyChanged(nameof(ShowEmailSubject));
                }
            }
        }

        public bool ShowEmailSubject => _isEmail;

        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                RaisePropertyChanged();
                ((TrackerCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }

        public string EmailSubject
        {
            get => _emailSubject;
            set
            {
                _emailSubject = value;
                RaisePropertyChanged();
                ((TrackerCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }

        public MessageTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                RaisePropertyChanged();
                ApplyTemplate(value);
            }
        }

        public bool IsSending
        {
            get => _isSending;
            set
            {
                _isSending = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNotSending));
                ((TrackerCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsNotSending => !_isSending;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                RaisePropertyChanged();
            }
        }

        public IEnumerable<MessageTemplate> Templates => Enum.GetValues<MessageTemplate>();

        #endregion

        #region Commands

        public ICommand SendCommand => _sendCommand ??= new TrackerCommand(ExecuteSend, CanExecuteSend);
        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(ExecuteCancel);
        public ICommand ApplyTemplateCommand => _applyTemplateCommand ??= new TrackerCommand(
            p => ApplyTemplate((MessageTemplate)p!));

        private bool CanExecuteSend(object? parameter)
        {
            if (_isSending || _recipient == null || string.IsNullOrEmpty(_recipient.Email))
                return false;

            if (_isEmail)
                return !string.IsNullOrWhiteSpace(_emailSubject) && !string.IsNullOrWhiteSpace(_messageText);
            
            return !string.IsNullOrWhiteSpace(_messageText);
        }

        private async void ExecuteSend(object? parameter)
        {
            if (_recipient == null) return;

            IsSending = true;
            StatusMessage = "Sending...";
            HasError = false;

            try
            {
                bool success;
                string? error = null;

                if (_isTeamsMessage)
                {
                    (success, error) = await QuickMessageService.Instance.SendTeamsMessageAsync(
                        _recipient.Email, _messageText);
                }
                else if (_isSlackMessage)
                {
                    success = await SlackService.Instance.SendDirectMessageByEmailAsync(
                        _recipient.Email, _messageText);
                    if (!success)
                    {
                        error = "Failed to send Slack message. Make sure the recipient has a Slack account with the same email.";
                    }
                }
                else
                {
                    var htmlBody = ConvertToHtml(_messageText);
                    (success, error) = await QuickMessageService.Instance.SendEmailAsync(
                        _recipient.Email, RecipientName, _emailSubject, htmlBody);
                }

                var platform = _isTeamsMessage ? "Teams" : _isSlackMessage ? "Slack" : "Email";
                
                if (success)
                {
                    _logger.Info($"Message sent to {RecipientEmail} via {platform}");
                    StatusMessage = "✓ Message sent!";
                    HasError = false;

                    // Close after brief delay
                    await Task.Delay(1500);
                    Callback?.Invoke();
                }
                else
                {
                    StatusMessage = error ?? "Failed to send message.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Send message failed");
                StatusMessage = ex.Message;
                HasError = true;
            }
            finally
            {
                IsSending = false;
            }
        }

        private void ExecuteCancel(object? parameter)
        {
            Callback?.Invoke();
        }

        #endregion

        #region Template Methods

        private void ApplyTemplate(MessageTemplate template)
        {
            if (_recipient == null) return;

            switch (template)
            {
                case MessageTemplate.PreMeetingReminder:
                    if (_relatedMeeting != null)
                    {
                        MessageText = QuickMessageService.GetPreMeetingTeamsMessage(_relatedMeeting, _recipient);
                        EmailSubject = $"Reminder: 1:1 on {_relatedMeeting.Date:MMM d}";
                    }
                    break;

                case MessageTemplate.ActionItemCheckIn:
                    MessageText = $"Hey {_recipient.FirstName}! 👋\n\n" +
                                 "Quick check-in - how's progress going on [task]?\n\n" +
                                 "Any blockers I can help with?";
                    EmailSubject = "Quick check-in";
                    break;

                case MessageTemplate.Kudos:
                    MessageText = $"Hey {_recipient.FirstName}! 🎉\n\n" +
                                 "[Great work on X!]\n\n" +
                                 "Really appreciate your effort - keep it up!";
                    EmailSubject = "Great work!";
                    break;

                case MessageTemplate.MeetingRescheduled:
                    if (_relatedMeeting != null)
                    {
                        MessageText = QuickMessageService.GetRescheduleTeamsMessage(_recipient, _relatedMeeting);
                        EmailSubject = "1:1 Rescheduled";
                    }
                    break;

                case MessageTemplate.OneOnOneSummary:
                    if (_relatedMeeting != null)
                    {
                        IsEmail = true;
                        var managerName = UserSettingsManager.Instance.Settings.CurrentUser;
                        var (subject, body) = QuickMessageService.GetSummaryEmail(
                            _relatedMeeting, _recipient, managerName);
                        EmailSubject = subject;
                        MessageText = StripHtml(body); // Show plain text in editor
                    }
                    break;

                case MessageTemplate.PrepRequest:
                    if (_relatedMeeting != null)
                    {
                        IsEmail = true;
                        var managerName = UserSettingsManager.Instance.Settings.CurrentUser;
                        var (subject, body) = QuickMessageService.GetPrepRequestEmail(
                            _relatedMeeting, _recipient, managerName);
                        EmailSubject = subject;
                        MessageText = StripHtml(body);
                    }
                    break;

                case MessageTemplate.Custom:
                default:
                    // Don't change anything
                    break;
            }
        }

        private string ConvertToHtml(string plainText)
        {
            // Basic conversion for email
            var html = plainText
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\n", "<br/>");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; padding: 20px; }}
    </style>
</head>
<body>
    <p>{html}</p>
    <br/>
    <p style='color: #888; font-size: 12px;'>Sent via Tracker</p>
</body>
</html>";
        }

        private string StripHtml(string html)
        {
            // Very basic HTML stripping for preview
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Trim();
        }

        #endregion
    }

    /// <summary>
    /// Pre-defined message templates.
    /// </summary>
    public enum MessageTemplate
    {
        Custom,
        PreMeetingReminder,
        ActionItemCheckIn,
        Kudos,
        MeetingRescheduled,
        OneOnOneSummary,
        PrepRequest
    }
}

