using System.Windows;
using System.Windows.Media.Animation;
using Tracker.Helpers;

namespace Tracker.Views
{
    /// <summary>
    /// Professional splash screen with loading animations and progress reporting.
    /// </summary>
    public partial class SplashScreen : Window
    {
        private Storyboard? _spinnerAnimation;
        private Storyboard? _pulseAnimation;

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
                var maxWidth = ProgressFill.Parent is FrameworkElement parent 
                    ? parent.ActualWidth 
                    : 420; // fallback width
                
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
                    Close();
                    onComplete?.Invoke();
                };

                BeginAnimation(OpacityProperty, fadeOut);
            });
        }
    }
}

