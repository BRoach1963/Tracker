using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Tracker.Logging;

namespace Tracker.Controls.Settings
{
    /// <summary>
    /// About settings control showing app version, company info, and legal links.
    /// </summary>
    public partial class AboutSettingsControl : UserControl
    {
        #region Constants

        /// <summary>
        /// URL for the Terms of Service page.
        /// </summary>
        private const string TermsOfServiceUrl = "https://pricklycactussoftware.com/tracker/terms";

        /// <summary>
        /// URL for the Privacy Policy page.
        /// </summary>
        private const string PrivacyPolicyUrl = "https://pricklycactussoftware.com/prickly-cactus/privacy";

        /// <summary>
        /// URL for the company website.
        /// </summary>
        private const string WebsiteUrl = "https://pricklycactussoftware.com";

        /// <summary>
        /// Support email address.
        /// </summary>
        private const string SupportEmail = "support@pricklycactus.com";

        #endregion

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("AboutSettingsControl");

        public AboutSettingsControl()
        {
            InitializeComponent();
            LoadVersionInfo();
            LoadPlatformInfo();
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

        private void LoadPlatformInfo()
        {
            try
            {
                var frameworkDescription = RuntimeInformation.FrameworkDescription;
                var osDescription = RuntimeInformation.OSDescription;
                var architecture = RuntimeInformation.OSArchitecture.ToString();
                
                PlatformText.Text = $"{frameworkDescription} / {architecture}";
            }
            catch
            {
                PlatformText.Text = ".NET 8.0 / Windows";
            }
        }

        #endregion

        #region Event Handlers

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

        #region Private Methods

        private void OpenUrl(string url)
        {
            try
            {
                // Use ProcessStartInfo with UseShellExecute for proper URL handling
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to open URL {0}: {1}", url, ex.Message);
            }
        }

        #endregion
    }
}
