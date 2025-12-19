using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts task status string to a color brush.
    /// </summary>
    public class TaskStatusToColorConverter : IValueConverter
    {
        public static TaskStatusToColorConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

            return status switch
            {
                "incomplete" or "not started" or "notstarted" or "open" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)), // Amber
                "in progress" or "inprogress" => new SolidColorBrush(Color.FromRgb(0x38, 0x86, 0xF1)), // Blue
                "completed" or "done" or "complete" => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)), // Emerald
                "blocked" => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)), // Red
                "on hold" or "onhold" => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)), // Slate
                _ => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) // Default slate
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
