using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Tracker.Database;
using Tracker.Helpers;
using Tracker.Managers;

namespace Tracker.Views
{
    /// <summary>
    /// Professional splash screen with loading animations and integrated login.
    /// 
    /// This provides a unified startup experience:
    /// - Shows loading animation during initialization
    /// - Can transition to login form when manual auth is required
    /// - Handles Windows authentication option
    /// - Smooth animations throughout
    /// </summary>
    public partial class SplashScreen : Window
    {
        private Storyboard? _spinnerAnimation;
        private Storyboard? _pulseAnimation;
        
        /// <summary>
        /// Event raised when login is successful.
        /// </summary>
        public event EventHandler<LoginSuccessEventArgs>? LoginSuccessful;
        
        public SplashScreen()
        {
            InitializeComponent();
            
            // Set version from assembly
            VersionText.Text = $"Version {VersionHelper.GetVersion()}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start animations
            _spinnerAnimation = (Storyboard)FindResource("SpinnerAnimation");
            _spinnerAnimation.Begin();

            _pulseAnimation = (Storyboard)FindResource("LogoPulse");
            _pulseAnimation.Begin();
        }

        /// <summary>
        /// Updates the status text displayed on the splash screen.
        /// </summary>
        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        /// <summary>
        /// Updates the progress bar (0-100).
        /// </summary>
        public void UpdateProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                var maxWidth = ProgressBarContainer.ActualWidth > 0 
                    ? ProgressBarContainer.ActualWidth 
                    : 200;
                
                var targetWidth = (percentage / 100.0) * maxWidth;
                
                var animation = new DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                ProgressFill.BeginAnimation(WidthProperty, animation);
            });
        }

        /// <summary>
        /// Transitions from loading state to login form.
        /// </summary>
        /// <param name="showWindowsAuthOption">Whether to show the Windows auth link.</param>
        public void ShowLogin(bool showWindowsAuthOption = true)
        {
            Dispatcher.Invoke(() =>
            {
                // Hide progress bar
                ProgressBarContainer.Visibility = Visibility.Collapsed;
                
                // Fade out loading panel
                var fadeOutLoading = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                fadeOutLoading.Completed += (s, e) =>
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    
                    // Show login panel
                    LoginPanel.Visibility = Visibility.Visible;
                    
                    // Fade in login panel
                    var fadeInLogin = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    LoginPanel.BeginAnimation(OpacityProperty, fadeInLogin);
                    
                    // Focus username field
                    UsernameTextBox.Focus();
                };
                
                LoadingPanel.BeginAnimation(OpacityProperty, fadeOutLoading);
            });
        }

        /// <summary>
        /// Shows an error message on the login form.
        /// </summary>
        public void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ErrorText.Text = message;
                ErrorText.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Clears any error message.
        /// </summary>
        public void ClearError()
        {
            Dispatcher.Invoke(() =>
            {
                ErrorText.Text = "";
                ErrorText.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// Sets the login button enabled state.
        /// </summary>
        public void SetLoginEnabled(bool enabled)
        {
            Dispatcher.Invoke(() =>
            {
                LoginButton.IsEnabled = enabled;
                LoginButton.Content = enabled ? "Sign In" : "Signing in...";
            });
        }

        /// <summary>
        /// Closes the splash screen with a fade-out animation.
        /// </summary>
        public void CloseSplash(Action? onComplete = null)
        {
            Dispatcher.Invoke(() =>
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s, e) =>
                {
                    _spinnerAnimation?.Stop();
                    _pulseAnimation?.Stop();
                    
                    // IMPORTANT: Invoke callback BEFORE closing to ensure
                    // main window is created before this window closes
                    // (otherwise app shuts down with OnLastWindowClose mode)
                    onComplete?.Invoke();
                    Close();
                };

                BeginAnimation(OpacityProperty, fadeOut);
            });
        }

        #region Event Handlers

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await AttemptLoginAsync();
        }

        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AttemptLoginAsync();
            }
        }

        private async Task AttemptLoginAsync()
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            // Validate
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Please enter a username.");
                UsernameTextBox.Focus();
                return;
            }

            ClearError();
            SetLoginEnabled(false);

            try
            {
                // Verify database connection and create/get user
                var user = await TrackerDbManager.Instance!.GetOrCreateUserAsync(username);
                
                if (user != null)
                {
                    // Update settings
                    UserSettingsManager.Instance.CurrentUser = username;
                    UserSettingsManager.Instance.CurrentUserId = user.Id;
                    UserSettingsManager.Instance.Settings.Authentication.StoredUserId = user.Id;
                    UserSettingsManager.Instance.Settings.Authentication.AccountSetupCompleted = true;
                    UserSettingsManager.Instance.SaveSettings();
                    
                    // Raise success event
                    LoginSuccessful?.Invoke(this, new LoginSuccessEventArgs(username, user.Id));
                }
                else
                {
                    ShowError("Failed to create user account.");
                    SetLoginEnabled(true);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
                SetLoginEnabled(true);
            }
        }

        #endregion
    }

    /// <summary>
    /// Event args for successful login.
    /// </summary>
    public class LoginSuccessEventArgs : EventArgs
    {
        public string Username { get; }
        public int UserId { get; }
        public bool IsAdminLogin { get; }

        public LoginSuccessEventArgs(string username, int userId, bool isAdminLogin = false)
        {
            Username = username;
            UserId = userId;
            IsAdminLogin = isAdminLogin;
        }
    }
}
