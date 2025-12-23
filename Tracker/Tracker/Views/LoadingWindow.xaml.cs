using System.Windows;
using System.Windows.Media.Animation;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.Views
{
    /// <summary>
    /// Loading window shown between login and main window display.
    /// Shows a welcome message and spinner while the main window initializes.
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private Storyboard? _spinnerAnimation;
        private readonly ILogger _logger;

        public LoadingWindow()
        {
            _logger = LoggingManager.GetComponentLogger("LoadingWindow");
            _logger.Info(">>> LoadingWindow constructor called");
            
            InitializeComponent();
            _logger.Info(">>> InitializeComponent complete");
            
            // Set welcome message with username if available
            var currentUser = SupabaseService.Instance.CurrentUser;
            var userName = "User";
            
            if (currentUser?.Email != null)
            {
                userName = currentUser.Email;
                if (userName.Contains("@"))
                {
                    // Extract name from email (everything before @)
                    userName = userName.Split('@')[0];
                    // Capitalize first letter
                    if (userName.Length > 0)
                    {
                        userName = char.ToUpper(userName[0]) + userName.Substring(1);
                    }
                }
            }
            else if (SupabaseService.Instance.CurrentProfile?.DisplayName != null)
            {
                userName = SupabaseService.Instance.CurrentProfile.DisplayName;
            }
            
            WelcomeText.Text = $"Welcome, {userName}!";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.Info(">>> LoadingWindow.Loaded event fired - window should be visible now");
            // Start spinner animation
            _spinnerAnimation = (Storyboard)FindResource("SpinnerAnimation");
            _spinnerAnimation?.Begin();
        }

        /// <summary>
        /// Closes the loading window with a fade-out animation.
        /// </summary>
        public void CloseWithFade()
        {
            _logger.Info(">>> CloseWithFade called - starting fade animation");
            _spinnerAnimation?.Stop();
            
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
