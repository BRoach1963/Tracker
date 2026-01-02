using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a boolean to a Thickness for border highlighting.
    /// True = 2px border, False = 0px border.
    /// </summary>
    public class BoolToThicknessConverter : IValueConverter
    {
        public static BoolToThicknessConverter Instance { get; } = new BoolToThicknessConverter();

        /// <summary>
        /// Public parameterless constructor required for XAML resource instantiation.
        /// Prefer using the static Instance property when possible.
        /// </summary>
        public BoolToThicknessConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
            {
                return new Thickness(2);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
