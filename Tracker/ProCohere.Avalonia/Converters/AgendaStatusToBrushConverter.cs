using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts meeting agenda item status to a color brush.
/// Status values: 'open', 'discussed', 'action_created', 'deferred', 'dropped'
/// </summary>
public class AgendaStatusToBrushConverter : IValueConverter
{
    // Status colors
    private static readonly SolidColorBrush OpenBrush = new(Color.Parse("#3B82F6"));       // Blue - pending
    private static readonly SolidColorBrush DiscussedBrush = new(Color.Parse("#10B981")); // Green - completed
    private static readonly SolidColorBrush ActionBrush = new(Color.Parse("#F59E0B"));    // Amber - action needed
    private static readonly SolidColorBrush DeferredBrush = new(Color.Parse("#6B7280"));  // Gray - postponed
    private static readonly SolidColorBrush DroppedBrush = new(Color.Parse("#9CA3AF"));   // Light gray - dropped
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#6B7280"));   // Gray - unknown

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string;
        
        return status?.ToLowerInvariant() switch
        {
            "open" => OpenBrush,
            "discussed" => DiscussedBrush,
            "action_created" => ActionBrush,
            "deferred" => DeferredBrush,
            "dropped" => DroppedBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
