using System;
using System.Globalization;
using System.Windows.Data;

namespace Tracker.Common.Converters
{
    /// <summary>
    /// Extracts the first letter (or initials) from a name for avatar displays.
    /// </summary>
    public class FirstLetterConverter : IValueConverter
    {
        public static readonly FirstLetterConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                // Get initials from full name
                var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // First + Last initial
                    return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
                }
                else if (parts.Length == 1)
                {
                    // Single initial
                    return parts[0][0].ToString().ToUpperInvariant();
                }
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
