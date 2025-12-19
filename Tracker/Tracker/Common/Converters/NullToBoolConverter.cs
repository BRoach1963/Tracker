using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts null to true and non-null to false.
    /// Useful for binding "All" RadioButton.IsChecked to a nullable filter property.
    /// When ConverterParameter is "True", returns true when value is null.
    /// When ConverterParameter is "False" or absent, returns true when value is non-null.
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public static readonly NullToBoolConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            
            // If parameter is "True", we want true when null (for "All" filter)
            if (parameter is string paramStr && paramStr.Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                return isNull;
            }
            
            // Default: return true when NOT null
            return !isNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // When RadioButton becomes checked and we want to set value to null
            if (value is bool boolValue && boolValue)
            {
                if (parameter is string paramStr && paramStr.Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }
            
            return Binding.DoNothing;
        }
    }
}

