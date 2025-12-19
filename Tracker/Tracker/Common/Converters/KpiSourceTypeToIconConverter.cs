using System.Globalization;
using System.Windows.Data;
using Tracker.Interfaces;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts KpiSourceType to an icon path for display.
    /// </summary>
    public class KpiSourceTypeToIconConverter : IValueConverter
    {
        public static readonly KpiSourceTypeToIconConverter Instance = new();

        // Icon paths
        private const string ProjectIcon = "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,5V19H5V5H19M17,17H7V7H17V17M15,9H9V15H15V9Z";
        private const string TaskQueryIcon = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M13,13V18H10V13H7L12,8L17,13H13Z";
        private const string ChildKpiIcon = "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z";
        private const string ManualIcon = "M14.06,9L15,9.94L5.92,19H5V18.08L14.06,9M17.66,3C17.41,3 17.15,3.1 16.96,3.29L15.13,5.12L18.88,8.87L20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18.17,3.09 17.92,3 17.66,3M14.06,6.19L3,17.25V21H6.75L17.81,9.94L14.06,6.19Z";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KpiSourceType sourceType)
            {
                return sourceType switch
                {
                    KpiSourceType.Project => ProjectIcon,
                    KpiSourceType.TaskQuery => TaskQueryIcon,
                    KpiSourceType.ChildKpi => ChildKpiIcon,
                    KpiSourceType.Manual => ManualIcon,
                    _ => ManualIcon
                };
            }
            return ManualIcon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

