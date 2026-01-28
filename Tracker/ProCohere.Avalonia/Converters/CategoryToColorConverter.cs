using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// Converts note category names to subtle background colors.
/// Returns a semi-transparent color based on the category.
/// </summary>
public class CategoryToColorConverter : IValueConverter
{
    public static readonly CategoryToColorConverter Instance = new();

    // Predefined subtle colors for each category
    private static readonly IBrush MeetingNotesBrush = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246));   // Blue
    private static readonly IBrush IdeasBrush = new SolidColorBrush(Color.FromArgb(30, 168, 85, 247));         // Purple
    private static readonly IBrush ActionItemsBrush = new SolidColorBrush(Color.FromArgb(30, 239, 68, 68));    // Red
    private static readonly IBrush ResearchBrush = new SolidColorBrush(Color.FromArgb(30, 34, 197, 94));       // Green
    private static readonly IBrush PersonalBrush = new SolidColorBrush(Color.FromArgb(30, 251, 191, 36));      // Amber
    private static readonly IBrush FollowUpBrush = new SolidColorBrush(Color.FromArgb(30, 236, 72, 153));      // Pink
    private static readonly IBrush DefaultBrush = Brushes.Transparent;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string category || string.IsNullOrWhiteSpace(category))
            return DefaultBrush;

        return category.ToLowerInvariant() switch
        {
            "meeting notes" => MeetingNotesBrush,
            "ideas" => IdeasBrush,
            "action items" => ActionItemsBrush,
            "research" => ResearchBrush,
            "personal" => PersonalBrush,
            "follow-up" => FollowUpBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts note category names to accent colors for text/icons.
/// Returns a more saturated color for the category badge text.
/// </summary>
public class CategoryToAccentConverter : IValueConverter
{
    public static readonly CategoryToAccentConverter Instance = new();

    // Predefined accent colors for text
    private static readonly IBrush MeetingNotesBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));    // Blue
    private static readonly IBrush IdeasBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247));           // Purple
    private static readonly IBrush ActionItemsBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));      // Red
    private static readonly IBrush ResearchBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));         // Green
    private static readonly IBrush PersonalBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6));         // Amber (darker for contrast)
    private static readonly IBrush FollowUpBrush = new SolidColorBrush(Color.FromRgb(236, 72, 153));        // Pink

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string category || string.IsNullOrWhiteSpace(category))
            return null; // Let it use default

        return category.ToLowerInvariant() switch
        {
            "meeting notes" => MeetingNotesBrush,
            "ideas" => IdeasBrush,
            "action items" => ActionItemsBrush,
            "research" => ResearchBrush,
            "personal" => PersonalBrush,
            "follow-up" => FollowUpBrush,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
