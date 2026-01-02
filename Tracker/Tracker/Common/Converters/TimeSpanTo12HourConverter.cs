using System;
using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a TimeSpan to a 12-hour format string with AM/PM.
    /// </summary>
    public class TimeSpanTo12HourConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for use in XAML bindings.
        /// </summary>
        public static readonly TimeSpanTo12HourConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                // Create a DateTime with the TimeSpan to use DateTime formatting
                var dateTime = DateTime.Today.Add(timeSpan);
                return dateTime.ToString("h:mm tt", culture);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
