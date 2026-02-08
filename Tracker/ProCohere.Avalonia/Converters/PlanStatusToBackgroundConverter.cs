using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts development plan status to a background brush.
/// </summary>
public class PlanStatusToBackgroundConverter : IValueConverter
{
    public static PlanStatusToBackgroundConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? "draft";
        
        return status switch
        {
            "active" => new SolidColorBrush(Color.Parse("#10B981")),    // Green
            "completed" => new SolidColorBrush(Color.Parse("#6366F1")), // Indigo
            "cancelled" => new SolidColorBrush(Color.Parse("#6B7280")), // Gray
            "draft" => new SolidColorBrush(Color.Parse("#F59E0B")),     // Amber
            _ => new SolidColorBrush(Color.Parse("#6B7280"))            // Gray fallback
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
