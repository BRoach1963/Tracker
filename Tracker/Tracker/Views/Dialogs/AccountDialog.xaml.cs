using System.Windows;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog showing user account information and subscription status.
    /// Uses the shared AccountInfoControl for consistency with Settings page.
    /// </summary>
    public partial class AccountDialog : BaseWindow
    {
        public AccountDialog()
        {
            InitializeComponent();
        }

        private new void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
