using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    public class ActualWidthToDoubleAnimationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double actualWidth && values[1] is double factor)
            {
                return actualWidth * factor;
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
