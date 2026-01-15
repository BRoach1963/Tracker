using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    public partial class LoginDialog : BaseWindow
    {
        private bool _isPasswordVisible;
        private bool _isConfirmPasswordVisible;
        private bool _isSyncing; // Prevent infinite loops

        public LoginDialog()
        {
            InitializeComponent();
            
            // Set initial button styles and handle mode changes
            Loaded += (s, e) =>
            {
                UpdateModeButtonStyles();
                
                if (DataContext is LoginDialogViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.IsCreateAccountMode))
                        {
                            UpdateModeButtonStyles();
                        }
                    };
                }
            };
        }

        private void UpdateModeButtonStyles()
        {
            if (DataContext is not LoginDialogViewModel vm) return;
            
            var accentBrush = FindResource("AccentBrush") as System.Windows.Media.Brush;
            var backgroundBrush = FindResource("BackgroundBrush") as System.Windows.Media.Brush;
            var surfaceBrush = FindResource("SurfaceBrush") as System.Windows.Media.Brush;
            var foregroundBrush = FindResource("ForegroundBrush") as System.Windows.Media.Brush;
            var hintBrush = FindResource("HintTextBrush") as System.Windows.Media.Brush;
            
            if (vm.IsCreateAccountMode)
            {
                // Create Account is selected
                CreateAccountButton.Background = accentBrush;
                CreateAccountButton.Foreground = backgroundBrush;
                CreateAccountButton.FontWeight = FontWeights.SemiBold;
                
                SignInButton.Background = surfaceBrush;
                SignInButton.Foreground = hintBrush;
                SignInButton.FontWeight = FontWeights.Normal;
            }
            else
            {
                // Sign In is selected
                SignInButton.Background = accentBrush;
                SignInButton.Foreground = backgroundBrush;
                SignInButton.FontWeight = FontWeights.SemiBold;
                
                CreateAccountButton.Background = surfaceBrush;
                CreateAccountButton.Foreground = hintBrush;
                CreateAccountButton.FontWeight = FontWeights.Normal;
            }
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginDialogViewModel vm)
            {
                vm.IsCreateAccountMode = false;
            }
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginDialogViewModel vm)
            {
                vm.IsCreateAccountMode = true;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginDialogViewModel vm && sender is PasswordBox pb)
            {
                _isSyncing = true;
                vm.Password = pb.Password;
                PasswordTextBox.Text = pb.Password;
                _isSyncing = false;
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginDialogViewModel vm && sender is TextBox tb)
            {
                _isSyncing = true;
                vm.Password = tb.Text;
                PasswordBox.Password = tb.Text;
                _isSyncing = false;
            }
        }

        private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            
            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                ShowPasswordButton.Content = "??";
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                ShowPasswordButton.Content = "??";
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginDialogViewModel vm && sender is PasswordBox pb)
            {
                _isSyncing = true;
                vm.ConfirmPassword = pb.Password;
                ConfirmPasswordTextBox.Text = pb.Password;
                _isSyncing = false;
            }
        }

        private void ConfirmPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginDialogViewModel vm && sender is TextBox tb)
            {
                _isSyncing = true;
                vm.ConfirmPassword = tb.Text;
                ConfirmPasswordBox.Password = tb.Text;
                _isSyncing = false;
            }
        }

        private void ShowConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            
            if (_isConfirmPasswordVisible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ShowConfirmPasswordButton.Content = "??";
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ShowConfirmPasswordButton.Content = "??";
            }
        }

        /// <summary>
        /// Handle Enter key on password field to submit the form.
        /// </summary>
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is LoginDialogViewModel vm)
            {
                if (vm.IsCreateAccountMode)
                {
                    // In create account mode, move focus to confirm password
                    ConfirmPasswordBox.Focus();
                }
                else
                {
                    // In sign-in mode, execute the sign-in command
                    if (vm.SignInCommand.CanExecute(null))
                    {
                        vm.SignInCommand.Execute(null);
                    }
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle Enter key on confirm password field to submit create account.
        /// </summary>
        private void ConfirmPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is LoginDialogViewModel vm)
            {
                if (vm.CreateAccountCommand.CanExecute(null))
                {
                    vm.CreateAccountCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle Exit Application button click - shuts down the application.
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
