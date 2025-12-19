using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a percentage (0-100) to a proportional width for progress bars.
    /// Returns a string like "50%" for use with Width binding.
    /// </summary>
    public class PercentToWidthConverter : IValueConverter
    {
        public static PercentToWidthConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;
            
            if (value is int intVal)
                percent = intVal;
            else if (value is double doubleVal)
                percent = doubleVal;
            else if (value is decimal decVal)
                percent = (double)decVal;
            else if (double.TryParse(value?.ToString(), out var parsed))
                percent = parsed;

            // Clamp to 0-100
            percent = Math.Max(0, Math.Min(100, percent));

            // Return as percentage string for width
            return $"{percent}%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

