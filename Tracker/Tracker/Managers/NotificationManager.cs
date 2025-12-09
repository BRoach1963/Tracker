using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Tracker.Common.Enums;
using Tracker.Factories;
using Tracker.Views.Toasts;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages in-app and native toast notifications.
    /// </summary>
    public class NotificationManager
    {
        #region Fields

        private readonly List<TrackerToast> _activeToasts = new();
        private readonly object _toastLock = new();

        #endregion

        #region Singleton Instance

        private static NotificationManager? _instance;
        private static readonly object SyncRoot = new();

        public static NotificationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new NotificationManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Public Methods - In-App Toasts

        /// <summary>
        /// Shows an information toast.
        /// </summary>
        public void ShowInfo(string title, string message, int durationSeconds = 5)
        {
            ShowToast(title, message, ToastType.Information, durationSeconds);
        }

        /// <summary>
        /// Shows a success toast.
        /// </summary>
        public void ShowSuccess(string title, string message, int durationSeconds = 5)
        {
            ShowToast(title, message, ToastType.Success, durationSeconds);
        }

        /// <summary>
        /// Shows a warning toast.
        /// </summary>
        public void ShowWarning(string title, string message, int durationSeconds = 5)
        {
            ShowToast(title, message, ToastType.Warning, durationSeconds);
        }

        /// <summary>
        /// Shows an error toast.
        /// </summary>
        public void ShowError(string title, string message, int durationSeconds = 7)
        {
            ShowToast(title, message, ToastType.Error, durationSeconds);
        }

        /// <summary>
        /// Shows a toast notification with the specified type.
        /// </summary>
        public void ShowToast(string title, string message, ToastType type = ToastType.Information, int durationSeconds = 5)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                var toast = new TrackerToast(title, message, type, durationSeconds);

                lock (_toastLock)
                {
                    // Set stack position
                    toast.SetStackOffset(_activeToasts.Count);
                    _activeToasts.Add(toast);
                }

                toast.Closed += (s, e) => OnToastClosed(toast);
                toast.Show();
            });
        }

        /// <summary>
        /// Legacy method - shows an in-app toast notification.
        /// </summary>
        [Obsolete("Use ShowToast, ShowInfo, ShowSuccess, ShowWarning, or ShowError instead.")]
        public void SendTrackerToast(string title, string message)
        {
            ShowInfo(title, message);
        }

        #endregion

        #region Public Methods - Native Windows Toasts

        /// <summary>
        /// Sends a native Windows toast notification based on the action type.
        /// </summary>
        public void SendNativeToast(ToastNotificationAction action)
        {
            if (ToastContentFactory.TryGetToastContent(action, out ToastContentBuilder? toast))
            {
                toast?.Show();
            }
        }

        /// <summary>
        /// Sends a native Windows toast notification with custom content.
        /// </summary>
        public void SendNativeToast(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }

        #endregion

        #region Public Methods - Cleanup

        /// <summary>
        /// Closes all active toast notifications.
        /// Called during application shutdown to ensure clean exit.
        /// </summary>
        public void CloseAllToasts()
        {
            lock (_toastLock)
            {
                foreach (var toast in _activeToasts.ToList())
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            toast.Close();
                        }
                        catch
                        {
                            // Ignore errors during shutdown
                        }
                    });
                }
                _activeToasts.Clear();
            }
        }

        #endregion

        #region Private Methods

        private void OnToastClosed(TrackerToast closedToast)
        {
            lock (_toastLock)
            {
                _activeToasts.Remove(closedToast);
                
                // Reposition remaining toasts
                for (int i = 0; i < _activeToasts.Count; i++)
                {
                    var toast = _activeToasts[i];
                    Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        AnimateToastToPosition(toast, i);
                    });
                }
            }
        }

        private static void AnimateToastToPosition(TrackerToast toast, int stackIndex)
        {
            var workingArea = SystemParameters.WorkArea;
            var targetTop = workingArea.Bottom - (toast.Height * (stackIndex + 1)) - (10 * (stackIndex + 1));

            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.CubicEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut 
                }
            };

            toast.BeginAnimation(Window.TopProperty, animation);
        }

        #endregion
    }
}
