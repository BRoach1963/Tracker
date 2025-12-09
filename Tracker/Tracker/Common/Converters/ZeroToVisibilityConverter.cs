using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts an integer count to Visibility.
    /// Returns Visible when the count is 0, Collapsed otherwise.
    /// Useful for showing "empty state" messages when a collection is empty.
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for use in XAML bindings.
        /// </summary>
        public static readonly ZeroToVisibilityConverter Instance = new();

        /// <summary>
        /// Converts an integer to Visibility.
        /// </summary>
        /// <param name="value">The integer count value.</param>
        /// <param name="targetType">The target type (Visibility).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>Visible if count is 0, Collapsed otherwise.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Not implemented - one-way converter only.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

