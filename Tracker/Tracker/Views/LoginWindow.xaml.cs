using System.Windows;
using System.Windows.Input;
using Tracker.ViewModels;

namespace Tracker.Views;

/// <summary>
/// Modern login window with professional split-panel design.
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginWindowViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();
        _viewModel = new LoginWindowViewModel(OnLoginCompleted);
        DataContext = _viewModel;
        
        // Focus email on load
        Loaded += (s, e) => EmailTextBox.Focus();
    }

    /// <summary>
    /// Gets whether login was successful.
    /// </summary>
    public bool LoginSuccessful { get; private set; }

    /// <summary>
    /// Callback when login completes.
    /// </summary>
    private void OnLoginCompleted()
    {
        LoginSuccessful = _viewModel.LoginSuccessful;
        Close();
    }

    #region Window Controls

    private void BrandingPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LoginSuccessful = false;
        Close();
    }

    #endregion

    #region Password Handling

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.Password = PasswordBox.Password;
    }

    private bool _isPasswordVisible = false;

    private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        
        if (_isPasswordVisible)
        {
            // Show password in TextBox
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordTextBox.Focus();
            PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            
            // Update eye icon to "hide" state
            EyeIcon.Data = System.Windows.Media.Geometry.Parse(
                "M12,7c2.76,0 5,2.24 5,5 0,0.65 -0.13,1.26 -0.36,1.83l2.92,2.92c1.51,-1.26 2.7,-2.89 3.43,-4.75 -1.73,-4.39 -6,-7.5 -11,-7.5 -1.4,0 -2.74,0.25 -3.98,0.7l2.16,2.16C10.74,7.13 11.35,7 12,7M2,4.27l2.28,2.28 0.46,0.46C3.08,8.3 1.78,10.02 1,12c1.73,4.39 6,7.5 11,7.5 1.55,0 3.03,-0.3 4.38,-0.84l0.42,0.42L19.73,22 21,20.73 3.27,3 2,4.27M7.53,9.8l1.55,1.55c-0.05,0.21 -0.08,0.43 -0.08,0.65 0,1.66 1.34,3 3,3 0.22,0 0.44,-0.03 0.65,-0.08l1.55,1.55c-0.67,0.33 -1.41,0.53 -2.2,0.53 -2.76,0 -5,-2.24 -5,-5 0,-0.79 0.2,-1.53 0.53,-2.2m4.31,-0.78l3.15,3.15 0.02,-0.16c0,-1.66 -1.34,-3 -3,-3l-0.17,0.01z");
            EyeIcon.Fill = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C7A450")!);
            ShowPasswordButton.ToolTip = "Hide password";
        }
        else
        {
            // Hide password in PasswordBox
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
            
            // Update eye icon to "show" state
            EyeIcon.Data = System.Windows.Media.Geometry.Parse(
                "M12,4.5C7,4.5 2.73,7.61 1,12c1.73,4.39 6,7.5 11,7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5M12,17c-2.76,0-5-2.24-5-5s2.24-5 5-5 5,2.24 5,5-2.24,5-5,5m0-8c-1.66,0-3,1.34-3,3s1.34,3 3,3 3-1.34 3-3-1.34-3-3-3");
            EyeIcon.Fill = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")!);
            ShowPasswordButton.ToolTip = "Show password";
        }
    }

    private void PasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _viewModel.Password = PasswordTextBox.Text;
    }

    #endregion

    #region Keyboard Handling

    private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasswordBox.Focus();
            e.Handled = true;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Trigger sign in
            if (_viewModel.SignInCommand.CanExecute(null))
            {
                _viewModel.SignInCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    #endregion
}
