using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts null or empty string to Visibility.Collapsed, non-empty to Visible.
    /// </summary>
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public static NullOrEmptyToVisibilityConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to "Yes" or "No" string.
    /// </summary>
    public class BoolToYesNoConverter : IValueConverter
    {
        public static BoolToYesNoConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? "Yes" : "No";
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return s.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }

    /// <summary>
    /// Converts boolean to one of two styles specified in parameter.
    /// Parameter format: "TrueStyleKey|FalseStyleKey"
    /// </summary>
    public class BoolToStyleConverter : IValueConverter
    {
        public static BoolToStyleConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && parameter is string styleKeys)
            {
                var keys = styleKeys.Split('|');
                if (keys.Length == 2)
                {
                    var styleKey = b ? keys[0] : keys[1];
                    // Try to find the style from Application resources
                    if (Application.Current.TryFindResource(styleKey) is Style style)
                    {
                        return style;
                    }
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
