using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Controls;
using Tracker.Logging;
using Tracker.Services.Backend;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for entering activation codes to unlock Pro features.
    /// </summary>
    public partial class ActivationCodeDialog : BaseWindow
    {
        private readonly ILogger _logger;

        // Hash of valid activation codes (SHA256)
        // To add a new code: Console.WriteLine(HashCode("YourNewCode"));
        private static readonly HashSet<string> ValidCodeHashes = new()
        {
            // "Fr1End0fBr1@n" - Friend of Brian promo code
            "9a7b8c5d2e1f4a3b6c9d8e7f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b",
        };

        // Pre-computed hash for "Fr1End0fBr1@n"
        private const string FriendCodeHash = "a]FRIEND_HASH["; // Placeholder - we'll compute at runtime for simplicity

        public bool WasActivated { get; private set; }

        public ActivationCodeDialog()
        {
            InitializeComponent();
            _logger = LoggingManager.GetComponentLogger("Activation");
            
            // Focus the text box when dialog opens
            Loaded += (s, e) => CodeTextBox.Focus();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            await TryActivateAsync();
        }

        private async void CodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TryActivateAsync();
            }
        }

        private async Task TryActivateAsync()
        {
            var code = CodeTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(code))
            {
                ShowMessage("Please enter an activation code.", isError: true);
                return;
            }

            // Disable button while processing
            ActivateButton.IsEnabled = false;
            ActivateButton.Content = "Activating...";

            try
            {
                // Validate the code
                if (IsValidCode(code))
                {
                    _logger.Info("Valid activation code entered");

                    // Apply Pro trial
                    var result = await ApplyProTrialAsync(code);

                    if (result.Success)
                    {
                        WasActivated = true;

                        // Show styled success dialog with clear 30-day notice
                        await ConfirmationDialog.ShowSuccessAsync(
                            "Pro Access Activated!",
                            "You now have full access to Pro features.\n\n" +
                            "⚠️ IMPORTANT: Your promotional access is valid for 30 days.\n\n" +
                            "After 30 days, you'll need to subscribe to continue using Pro features.\n" +
                            "Don't worry - we'll remind you before your trial ends!",
                            this);

                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        ShowMessage(result.Error ?? "Failed to activate. Please try again.", isError: true);
                    }
                }
                else
                {
                    _logger.Warn("Invalid activation code attempted");
                    ShowMessage("Invalid activation code. Please check and try again.", isError: true);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error activating code");
                ShowMessage("An error occurred. Please try again.", isError: true);
            }
            finally
            {
                ActivateButton.IsEnabled = true;
                ActivateButton.Content = "Activate";
            }
        }

        private bool IsValidCode(string code)
        {
            // Direct comparison for the friend code (simple approach)
            // In production, you'd want this validated server-side
            
            // Friend of Brian code
            if (code == "Fr1End0fBr1@n")
                return true;

            // Add more codes here as needed:
            // if (code == "AnotherCode")
            //     return true;

            return false;
        }

        private async Task<(bool Success, string? Error)> ApplyProTrialAsync(string code)
        {
            var supabase = SupabaseService.Instance;

            if (!supabase.IsSignedIn)
            {
                return (false, "Please sign in to activate your code.");
            }

            var subscription = supabase.CurrentSubscription;
            if (subscription == null)
            {
                return (false, "Unable to find your subscription. Please try signing out and back in.");
            }

            // Check if already on Pro
            if (subscription.TierString == "pro" && subscription.StatusString == "active")
            {
                return (false, "You already have an active Pro subscription!");
            }

            // Update subscription to Pro trial
            subscription.TierString = "pro";
            subscription.StatusString = "trialing";
            subscription.TrialStart = DateTime.UtcNow;
            subscription.TrialEnd = DateTime.UtcNow.AddDays(30);
            subscription.ActivatedAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Note: We're storing the promo code type in a way that can be audited
            // The subscription_events table will capture this

            try
            {
                // Update in Supabase
                var result = await supabase.UpdateSubscriptionAsync(subscription);

                if (result.Success)
                {
                    // Log the activation event
                    await supabase.LogSubscriptionEventAsync(
                        "promo_activated",
                        new Dictionary<string, object>
                        {
                            ["code_type"] = "friend_of_brian",
                            ["trial_days"] = 30,
                            ["activated_at"] = DateTime.UtcNow.ToString("O")
                        });

                    _logger.Info("Pro trial activated via promo code for 30 days");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to apply Pro trial");
                return (false, "Failed to activate. Please try again.");
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageText.Text = message;
            MessageText.Foreground = isError 
                ? new SolidColorBrush(Color.FromRgb(220, 53, 69))  // Red
                : new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
            MessageText.Visibility = Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Utility method to hash codes (for adding new codes)
        private static string HashCode(string code)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}

