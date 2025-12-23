using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class ReverseBoolToVisConverter : IValueConverter
    {
        public static ReverseBoolToVisConverter Instance { get; } = new ReverseBoolToVisConverter();

        public ReverseBoolToVisConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
