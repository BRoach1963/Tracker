using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class ReverseBooleanConverter : IValueConverter
    {
        public static ReverseBooleanConverter Instance { get; } = new ReverseBooleanConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private ReverseBooleanConverter()
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
