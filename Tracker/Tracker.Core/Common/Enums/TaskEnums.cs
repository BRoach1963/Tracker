namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Status of a task or project.
    /// Maps to Supabase task_status enum.
    /// </summary>
    public enum WorkItemStatus
    {
        NotStarted,
        InProgress,
        Blocked,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Priority level for tasks and projects.
    /// Maps to Supabase task_priority enum: low, medium, high, critical
    /// </summary>
    public enum WorkItemPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
