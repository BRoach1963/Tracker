using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts MeetingStatusEnum to a color for status badges.
    /// </summary>
    public class MeetingStatusToColorConverter : IValueConverter
    {
        public static readonly MeetingStatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MeetingStatusEnum status)
            {
                return status switch
                {
                    MeetingStatusEnum.Scheduled => new SolidColorBrush(Color.FromRgb(59, 130, 246)),  // Blue
                    MeetingStatusEnum.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green
                    MeetingStatusEnum.Canceled => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
                    MeetingStatusEnum.Rescheduled => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
                    _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))  // Gray
                };
            }
            return new SolidColorBrush(Color.FromRgb(107, 114, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

