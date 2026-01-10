using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Tracker.Database.Entities.Supabase;

/// <summary>
/// Role entity - defines permissions within an organization.
/// Maps to the 'roles' table in Supabase.
/// </summary>
[Table("roles")]
public class Role : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// Role name (admin, manager, team_lead, member, viewer).
    /// </summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description.
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    // Organization permissions
    [Column("can_manage_org")]
    public bool CanManageOrg { get; set; }

    [Column("can_manage_billing")]
    public bool CanManageBilling { get; set; }

    // User permissions
    [Column("can_manage_users")]
    public bool CanManageUsers { get; set; }

    [Column("can_invite_users")]
    public bool CanInviteUsers { get; set; }

    [Column("can_assign_roles")]
    public bool CanAssignRoles { get; set; }

    // Team permissions
    [Column("can_manage_teams")]
    public bool CanManageTeams { get; set; }

    [Column("can_create_teams")]
    public bool CanCreateTeams { get; set; }

    // Goal permissions
    [Column("can_create_goals")]
    public bool CanCreateGoals { get; set; }

    [Column("can_edit_all_goals")]
    public bool CanEditAllGoals { get; set; }

    [Column("can_edit_own_goals")]
    public bool CanEditOwnGoals { get; set; }

    [Column("can_view_team_goals")]
    public bool CanViewTeamGoals { get; set; }

    [Column("can_view_org_goals")]
    public bool CanViewOrgGoals { get; set; }

    // Metric permissions
    [Column("can_create_metrics")]
    public bool CanCreateMetrics { get; set; }

    [Column("can_edit_metrics")]
    public bool CanEditMetrics { get; set; }

    [Column("can_view_team_metrics")]
    public bool CanViewTeamMetrics { get; set; }

    [Column("can_view_org_metrics")]
    public bool CanViewOrgMetrics { get; set; }

    // Task permissions
    [Column("can_create_tasks")]
    public bool CanCreateTasks { get; set; }

    [Column("can_assign_tasks")]
    public bool CanAssignTasks { get; set; }

    [Column("can_view_team_tasks")]
    public bool CanViewTeamTasks { get; set; }

    // Meeting permissions
    [Column("can_schedule_meetings")]
    public bool CanScheduleMeetings { get; set; }

    [Column("can_run_meetings")]
    public bool CanRunMeetings { get; set; }

    [Column("can_participate_meetings")]
    public bool CanParticipateMeetings { get; set; }

    [Column("can_view_meeting_notes")]
    public bool CanViewMeetingNotes { get; set; }

    // Feedback permissions
    [Column("can_give_feedback")]
    public bool CanGiveFeedback { get; set; }

    [Column("can_receive_feedback")]
    public bool CanReceiveFeedback { get; set; }

    [Column("can_view_team_feedback")]
    public bool CanViewTeamFeedback { get; set; }

    // Analytics permissions
    [Column("can_view_team_analytics")]
    public bool CanViewTeamAnalytics { get; set; }

    [Column("can_view_org_analytics")]
    public bool CanViewOrgAnalytics { get; set; }

    [Column("can_export_data")]
    public bool CanExportData { get; set; }

    /// <summary>
    /// System roles cannot be deleted.
    /// </summary>
    [Column("is_system_role")]
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Display order in role lists.
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
