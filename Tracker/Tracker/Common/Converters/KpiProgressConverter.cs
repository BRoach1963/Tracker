using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Multi-value converter that takes current value, target value, and container width 
    /// to calculate the progress bar fill width.
    /// </summary>
    public class KpiProgressConverter : IMultiValueConverter
    {
        public static KpiProgressConverter Instance { get; } = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return 0d;

            // Handle unset values during binding
            if (values[0] == DependencyProperty.UnsetValue ||
                values[1] == DependencyProperty.UnsetValue ||
                values[2] == DependencyProperty.UnsetValue)
                return 0d;

            try
            {
                var current = System.Convert.ToDouble(values[0]);
                var target = System.Convert.ToDouble(values[1]);
                var containerWidth = System.Convert.ToDouble(values[2]);

                if (target <= 0 || containerWidth <= 0)
                    return 0d;

                // Calculate percentage, capped at 100%
                var percentage = Math.Min(1.0, Math.Max(0, current / target));
                return percentage * containerWidth;
            }
            catch
            {
                return 0d;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

