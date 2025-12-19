using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Classes;
using Tracker.Helpers;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Google;
using Tracker.Services.Slack;
using Tracker.Views.Dialogs;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for Calendar Settings dialog.
    /// </summary>
    public class CalendarSettingsViewModel : BaseDialogViewModel
    {
        #region Fields

        private CalendarSettings _settings;
        private bool _isConnectingGoogle;
        private bool _isConnectingOutlook;
        private bool _isConnectingSlack;
        private string _googleStatus = "Not Connected";
        private string _outlookStatus = "Not Connected";
        private string _slackStatus = "Not Connected";

        #endregion

        #region Properties

        public CalendarSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GoogleCalendarEnabled));
                RaisePropertyChanged(nameof(OutlookCalendarEnabled));
                RaisePropertyChanged(nameof(AutoSyncOnSave));
                RaisePropertyChanged(nameof(SyncMeetingInvitations));
                RaisePropertyChanged(nameof(SyncMeetingSummaries));
                RaisePropertyChanged(nameof(GoogleStatus));
                RaisePropertyChanged(nameof(OutlookStatus));
            }
        }

        public bool GoogleCalendarEnabled
        {
            get => _settings.GoogleCalendarEnabled;
            set
            {
                _settings.GoogleCalendarEnabled = value;
                RaisePropertyChanged();
                UpdateGoogleStatus();
                UserSettingsManager.Instance.SaveSettings();
            }
        }

        public bool OutlookCalendarEnabled
        {
            get => _settings.OutlookCalendarEnabled;
            set
            {
                _settings.OutlookCalendarEnabled = value;
                RaisePropertyChanged();
                UpdateOutlookStatus();
                UserSettingsManager.Instance.SaveSettings();
            }
        }

        public bool AutoSyncOnSave
        {
            get => _settings.AutoSyncOnSave;
            set
            {
                _settings.AutoSyncOnSave = value;
                RaisePropertyChanged();
                UserSettingsManager.Instance.SaveSettings();
            }
        }

        public bool SyncMeetingInvitations
        {
            get => _settings.SyncMeetingInvitations;
            set
            {
                _settings.SyncMeetingInvitations = value;
                RaisePropertyChanged();
                UserSettingsManager.Instance.SaveSettings();
            }
        }

        public bool SyncMeetingSummaries
        {
            get => _settings.SyncMeetingSummaries;
            set
            {
                _settings.SyncMeetingSummaries = value;
                RaisePropertyChanged();
                UserSettingsManager.Instance.SaveSettings();
            }
        }

        public string GoogleStatus
        {
            get => _googleStatus;
            set
            {
                _googleStatus = value;
                RaisePropertyChanged();
            }
        }

        public string OutlookStatus
        {
            get => _outlookStatus;
            set
            {
                _outlookStatus = value;
                RaisePropertyChanged();
            }
        }

        public bool IsConnectingGoogle
        {
            get => _isConnectingGoogle;
            set
            {
                _isConnectingGoogle = value;
                RaisePropertyChanged();
            }
        }

        public bool IsConnectingOutlook
        {
            get => _isConnectingOutlook;
            set
            {
                _isConnectingOutlook = value;
                RaisePropertyChanged();
            }
        }

        public string GoogleUserEmail => _settings.GoogleUserEmail ?? "Not signed in";

        public string OutlookUserEmail => _settings.OutlookUserEmail ?? "Not signed in";

        public string SlackStatus
        {
            get => _slackStatus;
            set
            {
                _slackStatus = value;
                RaisePropertyChanged();
            }
        }

        public bool IsConnectingSlack
        {
            get => _isConnectingSlack;
            set
            {
                _isConnectingSlack = value;
                RaisePropertyChanged();
            }
        }

        public bool SlackConnected => UserSettingsManager.Instance.Settings.Slack.IsConnected;

        public string SlackWorkspace => UserSettingsManager.Instance.Settings.Slack.WorkspaceName ?? "Not connected";

        #endregion

        #region Commands

        private ICommand? _connectGoogleCommand;
        private ICommand? _disconnectGoogleCommand;
        private ICommand? _connectOutlookCommand;
        private ICommand? _disconnectOutlookCommand;
        private ICommand? _connectSlackCommand;
        private ICommand? _disconnectSlackCommand;

        public ICommand ConnectGoogleCommand =>
            _connectGoogleCommand ??= new TrackerCommand(ConnectGoogleExecuted, _ => !IsConnectingGoogle);

        public ICommand DisconnectGoogleCommand =>
            _disconnectGoogleCommand ??= new TrackerCommand(DisconnectGoogleExecuted, _ => GoogleCalendarEnabled);

        public ICommand ConnectOutlookCommand =>
            _connectOutlookCommand ??= new TrackerCommand(ConnectOutlookExecuted, _ => !IsConnectingOutlook);

        public ICommand DisconnectOutlookCommand =>
            _disconnectOutlookCommand ??= new TrackerCommand(DisconnectOutlookExecuted, _ => OutlookCalendarEnabled);

        public ICommand ConnectSlackCommand =>
            _connectSlackCommand ??= new TrackerCommand(ConnectSlackExecuted, _ => !IsConnectingSlack);

        public ICommand DisconnectSlackCommand =>
            _disconnectSlackCommand ??= new TrackerCommand(DisconnectSlackExecuted, _ => SlackConnected);

        #endregion

        #region Constructor

        public CalendarSettingsViewModel(Action? callback) : base(callback)
        {
            _settings = UserSettingsManager.Instance.Settings.Calendar;
            UpdateGoogleStatus();
            UpdateOutlookStatus();
            UpdateSlackStatus();
        }

        #endregion

        #region Private Methods

        private void UpdateGoogleStatus()
        {
            if (_settings.GoogleCalendarEnabled && !string.IsNullOrEmpty(_settings.GoogleAccessToken))
            {
                GoogleStatus = $"Connected ({GoogleUserEmail})";
            }
            else
            {
                GoogleStatus = "Not Connected";
            }
        }

        private void UpdateOutlookStatus()
        {
            if (_settings.OutlookCalendarEnabled && !string.IsNullOrEmpty(_settings.OutlookAccessToken))
            {
                OutlookStatus = $"Connected ({OutlookUserEmail})";
            }
            else
            {
                OutlookStatus = "Not Connected";
            }
        }

        private async void ConnectGoogleExecuted(object? parameter)
        {
            IsConnectingGoogle = true;
            try
            {
                // Use Google's built-in web authorization flow
                var success = await GoogleAuthService.Instance.SignInAsync();

                if (success)
                {
                    GoogleCalendarEnabled = true;
                    
                    // Update settings
                    UserSettingsManager.Instance.Settings.Google.IsConnected = true;
                    UserSettingsManager.Instance.Settings.Google.CalendarSyncEnabled = true;
                    UserSettingsManager.Instance.SaveSettings();
                    
                    UpdateGoogleStatus();
                    NotificationManager.Instance.ShowSuccess("Connected", 
                        $"Google Calendar connected as {GoogleAuthService.Instance.UserEmail}");
                }
                else
                {
                    NotificationManager.Instance.ShowError("Error", "Failed to connect to Google. Please try again.");
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to connect Google Calendar: {ex.Message}");
            }
            finally
            {
                IsConnectingGoogle = false;
            }
        }

        private async void DisconnectGoogleExecuted(object? parameter)
        {
            var result = MessageBoxHelper.Show(
                "Are you sure you want to disconnect Google Calendar? This will stop syncing meetings to your Google Calendar.",
                "Disconnect Google Calendar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await GoogleAuthService.Instance.SignOutAsync();
                GoogleCalendarEnabled = false;
                
                // Update settings
                UserSettingsManager.Instance.Settings.Google.IsConnected = false;
                UserSettingsManager.Instance.Settings.Google.CalendarSyncEnabled = false;
                UserSettingsManager.Instance.SaveSettings();
                
                UpdateGoogleStatus();
                NotificationManager.Instance.ShowSuccess("Disconnected", "Google Calendar has been disconnected.");
            }
        }

        private void ConnectOutlookExecuted(object? parameter)
        {
            // TODO: Implement Outlook authentication
            NotificationManager.Instance.ShowInfo("Coming Soon", "Outlook Calendar integration will be available in Phase 2.");
        }

        private void DisconnectOutlookExecuted(object? parameter)
        {
            // TODO: Implement Outlook disconnect
            NotificationManager.Instance.ShowInfo("Coming Soon", "Outlook Calendar integration will be available in Phase 2.");
        }

        private void UpdateSlackStatus()
        {
            var slackSettings = UserSettingsManager.Instance.Settings.Slack;
            if (slackSettings.IsConnected && !string.IsNullOrEmpty(slackSettings.WorkspaceName))
            {
                SlackStatus = $"Connected ({slackSettings.WorkspaceName})";
            }
            else if (SlackAuthService.Instance.IsConnected)
            {
                // Bot token is valid even without user OAuth
                SlackStatus = "Connected (Bot Token)";
                slackSettings.IsConnected = true;
            }
            else
            {
                SlackStatus = "Not Connected";
            }
        }

        private async void ConnectSlackExecuted(object? parameter)
        {
            IsConnectingSlack = true;
            try
            {
                // First validate the bot token
                var botValid = await SlackAuthService.Instance.ValidateBotTokenAsync();
                
                if (botValid)
                {
                    // Bot token is always available, update settings
                    var slackSettings = UserSettingsManager.Instance.Settings.Slack;
                    slackSettings.IsConnected = true;
                    slackSettings.WorkspaceName = SlackAuthService.Instance.TeamName;
                    slackSettings.WorkspaceId = SlackAuthService.Instance.TeamId;
                    UserSettingsManager.Instance.SaveSettings();
                    
                    UpdateSlackStatus();
                    RaisePropertyChanged(nameof(SlackConnected));
                    RaisePropertyChanged(nameof(SlackWorkspace));
                    
                    NotificationManager.Instance.ShowSuccess("Connected", 
                        $"Slack connected to workspace: {SlackAuthService.Instance.TeamName}");
                }
                else
                {
                    NotificationManager.Instance.ShowError("Error", "Failed to connect to Slack. Please check the bot token configuration.");
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to connect Slack: {ex.Message}");
            }
            finally
            {
                IsConnectingSlack = false;
            }
        }

        private void DisconnectSlackExecuted(object? parameter)
        {
            var result = MessageBoxHelper.Show(
                "Are you sure you want to disconnect Slack? This will stop messaging and presence sync.",
                "Disconnect Slack",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SlackAuthService.Instance.Disconnect();
                
                // Update settings
                var slackSettings = UserSettingsManager.Instance.Settings.Slack;
                slackSettings.IsConnected = false;
                slackSettings.WorkspaceName = null;
                slackSettings.WorkspaceId = null;
                slackSettings.UserId = null;
                UserSettingsManager.Instance.SaveSettings();
                
                UpdateSlackStatus();
                RaisePropertyChanged(nameof(SlackConnected));
                RaisePropertyChanged(nameof(SlackWorkspace));
                
                NotificationManager.Instance.ShowSuccess("Disconnected", "Slack has been disconnected.");
            }
        }

        #endregion
    }
}

