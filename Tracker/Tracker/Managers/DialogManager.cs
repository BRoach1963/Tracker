using System.Collections.Concurrent;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Factories;

namespace Tracker.Managers
{
    public class DialogManager
    {
        #region Fields

        private ConcurrentDictionary<DialogType, BaseWindow?> _activeDialogs = new();

        #endregion

        #region Singleton Instance

        private static DialogManager? _instance;
        private static readonly object SyncRoot = new object();

        public static DialogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new DialogManager();
                        }
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Public Methods

        public void LaunchDialogByType(DialogType type, bool modal, Action? callback)
        {
            if (_activeDialogs.TryGetValue(type, out BaseWindow? dialog))
            {
                dialog?.Activate();
                return;
            }

            if (!DialogFactory.TryGetWindowFromType(type, callback, out BaseWindow? newDialog)) return;

            _activeDialogs.TryAdd(type, newDialog);
            if (modal)
            {
                newDialog?.ShowDialog();
            }
            else
            {
                newDialog?.Show();
            }
        }

        public void CloseDialog(BaseWindow dialog)
        {
            _activeDialogs.TryRemove(dialog.Type, out _);
            dialog.Close();
        }

        #endregion
    }
}
