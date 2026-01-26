using System;
using Avalonia.Media;

namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Represents a selectable entity in the entity picker dialog.
/// </summary>
public class EntityPickerItem
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // task, goal, metric, project
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? StatusText { get; set; }

    /// <summary>
    /// Whether there is status text to display.
    /// </summary>
    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

    /// <summary>
    /// SVG path for the entity type icon.
    /// </summary>
    public string TypeIcon => EntityType.ToLower() switch
    {
        "task" => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z", // Checkmark
        "goal" => "M5,16L3,5L8.5,10L12,4L15.5,10L21,5L19,16H5M19,19C19,19.55 18.55,20 18,20H6C5.45,20 5,19.55 5,19V18H19V19Z", // Flag/target
        "metric" => "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z", // Chart
        "project" => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z", // Folder
        _ => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
    };

    /// <summary>
    /// Color brush for the entity type.
    /// </summary>
    public IBrush TypeColor => EntityType.ToLower() switch
    {
        "task" => new SolidColorBrush(Color.Parse("#3498DB")),    // Blue
        "goal" => new SolidColorBrush(Color.Parse("#27AE60")),    // Green
        "metric" => new SolidColorBrush(Color.Parse("#9B59B6")),  // Purple
        "project" => new SolidColorBrush(Color.Parse("#E67E22")), // Orange
        _ => new SolidColorBrush(Color.Parse("#7F8C8D"))
    };
}
