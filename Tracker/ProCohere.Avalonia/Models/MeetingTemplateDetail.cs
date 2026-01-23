using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Meeting template model - maps to the meeting_templates table in Supabase.
/// Templates define reusable agenda structures for common meeting types.
/// </summary>
[Table("meeting_templates")]
public class MeetingTemplateDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Meeting type: 'one_on_one', 'team', 'project', 'custom'.
    /// </summary>
    [Column("meeting_type")]
    public string MeetingType { get; set; } = TemplateCategory.Custom;

    /// <summary>
    /// Default duration in minutes for meetings created from this template.
    /// </summary>
    [Column("default_duration")]
    public int? DefaultDuration { get; set; }

    /// <summary>
    /// Default agenda items stored as JSONB.
    /// </summary>
    [Column("default_agenda")]
    public string? DefaultAgendaJson { get; set; }

    /// <summary>
    /// Whether this is a built-in template (cannot be deleted by users).
    /// </summary>
    [Column("is_system_template")]
    public bool IsSystemTemplate { get; set; }

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

    #region Non-DB Properties

    /// <summary>
    /// Template items (parsed from DefaultAgendaJson). Set by service.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public List<MeetingTemplateItem> Items { get; set; } = new();

    /// <summary>
    /// Display icon based on meeting type.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string CategoryIcon => MeetingType switch
    {
        TemplateCategory.OneOnOne => "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
        TemplateCategory.Team => "M16,13C15.71,13 15.38,13 15.03,13.05C16.19,13.89 17,15 17,16.5V19H23V16.5C23,14.17 18.33,13 16,13M8,13C5.67,13 1,14.17 1,16.5V19H15V16.5C15,14.17 10.33,13 8,13M8,11A3,3 0 0,0 11,8A3,3 0 0,0 8,5A3,3 0 0,0 5,8A3,3 0 0,0 8,11M16,11A3,3 0 0,0 19,8A3,3 0 0,0 16,5A3,3 0 0,0 13,8A3,3 0 0,0 16,11Z",
        TemplateCategory.Project => "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,5V19H5V5H19M17,17H7V7H17V17Z",
        _ => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M12.9,14.5L15.8,19H14L12,15.6L10,19H8.2L11.1,14.5L8.2,10H10L12,13.4L14,10H15.8L12.9,14.5Z"
    };

    /// <summary>
    /// Display name for the meeting type.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string CategoryDisplay => TemplateCategory.GetDisplayName(MeetingType);

    /// <summary>
    /// Number of items in the template.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int ItemCount => Items.Count;

    /// <summary>
    /// Display text for item count.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string ItemCountDisplay => ItemCount == 1 ? "1 item" : $"{ItemCount} items";

    #endregion
}

/// <summary>
/// Meeting template item - stored as JSONB in meeting_templates.default_agenda.
/// NOT a separate table in the database.
/// </summary>
public class MeetingTemplateItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsOptional { get; set; }

    public int? SuggestedDurationMinutes { get; set; }
}

/// <summary>
/// Template category constants.
/// </summary>
public static class TemplateCategory
{
    public const string OneOnOne = "one_on_one";
    public const string Team = "team";
    public const string Project = "project";
    public const string Custom = "custom";

    public static readonly string[] All = { OneOnOne, Team, Project, Custom };

    public static string GetDisplayName(string? category) => category switch
    {
        OneOnOne => "1:1 Meeting",
        Team => "Team Meeting",
        Project => "Project Review",
        Custom => "Custom",
        _ => category ?? "Unknown"
    };
}
