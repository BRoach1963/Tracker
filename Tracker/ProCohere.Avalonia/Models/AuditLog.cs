using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Audit log model - maps to the audit_log table in Supabase procohere schema.
/// Immutable audit trail of all data changes. Append-only, no updates or deletes.
/// </summary>
[Table("audit_log")]
public class AuditLog : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Actor

    /// <summary>
    /// FK to public.users - the user who performed the action.
    /// </summary>
    [Column("actor_id")]
    public Guid? ActorId { get; set; }

    /// <summary>
    /// FK to team_members - the team member context.
    /// </summary>
    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }

    #endregion

    #region Action Details

    /// <summary>
    /// Action performed: 'create', 'update', 'delete', 'restore', 'login', 'logout', etc.
    /// </summary>
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected: 'meeting', 'task', 'goal', 'team_member', etc.
    /// </summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    [Column("entity_id")]
    public Guid? EntityId { get; set; }

    #endregion

    #region Change Data

    /// <summary>
    /// Previous values as JSON (for updates/deletes).
    /// </summary>
    [Column("old_values")]
    public string? OldValues { get; set; }

    /// <summary>
    /// New values as JSON (for creates/updates).
    /// </summary>
    [Column("new_values")]
    public string? NewValues { get; set; }

    #endregion

    #region Request Context

    /// <summary>
    /// Client IP address.
    /// </summary>
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Client user agent string.
    /// </summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    #endregion

    #region Timestamp

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Human-readable action description.
    /// </summary>
    public string ActionDisplay => Action switch
    {
        "create" => "Created",
        "update" => "Updated",
        "delete" => "Deleted",
        "restore" => "Restored",
        "login" => "Logged in",
        "logout" => "Logged out",
        _ => Action
    };

    /// <summary>
    /// Human-readable entity type.
    /// </summary>
    public string EntityTypeDisplay => EntityType switch
    {
        "meeting" => "Meeting",
        "task" => "Task",
        "goal" => "Goal",
        "team_member" => "Team Member",
        "feedback" => "Feedback",
        "note" => "Note",
        _ => EntityType
    };

    #endregion
}

/// <summary>
/// Audit action constants.
/// </summary>
public static class AuditActions
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Restore = "restore";
    public const string Login = "login";
    public const string Logout = "logout";
}
