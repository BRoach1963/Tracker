using System.Collections.Concurrent;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Factories;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Managers
{
    public class DialogManager
    {
        #region Fields

        private ConcurrentDictionary<DialogType, BaseWindow?> _activeDialogs = new();

        #endregion

        #region Singleton Instance

        private static readonly Lazy<DialogManager> _lazyInstance = 
            new(() => new DialogManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of DialogManager.
        /// </summary>
        public static DialogManager Instance => _lazyInstance.Value;

        #endregion

        #region Public Methods

        public void LaunchDialogByType(DialogType type, bool modal, Action? callback, object? dataObject = null)
        {
            if (_activeDialogs.TryGetValue(type, out BaseWindow? dialog))
            {
                dialog?.Activate();
                return;
            }

            if (!DialogFactory.TryGetWindowFromType(type, callback, out BaseWindow? newDialog, dataObject)) return;

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
            if (dialog.DataContext is BaseDialogViewModel vm)
            {
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    vm.Callback?.Invoke();
                });
            }
            _activeDialogs.TryRemove(dialog.Type, out _);
            dialog.Close();
        }

        #endregion
    }
}
