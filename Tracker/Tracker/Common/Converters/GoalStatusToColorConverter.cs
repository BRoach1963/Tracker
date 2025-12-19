using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts GoalStatus to a color for status badges.
    /// </summary>
    public class GoalStatusToColorConverter : IValueConverter
    {
        public static readonly GoalStatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GoalStatus status)
            {
                return status switch
                {
                    GoalStatus.NotStarted => new SolidColorBrush(Color.FromRgb(107, 114, 128)),   // Gray
                    GoalStatus.InProgress => new SolidColorBrush(Color.FromRgb(59, 130, 246)),    // Blue
                    GoalStatus.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // Green
                    GoalStatus.OnHold => new SolidColorBrush(Color.FromRgb(249, 115, 22)),        // Orange
                    GoalStatus.Cancelled => new SolidColorBrush(Color.FromRgb(239, 68, 68)),      // Red
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

