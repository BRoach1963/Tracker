using Avalonia.Data.Converters;
using Avalonia.Media;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts enum values to boolean for selection comparison.
/// </summary>
public class EnumEqualConverter : IValueConverter
{
    public static readonly EnumEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
            return parameter;
        return null;
    }
}

/// <summary>
/// Converts enum values to boolean for NOT equal comparison.
/// </summary>
public class EnumNotEqualConverter : IValueConverter
{
    public static readonly EnumNotEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return true;

        return !value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts boolean (expanded state) to navigation rail width.
/// </summary>
public class NavWidthConverter : IValueConverter
{
    public static readonly NavWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? 200.0 : 64.0;
        }
        return 200.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts dark theme boolean to appropriate icon path.
/// </summary>
public class ThemeIconConverter : IValueConverter
{
    public static readonly ThemeIconConverter Instance = new();

    // Sun icon for dark mode (click to switch to light)
    private const string SunIcon = "M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,2L14.39,5.42C13.65,5.15 12.84,5 12,5C11.16,5 10.35,5.15 9.61,5.42L12,2M3.34,7L7.5,6.65C6.9,7.16 6.36,7.78 5.94,8.5C5.5,9.24 5.25,10 5.11,10.79L3.34,7M3.36,17L5.12,13.23C5.26,14 5.53,14.78 5.95,15.5C6.37,16.24 6.91,16.86 7.5,17.37L3.36,17M20.65,7L18.88,10.79C18.74,10 18.47,9.23 18.05,8.5C17.63,7.78 17.1,7.15 16.5,6.64L20.65,7M20.64,17L16.5,17.36C17.09,16.85 17.62,16.22 18.04,15.5C18.46,14.77 18.73,14 18.87,13.21L20.64,17M12,22L9.59,18.56C10.33,18.83 11.14,19 12,19C12.82,19 13.63,18.83 14.37,18.56L12,22Z";
    
    // Moon icon for light mode (click to switch to dark)
    private const string MoonIcon = "M17.75,4.09L15.22,6.03L16.13,9.09L13.5,7.28L10.87,9.09L11.78,6.03L9.25,4.09L12.44,4L13.5,1L14.56,4L17.75,4.09M21.25,11L19.61,12.25L20.2,14.23L18.5,13.06L16.8,14.23L17.39,12.25L15.75,11L17.81,10.95L18.5,9L19.19,10.95L21.25,11M18.97,15.95C19.8,15.87 20.69,17.05 20.16,17.8C19.84,18.25 19.5,18.67 19.08,19.07C15.17,23 8.84,23 4.94,19.07C1.03,15.17 1.03,8.83 4.94,4.93C5.34,4.53 5.76,4.17 6.21,3.85C6.96,3.32 8.14,4.21 8.06,5.04C7.79,7.9 8.75,10.87 10.95,13.06C13.14,15.26 16.1,16.22 18.97,15.95Z";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDark)
        {
            // Show sun when dark (to switch to light), moon when light (to switch to dark)
            return StreamGeometry.Parse(isDark ? SunIcon : MoonIcon);
        }
        return StreamGeometry.Parse(SunIcon);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts dark theme boolean to text label.
/// </summary>
public class ThemeTextConverter : IValueConverter
{
    public static readonly ThemeTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDark)
        {
            return isDark ? "Light Mode" : "Dark Mode";
        }
        return "Theme";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts expanded state to collapse/expand icon.
/// </summary>
public class CollapseIconConverter : IValueConverter
{
    public static readonly CollapseIconConverter Instance = new();

    // Chevron left (collapse)
    private const string CollapseIcon = "M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z";
    
    // Chevron right (expand)
    private const string ExpandIcon = "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return StreamGeometry.Parse(isExpanded ? CollapseIcon : ExpandIcon);
        }
        return StreamGeometry.Parse(CollapseIcon);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean (IsActive) to status color.
/// </summary>
public class BoolToColorConverter : IMultiValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool isActive)
        {
            return isActive ? Brushes.Green : Brushes.Gray;
        }
        return Brushes.Gray;
    }
}

/// <summary>
/// Converts meeting status (NeedsAttention) to color.
/// </summary>
public class MeetingStatusColorConverter : IMultiValueConverter
{
    public static readonly MeetingStatusColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool needsAttention)
        {
            return needsAttention 
                ? new SolidColorBrush(Color.Parse("#EF4444"))  // Red
                : new SolidColorBrush(Color.Parse("#1F2937")); // Normal text
        }
        return new SolidColorBrush(Color.Parse("#1F2937"));
    }
}

/// <summary>
/// Converts a number to boolean (true if zero).
/// </summary>
public class EqualToZeroConverter : IValueConverter
{
    public static readonly EqualToZeroConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal)
            return intVal == 0;
        if (value is long longVal)
            return longVal == 0;
        if (value is double doubleVal)
            return doubleVal == 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to width for animated panel (340 when open to include margin and scrollbar space, 0 when closed).
/// </summary>
public class BoolToWidthConverter : IValueConverter
{
    public static readonly BoolToWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOpen)
        {
            return isOpen ? 328.0 : 0.0; // 320 panel width + 8 left margin
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean IsToday to highlight background color (single-value version).
/// Returns Primary color for true, Transparent for false.
/// </summary>
public class BoolToHighlightBackgroundConverter : IValueConverter
{
    public static readonly BoolToHighlightBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isToday && isToday)
        {
            return new SolidColorBrush(Color.Parse("#6366F1")); // Primary color
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean IsToday to text foreground color.
/// Returns White for true (today), TextPrimary for false.
/// </summary>
public class BoolToForegroundConverter : IValueConverter
{
    public static readonly BoolToForegroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isToday && isToday)
        {
            return Brushes.White;
        }
        // Return a neutral dark color for non-today
        return new SolidColorBrush(Color.Parse("#E5E7EB")); // TextPrimary-ish
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts goal status string to background color.
/// on_track = green, at_risk = amber, off_track = red, in_progress = blue
/// </summary>
public class GoalStatusToBackgroundConverter : IValueConverter
{
    public static readonly GoalStatusToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant();
        
        return status switch
        {
            "on_track" => Color.Parse("#10B981"),    // Green
            "at_risk" => Color.Parse("#F59E0B"),     // Amber
            "off_track" => Color.Parse("#EF4444"),   // Red
            "in_progress" => Color.Parse("#3B82F6"), // Blue
            "completed" => Color.Parse("#6366F1"),   // Primary/Indigo
            _ => Color.Parse("#6B7280")              // Gray default
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts DisplayDepth (int) to left margin for tree view indentation.
/// Each level adds 24 pixels of left margin.
/// </summary>
public class TreeDepthToMarginConverter : IValueConverter
{
    public static readonly TreeDepthToMarginConverter Instance = new();

    /// <summary>
    /// Pixels per indent level.
    /// </summary>
    private const int IndentPerLevel = 28;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int depth)
        {
            var leftMargin = depth * IndentPerLevel;
            // For tree view: left indent, small top/bottom for vertical list
            return new global::Avalonia.Thickness(leftMargin, 0, 0, 6);
        }
        return new global::Avalonia.Thickness(0, 0, 0, 6);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if the integer value is greater than zero.
/// Used to show tree connectors for indented items.
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public static readonly GreaterThanZeroConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a double (top offset) to a Thickness for absolute positioning via Margin.
/// Used for calendar meeting positioning where Canvas.Top binding doesn't work.
/// </summary>
public class TopOffsetToMarginConverter : IValueConverter
{
    public static readonly TopOffsetToMarginConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double offset)
        {
            return new global::Avalonia.Thickness(0, offset, 0, 0);
        }
        if (value is int intOffset)
        {
            return new global::Avalonia.Thickness(0, intOffset, 0, 0);
        }
        return new global::Avalonia.Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to strikethrough text decoration.
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
    public static readonly BoolToStrikethroughConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCompleted && isCompleted)
        {
            return TextDecorations.Strikethrough;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean (overdue) to color - red if overdue, normal text color otherwise.
/// </summary>
public class BoolToOverdueColorConverter : IValueConverter
{
    public static readonly BoolToOverdueColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOverdue && isOverdue)
        {
            return new SolidColorBrush(Color.Parse("#EF4444")); // Red
        }
        return new SolidColorBrush(Color.Parse("#6B7280")); // Gray (text tertiary)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts priority string to background color for badges.
/// </summary>
public class PriorityToBgColorConverter : IMultiValueConverter
{
    public static readonly PriorityToBgColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = values.FirstOrDefault()?.ToString()?.ToLowerInvariant();
        
        return priority switch
        {
            "high" => new SolidColorBrush(Color.Parse("#FEE2E2")),    // Light red
            "medium" => new SolidColorBrush(Color.Parse("#FEF3C7")), // Light amber
            "low" => new SolidColorBrush(Color.Parse("#D1FAE5")),    // Light green
            _ => new SolidColorBrush(Color.Parse("#F3F4F6"))         // Light gray
        };
    }
}

/// <summary>
/// Converts priority string to foreground color for badge text.
/// </summary>
public class PriorityToFgColorConverter : IMultiValueConverter
{
    public static readonly PriorityToFgColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var priority = values.FirstOrDefault()?.ToString()?.ToLowerInvariant();
        
        return priority switch
        {
            "high" => new SolidColorBrush(Color.Parse("#991B1B")),    // Dark red
            "medium" => new SolidColorBrush(Color.Parse("#92400E")), // Dark amber
            "low" => new SolidColorBrush(Color.Parse("#065F46")),    // Dark green
            _ => new SolidColorBrush(Color.Parse("#374151"))         // Dark gray
        };
    }
}

/// <summary>
/// Converts a Guid to "New Goal" or "Edit Goal" text.
/// </summary>
public class GuidIsEmptyToNewOrEditConverter : IValueConverter
{
    public static readonly GuidIsEmptyToNewOrEditConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Guid guid)
        {
            return guid == Guid.Empty ? "New Goal" : "Edit Goal";
        }
        return "New Goal";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts GoalType enum to ComboBox index and back.
/// </summary>
public class GoalTypeToIndexConverter : IValueConverter
{
    public static readonly GoalTypeToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProCohere.Avalonia.Models.GoalType goalType)
        {
            return goalType switch
            {
                ProCohere.Avalonia.Models.GoalType.Growth => 0,
                ProCohere.Avalonia.Models.GoalType.Execution => 1,
                ProCohere.Avalonia.Models.GoalType.Operational => 2,
                ProCohere.Avalonia.Models.GoalType.Directional => 3,
                _ => 0
            };
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => ProCohere.Avalonia.Models.GoalType.Growth,
                1 => ProCohere.Avalonia.Models.GoalType.Execution,
                2 => ProCohere.Avalonia.Models.GoalType.Operational,
                3 => ProCohere.Avalonia.Models.GoalType.Directional,
                _ => ProCohere.Avalonia.Models.GoalType.Growth
            };
        }
        return ProCohere.Avalonia.Models.GoalType.Growth;
    }
}

/// <summary>
/// Converts GoalHealth to display text.
/// </summary>
public class GoalHealthToDisplayConverter : IValueConverter
{
    public static readonly GoalHealthToDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProCohere.Avalonia.Models.GoalHealth health)
        {
            return health.ToDisplayName();
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts GoalHealth to background color (neutral blues, NO red/yellow/green).
/// Philosophy: Use blue-scale to indicate state without judgment.
/// </summary>
public class GoalHealthToBackgroundConverter : IValueConverter
{
    public static readonly GoalHealthToBackgroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProCohere.Avalonia.Models.GoalHealth health)
        {
            return health switch
            {
                ProCohere.Avalonia.Models.GoalHealth.OnTrack => new SolidColorBrush(Color.Parse("#3B82F6")), // Blue
                ProCohere.Avalonia.Models.GoalHealth.NeedsAttention => new SolidColorBrush(Color.Parse("#6366F1")), // Indigo
                ProCohere.Avalonia.Models.GoalHealth.AtRisk => new SolidColorBrush(Color.Parse("#8B5CF6")), // Purple
                ProCohere.Avalonia.Models.GoalHealth.ReframingNeeded => new SolidColorBrush(Color.Parse("#A855F7")), // Light Purple
                _ => new SolidColorBrush(Color.Parse("#64748B")) // Slate
            };
        }
        return new SolidColorBrush(Color.Parse("#64748B"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts GoalHealth to reflection prompt text.
/// </summary>
public class GoalHealthToPromptConverter : IValueConverter
{
    public static readonly GoalHealthToPromptConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProCohere.Avalonia.Models.GoalHealth health)
        {
            return health.GetReflectionPrompt();
        }
        return "What has changed?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Generic boolean to object converter with configurable true/false values.
/// Used for conditional styling (backgrounds, fonts, etc.) based on boolean state.
/// </summary>
public class BoolToObjectConverter : IValueConverter
{
    /// <summary>
    /// Value returned when input is true.
    /// </summary>
    public object? TrueValue { get; set; }

    /// <summary>
    /// Value returned when input is false.
    /// </summary>
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
