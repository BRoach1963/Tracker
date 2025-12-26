using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;
using Tracker.Services.Subscription;
using Tracker.Views.Dialogs;
using static Tracker.Views.Dialogs.ConfirmationDialog;

namespace Tracker.Controls
{
    /// <summary>
    /// Reusable control for displaying and managing account information.
    /// Used in both the Account Dialog and Settings page.
    /// </summary>
    public partial class AccountInfoControl : UserControl
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Event raised when account info changes (for parent to refresh).
        /// </summary>
        public event EventHandler? AccountInfoChanged;

        public AccountInfoControl()
        {
            InitializeComponent();
            _logger = LoggingManager.GetComponentLogger("AccountInfo");
            
            Loaded += (s, e) => LoadAccountInfo();
        }

        /// <summary>
        /// Loads and displays current account information.
        /// </summary>
        public void LoadAccountInfo()
        {
            try
            {
                var settings = UserSettingsManager.Instance.Settings;
                var subscription = SubscriptionService.Instance;
                var limits = subscription.Limits;

                // User info
                var displayName = settings.CurrentUser ?? "User";
                DisplayNameText.Text = displayName;

                // Generate initials
                var parts = displayName.Split(new[] { ' ', '.', '\\', '@' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    AvatarInitials.Text = $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                }
                else if (parts.Length == 1 && parts[0].Length >= 2)
                {
                    AvatarInitials.Text = parts[0][..2].ToUpper();
                }
                else
                {
                    AvatarInitials.Text = "U";
                }

                // Load avatar image if available
                LoadAvatarImage();

                // Email
                var auth = settings.Authentication;
                EmailText.Text = auth?.CloudUserEmail ?? "Not configured";
                
                // Load profile fields from Supabase
                LoadProfileFields();

                // Subscription info
                PlanNameText.Text = limits.DisplayName;

                // Show beta badge for Internal tier
                BetaBadge.Visibility = subscription.CurrentTier == Common.Enums.SubscriptionTier.Internal
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Show beta info section for Internal tier
                BetaInfoSection.Visibility = subscription.CurrentTier == Common.Enums.SubscriptionTier.Internal
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Show upgrade button for non-Pro tiers
                var currentTier = subscription.CurrentTier;
                if (currentTier == Common.Enums.SubscriptionTier.Pro)
                {
                    UpgradeButton.Content = "Manage";
                    UpgradeButton.Visibility = Visibility.Visible;
                }
                else if (currentTier == Common.Enums.SubscriptionTier.Internal)
                {
                    UpgradeButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    UpgradeButton.Content = "Upgrade";
                    UpgradeButton.Visibility = Visibility.Visible;
                }

                // Build features list
                var features = new List<string>();

                // Team members
                if (limits.MaxTeamMembers >= 9999)
                    features.Add("Unlimited team members");
                else
                    features.Add($"Up to {limits.MaxTeamMembers} team members");

                // Core features
                features.Add("Unlimited 1:1s, tasks, projects, OKRs & KPIs");

                // AI features
                if (limits.HasAIAssistant)
                    features.Add("AI Assistant");
                if (limits.HasAIDataAnalysis)
                    features.Add("AI-powered insights");

                // Reports
                if (limits.HasAdvancedReports)
                    features.Add("Advanced reports");
                else if (limits.HasBasicReports)
                    features.Add("Basic reports");

                // Database
                if (limits.AllowsNetworkDatabase)
                    features.Add("Network/Enterprise database support");
                else
                    features.Add("Local database");

                // Support
                if (limits.HasPrioritySupport)
                    features.Add("Priority support");
                else if (limits.HasEmailSupport)
                    features.Add("Email support");

                FeaturesItemsControl.ItemsSource = features;

                // Version
                VersionText.Text = $"Version {VersionHelper.GetAppVersion()}";
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading account info");
            }
        }

        private void LoadAvatarImage()
        {
            try
            {
                var profile = SupabaseService.Instance.CurrentProfile;
                _logger.Debug("LoadAvatarImage called. Profile: {0}, AvatarUrl: '{1}'", 
                    profile != null ? "exists" : "null", 
                    profile?.AvatarUrl ?? "null");
                
                if (profile?.AvatarUrl != null && !string.IsNullOrEmpty(profile.AvatarUrl))
                {
                    // Build full URL from stored relative path
                    // AvatarUrl is stored as "userId/avatar.jpg" (relative path)
                    var avatarUrl = profile.AvatarUrl;
                    if (!avatarUrl.StartsWith("http"))
                    {
                        // Add cache-busting timestamp to force reload after upload
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        avatarUrl = $"{SupabaseConfig.ProjectUrl}/storage/v1/object/public/{SupabaseConfig.AvatarBucket}/{profile.AvatarUrl}?t={timestamp}";
                    }
                    
                    _logger.Debug("Loading avatar from URL: {0}", avatarUrl);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(avatarUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    AvatarImageBrush.ImageSource = bitmap;
                    AvatarImageBorder.Visibility = Visibility.Visible;
                    AvatarBorder.Visibility = Visibility.Collapsed;
                    _logger.Debug("Avatar image loaded successfully");
                }
                else
                {
                    _logger.Debug("No avatar URL, showing initials");
                    AvatarImageBorder.Visibility = Visibility.Collapsed;
                    AvatarBorder.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // If image fails to load, keep showing initials
                _logger.Warn("Failed to load avatar image: {0}", ex.Message);
                AvatarImageBorder.Visibility = Visibility.Collapsed;
                AvatarBorder.Visibility = Visibility.Visible;
            }
        }

        private void LoadProfileFields()
        {
            try
            {
                var profile = SupabaseService.Instance.CurrentProfile;
                if (profile != null)
                {
                    // Load into edit fields
                    FirstNameTextBox.Text = profile.FirstName ?? "";
                    LastNameTextBox.Text = profile.LastName ?? "";
                    JobTitleTextBox.Text = profile.JobTitle ?? "";
                    CompanyTextBox.Text = profile.Company ?? "";
                    PhoneTextBox.Text = profile.Phone ?? "";
                    
                    // Update view mode display
                    UpdateViewModeDisplay(profile);
                    
                    // Update display name if we have first/last name
                    if (!string.IsNullOrEmpty(profile.FirstName) || !string.IsNullOrEmpty(profile.LastName))
                    {
                        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            DisplayNameText.Text = fullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to load profile fields: {0}", ex.Message);
            }
        }

        private void UpdateViewModeDisplay(Services.Backend.Models.UserProfile profile)
        {
            var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
            ViewFullName.Text = string.IsNullOrEmpty(fullName) ? "Not set" : fullName;
            ViewJobTitle.Text = profile.JobTitle ?? "";
            ViewCompany.Text = profile.Company ?? "";
            ViewPhone.Text = profile.Phone ?? "";
            
            // Hide empty fields in view mode
            ViewJobTitle.Visibility = string.IsNullOrEmpty(profile.JobTitle) ? Visibility.Collapsed : Visibility.Visible;
            ViewCompany.Visibility = string.IsNullOrEmpty(profile.Company) ? Visibility.Collapsed : Visibility.Visible;
            ViewPhone.Visibility = string.IsNullOrEmpty(profile.Phone) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            // Switch to edit mode
            ProfileViewMode.Visibility = Visibility.Collapsed;
            ProfileEditMode.Visibility = Visibility.Visible;
            EditProfileButton.Visibility = Visibility.Collapsed;
            EditModeButtons.Visibility = Visibility.Visible;
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            // Reload fields and switch back to view mode
            LoadProfileFields();
            ProfileViewMode.Visibility = Visibility.Visible;
            ProfileEditMode.Visibility = Visibility.Collapsed;
            EditProfileButton.Visibility = Visibility.Visible;
            EditModeButtons.Visibility = Visibility.Collapsed;
        }

        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveProfileButton.IsEnabled = false;
                SaveProfileButton.Content = "Saving...";

                var profile = SupabaseService.Instance.CurrentProfile;
                var userId = SupabaseService.Instance.CurrentUser?.Id;

                if (profile == null || userId == null)
                {
                    NotificationManager.Instance.ShowError("Error", "Not signed in. Please sign in again.");
                    return;
                }

                // Update profile fields using targeted updates
                var client = GetSupabaseClient();
                if (client == null)
                {
                    NotificationManager.Instance.ShowError("Error", "Unable to connect to server.");
                    return;
                }

                // Build display name from first and last name
                var firstName = FirstNameTextBox.Text.Trim();
                var lastName = LastNameTextBox.Text.Trim();
                var displayName = $"{firstName} {lastName}".Trim();
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = profile.Email?.Split('@')[0] ?? "User";
                }

                await client.From<Services.Backend.Models.UserProfile>()
                    .Where(p => p.Id == userId)
                    .Set(p => p.FirstName!, firstName)
                    .Set(p => p.LastName!, lastName)
                    .Set(p => p.DisplayName!, displayName)
                    .Set(p => p.JobTitle!, JobTitleTextBox.Text.Trim())
                    .Set(p => p.Company!, CompanyTextBox.Text.Trim())
                    .Set(p => p.Phone!, PhoneTextBox.Text.Trim())
                    .Set(p => p.UpdatedAt, DateTime.UtcNow)
                    .Update();

                // Update local profile copy
                profile.FirstName = firstName;
                profile.LastName = lastName;
                profile.DisplayName = displayName;
                profile.JobTitle = JobTitleTextBox.Text.Trim();
                profile.Company = CompanyTextBox.Text.Trim();
                profile.Phone = PhoneTextBox.Text.Trim();

                // Update display name
                var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    DisplayNameText.Text = fullName;
                    
                    // Update initials
                    if (!string.IsNullOrEmpty(profile.FirstName) && !string.IsNullOrEmpty(profile.LastName))
                    {
                        AvatarInitials.Text = $"{profile.FirstName[0]}{profile.LastName[0]}".ToUpper();
                    }
                }

                // Update view mode display
                UpdateViewModeDisplay(profile);
                
                // Switch back to view mode
                ProfileViewMode.Visibility = Visibility.Visible;
                ProfileEditMode.Visibility = Visibility.Collapsed;
                EditProfileButton.Visibility = Visibility.Visible;
                EditModeButtons.Visibility = Visibility.Collapsed;

                NotificationManager.Instance.ShowSuccess("Profile Saved", "Your profile has been updated.");
                AccountInfoChanged?.Invoke(this, EventArgs.Empty);
                DataMessenger.SendRefresh(DataChangeType.UserProfile);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to save profile");
                NotificationManager.Instance.ShowError("Error", "Failed to save profile. Please try again.");
            }
            finally
            {
                SaveProfileButton.IsEnabled = true;
            }
        }

        private Supabase.Client? GetSupabaseClient()
        {
            // Access the internal client via reflection or add a public accessor
            // For now, we'll use a workaround
            var field = typeof(SupabaseService).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(SupabaseService.Instance) as Supabase.Client;
        }

        #region Event Handlers

        private void AvatarOverlay_MouseEnter(object sender, MouseEventArgs e)
        {
            AvatarEditOverlay.Opacity = 1;
        }

        private void AvatarOverlay_MouseLeave(object sender, MouseEventArgs e)
        {
            AvatarEditOverlay.Opacity = 0;
        }

        private async void ChangeAvatar_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Profile Picture",
                    Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    var filePath = dialog.FileName;
                    
                    // Show loading state
                    AvatarEditOverlay.Opacity = 0.5;

                    var result = await SupabaseService.Instance.UploadAvatarAsync(filePath);

                    if (result.Success)
                    {
                        NotificationManager.Instance.ShowSuccess("Avatar Updated", "Your profile picture has been updated.");
                        LoadAvatarImage();
                        AccountInfoChanged?.Invoke(this, EventArgs.Empty);
                        
                        // Notify main window to refresh avatar
                        DataMessenger.SendRefresh(DataChangeType.UserProfile);
                    }
                    else
                    {
                        NotificationManager.Instance.ShowError("Upload Failed", result.Error ?? "Could not upload image.");
                    }

                    AvatarEditOverlay.Opacity = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error changing avatar");
                NotificationManager.Instance.ShowError("Error", "Failed to change profile picture.");
            }
        }

        private async void ChangeEmail_Click(object sender, RoutedEventArgs e)
        {
            var currentEmail = EmailText.Text;
            var parentWindow = Window.GetWindow(this);
            
            var newEmail = await InputDialog.ShowEmailAsync(
                "Change Email",
                "Enter your new email address:",
                currentEmail,
                parentWindow);

            if (newEmail != null && newEmail != currentEmail)
            {
                try
                {
                    var result = await SupabaseService.Instance.UpdateEmailAsync(newEmail);

                    if (result.Success)
                    {
                        await ConfirmationDialog.ShowSuccessAsync(
                            "Email Update Requested",
                            "A confirmation email has been sent to your new address.\n\nPlease check your inbox to complete the change.",
                            parentWindow);
                    }
                    else
                    {
                        await ConfirmationDialog.ShowErrorAsync(
                            "Update Failed",
                            result.Error ?? "Could not update email. Please try again.",
                            parentWindow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Error changing email");
                    await ConfirmationDialog.ShowErrorAsync(
                        "Error",
                        "Failed to update email. Please try again.",
                        parentWindow);
                }
            }
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailText.Text;
            var parentWindow = Window.GetWindow(this);
            
            if (string.IsNullOrEmpty(email) || email == "Not configured")
            {
                await ConfirmationDialog.ShowWarningAsync(
                    "No Email",
                    "Please configure your email address first.",
                    parentWindow);
                return;
            }

            // Check if Supabase is initialized and user is signed in
            if (!SupabaseService.Instance.IsSignedIn)
            {
                await ConfirmationDialog.ShowWarningAsync(
                    "Not Signed In",
                    "You need to be signed in to your cloud account to reset your password.\n\n" +
                    "Please sign in from the login screen first.",
                    parentWindow);
                return;
            }

            var confirmed = await ConfirmationDialog.ShowAsync(
                "Reset Password",
                $"We'll send a password reset link to:\n\n{email}\n\n" +
                "You'll receive an email with instructions to create a new password.\n\n" +
                "Continue?",
                "Send Reset Link",
                "Cancel",
                DialogIcon.Question,
                parentWindow);

            if (confirmed)
            {
                try
                {
                    _logger.Info("Requesting password reset for: {0}", email);
                    var resetResult = await SupabaseService.Instance.ResetPasswordAsync(email);

                    if (resetResult.Success)
                    {
                        await ConfirmationDialog.ShowSuccessAsync(
                            "Password Reset Sent",
                            $"If an account exists for {email}, you'll receive an email shortly.\n\n" +
                            "Please check your inbox (and spam folder) for the reset link.\n\n" +
                            "Note: The link will expire in 24 hours.",
                            parentWindow);
                    }
                    else
                    {
                        _logger.Warn("Password reset failed: {0}", resetResult.Error);
                        await ConfirmationDialog.ShowErrorAsync(
                            "Reset Failed",
                            resetResult.Error ?? "Could not send reset email. Please try again.",
                            parentWindow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Error requesting password reset");
                    await ConfirmationDialog.ShowErrorAsync(
                        "Error",
                        "Failed to send password reset email.\n\n" +
                        "Please check your internet connection and try again.",
                        parentWindow);
                }
            }
        }

        private void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            var dialog = new UpgradePlanDialog 
            { 
                Owner = parentWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == true && dialog.WasUpgraded)
            {
                LoadAccountInfo();
                AccountInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ActivationCode_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            var dialog = new ActivationCodeDialog 
            { 
                Owner = parentWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == true && dialog.WasActivated)
            {
                LoadAccountInfo();
                AccountInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Helpers

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}

