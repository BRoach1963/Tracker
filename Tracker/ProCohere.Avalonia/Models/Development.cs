using System;
using System.Collections.Generic;
using System.Linq;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Competency model - maps to the competencies table in Supabase procohere schema.
/// Organization-defined skills that team members can be assessed against.
/// </summary>
[Table("competencies")]
public class Competency : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Category: 'technical', 'leadership', 'communication', etc.
    /// </summary>
    [Column("category")]
    public string? Category { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool HasCategory => !string.IsNullOrEmpty(Category);

    public string CategoryDisplay => Category switch
    {
        "technical" => "Technical",
        "leadership" => "Leadership",
        "communication" => "Communication",
        "problem_solving" => "Problem Solving",
        "teamwork" => "Teamwork",
        _ => Category ?? "General"
    };

    #endregion
}

/// <summary>
/// Team member competency model - maps to team_member_competencies table.
/// Tracks a team member's proficiency in a specific competency.
/// </summary>
[Table("team_member_competencies")]
public class TeamMemberCompetency : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    [Column("competency_id")]
    public Guid CompetencyId { get; set; }

    #endregion

    #region Assessment

    /// <summary>
    /// Proficiency level (e.g., 1-5).
    /// </summary>
    [Column("proficiency_level")]
    public int? ProficiencyLevel { get; set; }

    /// <summary>
    /// Team member who performed the assessment.
    /// </summary>
    [Column("assessed_by")]
    public Guid? AssessedBy { get; set; }

    [Column("assessed_at")]
    public DateTime? AssessedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool HasAssessment => ProficiencyLevel.HasValue;
    public bool HasNotes => !string.IsNullOrEmpty(Notes);

    public string ProficiencyDisplay => ProficiencyLevel switch
    {
        1 => "Beginner",
        2 => "Developing",
        3 => "Proficient",
        4 => "Advanced",
        5 => "Expert",
        _ => "Not Assessed"
    };

    #endregion
}

/// <summary>
/// Development plan model - maps to development_plans table.
/// Career development plans for team members.
/// </summary>
[Table("development_plans")]
public class DevelopmentPlan : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    #endregion

    #region Status & Dates

    /// <summary>
    /// Status: 'draft', 'active', 'completed', 'cancelled'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("target_date")]
    public DateTime? TargetDate { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Navigation (not mapped)

    /// <summary>
    /// Items in this development plan (populated by service).
    /// </summary>
    public List<DevelopmentPlanItem> Items { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsDraft => Status == "draft";
    public bool IsActive => Status == "active";
    public bool IsCompleted => Status == "completed";
    public bool IsCancelled => Status == "cancelled";

    public string StatusDisplay => Status switch
    {
        "draft" => "Draft",
        "active" => "Active",
        "completed" => "Completed",
        "cancelled" => "Cancelled",
        _ => Status
    };

    public int ItemCount => Items.Count;
    
    public int CompletedItemCount => Items.Count(i => i.IsCompleted);
    
    /// <summary>
    /// Progress percentage (0-100) based on completed items.
    /// </summary>
    public decimal ProgressPercentage => ItemCount > 0 
        ? (decimal)CompletedItemCount / ItemCount * 100 
        : 0;
    
    /// <summary>
    /// Formatted target date for display.
    /// </summary>
    public string TargetDateDisplay => TargetDate?.ToString("MMM d, yyyy") ?? "";

    #endregion
}

/// <summary>
/// Development plan item model - maps to development_plan_items table.
/// Individual action items within a development plan.
/// </summary>
[Table("development_plan_items")]
public class DevelopmentPlanItem : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("development_plan_id")]
    public Guid DevelopmentPlanId { get; set; }

    /// <summary>
    /// Optional link to a competency this item develops.
    /// </summary>
    [Column("competency_id")]
    public Guid? CompetencyId { get; set; }

    #endregion

    #region Content

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Item type: 'training', 'project', 'mentoring', 'reading', etc.
    /// </summary>
    [Column("item_type")]
    public string? ItemType { get; set; }

    #endregion

    #region Status & Dates

    /// <summary>
    /// Status: 'not_started', 'in_progress', 'completed'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "not_started";

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool IsNotStarted => Status == "not_started";
    public bool IsInProgress => Status == "in_progress";
    public bool IsCompleted => Status == "completed";

    public bool HasCompetency => CompetencyId.HasValue;
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && !IsCompleted;

    public string StatusDisplay => Status switch
    {
        "not_started" => "Not Started",
        "in_progress" => "In Progress",
        "completed" => "Completed",
        _ => Status
    };

    public string ItemTypeDisplay => ItemType switch
    {
        "training" => "Training",
        "project" => "Project",
        "mentoring" => "Mentoring",
        "reading" => "Reading",
        "certification" => "Certification",
        "workshop" => "Workshop",
        _ => ItemType ?? "Other"
    };

    #endregion
}
