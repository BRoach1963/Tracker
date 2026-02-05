using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converter that checks if an integer value equals a specific integer parameter.
/// Returns true if equal, false otherwise.
/// </summary>
public class IntEqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue || parameter is null)
            return false;

        if (parameter is int paramInt)
            return intValue == paramInt;

        if (int.TryParse(parameter.ToString(), out var parsedParam))
            return intValue == parsedParam;

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is int paramInt)
            return paramInt;

        if (value is true && int.TryParse(parameter?.ToString(), out var parsedParam))
            return parsedParam;

        return 0;
    }
}
