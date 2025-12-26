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

        /// <summary>
        /// Shows a delete confirmation dialog and returns true if user confirms.
        /// </summary>
        /// <param name="itemName">Name of the item being deleted</param>
        /// <param name="itemType">Type of item (e.g., "OKR", "Team Member", "Feedback")</param>
        /// <param name="additionalMessage">Optional additional warning message</param>
        /// <returns>True if user confirmed deletion, false otherwise</returns>
        public static bool ConfirmDelete(string itemName, string itemType, string? additionalMessage = null)
        {
            var message = $"Are you sure you want to delete this {itemType}?\n\n\"{itemName}\"";
            if (!string.IsNullOrEmpty(additionalMessage))
            {
                message += $"\n\n{additionalMessage}";
            }

            var result = Show(
                message,
                $"Delete {itemType}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Shows a generic confirmation dialog and returns true if user confirms.
        /// </summary>
        /// <param name="message">The confirmation message</param>
        /// <param name="title">Dialog title</param>
        /// <returns>True if user confirmed, false otherwise</returns>
        public static bool Confirm(string message, string title = "Confirm")
        {
            var result = Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
    }
}

