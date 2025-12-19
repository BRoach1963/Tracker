using System.Windows;
using System.Windows.Input;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Styled input dialog for getting text input from users.
    /// Replaces ugly Microsoft.VisualBasic.Interaction.InputBox.
    /// 
    /// Usage:
    /// <code>
    /// var email = await InputDialog.ShowAsync("Change Email", "Enter your new email:", currentEmail);
    /// if (email != null)
    /// {
    ///     // User confirmed with a value
    /// }
    /// </code>
    /// </summary>
    public partial class InputDialog : BaseWindow
    {
        public string? Result { get; private set; }
        
        private Func<string, string?>? _validator;

        public InputDialog()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        #region Static Factory Methods

        /// <summary>
        /// Shows an input dialog and returns the entered value, or null if cancelled.
        /// </summary>
        public static Task<string?> ShowAsync(
            string title, 
            string prompt, 
            string defaultValue = "",
            string confirmText = "Confirm",
            Func<string, string?>? validator = null,
            Window? owner = null)
        {
            var tcs = new TaskCompletionSource<string?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new InputDialog();
                dialog.Configure(title, prompt, defaultValue, confirmText, validator);
                
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
        /// Shows an email input dialog with validation.
        /// </summary>
        public static Task<string?> ShowEmailAsync(
            string title,
            string prompt,
            string currentEmail = "",
            Window? owner = null)
        {
            return ShowAsync(
                title,
                prompt,
                currentEmail,
                "Update Email",
                ValidateEmail,
                owner);
        }

        private static string? ValidateEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Email cannot be empty.";
            
            try
            {
                var addr = new System.Net.Mail.MailAddress(value);
                if (addr.Address != value)
                    return "Please enter a valid email address.";
            }
            catch
            {
                return "Please enter a valid email address.";
            }

            return null; // Valid
        }

        #endregion

        #region Configuration

        private void Configure(
            string title,
            string prompt,
            string defaultValue,
            string confirmText,
            Func<string, string?>? validator)
        {
            TitleText.Text = title;
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            ConfirmButton.Content = confirmText;
            _validator = validator;
        }

        #endregion

        #region Event Handlers

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            TryConfirm();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryConfirm();
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();
            }
        }

        private void TryConfirm()
        {
            var value = InputTextBox.Text?.Trim() ?? "";

            // Validate if validator provided
            if (_validator != null)
            {
                var error = _validator(value);
                if (error != null)
                {
                    ErrorText.Text = error;
                    ErrorText.Visibility = Visibility.Visible;
                    InputTextBox.Focus();
                    return;
                }
            }

            Result = value;
            DialogResult = true;
            Close();
        }

        private void Cancel()
        {
            Result = null;
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        #endregion
    }
}


