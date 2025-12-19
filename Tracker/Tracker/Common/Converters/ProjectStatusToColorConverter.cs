using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts project status string to a color brush.
    /// </summary>
    public class ProjectStatusToColorConverter : IValueConverter
    {
        public static ProjectStatusToColorConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

            return status switch
            {
                "active" or "in progress" or "inprogress" => new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)), // Green
                "completed" or "done" or "finished" => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)), // Emerald
                "at risk" or "atrisk" or "delayed" => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), // Red
                "on hold" or "paused" or "onhold" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)), // Amber
                "planning" or "planned" or "not started" => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)), // Slate
                "cancelled" or "canceled" => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)), // Gray
                _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) // Default slate
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

