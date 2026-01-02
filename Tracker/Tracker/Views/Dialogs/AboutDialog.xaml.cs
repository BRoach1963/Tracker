using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// About dialog showing app version, company info, and legal links.
    /// </summary>
    public partial class AboutDialog : BaseWindow
    {
        #region Constants

        /// <summary>
        /// URL for the Terms of Service page.
        /// </summary>
        public const string TermsOfServiceUrl = "https://pricklycactussoftware.com/tracker/terms";

        /// <summary>
        /// URL for the Privacy Policy page.
        /// </summary>
        public const string PrivacyPolicyUrl = "https://pricklycactussoftware.com/prickly-cactus/privacy";

        /// <summary>
        /// URL for the company website.
        /// </summary>
        public const string WebsiteUrl = "https://pricklycactussoftware.com";

        /// <summary>
        /// Support email address.
        /// </summary>
        public const string SupportEmail = "support@pricklycactus.com";

        #endregion

        public AboutDialog()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        #region Initialization

        private void LoadVersionInfo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                
                if (version != null)
                {
                    VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                VersionText.Text = "Version 1.0.0";
            }
        }

        #endregion

        #region Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TermsOfService_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(TermsOfServiceUrl);
        }

        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(PrivacyPolicyUrl);
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(WebsiteUrl);
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl($"mailto:{SupportEmail}?subject=Tracker Support Request");
        }

        #endregion

        #region Helpers

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail - user can manually navigate
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Shows the About dialog.
        /// </summary>
        public static void Show(Window? owner = null)
        {
            var dialog = new AboutDialog
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        #endregion
    }
}
