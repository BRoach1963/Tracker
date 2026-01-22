namespace ProCohere.Avalonia.Models;

/// <summary>
/// Outcome types for agenda items.
/// These represent the different kinds of results that can come from discussing an agenda item.
/// </summary>
public static class OutcomeType
{
    /// <summary>A task was created from this agenda item.</summary>
    public const string TaskCreated = "task_created";
    
    /// <summary>A new goal was created from this agenda item.</summary>
    public const string GoalCreated = "goal_created";
    
    /// <summary>An existing goal was updated based on discussion.</summary>
    public const string GoalUpdated = "goal_updated";
    
    /// <summary>A follow-up meeting was scheduled.</summary>
    public const string FollowUpScheduled = "follow_up_scheduled";
    
    /// <summary>A decision was recorded from the discussion.</summary>
    public const string DecisionRecorded = "decision_recorded";
    
    /// <summary>Feedback was captured during the discussion.</summary>
    public const string FeedbackCaptured = "feedback_captured";
    
    /// <summary>Notes were added to capture discussion context.</summary>
    public const string NotesAdded = "notes_added";

    /// <summary>All valid outcome types.</summary>
    public static readonly string[] All =
    {
        TaskCreated, GoalCreated, GoalUpdated, FollowUpScheduled,
        DecisionRecorded, FeedbackCaptured, NotesAdded
    };

    /// <summary>
    /// Gets a display-friendly name for an outcome type.
    /// </summary>
    public static string GetDisplayName(string? type) => type switch
    {
        TaskCreated => "Task Created",
        GoalCreated => "Goal Created",
        GoalUpdated => "Goal Updated",
        FollowUpScheduled => "Follow-Up Scheduled",
        DecisionRecorded => "Decision Recorded",
        FeedbackCaptured => "Feedback Captured",
        NotesAdded => "Notes Added",
        _ => type ?? "Unknown"
    };

    /// <summary>
    /// Gets the Material Design icon path data for an outcome type.
    /// </summary>
    public static string GetIcon(string? type) => type switch
    {
        // Checkbox icon for task
        TaskCreated => "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M10,17L5,12L6.41,10.58L10,14.17L17.59,6.58L19,8L10,17Z",
        // Target icon for goal
        GoalCreated or GoalUpdated => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8Z",
        // Calendar icon for follow-up
        FollowUpScheduled => "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1M17,12H12V17H17V12Z",
        // Speech bubble icon for decision
        DecisionRecorded => "M9,22A1,1 0 0,1 8,21V18H4A2,2 0 0,1 2,16V4C2,2.89 2.9,2 4,2H20A2,2 0 0,1 22,4V16A2,2 0 0,1 20,18H13.9L10.2,21.71C10,21.9 9.75,22 9.5,22V22H9M10,16V19.08L13.08,16H20V4H4V16H10Z",
        // Message icon for feedback
        FeedbackCaptured => "M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M6,9H18V11H6M14,14H6V12H14M18,8H6V6H18",
        // Document icon for notes
        NotesAdded => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M13,13H7V11H13V13M17,17H7V15H17V17Z",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the color for an outcome type badge.
    /// </summary>
    public static string GetColor(string? type) => type switch
    {
        TaskCreated => "#4CAF50",        // Green
        GoalCreated => "#2196F3",        // Blue
        GoalUpdated => "#03A9F4",        // Light Blue
        FollowUpScheduled => "#9C27B0",  // Purple
        DecisionRecorded => "#FF9800",   // Orange
        FeedbackCaptured => "#E91E63",   // Pink
        NotesAdded => "#607D8B",         // Blue Grey
        _ => "#9E9E9E"                   // Grey
    };
}

/// <summary>
/// Carry-forward states for deferred agenda items.
/// These track the lifecycle of an item that was deferred to a future meeting.
/// </summary>
public static class CarryForwardState
{
    /// <summary>Item has been deferred but not yet shown in a future meeting.</summary>
    public const string Pending = "pending";
    
    /// <summary>Item has been shown as a suggestion in meeting prep.</summary>
    public const string Surfaced = "surfaced";
    
    /// <summary>Item was discussed and resolved in a subsequent meeting.</summary>
    public const string Resolved = "resolved";
    
    /// <summary>Item was converted to a task or other action.</summary>
    public const string Converted = "converted";
    
    /// <summary>Item expired without being addressed (30 days or 2 meetings).</summary>
    public const string Expired = "expired";

    /// <summary>All valid carry-forward states.</summary>
    public static readonly string[] All =
    {
        Pending, Surfaced, Resolved, Converted, Expired
    };

    /// <summary>
    /// Gets a display-friendly name for a carry-forward state.
    /// </summary>
    public static string GetDisplayName(string? state) => state switch
    {
        Pending => "Pending",
        Surfaced => "Surfaced",
        Resolved => "Resolved",
        Converted => "Converted",
        Expired => "Expired",
        _ => state ?? "Unknown"
    };

    /// <summary>
    /// Gets the color for a carry-forward state badge.
    /// </summary>
    public static string GetColor(string? state) => state switch
    {
        Pending => "#FFC107",    // Amber
        Surfaced => "#2196F3",   // Blue
        Resolved => "#4CAF50",   // Green
        Converted => "#9C27B0",  // Purple
        Expired => "#9E9E9E",    // Grey
        _ => "#9E9E9E"
    };
}

/// <summary>
/// Visibility levels for outcomes and notes.
/// Controls who can see the outcome content.
/// </summary>
public static class OutcomeVisibility
{
    /// <summary>Only the creator can see this outcome.</summary>
    public const string Private = "private";
    
    /// <summary>Only meeting attendees can see this outcome.</summary>
    public const string Attendees = "attendees";
    
    /// <summary>The creator's team can see this outcome.</summary>
    public const string Team = "team";
    
    /// <summary>The entire organization can see this outcome.</summary>
    public const string Organization = "organization";

    /// <summary>All valid visibility levels.</summary>
    public static readonly string[] All =
    {
        Private, Attendees, Team, Organization
    };

    /// <summary>
    /// Gets a display-friendly name for a visibility level.
    /// </summary>
    public static string GetDisplayName(string? visibility) => visibility switch
    {
        Private => "Private (only me)",
        Attendees => "Meeting Attendees",
        Team => "My Team",
        Organization => "Organization",
        _ => visibility ?? "Unknown"
    };
}
