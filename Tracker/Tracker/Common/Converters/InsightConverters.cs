using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Tracker.DataModels;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts InsightSeverity to a corresponding brush color.
    /// </summary>
    public class InsightSeverityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(0x21, 0x96, 0xF3));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xFF, 0x98, 0x00));
        private static readonly SolidColorBrush CriticalBrush = new(Color.FromRgb(0xF4, 0x43, 0x36));
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InsightSeverity severity)
            {
                return severity switch
                {
                    InsightSeverity.Info => InfoBrush,
                    InsightSeverity.Warning => WarningBrush,
                    InsightSeverity.Critical => CriticalBrush,
                    _ => InfoBrush
                };
            }
            return InfoBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Converts IsRead boolean to opacity (read items are slightly dimmed).
    /// </summary>
    public class ReadToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? 0.7 : 1.0;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
