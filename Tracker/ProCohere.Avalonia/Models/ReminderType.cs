namespace ProCohere.Avalonia.Models;

/// <summary>
/// Types of reminders that can be scheduled.
/// </summary>
public enum ReminderType
{
    /// <summary>
    /// Reminder for an upcoming meeting.
    /// </summary>
    Meeting,
    
    /// <summary>
    /// Reminder for a task deadline.
    /// </summary>
    Task,
    
    /// <summary>
    /// Reminder for a goal deadline or check-in.
    /// </summary>
    Goal,
    
    /// <summary>
    /// Alert when a team member hasn't had a 1:1 recently.
    /// </summary>
    Engagement,
    
    /// <summary>
    /// User-created custom reminder.
    /// </summary>
    Custom
}
