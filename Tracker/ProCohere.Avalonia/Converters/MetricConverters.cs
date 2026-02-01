using Avalonia.Data.Converters;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts metric trend and direction to a signal color.
/// Implements the "signals-not-targets" philosophy:
/// - Green: On track (trending in desired direction)
/// - Amber/Yellow: At risk (stable or slight deviation)
/// - Red: Off track (trending against desired direction)
/// - Gray: Unknown (no data)
/// </summary>
public class MetricSignalColorConverter : IMultiValueConverter
{
    public static readonly MetricSignalColorConverter Instance = new();

    // Signal colors
    private static readonly SolidColorBrush GreenBrush = new(Color.Parse("#10B981"));   // On Track
    private static readonly SolidColorBrush AmberBrush = new(Color.Parse("#F59E0B"));   // At Risk
    private static readonly SolidColorBrush RedBrush = new(Color.Parse("#EF4444"));     // Off Track
    private static readonly SolidColorBrush GrayBrush = new(Color.Parse("#9CA3AF"));    // Unknown

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return GrayBrush;

        var trend = values[0] as MetricTrend? ?? MetricTrend.Unknown;
        var directionStr = values[1] as string;

        // If no direction specified, use trend alone
        if (string.IsNullOrEmpty(directionStr))
        {
            return trend switch
            {
                MetricTrend.TrendingUp => GreenBrush,
                MetricTrend.Stable => AmberBrush,
                MetricTrend.TrendingDown => RedBrush,
                MetricTrend.MoreVariable => AmberBrush,
                _ => GrayBrush
            };
        }

        // Parse direction
        var isHigherBetter = directionStr.Equals("higher_is_better", StringComparison.OrdinalIgnoreCase);
        var isLowerBetter = directionStr.Equals("lower_is_better", StringComparison.OrdinalIgnoreCase);
        var isNeutral = directionStr.Equals("neutral", StringComparison.OrdinalIgnoreCase);

        if (isNeutral)
        {
            // Neutral metrics: stable is good, any change is informational
            return trend switch
            {
                MetricTrend.Stable => GreenBrush,
                MetricTrend.TrendingUp or MetricTrend.TrendingDown => AmberBrush,
                MetricTrend.MoreVariable => AmberBrush,
                _ => GrayBrush
            };
        }

        // Evaluate trend against direction
        if (isHigherBetter)
        {
            return trend switch
            {
                MetricTrend.TrendingUp => GreenBrush,    // Good: going up
                MetricTrend.Stable => AmberBrush,        // OK: holding steady
                MetricTrend.TrendingDown => RedBrush,    // Bad: going down
                MetricTrend.MoreVariable => AmberBrush,
                _ => GrayBrush
            };
        }
        else if (isLowerBetter)
        {
            return trend switch
            {
                MetricTrend.TrendingDown => GreenBrush,  // Good: going down
                MetricTrend.Stable => AmberBrush,        // OK: holding steady
                MetricTrend.TrendingUp => RedBrush,      // Bad: going up
                MetricTrend.MoreVariable => AmberBrush,
                _ => GrayBrush
            };
        }

        return GrayBrush;
    }
}

/// <summary>
/// Converts a value to boolean by comparing equality with the parameter.
/// Usage: {Binding DetailTab, Converter={x:Static EqualityConverter.Instance}, ConverterParameter=0}
/// </summary>
public class EqualityConverter : IValueConverter
{
    public static readonly EqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return true;
        if (value == null || parameter == null)
            return false;

        // Handle numeric comparisons (int vs string parameter)
        if (value is int intValue && parameter is string strParam)
        {
            if (int.TryParse(strParam, out var paramInt))
                return intValue == paramInt;
        }

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts bool to opacity (true = 1.0, false = 0.0).
/// </summary>
public class BooleanToOpacityConverter : IValueConverter
{
    public static readonly BooleanToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? 1.0 : 0.0;
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts null to false, non-null to true.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts MetricTrend to a color for display.
/// Simple single-value converter for trend arrows.
/// </summary>
public class MetricTrendToColorConverter : IValueConverter
{
    public static readonly MetricTrendToColorConverter Instance = new();

    private static readonly SolidColorBrush GreenBrush = new(Color.Parse("#10B981"));
    private static readonly SolidColorBrush AmberBrush = new(Color.Parse("#F59E0B"));
    private static readonly SolidColorBrush RedBrush = new(Color.Parse("#EF4444"));
    private static readonly SolidColorBrush GrayBrush = new(Color.Parse("#9CA3AF"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var trend = value as MetricTrend? ?? MetricTrend.Unknown;

        return trend switch
        {
            MetricTrend.TrendingUp => GreenBrush,
            MetricTrend.Stable => AmberBrush,
            MetricTrend.TrendingDown => RedBrush,
            MetricTrend.MoreVariable => AmberBrush,
            _ => GrayBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts MetricTrend to a signal brush color (for the indicator dot).
/// Same as MetricTrendToColorConverter but for the signal dot.
/// </summary>
public class MetricTrendToSignalBrushConverter : IValueConverter
{
    public static readonly MetricTrendToSignalBrushConverter Instance = new();

    private static readonly SolidColorBrush GreenBrush = new(Color.Parse("#10B981"));
    private static readonly SolidColorBrush AmberBrush = new(Color.Parse("#F59E0B"));
    private static readonly SolidColorBrush RedBrush = new(Color.Parse("#EF4444"));
    private static readonly SolidColorBrush GrayBrush = new(Color.Parse("#9CA3AF"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var trend = value as MetricTrend? ?? MetricTrend.Unknown;

        return trend switch
        {
            MetricTrend.TrendingUp => GreenBrush,
            MetricTrend.Stable => AmberBrush,
            MetricTrend.TrendingDown => RedBrush,
            MetricTrend.MoreVariable => AmberBrush,
            _ => GrayBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a DateTime to a human-readable age string.
/// Examples: "today", "1d ago", "2w ago", "1mo ago"
/// </summary>
public class DateTimeToAgeConverter : IValueConverter
{
    public static readonly DateTimeToAgeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return "unknown";

        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalDays switch
        {
            < 1 => "today",
            < 2 => "1d ago",
            < 7 => $"{(int)elapsed.TotalDays}d ago",
            < 14 => "1w ago",
            < 30 => $"{(int)(elapsed.TotalDays / 7)}w ago",
            < 60 => "1mo ago",
            _ => $"{(int)(elapsed.TotalDays / 30)}mo ago"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

// Note: InverseBoolConverter is defined in NavigationConverters.cs
