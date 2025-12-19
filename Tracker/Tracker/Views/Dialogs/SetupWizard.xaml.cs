using System.Windows;
using System.Windows.Controls;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    public partial class SetupWizard : BaseWindow
    {
        public SetupWizard()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                UpdateAccountModeButtons();
                
                if (DataContext is SetupWizardViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.IsCreatingAccount))
                        {
                            UpdateAccountModeButtons();
                        }
                    };
                }
            };
        }

        private void UpdateAccountModeButtons()
        {
            if (DataContext is not SetupWizardViewModel vm) return;
            if (CreateAccountModeButton == null || SignInModeButton == null) return;
            
            var accentBrush = FindResource("AccentBrush") as System.Windows.Media.Brush;
            var backgroundBrush = FindResource("BackgroundBrush") as System.Windows.Media.Brush;
            var surfaceBrush = FindResource("SurfaceBrush") as System.Windows.Media.Brush;
            var hintBrush = FindResource("HintTextBrush") as System.Windows.Media.Brush;
            
            if (vm.IsCreatingAccount)
            {
                CreateAccountModeButton.Background = accentBrush;
                CreateAccountModeButton.Foreground = backgroundBrush;
                CreateAccountModeButton.FontWeight = FontWeights.SemiBold;
                
                SignInModeButton.Background = surfaceBrush;
                SignInModeButton.Foreground = hintBrush;
                SignInModeButton.FontWeight = FontWeights.Normal;
            }
            else
            {
                SignInModeButton.Background = accentBrush;
                SignInModeButton.Foreground = backgroundBrush;
                SignInModeButton.FontWeight = FontWeights.SemiBold;
                
                CreateAccountModeButton.Background = surfaceBrush;
                CreateAccountModeButton.Foreground = hintBrush;
                CreateAccountModeButton.FontWeight = FontWeights.Normal;
            }
        }

        private void CreateAccountModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SetupWizardViewModel vm)
            {
                vm.IsCreatingAccount = true;
            }
        }

        private void SignInModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SetupWizardViewModel vm)
            {
                vm.IsCreatingAccount = false;
            }
        }

        /// <summary>
        /// Handles password box changes for account password.
        /// PasswordBox doesn't support binding for security reasons.
        /// </summary>
        private void AccountPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb)
            {
                vm.AccountPassword = pb.Password;
            }
        }

        /// <summary>
        /// Handles password box changes for account password confirmation.
        /// </summary>
        private void AccountPasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SetupWizardViewModel vm && sender is PasswordBox pb)
            {
                vm.AccountPasswordConfirm = pb.Password;
            }
        }
    }
}

