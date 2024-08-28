using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Helpers;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for OkrsControl.xaml
    /// </summary>
    public partial class OkrsControl : UserControl
    {
        public OkrsControl()
        {
            InitializeComponent();
        }

        private void Expand_Details_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button == null) return;

            // Find the DataGridRow containing this ToggleButton
            var row = TrackerVisualTreeHelper.FindVisualParent<DataGridRow>(button);
            if (row == null) return;

            // Toggle the RowDetailsVisibility
            row.DetailsVisibility = button.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
