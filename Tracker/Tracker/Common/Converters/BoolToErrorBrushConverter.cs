using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts a boolean (HasError) to an appropriate background brush.
    /// True = Error (red tint), False = Success (green tint)
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToErrorBackgroundConverter : IValueConverter
    {
        public static readonly BoolToErrorBackgroundConverter Instance = new();

        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(59, 28, 28)); // #3B1C1C
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(28, 59, 28)); // #1C3B1C

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasError)
            {
                return hasError ? ErrorBrush : SuccessBrush;
            }
            return SuccessBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean (HasError) to an appropriate foreground brush.
    /// True = Error (red text), False = Success (green text)
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToErrorForegroundConverter : IValueConverter
    {
        public static readonly BoolToErrorForegroundConverter Instance = new();

        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(248, 113, 113)); // #F87171
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(74, 222, 128)); // #4ADE80

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasError)
            {
                return hasError ? ErrorBrush : SuccessBrush;
            }
            return SuccessBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

