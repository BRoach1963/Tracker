using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

        private void Expland_Details_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button == null) return;

            // Find the DataGridRow containing this ToggleButton
            var row = FindVisualParent<DataGridRow>(button);
            if (row == null) return;

            // Toggle the RowDetailsVisibility
            if (button.IsChecked == true)
            {
                row.DetailsVisibility = Visibility.Visible;
            }
            else
            {
                row.DetailsVisibility = Visibility.Collapsed;
            }
        }

        private T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            // Traverse the visual tree to find the parent
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                // Get the parent, knowing child is not null here
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
