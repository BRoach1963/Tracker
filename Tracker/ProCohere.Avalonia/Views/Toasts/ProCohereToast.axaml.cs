using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Toasts;

/// <summary>
/// Modern toast notification with type-based styling and animations.
/// Supports pause on hover for better user experience.
/// </summary>
public partial class ProCohereToast : Window
{
    #region Win32 Interop for Screen Positioning
    
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private const int SPI_GETWORKAREA = 0x0030;
    
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    
    private static RECT GetWorkArea()
    {
        var rect = new RECT();
        SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0);
        return rect;
    }
    
    #endregion
    
    #region Fields

    private readonly DispatcherTimer _dismissTimer;
    private readonly int _durationSeconds;
    private readonly ToastType _toastType;
    private bool _isClosing;
    private bool _isPaused;
    private DateTime _timerStartTime;
    private TimeSpan _remainingTime;
    private ScaleTransform? _progressScaleTransform;
    private int _stackIndex;

    #endregion

    #region Constructor

    public ProCohereToast() : this("Notification", "Message", ToastType.Information, 5) { }

    public ProCohereToast(string title, string message, ToastType type = ToastType.Information, int durationSeconds = 5)
    {
        InitializeComponent();

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

        // Get the scale transform from XAML
        _progressScaleTransform = ProgressBar.RenderTransform as ScaleTransform;

        // Position and animate when opened
        Opened += OnOpened;
    }

    #endregion

    #region Toast Type Styling

    private void ApplyToastType(ToastType type)
    {
        IBrush accentBrush = type switch
        {
            ToastType.Success => (IBrush)Resources["SuccessAccentBrush"]!,
            ToastType.Warning => (IBrush)Resources["WarningAccentBrush"]!,
            ToastType.Error => (IBrush)Resources["ErrorAccentBrush"]!,
            _ => (IBrush)Resources["InfoAccentBrush"]!
        };

        StreamGeometry iconGeometry = type switch
        {
            ToastType.Success => (StreamGeometry)Resources["SuccessIcon"]!,
            ToastType.Warning => (StreamGeometry)Resources["WarningIcon"]!,
            ToastType.Error => (StreamGeometry)Resources["ErrorIcon"]!,
            _ => (StreamGeometry)Resources["InfoIcon"]!
        };

        AccentBar.Background = accentBrush;
        ToastIcon.Data = iconGeometry;
        ToastIcon.Foreground = accentBrush;
        ProgressBar.Background = accentBrush;
    }

    #endregion

    #region Window Events

    private void OnOpened(object? sender, EventArgs e)
    {
        // Position after window is shown and measured
        PositionToast(_stackIndex);
        AnimateIn();
        _timerStartTime = DateTime.Now;
        _dismissTimer.Start();
    }

    #endregion

    #region Animations

    private void AnimateIn()
    {
        // Set initial state
        Opacity = 0;
        var translateTransform = new TranslateTransform(50, 0);
        ToastBorder.RenderTransform = translateTransform;

        // Animate opacity using simple timer-based approach
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromMilliseconds(300);

        var animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        animTimer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

            // Cubic ease out
            var easedProgress = 1 - Math.Pow(1 - progress, 3);

            Opacity = easedProgress;
            translateTransform.X = 50 * (1 - easedProgress);

            if (progress >= 1.0)
            {
                animTimer.Stop();
                Opacity = 1;
                translateTransform.X = 0;
            }
        };
        animTimer.Start();

        // Start progress bar animation
        StartProgressAnimation(_durationSeconds);
    }

    private void StartProgressAnimation(double seconds)
    {
        if (_progressScaleTransform == null) return;

        _progressScaleTransform.ScaleX = 1.0;

        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(seconds);

        var progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        progressTimer.Tick += (s, e) =>
        {
            if (_isPaused)
            {
                progressTimer.Stop();
                return;
            }

            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

            _progressScaleTransform.ScaleX = 1.0 - progress;

            if (progress >= 1.0)
            {
                progressTimer.Stop();
            }
        };
        progressTimer.Start();
    }

    private void AnimateOut(Action onComplete)
    {
        if (_isClosing) return;
        _isClosing = true;

        var startTime = DateTime.Now;
        var duration = TimeSpan.FromMilliseconds(200);

        var animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        animTimer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - startTime;
            var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

            // Cubic ease in
            var easedProgress = Math.Pow(progress, 3);

            Opacity = 1 - easedProgress;

            if (progress >= 1.0)
            {
                animTimer.Stop();
                onComplete();
            }
        };
        animTimer.Start();
    }

    #endregion

    #region Pause/Resume on Hover

    private void Toast_PointerEntered(object? sender, PointerEventArgs e)
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
    }

    private void Toast_PointerExited(object? sender, PointerEventArgs e)
    {
        if (_isClosing || !_isPaused) return;

        _isPaused = false;

        // Resume with remaining time
        if (_remainingTime > TimeSpan.Zero)
        {
            _dismissTimer.Interval = _remainingTime;
            _timerStartTime = DateTime.Now;
            _dismissTimer.Start();

            // Resume progress animation with remaining time
            StartProgressAnimation(_remainingTime.TotalSeconds);
        }
        else
        {
            CloseToast();
        }
    }

    #endregion

    #region Timer and Close

    private void DismissTimer_Tick(object? sender, EventArgs e)
    {
        _dismissTimer.Stop();
        CloseToast();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        _dismissTimer.Stop();
        CloseToast();
    }

    private void CloseToast()
    {
        if (_isClosing) return;

        AnimateOut(() =>
        {
            Dispatcher.UIThread.Post(() =>
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

    #endregion

    #region Positioning

    private void PositionToast(int stackIndex)
    {
        // Get the work area (screen minus taskbar) in physical pixels
        var workArea = GetWorkArea();
        
        // Get DPI scaling from primary screen
        var screen = Screens.Primary;
        var scaling = screen?.Scaling ?? 1.0;
        
        // Toast dimensions in physical pixels
        // Width=400 DIPs, Height estimate=100 DIPs
        var toastWidthPx = (int)(400 * scaling);
        var toastHeightPx = (int)(100 * scaling);
        var marginPx = (int)(12 * scaling);
        var stackGapPx = (int)(8 * scaling);

        // Calculate position in bottom-right corner of work area
        var x = workArea.Right - toastWidthPx - marginPx;
        var y = workArea.Bottom - (toastHeightPx + stackGapPx) * (stackIndex + 1) - marginPx + stackGapPx;
        
        System.Diagnostics.Debug.WriteLine($"[Toast] WorkArea: R={workArea.Right} B={workArea.Bottom}, Scaling={scaling}");
        System.Diagnostics.Debug.WriteLine($"[Toast] Positioning to ({x}, {y})");
        
        // Get the native window handle and use SetWindowPos for reliable positioning
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            // Use SetWindowPos to position at absolute screen coordinates
            // SWP_NOSIZE = don't change size, SWP_NOZORDER = don't change z-order, SWP_NOACTIVATE = don't activate
            SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            System.Diagnostics.Debug.WriteLine($"[Toast] SetWindowPos called with handle {handle}");
        }
        else
        {
            // Fallback to Avalonia Position property
            Position = new PixelPoint(x, y);
            System.Diagnostics.Debug.WriteLine($"[Toast] Fallback: Position={Position}");
        }
    }

    /// <summary>
    /// Sets the vertical offset for toast stacking. Call before Show().
    /// </summary>
    public void SetStackOffset(int stackIndex)
    {
        _stackIndex = stackIndex;
    }

    /// <summary>
    /// Animates the toast to a new stack position.
    /// </summary>
    public void AnimateToStackPosition(int stackIndex)
    {
        _stackIndex = stackIndex;
        PositionToast(stackIndex);
    }

    #endregion
}
