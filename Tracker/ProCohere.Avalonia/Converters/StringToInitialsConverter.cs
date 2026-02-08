using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Linq;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts a name string to initials (e.g., "John Doe" -> "JD").
/// Returns up to 2 initials from the first and last words.
/// </summary>
public class StringToInitialsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return "?";

        var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
            return "?";

        if (words.Length == 1)
            return words[0][0].ToString().ToUpperInvariant();

        // Take first letter of first word and first letter of last word
        var firstInitial = words[0][0];
        var lastInitial = words[^1][0];
        
        return $"{firstInitial}{lastInitial}".ToUpperInvariant();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("StringToInitialsConverter does not support ConvertBack.");
    }
}
