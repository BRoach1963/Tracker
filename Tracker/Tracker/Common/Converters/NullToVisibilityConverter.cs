using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Converts null to Collapsed and non-null to Visible.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public static NullToVisibilityConverter Instance { get; } = new NullToVisibilityConverter();
        
        /// <summary>
        /// Inverse instance: Visible when null, Collapsed when non-null.
        /// </summary>
        public static NullToVisibilityConverter InverseInstance { get; } = new NullToVisibilityConverter { Inverse = true };

        /// <summary>
        /// When true, returns Visible for null and Collapsed for non-null.
        /// </summary>
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            
            // Handle empty strings as "null" as well
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                isNull = true;
            }

            if (Inverse)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
