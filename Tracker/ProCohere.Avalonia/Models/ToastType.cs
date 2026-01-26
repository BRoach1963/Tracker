namespace ProCohere.Avalonia.Models;

/// <summary>
/// Defines the visual style and icon for toast notifications.
/// </summary>
public enum ToastType
{
    /// <summary>
    /// Informational message - uses blue accent color.
    /// </summary>
    Information,

    /// <summary>
    /// Success message - uses green accent color.
    /// </summary>
    Success,

    /// <summary>
    /// Warning message - uses amber/yellow accent color.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message - uses red accent color.
    /// </summary>
    Error
}
