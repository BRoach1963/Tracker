using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.Common.Enums;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts FeedbackType to a color for type badges.
    /// </summary>
    public class FeedbackTypeToColorConverter : IValueConverter
    {
        public static readonly FeedbackTypeToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FeedbackType type)
            {
                return type switch
                {
                    FeedbackType.Positive => new SolidColorBrush(Color.FromRgb(34, 197, 94)),        // Green
                    FeedbackType.Constructive => new SolidColorBrush(Color.FromRgb(249, 115, 22)),  // Orange
                    FeedbackType.Recognition => new SolidColorBrush(Color.FromRgb(59, 130, 246)),   // Blue
                    FeedbackType.Coaching => new SolidColorBrush(Color.FromRgb(139, 92, 246)),      // Purple
                    FeedbackType.PerformanceReview => new SolidColorBrush(Color.FromRgb(236, 72, 153)), // Pink
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

