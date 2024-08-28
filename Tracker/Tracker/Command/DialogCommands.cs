using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Managers;

namespace Tracker.Command
{
    public static class DialogCommands
    {
        #region Fields

        private static ICommand? _closeDialogCommand;
        private static ICommand? _launchDialogCommand;

        #endregion

        #region Public Commands

        public static ICommand CloseDialogCommand => _closeDialogCommand ?? new TrackerCommand(CloseDialogExecuted);

        public static ICommand LaunchDialogCommand => _launchDialogCommand ??= new TrackerCommand(LaunchDialogExecuted);

        #endregion

        #region Private Methods

        private static void CloseDialogExecuted(object? parameter)
        {
            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        private static void LaunchDialogExecuted(object? parameter)
        {
            if (parameter is DialogType dialogType)
            {
                DialogManager.Instance.LaunchDialogByType(dialogType, false, null);
            }
        }

        #endregion
    }
}
