using System.Windows;
using Tracker.Views.Dialogs;

namespace Tracker.Helpers
{
    /// <summary>
    /// Helper class to show custom styled message boxes instead of Windows MessageBox.
    /// </summary>
    public static class MessageBoxHelper
    {
        /// <summary>
        /// Shows a custom styled message box.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, Window? owner = null)
        {
            var dialog = new MessageBoxDialog(messageBoxText, caption, button, icon)
            {
                Owner = owner ?? Application.Current.MainWindow
            };

            dialog.ShowDialog();
            return dialog.Result;
        }

        /// <summary>
        /// Shows a custom styled message box with OK button.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, string caption, Window? owner = null)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Information, owner);
        }

        /// <summary>
        /// Shows a custom styled message box with just a message.
        /// </summary>
        public static MessageBoxResult Show(string messageBoxText, Window? owner = null)
        {
            return Show(messageBoxText, "Tracker", MessageBoxButton.OK, MessageBoxImage.Information, owner);
        }
    }
}

