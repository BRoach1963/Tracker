using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Kudos model - maps to the kudos table in Supabase procohere schema.
/// Peer or manager recognition messages.
/// </summary>
[Table("kudos")]
public class Kudos : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Participants

    /// <summary>
    /// Team member who sent the kudos.
    /// </summary>
    [Column("from_member_id")]
    public Guid FromMemberId { get; set; }

    /// <summary>
    /// Team member who received the kudos.
    /// </summary>
    [Column("to_member_id")]
    public Guid ToMemberId { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Recognition message.
    /// </summary>
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Category: 'teamwork', 'innovation', 'leadership', etc.
    /// </summary>
    [Column("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Whether visible to entire organization.
    /// </summary>
    [Column("is_public")]
    public bool IsPublic { get; set; }

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
        "teamwork" => "Teamwork",
        "innovation" => "Innovation",
        "leadership" => "Leadership",
        "customer_focus" => "Customer Focus",
        "quality" => "Quality",
        "above_and_beyond" => "Above & Beyond",
        _ => Category ?? "General"
    };

    #endregion
}
