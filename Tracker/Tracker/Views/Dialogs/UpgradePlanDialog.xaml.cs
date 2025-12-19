using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Controls;
using Tracker.Logging;
using Tracker.Services.Backend;
using Tracker.Services.Square;
using Tracker.Services.Subscription;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for selecting and upgrading subscription plans.
    /// </summary>
    public partial class UpgradePlanDialog : BaseWindow
    {
        private readonly ILogger _logger;
        private bool _isAnnual = false;
        private string? _selectedPlan = null;

        public bool WasUpgraded { get; private set; }
        public string? SelectedPlanId { get; private set; }

        public UpgradePlanDialog()
        {
            InitializeComponent();
            _logger = LoggingManager.GetComponentLogger("Upgrade");
            
            UpdatePricing();
            UpdateCurrentPlanNotice();
        }

        private void BillingToggle_Changed(object sender, RoutedEventArgs e)
        {
            // Guard against event firing during initialization
            if (AnnualToggle == null || StandardPrice == null) return;
            
            _isAnnual = AnnualToggle.IsChecked == true;
            UpdatePricing();
        }

        private void UpdatePricing()
        {
            // Guard against being called before controls are initialized
            if (StandardPrice == null || ProPrice == null) return;
            
            if (_isAnnual)
            {
                // Annual pricing
                StandardPrice.Text = "$99.99";
                StandardPeriod.Text = "/yr";
                StandardEffective.Text = "≈ $8.33/mo - Save $19.89!";
                StandardEffective.Visibility = Visibility.Visible;

                ProPrice.Text = "$199.99";
                ProPeriod.Text = "/yr";
                ProEffective.Text = "≈ $16.67/mo - Save $39.89!";
                ProEffective.Visibility = Visibility.Visible;
            }
            else
            {
                // Monthly pricing
                StandardPrice.Text = "$9.99";
                StandardPeriod.Text = "/mo";
                StandardEffective.Visibility = Visibility.Collapsed;

                ProPrice.Text = "$19.99";
                ProPeriod.Text = "/mo";
                ProEffective.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateCurrentPlanNotice()
        {
            var currentTier = SubscriptionService.Instance.CurrentTier;
            var subscription = SupabaseService.Instance.CurrentSubscription;

            if (currentTier == Common.Enums.SubscriptionTier.Pro)
            {
                CurrentPlanNotice.Text = "You're already on Pro! Manage your subscription in account settings.";
            }
            else if (currentTier == Common.Enums.SubscriptionTier.Standard)
            {
                CurrentPlanNotice.Text = "You're on Standard. Upgrade to Pro for unlimited team members and AI features!";
            }
            else if (currentTier == Common.Enums.SubscriptionTier.Internal)
            {
                CurrentPlanNotice.Text = "You have Internal/Beta access with all features unlocked.";
            }
            else if (subscription?.IsTrialing == true)
            {
                var daysLeft = subscription.DaysRemaining;
                CurrentPlanNotice.Text = $"Your trial ends in {daysLeft} days. Choose a plan to continue using Tracker.";
            }
            else
            {
                CurrentPlanNotice.Text = "You're on the Free plan. Upgrade to unlock more features!";
            }
        }

        private void StandardCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectPlan("standard");
        }

        private void ProCard_Click(object sender, MouseButtonEventArgs e)
        {
            SelectPlan("pro");
        }

        private void SelectStandard_Click(object sender, RoutedEventArgs e)
        {
            SelectPlan("standard");
            ProceedToCheckout();
        }

        private void SelectPro_Click(object sender, RoutedEventArgs e)
        {
            SelectPlan("pro");
            ProceedToCheckout();
        }

        private void SelectPlan(string plan)
        {
            _selectedPlan = plan;

            // Visual feedback
            var accentBrush = (Brush)FindResource("AccentBrush");
            var borderBrush = (Brush)FindResource("BorderBrush");

            if (plan == "standard")
            {
                StandardCard.BorderBrush = accentBrush;
                ProCard.BorderBrush = borderBrush;
            }
            else
            {
                StandardCard.BorderBrush = borderBrush;
                ProCard.BorderBrush = accentBrush;
            }
        }

        private async void ProceedToCheckout()
        {
            if (string.IsNullOrEmpty(_selectedPlan))
            {
                MessageBox.Show("Please select a plan.", "Select Plan", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var cadence = _isAnnual ? "annual" : "monthly";
            var planId = SquareConfig.GetPlanId(_selectedPlan, cadence);

            _logger.Info($"User selected plan: {planId}");

            // For now, show a message about checkout
            // In full implementation, this would open a browser to Square checkout
            var result = MessageBox.Show(
                $"You selected: {_selectedPlan.ToUpper()} ({cadence})\n\n" +
                $"Price: {(_isAnnual ? (_selectedPlan == "pro" ? "$199.99/year" : "$99.99/year") : (_selectedPlan == "pro" ? "$19.99/month" : "$9.99/month"))}\n\n" +
                "Continue to payment?",
                "Confirm Plan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Open Square checkout in browser
                    await OpenCheckoutAsync(planId);
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Failed to open checkout");
                    MessageBox.Show(
                        "Unable to open checkout. Please try again later.",
                        "Checkout Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async Task OpenCheckoutAsync(string planId)
        {
            var supabase = SupabaseService.Instance;
            
            if (!supabase.IsSignedIn || supabase.CurrentUser == null)
            {
                MessageBox.Show("Please sign in to upgrade your plan.", "Sign In Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Call the Edge Function to create checkout session
            var checkoutUrl = SquareConfig.CreateCheckoutEndpoint;
            
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabase.AccessToken}");

            var requestBody = new
            {
                plan_id = planId,
                user_id = supabase.CurrentUser.Id,
                return_url = "tracker://checkout-complete"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(checkoutUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.Info("Checkout session created successfully");
                    
                    // Parse response and handle
                    // For Square subscriptions created via API, the subscription is created directly
                    // Show success message
                    MessageBox.Show(
                        "Your subscription has been set up!\n\n" +
                        "Your account will be upgraded shortly. You may need to restart Tracker to see the changes.",
                        "Subscription Created",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    WasUpgraded = true;
                    SelectedPlanId = planId;
                    
                    // Refresh subscription data
                    await supabase.LoadUserDataAsync();
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _logger.Warn($"Checkout failed: {responseBody}");
                    
                    // For sandbox/testing, show helpful message
                    MessageBox.Show(
                        "Payment processing is not fully configured yet.\n\n" +
                        "This feature will be available soon!\n\n" +
                        "(Sandbox testing in progress)",
                        "Coming Soon",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error calling checkout endpoint");
                throw;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

