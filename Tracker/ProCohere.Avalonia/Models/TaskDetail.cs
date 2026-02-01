using System;
using System.Windows.Input;
using Avalonia.Media;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Task model - maps to the tasks table in Supabase.
/// Used for dashboard upcoming tasks.
/// Implements IDetailEntity for use in EntityDetailFlyout.
/// </summary>
[Table("tasks")]
public class TaskDetail : BaseModel, IDetailEntity
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("priority")]
    public string? Priority { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("assigned_to")]
    public Guid? OwnerTeamMemberId { get; set; }

    [Column("created_by")]
    public Guid CreatedByTeamMemberId { get; set; }

    /// <summary>
    /// Source type for provenance tracking (e.g., 'meeting', 'agenda_item', 'goal', 'feedback', 'note').
    /// NULL means manually created task.
    /// </summary>
    [Column("source_type")]
    public string? SourceType { get; set; }

    /// <summary>
    /// Source entity ID for provenance tracking.
    /// References the entity that this task was created from.
    /// </summary>
    [Column("source_id")]
    public Guid? SourceId { get; set; }

    /// <summary>
    /// When the task was marked as completed.
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #region Project Link
    
    /// <summary>
    /// ID of the linked project (populated from project_links table).
    /// Not a DB column - set by service when fetching tasks.
    /// </summary>
    public Guid? ProjectId { get; set; }
    
    /// <summary>
    /// Title of the linked project (for display).
    /// Not a DB column - set by service when fetching tasks.
    /// </summary>
    public string? ProjectTitle { get; set; }
    
    /// <summary>
    /// Whether this task is linked to a project.
    /// </summary>
    public bool HasProject => ProjectId.HasValue;
    
    #endregion

    #region Computed Properties

    /// <summary>
    /// Name of the owner (set by DashboardService join).
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Name of who created/assigned the task (set by service join).
    /// </summary>
    public string? AssignedByName { get; set; }

    /// <summary>
    /// Whether the task is overdue.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != "completed";

    /// <summary>
    /// Whether the task has a due date set.
    /// </summary>
    public bool HasDueDate => DueDate.HasValue;

    /// <summary>
    /// Whether the task is completed.
    /// </summary>
    public bool IsCompleted => Status == "completed";

    /// <summary>
    /// Display text for the task status.
    /// </summary>
    public string StatusDisplay => Status?.ToLower() switch
    {
        "not_started" => "Not Started",
        "in_progress" => "In Progress",
        "completed" => "Completed",
        "blocked" => "Blocked",
        _ => Status ?? "Unknown"
    };

    /// <summary>
    /// Whether this task was created from another entity (has provenance).
    /// </summary>
    public bool HasSource => !string.IsNullOrEmpty(SourceType) && SourceId.HasValue;

    /// <summary>
    /// Display text for the source type.
    /// </summary>
    public string SourceTypeDisplay => SourceType?.ToLower() switch
    {
        "agenda_item" => "Agenda Item",
        "meeting" => "Meeting",
        "goal" => "Goal",
        "feedback" => "Feedback",
        "note" => "Note",
        _ => "Manual"
    };

    /// <summary>
    /// Name/title of the source entity (set by service when loading tasks).
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Display text for provenance, combining type and name.
    /// </summary>
    public string ProvenanceDisplay
    {
        get
        {
            if (!HasSource) return "Created manually";
            if (!string.IsNullOrEmpty(SourceName))
                return $"From {SourceTypeDisplay}: {SourceName}";
            return $"From {SourceTypeDisplay}";
        }
    }

    /// <summary>
    /// Short provenance text for list display.
    /// </summary>
    public string ProvenanceShort
    {
        get
        {
            if (!HasSource) return "";
            return $"From {SourceTypeDisplay}";
        }
    }

    /// <summary>
    /// Icon path for the source type.
    /// </summary>
    public string SourceIconPath => SourceType?.ToLower() switch
    {
        "agenda_item" => "M3,5H9V11H3V5M5,7V9H7V7H5M11,7H21V9H11V7M11,15H21V17H11V15M5,20L1.5,16.5L2.91,15.09L5,17.17L9.59,12.59L11,14L5,20Z",
        "meeting" => "M12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22C6.47,22 2,17.5 2,12A10,10 0 0,1 12,2M12.5,7V12.25L17,14.92L16.25,16.15L11,13V7H12.5Z",
        "goal" => "M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3",
        "feedback" => "M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M6,9H18V11H6M14,14H6V12H14M18,8H6V6H18",
        "note" => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z",
        _ => "M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"
    };

    /// <summary>
    /// Friendly due date text.
    /// </summary>
    public string DueDateText
    {
        get
        {
            if (!DueDate.HasValue)
                return "No due date";

            var today = DateTime.UtcNow.Date;
            var dueDate = DueDate.Value.Date;

            if (dueDate == today)
                return "Due today";
            if (dueDate == today.AddDays(1))
                return "Due tomorrow";
            if (dueDate < today)
                return "Overdue";
            if ((dueDate - today).Days <= 7)
                return $"Due in {(dueDate - today).Days}d";
            return dueDate.ToString("MMM d");
        }
    }

    /// <summary>
    /// Color for due date indicator: muted warning for overdue, neutral for everything else.
    /// Design: Urgency communicated through language, not bright colors.
    /// </summary>
    public IBrush DueDateBrush
    {
        get
        {
            if (!DueDate.HasValue)
                return new SolidColorBrush(Color.Parse("#9CA3AF")); // Neutral gray

            var today = DateTime.UtcNow.Date;
            var dueDate = DueDate.Value.Date;

            if (dueDate < today)
                return new SolidColorBrush(Color.Parse("#B45309")); // Muted amber - overdue (only warning)
            return new SolidColorBrush(Color.Parse("#6B7280")); // Neutral gray for all else
        }
    }

    /// <summary>
    /// Whether this task was assigned by someone else (not self-assigned).
    /// Returns true if CreatedByTeamMemberId differs from OwnerTeamMemberId.
    /// </summary>
    public bool IsAssignedByOther => 
        OwnerTeamMemberId.HasValue && 
        CreatedByTeamMemberId != OwnerTeamMemberId.Value;

    /// <summary>
    /// Priority display text - no emoji colors, just clear text.
    /// </summary>
    public string PriorityDisplay => Priority?.ToLower() switch
    {
        "high" => "High",
        "medium" => "Medium",
        "low" => "Low",
        _ => ""
    };

    /// <summary>
    /// Alias for DueDateText for XAML binding.
    /// </summary>
    public string DueDateDisplay => DueDateText;

    /// <summary>
    /// Brush for priority badge background - using neutral tones, not traffic-light colors.
    /// Priority is communicated through ordering and language, not judgmental colors.
    /// </summary>
    public IBrush PriorityBrush => Priority?.ToLower() switch
    {
        "high" => new SolidColorBrush(Color.Parse("#4B5563")), // Dark neutral
        "medium" => new SolidColorBrush(Color.Parse("#6B7280")), // Medium neutral
        "low" => new SolidColorBrush(Color.Parse("#9CA3AF")), // Light neutral
        _ => new SolidColorBrush(Color.Parse("#D1D5DB")) // Very light
    };

    /// <summary>
    /// String color for priority badge (for XAML binding without converter).
    /// </summary>
    public string PriorityColor => Priority?.ToLower() switch
    {
        "high" => "#4B5563", // Dark neutral
        "medium" => "#6B7280", // Medium neutral
        "low" => "#9CA3AF", // Light neutral
        _ => "#D1D5DB" // Very light
    };

    /// <summary>
    /// Alias for OwnerName for consistent naming in UI.
    /// </summary>
    public string? AssignedToName => OwnerName;

    #endregion

    #region IDetailEntity Commands (wired up by parent ViewModel)

    /// <summary>
    /// Command to close the detail flyout. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? CloseCommand { get; set; }

    /// <summary>
    /// Command to edit this task. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? EditCommand { get; set; }

    /// <summary>
    /// Command to delete this task. Wired up by parent ViewModel.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ICommand? DeleteCommand { get; set; }

    #endregion
}
