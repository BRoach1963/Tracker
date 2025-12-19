using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts an enum value to Visibility.
    /// When value equals the ConverterParameter, returns Collapsed.
    /// Otherwise returns Visible.
    /// Useful for showing a "Clear Filter" button when filter is not the default.
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public static readonly EnumToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // If value equals parameter, hide the element (it's the default value)
            return value.Equals(parameter) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

