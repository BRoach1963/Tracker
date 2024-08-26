using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public  class BooleanToOpacityConverter : IValueConverter
    {
        public static BooleanToOpacityConverter Instance { get; } = new BooleanToOpacityConverter();

        /// <summary>
        /// This constructor is private to prevent new instances from being created. Access the static instance through
        /// the Instance property instead of creating a new object.
        /// </summary>
        private BooleanToOpacityConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isSelected = (bool)value;
            return isSelected ? 1.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
