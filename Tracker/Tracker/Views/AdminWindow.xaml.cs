using System.Windows;
using System.Windows.Input;
using Tracker.Controls;
using Tracker.Helpers;
using Tracker.Services.Backend;
using Tracker.ViewModels;

namespace Tracker.Views
{
    public partial class AdminWindow : BaseWindow
    {
        private AdminWindowViewModel? _viewModel;

        public AdminWindow()
        {
            InitializeComponent();
            _viewModel = new AdminWindowViewModel();
            DataContext = _viewModel;
            
            // Validate admin access on window load
            Loaded += AdminWindow_Loaded;
            
            // Add F5 keyboard shortcut for execute
            KeyDown += AdminWindow_KeyDown;
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Double-check admin privileges (server-side validation)
            var profile = SupabaseService.Instance.CurrentProfile;
            if (profile?.IsAdmin != true)
            {
                MessageBoxHelper.Show(
                    "Access Denied\n\nYou do not have administrator privileges.\n\nThis window will now close.",
                    "Unauthorized Access",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private void AdminWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // F5 to execute query (like SSMS)
            if (e.Key == Key.F5 && _viewModel?.ExecuteQueryCommand.CanExecute(null) == true)
            {
                _viewModel.ExecuteQueryCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void TitleBar_CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitleBar_MaximizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void TitleBar_RestoreClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }
    }
}
