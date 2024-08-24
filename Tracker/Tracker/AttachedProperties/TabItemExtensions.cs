using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Tracker.AttachedProperties
{
    public static class TabItemExtensions
    {
        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.RegisterAttached(
                "IconData",
                typeof(Geometry),
                typeof(TabItemExtensions),
                new PropertyMetadata(null));

        public static void SetIconData(UIElement element, Geometry value)
        {
            element.SetValue(IconDataProperty, value);
        }

        public static Geometry GetIconData(UIElement element)
        {
            return (Geometry)element.GetValue(IconDataProperty);
        }
    }
}
