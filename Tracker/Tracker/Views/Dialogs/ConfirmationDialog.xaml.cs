using System.Windows;
using System.Windows.Media;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Styled confirmation/message dialog that replaces ugly Windows MessageBox.
    /// 
    /// Usage:
    /// <code>
    /// // Simple confirmation
    /// if (await ConfirmationDialog.ShowAsync("Delete Item", "Are you sure?"))
    /// {
    ///     // User clicked Yes/Confirm
    /// }
    /// 
    /// // Info message (OK only)
    /// await ConfirmationDialog.ShowInfoAsync("Success", "Your changes have been saved.");
    /// 
    /// // Warning
    /// await ConfirmationDialog.ShowWarningAsync("Warning", "This action cannot be undone.");
    /// </code>
    /// </summary>
    public partial class ConfirmationDialog : BaseWindow
    {
        public enum DialogIcon
        {
            Question,
            Info,
            Warning,
            Error,
            Success
        }

        public bool Result { get; private set; }

        public ConfirmationDialog()
        {
            InitializeComponent();
        }

        #region Static Factory Methods

        /// <summary>
        /// Shows a confirmation dialog with Yes/No buttons.
        /// </summary>
        public static Task<bool> ShowAsync(string title, string message, Window? owner = null)
        {
            return ShowAsync(title, message, "Yes", "No", DialogIcon.Question, owner);
        }

        /// <summary>
        /// Shows a confirmation dialog with custom button text.
        /// </summary>
        public static Task<bool> ShowAsync(
            string title, 
            string message, 
            string primaryText, 
            string secondaryText,
            DialogIcon icon = DialogIcon.Question,
            Window? owner = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new ConfirmationDialog();
                dialog.Configure(title, message, primaryText, secondaryText, icon, showSecondary: true);
                
                if (owner != null)
                    dialog.Owner = owner;
                else
                    dialog.Owner = Application.Current.MainWindow;

                dialog.Closed += (s, e) => tcs.SetResult(dialog.Result);
                dialog.ShowDialog();
            });

            return tcs.Task;
        }

        /// <summary>
        /// Shows an info dialog with OK button only.
        /// </summary>
        public static Task ShowInfoAsync(string title, string message, Window? owner = null)
        {
            return ShowMessageAsync(title, message, "OK", DialogIcon.Info, owner);
        }

        /// <summary>
        /// Shows a success dialog with OK button only.
        /// </summary>
        public static Task ShowSuccessAsync(string title, string message, Window? owner = null)
        {
            return ShowMessageAsync(title, message, "OK", DialogIcon.Success, owner);
        }

        /// <summary>
        /// Shows a warning dialog with OK button only.
        /// </summary>
        public static Task ShowWarningAsync(string title, string message, Window? owner = null)
        {
            return ShowMessageAsync(title, message, "OK", DialogIcon.Warning, owner);
        }

        /// <summary>
        /// Shows an error dialog with OK button only.
        /// </summary>
        public static Task ShowErrorAsync(string title, string message, Window? owner = null)
        {
            return ShowMessageAsync(title, message, "OK", DialogIcon.Error, owner);
        }

        private static Task ShowMessageAsync(string title, string message, string buttonText, DialogIcon icon, Window? owner)
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new ConfirmationDialog();
                dialog.Configure(title, message, buttonText, "", icon, showSecondary: false);
                
                if (owner != null)
                    dialog.Owner = owner;
                else
                    dialog.Owner = Application.Current.MainWindow;

                dialog.Closed += (s, e) => tcs.SetResult(true);
                dialog.ShowDialog();
            });

            return tcs.Task;
        }

        #endregion

        #region Configuration

        private void Configure(
            string title, 
            string message, 
            string primaryText, 
            string secondaryText,
            DialogIcon icon,
            bool showSecondary)
        {
            TitleText.Text = title;
            Title = title;
            MessageText.Text = message;
            PrimaryButton.Content = primaryText;
            SecondaryButton.Content = secondaryText;
            SecondaryButton.Visibility = showSecondary ? Visibility.Visible : Visibility.Collapsed;

            // Set icon and color based on type
            var (iconData, iconColor) = GetIconConfig(icon);
            IconPath.Data = Geometry.Parse(iconData);
            IconBorder.Background = iconColor;
        }

        private (string IconData, Brush Color) GetIconConfig(DialogIcon icon)
        {
            return icon switch
            {
                DialogIcon.Question => (
                    "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z",
                    (Brush)FindResource("AccentBrush")),
                    
                DialogIcon.Info => (
                    "M11,9H13V7H11M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z",
                    new SolidColorBrush(Color.FromRgb(59, 130, 246))), // Blue
                    
                DialogIcon.Success => (
                    "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,16.5L18,9.5L16.59,8.09L11,13.67L7.91,10.59L6.5,12L11,16.5Z",
                    new SolidColorBrush(Color.FromRgb(34, 197, 94))), // Green
                    
                DialogIcon.Warning => (
                    "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z",
                    new SolidColorBrush(Color.FromRgb(245, 158, 11))), // Amber
                    
                DialogIcon.Error => (
                    "M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z",
                    new SolidColorBrush(Color.FromRgb(239, 68, 68))), // Red
                    
                _ => (
                    "M11,9H13V7H11M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z",
                    (Brush)FindResource("AccentBrush"))
            };
        }

        #endregion

        #region Event Handlers

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void Secondary_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        private new void Close_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        #endregion
    }
}


