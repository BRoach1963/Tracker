using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class BooleanToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCollapsed = (bool)value;
            return isCollapsed ? 180 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
