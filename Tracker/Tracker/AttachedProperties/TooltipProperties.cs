using System.Windows;
using System.Windows.Controls;
using Tracker.Controls;

namespace Tracker.AttachedProperties
{
    /// <summary>
    /// Attached properties for simplified tooltip management throughout the application.
    /// Automatically creates styled TrackerToolTip instances.
    /// </summary>
    public static class TooltipProperties
    {
        #region Text Property
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
                "Text",
                typeof(string),
                typeof(TooltipProperties),
                new PropertyMetadata(null, OnTooltipPropertiesChanged));

        public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
        public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);
        #endregion

        #region Title Property
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.RegisterAttached(
                "Title",
                typeof(string),
                typeof(TooltipProperties),
                new PropertyMetadata(null, OnTooltipPropertiesChanged));

        public static string GetTitle(DependencyObject obj) => (string)obj.GetValue(TitleProperty);
        public static void SetTitle(DependencyObject obj, string value) => obj.SetValue(TitleProperty, value);
        #endregion

        #region Shortcut Property
        public static readonly DependencyProperty ShortcutProperty =
            DependencyProperty.RegisterAttached(
                "Shortcut",
                typeof(string),
                typeof(TooltipProperties),
                new PropertyMetadata(null, OnTooltipPropertiesChanged));

        public static string GetShortcut(DependencyObject obj) => (string)obj.GetValue(ShortcutProperty);
        public static void SetShortcut(DependencyObject obj, string value) => obj.SetValue(ShortcutProperty, value);
        #endregion

        private static void OnTooltipPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                UpdateTooltip(element);
            }
        }

        private static void UpdateTooltip(FrameworkElement element)
        {
            var text = GetText(element);
            
            if (string.IsNullOrWhiteSpace(text))
            {
                element.ToolTip = null;
                return;
            }

            var title = GetTitle(element);
            var shortcut = GetShortcut(element);

            // Create the TrackerToolTip content
            var tooltipContent = new TrackerToolTip
            {
                ToolTipText = text,
                Title = title ?? string.Empty,
                Shortcut = shortcut ?? string.Empty
            };

            // Create styled ToolTip container
            var tooltip = new ToolTip
            {
                Content = tooltipContent,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                HasDropShadow = true
            };

            // Apply the styled tooltip
            element.ToolTip = tooltip;
        }
    }
}

