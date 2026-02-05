using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts a percentage (0-100) to a pixel width based on a max width parameter.
/// Used for progress bars and visual percentage displays.
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public static PercentToWidthConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not decimal percent)
            return 0.0;

        if (parameter is not string maxWidthStr || !double.TryParse(maxWidthStr, out var maxWidth))
            maxWidth = 100.0;

        // Clamp percent to 0-100
        percent = Math.Max(0, Math.Min(100, percent));

        // Calculate actual width
        return (double)(percent / 100m * (decimal)maxWidth);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
