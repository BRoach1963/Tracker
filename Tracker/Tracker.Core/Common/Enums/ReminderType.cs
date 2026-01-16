namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Types of reminders in the system.
    /// </summary>
    public enum ReminderType
    {
        /// <summary>Reminder for an upcoming 1:1 meeting.</summary>
        Meeting,
        
        /// <summary>Reminder that a team member hasn't had a 1:1 recently.</summary>
        Engagement,
        
        /// <summary>Reminder for a task deadline.</summary>
        Task,
        
        /// <summary>Reminder for a goal deadline.</summary>
        Goal,
        
        /// <summary>Custom user-created reminder.</summary>
        Custom
    }
}

