using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts enum values to human-readable display strings.
    /// Splits PascalCase into words.
    /// </summary>
    public class EnumToDisplayConverter : IValueConverter
    {
        public static readonly EnumToDisplayConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var enumString = value.ToString() ?? string.Empty;
            
            // Split PascalCase into words
            var display = Regex.Replace(enumString, "([a-z])([A-Z])", "$1 $2");
            
            return display;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

