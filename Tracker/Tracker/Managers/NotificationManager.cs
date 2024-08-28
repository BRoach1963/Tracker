using Tracker.Common.Enums;

namespace Tracker.Managers
{
    public class NotificationManager
    {

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

        public void SendToast(ToastNotificationAction action)
        {

        }
    }
}
