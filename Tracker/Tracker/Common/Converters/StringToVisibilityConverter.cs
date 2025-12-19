using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a string to Visibility. 
    /// Non-empty string = Visible, Empty/null = Collapsed.
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

