
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Tracker.Common.Enums;
using Tracker.Factories;
using Tracker.Views.Toasts;

namespace Tracker.Managers
{
    public class NotificationManager
    {
        private const int ToastSpacing = 5; // Spacing between stacked toasts
        private const int ToastWidth = 300;
        private const int ToastHeight = 100;
        private static int activeToastCount = 0;

        #region Singleton Instance

        private static NotificationManager? _instance;
        private static readonly object SyncRoot = new object();

        public static NotificationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new NotificationManager();
                        }
                    }
                }

                return _instance;
            }
        }

        #endregion

        public void SendNativeToast(ToastNotificationAction action)
        {
            ToastContentFactory.TryGetToastContent(action, out ToastContentBuilder? toast);
            toast?.Show();
        }

        public void SendTrackerToast(string title, string message)
        {
            var toast = new TrackerToast(title, message);

            // Calculate position for stacking
            var workingArea = SystemParameters.WorkArea;
            double topOffset = workingArea.Bottom - (ToastHeight + 10) * (activeToastCount + 1) - ToastSpacing * activeToastCount;

            toast.Top = topOffset;
            toast.Left = workingArea.Right - ToastWidth - 10;

            // Increment the active toast count to manage stacking
            activeToastCount++;

            toast.Closed += (s, e) => activeToastCount--; // Decrement count when toast is closed

            toast.Show();
        }
    }
}
