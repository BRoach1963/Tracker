using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    public class KpiStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            KpiStatusEnum status = (KpiStatusEnum)value;
            switch (status)
            {
                case KpiStatusEnum.OnTarget:
                    return Brushes.Green;
                case KpiStatusEnum.OffTarget:
                    return Brushes.Red;
                case KpiStatusEnum.CloseToTarget:
                    return Brushes.SlateGray;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ObjectiveStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObjectiveStatusEnum status = (ObjectiveStatusEnum)value;
            switch (status)
            {
                case ObjectiveStatusEnum.OnTrack:
                    return Brushes.Green;
                case ObjectiveStatusEnum.OffTrack:
                    return Brushes.Red;
                case ObjectiveStatusEnum.AtRisk:
                    return Brushes.SlateGray;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
