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
        private string _googleStatus = "Not Connected";
        private string _outlookStatus = "Not Connected";

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

        #endregion

        #region Commands

        private ICommand? _connectGoogleCommand;
        private ICommand? _disconnectGoogleCommand;
        private ICommand? _connectOutlookCommand;
        private ICommand? _disconnectOutlookCommand;

        public ICommand ConnectGoogleCommand =>
            _connectGoogleCommand ??= new TrackerCommand(ConnectGoogleExecuted, _ => !IsConnectingGoogle);

        public ICommand DisconnectGoogleCommand =>
            _disconnectGoogleCommand ??= new TrackerCommand(DisconnectGoogleExecuted, _ => GoogleCalendarEnabled);

        public ICommand ConnectOutlookCommand =>
            _connectOutlookCommand ??= new TrackerCommand(ConnectOutlookExecuted, _ => !IsConnectingOutlook);

        public ICommand DisconnectOutlookCommand =>
            _disconnectOutlookCommand ??= new TrackerCommand(DisconnectOutlookExecuted, _ => OutlookCalendarEnabled);

        #endregion

        #region Constructor

        public CalendarSettingsViewModel(Action? callback) : base(callback)
        {
            _settings = UserSettingsManager.Instance.Settings.Calendar;
            UpdateGoogleStatus();
            UpdateOutlookStatus();
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
                const int callbackPort = 8080;
                var redirectUri = $"http://localhost:{callbackPort}/";

                // Get authorization URL
                var authUrl = GoogleAuthService.Instance.GetAuthorizationUrl(redirectUri);
                
                // Start callback listener
                var callbackHandler = OAuthCallbackHandler.Instance;
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minute timeout
                
                var listenTask = callbackHandler.ListenForCallbackAsync(callbackPort, cancellationTokenSource.Token);
                
                // Open browser for user to authenticate
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                NotificationManager.Instance.ShowInfo("Google Calendar", 
                    "Please complete authentication in your browser. Waiting for callback...");

                // Wait for callback (with timeout)
                var authorizationCode = await listenTask;

                // Stop listener
                callbackHandler.Stop();

                if (string.IsNullOrEmpty(authorizationCode))
                {
                    // Fallback to manual entry
                    var manualDialog = new Views.Dialogs.ManualAuthCodeDialog
                    {
                        Owner = Application.Current.MainWindow
                    };
                    manualDialog.ShowDialog();

                    if (manualDialog.DialogResult && !string.IsNullOrEmpty(manualDialog.AuthorizationCode))
                    {
                        authorizationCode = manualDialog.AuthorizationCode;
                    }
                    else
                    {
                        NotificationManager.Instance.ShowWarning("Authentication Cancelled", 
                            "Authentication was cancelled. Please try again.");
                        return;
                    }
                }

                // Exchange code for tokens
                var success = await GoogleAuthService.Instance.CompleteAuthenticationAsync(authorizationCode, redirectUri);

                if (success)
                {
                    GoogleCalendarEnabled = true;
                    UpdateGoogleStatus();
                    NotificationManager.Instance.ShowSuccess("Connected", "Google Calendar has been connected successfully!");
                }
                else
                {
                    NotificationManager.Instance.ShowError("Error", "Failed to complete authentication. Please try again.");
                }
            }
            catch (Exception ex)
            {
                OAuthCallbackHandler.Instance.Stop();
                NotificationManager.Instance.ShowError("Error", $"Failed to connect Google Calendar: {ex.Message}");
            }
            finally
            {
                IsConnectingGoogle = false;
            }
        }

        private void DisconnectGoogleExecuted(object? parameter)
        {
            var result = MessageBoxHelper.Show(
                "Are you sure you want to disconnect Google Calendar? This will stop syncing meetings to your Google Calendar.",
                "Disconnect Google Calendar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                GoogleAuthService.Instance.Disconnect(_settings);
                GoogleCalendarEnabled = false;
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

        #endregion
    }
}

