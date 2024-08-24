using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Command;

namespace Tracker.AttachedProperties
{
    public static  class TabControlExtensions
    {
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.RegisterAttached(
                "IsCollapsed",
                typeof(bool),
                typeof(TabControlExtensions),
                new PropertyMetadata(false));

        public static bool GetIsCollapsed(UIElement element)
        {
            return (bool)element.GetValue(IsCollapsedProperty);
        }

        public static void SetIsCollapsed(UIElement element, bool value)
        {
            element.SetValue(IsCollapsedProperty, value);
        }

        public static ICommand ToggleCollapseCommand { get; } = new TrackerCommand(ToggleCollapse);

        private static void ToggleCollapse(object? tabControl)
        {
            if (tabControl is TabControl control)
            {
                bool isCollapsed = GetIsCollapsed(control);
                SetIsCollapsed(control, !isCollapsed);
            }
        }
    }
}
