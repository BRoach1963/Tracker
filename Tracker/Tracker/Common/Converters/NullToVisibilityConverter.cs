using System.Windows.Data;
using System.Windows;

namespace Tracker.Common.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public static NullToVisibilityConverter Instance { get; } = new NullToVisibilityConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private NullToVisibilityConverter()
        {
        }

        public object Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    } 
}
