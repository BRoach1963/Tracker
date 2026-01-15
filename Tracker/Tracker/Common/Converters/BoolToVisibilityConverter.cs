using System.Windows.Data;
using System.Windows;

namespace Tracker.Common.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static BoolToVisibilityConverter Instance { get; } = new BoolToVisibilityConverter();

        /// <summary>
        /// Public parameterless constructor required for XAML resource instantiation.
        /// Prefer using the static Instance property when possible.
        /// </summary>
        public BoolToVisibilityConverter()
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

    /// <summary>
    /// Converts boolean values to Visibility, with inverted logic (true = Collapsed, false = Visible).
    /// </summary>
    public class ReverseBoolToVisibilityConverter : IValueConverter
    {
        public static ReverseBoolToVisibilityConverter Instance { get; } = new ReverseBoolToVisibilityConverter();

        private ReverseBoolToVisibilityConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts boolean to Z-Index (true = 10, false = 0).
    /// Used to bring selected content panels to the front.
    /// </summary>
    public class BoolToZIndexConverter : IValueConverter
    {
        public static BoolToZIndexConverter Instance { get; } = new BoolToZIndexConverter();

        private BoolToZIndexConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return 10;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
