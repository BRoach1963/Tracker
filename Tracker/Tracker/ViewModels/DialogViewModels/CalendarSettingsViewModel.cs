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
using Tracker.Services.Microsoft365;
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
        private ICommand? _requestAdminConsentCommand;
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

        public ICommand RequestAdminConsentCommand =>
            _requestAdminConsentCommand ??= new TrackerCommand(RequestAdminConsentExecuted);

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
            
            // Try to restore auth sessions in the background
            _ = TryRestoreAuthSessionsAsync();
        }
        
        private async Task TryRestoreAuthSessionsAsync()
        {
            // Try to restore Microsoft 365 session
            if (_settings.OutlookCalendarEnabled && !MicrosoftGraphAuthService.Instance.IsAuthenticated)
            {
                var restored = await MicrosoftGraphAuthService.Instance.TrySignInSilentlyAsync();
                if (restored)
                {
                    UpdateOutlookStatus();
                    RaisePropertyChanged(nameof(OutlookCalendarEnabled));
                }
                else
                {
                    // Couldn't restore silently - mark as not connected
                    _settings.OutlookCalendarEnabled = false;
                    UserSettingsManager.Instance.SaveSettings();
                    UpdateOutlookStatus();
                }
            }
            
            // Try to restore Google session
            var googleSettings = UserSettingsManager.Instance.Settings.Google;
            if (googleSettings.IsConnected && !GoogleAuthService.Instance.IsAuthenticated)
            {
                var restored = await GoogleAuthService.Instance.TrySilentSignInAsync();
                if (restored)
                {
                    UpdateGoogleStatus();
                    RaisePropertyChanged(nameof(GoogleCalendarEnabled));
                }
                else
                {
                    // Couldn't restore - mark as not connected
                    googleSettings.IsConnected = false;
                    UserSettingsManager.Instance.SaveSettings();
                    UpdateGoogleStatus();
                }
            }
        }

        #endregion

        #region Private Methods

        private void UpdateGoogleStatus()
        {
            var googleSettings = UserSettingsManager.Instance.Settings.Google;
            if (googleSettings.IsConnected && GoogleAuthService.Instance.IsAuthenticated)
            {
                GoogleStatus = $"Connected ({GoogleAuthService.Instance.UserEmail})";
            }
            else if (googleSettings.IsConnected)
            {
                // Settings say connected but auth service isn't - try to restore
                GoogleStatus = "Reconnecting...";
            }
            else
            {
                GoogleStatus = "Not Connected";
            }
        }

        private void UpdateOutlookStatus()
        {
            if (_settings.OutlookCalendarEnabled && MicrosoftGraphAuthService.Instance.IsAuthenticated)
            {
                OutlookStatus = $"Connected ({MicrosoftGraphAuthService.Instance.UserEmail})";
            }
            else if (_settings.OutlookCalendarEnabled)
            {
                // Settings say connected but auth service isn't authenticated yet
                OutlookStatus = "Reconnecting...";
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

        private async void ConnectOutlookExecuted(object? parameter)
        {
            IsConnectingOutlook = true;
            try
            {
                var success = await MicrosoftGraphAuthService.Instance.SignInInteractiveAsync();

                if (success)
                {
                    OutlookCalendarEnabled = true;
                    
                    // Update settings
                    _settings.OutlookCalendarEnabled = true;
                    _settings.OutlookUserEmail = MicrosoftGraphAuthService.Instance.UserEmail;
                    UserSettingsManager.Instance.SaveSettings();
                    
                    UpdateOutlookStatus();
                    NotificationManager.Instance.ShowSuccess("Connected", 
                        $"Microsoft 365 connected as {MicrosoftGraphAuthService.Instance.UserEmail}");
                }
                else
                {
                    NotificationManager.Instance.ShowError("Error", "Failed to connect to Microsoft 365. Please try again.");
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to connect Microsoft 365: {ex.Message}");
            }
            finally
            {
                IsConnectingOutlook = false;
            }
        }

        private async void DisconnectOutlookExecuted(object? parameter)
        {
            var result = MessageBoxHelper.Show(
                "Are you sure you want to disconnect Microsoft 365? This will stop syncing meetings to your Outlook Calendar.",
                "Disconnect Microsoft 365",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await MicrosoftGraphAuthService.Instance.SignOutAsync();
                OutlookCalendarEnabled = false;
                
                // Update settings
                _settings.OutlookCalendarEnabled = false;
                _settings.OutlookAccessToken = null;
                _settings.OutlookRefreshToken = null;
                _settings.OutlookUserEmail = null;
                UserSettingsManager.Instance.SaveSettings();
                
                UpdateOutlookStatus();
                NotificationManager.Instance.ShowSuccess("Disconnected", "Microsoft 365 has been disconnected.");
            }
        }

        private void RequestAdminConsentExecuted(object? parameter)
        {
            try
            {
                // Generate and open the admin consent URL
                var adminConsentUrl = MicrosoftGraphConfig.GetAdminConsentUrl();
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adminConsentUrl,
                    UseShellExecute = true
                });

                NotificationManager.Instance.ShowInfo(
                    "Admin Consent",
                    "A browser window has opened. Please sign in with an administrator account to grant consent for your organization.");
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to open admin consent page: {ex.Message}");
            }
        }

        private void UpdateSlackStatus()
        {
            if (SlackAuthService.Instance.IsConnected)
            {
                SlackStatus = $"Connected ({SlackAuthService.Instance.TeamName})";
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
                // Use the OAuth flow to connect to the user's workspace
                var success = await SlackAuthService.Instance.ConnectWorkspaceAsync();
                
                if (success)
                {
                    UpdateSlackStatus();
                    RaisePropertyChanged(nameof(SlackConnected));
                    RaisePropertyChanged(nameof(SlackWorkspace));
                    
                    NotificationManager.Instance.ShowSuccess("Connected", 
                        $"Slack connected to workspace: {SlackAuthService.Instance.TeamName}");
                }
                else
                {
                    var errorDetail = SlackAuthService.Instance.LastError ?? "Connection failed";
                    NotificationManager.Instance.ShowError("Error", $"Failed to connect to Slack: {errorDetail}");
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
                "Are you sure you want to disconnect Slack? This will stop messaging and kudos delivery.",
                "Disconnect Slack",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SlackAuthService.Instance.Disconnect();
                
                UpdateSlackStatus();
                RaisePropertyChanged(nameof(SlackConnected));
                RaisePropertyChanged(nameof(SlackWorkspace));
                
                NotificationManager.Instance.ShowSuccess("Disconnected", "Slack has been disconnected.");
            }
        }

        #endregion
    }
}

