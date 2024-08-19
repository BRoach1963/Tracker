using System.Windows.Data;
using System.Windows;

namespace Tracker.Common.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static BoolToVisibilityConverter Instance { get; } = new BoolToVisibilityConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private BoolToVisibilityConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class BoolToVisibilityHiddenConverter : IValueConverter
    {
        public static BoolToVisibilityHiddenConverter Instance { get; } = new BoolToVisibilityHiddenConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private BoolToVisibilityHiddenConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return (bool)value ? Visibility.Visible : Visibility.Hidden;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
