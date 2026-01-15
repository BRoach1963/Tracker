using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts MetricStatus to a color brush for display.
    /// </summary>
    public class MetricStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MetricStatus status) return Brushes.Gray;
            
            return status switch
            {
                MetricStatus.OnTarget => Brushes.Green,
                MetricStatus.OffTarget => Brushes.Red,
                MetricStatus.CloseToTarget => Brushes.SlateGray,
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// Legacy alias for backwards compatibility.
    /// </summary>
    [Obsolete("Use MetricStatusToColorConverter instead")]
    public class KpiStatusToColorConverter : MetricStatusToColorConverter { }
}
