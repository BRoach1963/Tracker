using System.Windows;
using System.Windows.Media;

namespace Tracker.Helpers
{
    public static class TrackerVisualTreeHelper
    {

        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
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
