using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models.Dtos;

/// <summary>
/// Data transfer object for ai_insight_actions table in Supabase.
/// Maps to procohere.ai_insight_actions table.
/// </summary>
[Table("ai_insight_actions")]
public class InsightActionDto : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }
    
    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }
    
    [Column("insight_id")]
    public Guid? InsightId { get; set; }
    
    [Column("signature_hash")]
    public string SignatureHash { get; set; } = string.Empty;
    
    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;
    
    [Column("action_reason")]
    public string? ActionReason { get; set; }
    
    [Column("action_metadata")]
    public string ActionMetadata { get; set; } = "{}";
    
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    
    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }
}
