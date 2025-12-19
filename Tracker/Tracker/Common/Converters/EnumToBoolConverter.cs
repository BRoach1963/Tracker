using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts an enum value to a boolean by comparing it to the ConverterParameter.
    /// Useful for binding RadioButton.IsChecked to an enum property.
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
        public static readonly EnumToBoolConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
                return parameter;

            return Binding.DoNothing;
        }
    }
}

