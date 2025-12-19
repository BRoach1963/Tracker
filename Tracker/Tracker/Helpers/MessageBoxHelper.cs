using System.Linq;
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
            var dialog = new MessageBoxDialog(messageBoxText, caption, button, icon);
            
            // Find a valid owner window - avoid setting owner to itself
            Window? validOwner = null;
            if (owner != null && owner != dialog)
            {
                validOwner = owner;
            }
            else
            {
                // Try to find the active window or main window
                validOwner = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w != dialog) 
                    ?? Application.Current.MainWindow;
                
                // Don't set owner if it's the same window
                if (validOwner == dialog)
                {
                    validOwner = null;
                }
            }
            
            if (validOwner != null)
            {
                dialog.Owner = validOwner;
            }

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

