using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a boolean value to one of two strings.
    /// ConverterParameter should be in format "TrueValue|FalseValue".
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public static BoolToStringConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return string.Empty;

            if (parameter is not string paramStr)
                return boolValue.ToString();

            var parts = paramStr.Split('|');
            if (parts.Length != 2)
                return boolValue.ToString();

            return boolValue ? parts[0] : parts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

