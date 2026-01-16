using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Formats raw phone numbers into a human-readable format.
/// Supports US phone numbers: (XXX) XXX-XXXX
/// </summary>
public class PhoneNumberConverter : IValueConverter
{
    public static PhoneNumberConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
            return value;

        return FormatPhoneNumber(phone);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // When converting back, strip to digits only for storage
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
            return value;

        return StripToDigits(phone);
    }

    /// <summary>
    /// Formats a phone number string into (XXX) XXX-XXXX format.
    /// </summary>
    public static string FormatPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Strip to digits only
        var digits = StripToDigits(phone);

        // Handle different lengths
        return digits.Length switch
        {
            10 => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
            11 when digits.StartsWith('1') => $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}",
            > 11 => FormatInternational(digits),
            _ => phone // Return original if we can't parse
        };
    }

    private static string FormatInternational(string digits)
    {
        // For international numbers, just add some spacing
        if (digits.Length > 10)
        {
            // Assume country code + area code + number
            var countryCode = digits.Length > 11 ? digits[..^10] : digits[..1];
            var rest = digits[^10..];
            return $"+{countryCode} ({rest[..3]}) {rest[3..6]}-{rest[6..]}";
        }
        return digits;
    }

    /// <summary>
    /// Removes all non-digit characters from a phone string.
    /// </summary>
    public static string StripToDigits(string phone)
    {
        return Regex.Replace(phone, @"[^\d]", "");
    }
}
