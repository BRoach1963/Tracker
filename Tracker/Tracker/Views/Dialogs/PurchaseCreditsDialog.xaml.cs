using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.AI;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for purchasing additional AI credits.
    /// Currently UI-only - payment processing will be added with Square integration.
    /// </summary>
    public partial class PurchaseCreditsDialog : Window
    {
        private readonly ILogger _logger = LoggingManager.GetComponentLogger("PurchaseCredits");
        private CreditPack? _selectedPack;

        public PurchaseCreditsDialog()
        {
            InitializeComponent();
        }

        #region Credit Pack Selection

        private void StarterPack_Click(object sender, MouseButtonEventArgs e)
        {
            SelectPack(CreditPack.Starter);
        }

        private void ValuePack_Click(object sender, MouseButtonEventArgs e)
        {
            SelectPack(CreditPack.Value);
        }

        private void PowerPack_Click(object sender, MouseButtonEventArgs e)
        {
            SelectPack(CreditPack.Power);
        }

        private void SelectPack(CreditPack pack)
        {
            _selectedPack = pack;
            PurchaseButton.IsEnabled = true;

            // Update visual selection
            var defaultBorder = (Brush)FindResource("BorderBrush");
            var primaryBorder = (Brush)FindResource("PrimaryBrush");

            StarterPackBorder.BorderBrush = pack == CreditPack.Starter ? primaryBorder : defaultBorder;
            StarterPackBorder.BorderThickness = pack == CreditPack.Starter ? new Thickness(2) : new Thickness(1);
            
            ValuePackBorder.BorderBrush = primaryBorder; // Always highlighted as best value
            ValuePackBorder.BorderThickness = pack == CreditPack.Value ? new Thickness(3) : new Thickness(2);
            
            PowerPackBorder.BorderBrush = pack == CreditPack.Power ? primaryBorder : defaultBorder;
            PowerPackBorder.BorderThickness = pack == CreditPack.Power ? new Thickness(2) : new Thickness(1);
        }

        #endregion

        #region Button Handlers

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Purchase_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPack == null) return;

            var credits = _selectedPack switch
            {
                CreditPack.Starter => 50,
                CreditPack.Value => 250,
                CreditPack.Power => 1000,
                _ => 0
            };

            var price = _selectedPack switch
            {
                CreditPack.Starter => "$1.49",
                CreditPack.Value => "$5.99",
                CreditPack.Power => "$19.99",
                _ => "$0"
            };

            // TODO: Integrate with Square for actual payment processing
            // For now, show a message and simulate the purchase
            var result = MessageBox.Show(
                $"Payment processing is not yet available.\n\n" +
                $"When Square integration is complete, you will be charged {price} for {credits} credits.\n\n" +
                $"Would you like to simulate this purchase for testing?",
                "Payment Not Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // Simulate the purchase for testing
                AIUsageTracker.Instance.AddPurchasedCredits(credits);
                
                _logger.Info("Simulated purchase of {0} credits ({1})", credits, price);
                
                NotificationManager.Instance.ShowSuccess(
                    "Credits Added",
                    $"{credits} AI credits have been added to your account.");

                DialogResult = true;
                Close();
            }
        }

        #endregion

        #region Helper Types

        private enum CreditPack
        {
            Starter,
            Value,
            Power
        }

        #endregion
    }
}
