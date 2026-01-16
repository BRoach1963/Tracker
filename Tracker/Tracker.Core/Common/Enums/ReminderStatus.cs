namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Status of a reminder.
    /// </summary>
    public enum ReminderStatus
    {
        /// <summary>Reminder is pending and will fire at the scheduled time.</summary>
        Pending,
        
        /// <summary>Reminder has been sent/triggered.</summary>
        Sent,
        
        /// <summary>Reminder has been triggered/shown to user.</summary>
        Triggered,
        
        /// <summary>Reminder has been snoozed.</summary>
        Snoozed,
        
        /// <summary>Reminder has been dismissed by user.</summary>
        Dismissed,
        
        /// <summary>Reminder was cancelled (e.g., meeting was deleted).</summary>
        Cancelled
    }
}

