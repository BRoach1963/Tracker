using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class ReverseBooleanConverter : IValueConverter
    {
        public static ReverseBooleanConverter Instance { get; } = new ReverseBooleanConverter();

        public ReverseBooleanConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool reverseIt) return !reverseIt;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
