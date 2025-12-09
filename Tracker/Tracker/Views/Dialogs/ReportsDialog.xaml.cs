using System.Windows;
using System.Windows.Input;
using Tracker.Controls;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ReportsDialog.xaml
    /// </summary>
    public partial class ReportsDialog : BaseWindow
    {
        public ReportsDialog(object dataContext)
        {
            DataContext = dataContext;
            InitializeComponent();
        }

        private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }
    }
}

