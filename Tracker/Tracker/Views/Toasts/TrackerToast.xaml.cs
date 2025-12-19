using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Tracker.Common.Enums;

namespace Tracker.Views.Toasts
{
    /// <summary>
    /// Modern toast notification with type-based styling and animations.
    /// Supports pause on hover for better user experience.
    /// </summary>
    public partial class TrackerToast : Window
    {
        private readonly DispatcherTimer _dismissTimer;
        private readonly int _durationSeconds;
        private readonly ToastType _toastType;
        private bool _isClosing;
        private bool _isPaused;
        private DateTime _timerStartTime;
        private TimeSpan _remainingTime;
        private DoubleAnimation? _progressAnimation;

        public TrackerToast(string title, string message, ToastType type = ToastType.Information, int durationSeconds = 5)
        {
            InitializeComponent();
            
            // Ensure toast is unparented (no Owner) and doesn't show in taskbar
            Owner = null;
            ShowInTaskbar = false;
            
            ToastTitle.Text = title;
            ToastMessage.Text = message;
            _toastType = type;
            _durationSeconds = durationSeconds;
            _remainingTime = TimeSpan.FromSeconds(durationSeconds);

            ApplyToastType(type);

            _dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationSeconds)
            };
            _dismissTimer.Tick += DismissTimer_Tick;
        }

        private void ApplyToastType(ToastType type)
        {
            var accentBrush = type switch
            {
                ToastType.Success => (Brush)FindResource("SuccessAccentBrush"),
                ToastType.Warning => (Brush)FindResource("WarningAccentBrush"),
                ToastType.Error => (Brush)FindResource("ErrorAccentBrush"),
                _ => (Brush)FindResource("InfoAccentBrush")
            };

            var iconGeometry = type switch
            {
                ToastType.Success => (Geometry)FindResource("SuccessIcon"),
                ToastType.Warning => (Geometry)FindResource("WarningIcon"),
                ToastType.Error => (Geometry)FindResource("ErrorIcon"),
                _ => (Geometry)FindResource("InfoIcon")
            };

            AccentBar.Background = accentBrush;
            ToastIcon.Data = iconGeometry;
            ToastIcon.Fill = accentBrush;
            ProgressBar.Background = accentBrush;
        }

        private void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionToast();
            AnimateIn();
            _timerStartTime = DateTime.Now;
            _dismissTimer.Start();
        }

        private void AnimateIn()
        {
            // Slide in from right + fade in
            var slideIn = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Progress bar shrink animation (linear - no easing)
            _progressAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(_durationSeconds)
            };

            var transform = new TranslateTransform(50, 0);
            ToastBorder.RenderTransform = transform;

            transform.BeginAnimation(TranslateTransform.XProperty, slideIn);
            BeginAnimation(OpacityProperty, fadeIn);

            ProgressBar.Width = ToastBorder.ActualWidth > 0 ? ToastBorder.ActualWidth : 380;
            var scaleTransform = (ScaleTransform)ProgressBar.RenderTransform;
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, _progressAnimation);
        }

        #region Pause/Resume on Hover

        private void Toast_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isClosing || _isPaused) return;
            
            _isPaused = true;
            
            // Calculate remaining time
            var elapsed = DateTime.Now - _timerStartTime;
            _remainingTime = TimeSpan.FromSeconds(_durationSeconds) - elapsed;
            if (_remainingTime < TimeSpan.Zero)
                _remainingTime = TimeSpan.Zero;
            
            // Stop the timer
            _dismissTimer.Stop();
            
            // Pause the progress bar animation by setting speed to 0
            var scaleTransform = (ScaleTransform)ProgressBar.RenderTransform;
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null); // Remove animation
            
            // Hold at current position
            var currentScale = scaleTransform.ScaleX;
            scaleTransform.ScaleX = currentScale;
        }

        private void Toast_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isClosing || !_isPaused) return;
            
            _isPaused = false;
            
            // Resume with remaining time
            if (_remainingTime > TimeSpan.Zero)
            {
                _dismissTimer.Interval = _remainingTime;
                _timerStartTime = DateTime.Now;
                _dismissTimer.Start();
                
                // Resume progress bar animation from current position
                var scaleTransform = (ScaleTransform)ProgressBar.RenderTransform;
                var currentScale = scaleTransform.ScaleX;
                
                var resumeAnimation = new DoubleAnimation
                {
                    From = currentScale,
                    To = 0,
                    Duration = _remainingTime
                };
                
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, resumeAnimation);
            }
            else
            {
                CloseToast();
            }
        }

        #endregion

        private void AnimateOut(Action onComplete)
        {
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = 50,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) => onComplete();

            if (ToastBorder.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            }
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void DismissTimer_Tick(object? sender, EventArgs e)
        {
            _dismissTimer.Stop();
            CloseToast();
        }

        private void Toast_Close(object sender, RoutedEventArgs e)
        {
            _dismissTimer.Stop();
            CloseToast();
        }

        private void CloseToast()
        {
            if (_isClosing) return;
            _isClosing = true;

            AnimateOut(() =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    Close();
                }, DispatcherPriority.Background);
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dismissTimer.Stop();
            _dismissTimer.Tick -= DismissTimer_Tick;
        }

        private void PositionToast()
        {
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width - 10;
            Top = workingArea.Bottom - Height - 10;
        }

        /// <summary>
        /// Sets the vertical offset for toast stacking.
        /// </summary>
        public void SetStackOffset(int stackIndex)
        {
            var workingArea = SystemParameters.WorkArea;
            Top = workingArea.Bottom - (Height * (stackIndex + 1)) - (10 * (stackIndex + 1));
        }
    }
}
