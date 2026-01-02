using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts PrepItemPriority to a corresponding brush color for visual indication.
    /// </summary>
    public class PrepItemPriorityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush CriticalBrush = new(Color.FromRgb(0xF4, 0x43, 0x36)); // Red
        private static readonly SolidColorBrush HighBrush = new(Color.FromRgb(0xFF, 0x98, 0x00));     // Orange
        private static readonly SolidColorBrush NormalBrush = new(Color.FromRgb(0x21, 0x96, 0xF3));   // Blue
        private static readonly SolidColorBrush LowBrush = new(Color.FromRgb(0x9E, 0x9E, 0x9E));      // Gray
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PrepItemPriority priority)
            {
                return priority switch
                {
                    PrepItemPriority.Critical => CriticalBrush,
                    PrepItemPriority.High => HighBrush,
                    PrepItemPriority.Normal => NormalBrush,
                    PrepItemPriority.Low => LowBrush,
                    _ => NormalBrush
                };
            }
            return NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean to an opacity value (true = dimmed, false = full opacity).
    /// Used to dim items that have been added to the agenda.
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public static readonly BoolToOpacityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAddedToAgenda)
            {
                return isAddedToAgenda ? 0.6 : 1.0;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse of BoolToVisibilityConverter - returns Visible when false.
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBoolToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
