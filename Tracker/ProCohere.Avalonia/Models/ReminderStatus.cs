namespace ProCohere.Avalonia.Models;

/// <summary>
/// Status of a reminder in its lifecycle.
/// </summary>
public enum ReminderStatus
{
    /// <summary>
    /// Scheduled and waiting to be triggered.
    /// </summary>
    Pending,
    
    /// <summary>
    /// Has been sent/triggered to the user.
    /// </summary>
    Sent,
    
    /// <summary>
    /// Shown to user and awaiting dismissal.
    /// </summary>
    Triggered,
    
    /// <summary>
    /// Snoozed until a later time.
    /// </summary>
    Snoozed,
    
    /// <summary>
    /// Dismissed by the user.
    /// </summary>
    Dismissed,
    
    /// <summary>
    /// Cancelled (entity was deleted).
    /// </summary>
    Cancelled
}
