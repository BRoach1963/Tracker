using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models.Dtos;

/// <summary>
/// Data transfer object for ai_insights table in Supabase.
/// Maps to existing database schema (procohere.ai_insights).
/// </summary>
[Table("ai_insights")]
public class InsightDto : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [Column("team_member_id")]
    public Guid? TeamMemberId { get; set; }
    
    [Column("generated_for")]
    public Guid GeneratedFor { get; set; }
    
    [Column("insight_type")]
    public string InsightType { get; set; } = string.Empty;
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("source_type")]
    public string? SourceType { get; set; }
    
    [Column("source_id")]
    public Guid? SourceId { get; set; }
    
    [Column("relevance_score")]
    public decimal? RelevanceScore { get; set; }
    
    [Column("is_dismissed")]
    public bool IsDismissed { get; set; } = false;
    
    [Column("dismissed_at")]
    public DateTime? DismissedAt { get; set; }
    
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    
    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
}
