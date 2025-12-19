using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Interfaces;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts KpiSourceType to a background color for icons.
    /// </summary>
    public class KpiSourceTypeToColorConverter : IValueConverter
    {
        public static readonly KpiSourceTypeToColorConverter Instance = new();

        private static readonly SolidColorBrush ProjectBrush = new(Color.FromRgb(16, 185, 129));   // Green
        private static readonly SolidColorBrush TaskQueryBrush = new(Color.FromRgb(245, 158, 11)); // Amber
        private static readonly SolidColorBrush ChildKpiBrush = new(Color.FromRgb(99, 102, 241));  // Indigo
        private static readonly SolidColorBrush ManualBrush = new(Color.FromRgb(107, 114, 128));   // Gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KpiSourceType sourceType)
            {
                return sourceType switch
                {
                    KpiSourceType.Project => ProjectBrush,
                    KpiSourceType.TaskQuery => TaskQueryBrush,
                    KpiSourceType.ChildKpi => ChildKpiBrush,
                    KpiSourceType.Manual => ManualBrush,
                    _ => ManualBrush
                };
            }
            return ManualBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

