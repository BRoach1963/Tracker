namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Represents a timezone option for the timezone dropdown.
/// </summary>
public class TimezoneItem
{
    /// <summary>
    /// The timezone ID (e.g., "America/New_York").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name for the timezone.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
