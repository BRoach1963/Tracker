using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public static EmptyStringToVisibilityConverter Instance { get; } = new EmptyStringToVisibilityConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private EmptyStringToVisibilityConverter()
        {
        }

        public object Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
