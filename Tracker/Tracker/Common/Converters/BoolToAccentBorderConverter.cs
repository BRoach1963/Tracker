using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a boolean to an accent color border brush for selection highlighting.
    /// </summary>
    public class BoolToAccentBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return Application.Current.TryFindResource("AccentBrush") as Brush ?? Brushes.Gold;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts a boolean to an accent color fill for step indicators.
    /// </summary>
    public class BoolToAccentFillConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
            {
                return Application.Current.TryFindResource("AccentBrush") as Brush ?? Brushes.Gold;
            }
            return Application.Current.TryFindResource("SurfaceBrush") as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts a boolean to authentication type display string.
    /// </summary>
    public class BoolToAuthTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool useWindowsAuth)
            {
                return useWindowsAuth ? "Windows Authentication" : "SQL Server Authentication";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
