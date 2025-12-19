using System.Windows;
using Tracker.Common.Enums;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for manually entering OAuth authorization code as a fallback.
    /// </summary>
    public partial class ManualAuthCodeDialog : BaseWindow
    {
        public string? AuthorizationCode { get; private set; }
        public new bool DialogResult { get; private set; }

        public ManualAuthCodeDialog() : base(DialogType.Settings)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void AuthCodeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ContinueButton.IsEnabled = !string.IsNullOrWhiteSpace(AuthCodeTextBox.Text);
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            AuthorizationCode = AuthCodeTextBox.Text?.Trim();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            AuthorizationCode = null;
            DialogResult = false;
            Close();
        }

        // Use base class OnDragHandleMouseDown and Close_Click
    }
}

