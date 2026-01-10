using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents public praise and recognition for team members.
/// Maps to Supabase recognition table.
/// </summary>
public class Recognition
{
    /// <summary>
    /// Unique identifier for this recognition.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Organization this recognition belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Team member who gave the recognition.
    /// </summary>
    [Required]
    public Guid FromTeamMemberId { get; set; }

    /// <summary>
    /// Team member who received the recognition.
    /// </summary>
    [Required]
    public Guid ToTeamMemberId { get; set; }

    /// <summary>
    /// Title of the recognition.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Recognition message content.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Badge type (team_player, innovator, customer_focus, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? BadgeType { get; set; }

    /// <summary>
    /// Related project ID (if any).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Related goal ID (if any).
    /// </summary>
    public Guid? GoalId { get; set; }

    /// <summary>
    /// Company values this demonstrates (stored as JSON).
    /// </summary>
    public string? CompanyValues { get; set; }

    /// <summary>
    /// Whether this recognition is public (shown in team feed).
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Count of reactions to this recognition.
    /// </summary>
    public int ReactionsCount { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this recognition is soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this recognition was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the recognition.
    /// </summary>
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(FromTeamMemberId))]
    public virtual TeamMember? FromTeamMember { get; set; }

    [ForeignKey(nameof(ToTeamMemberId))]
    public virtual TeamMember? ToTeamMember { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(GoalId))]
    public virtual Goal? Goal { get; set; }

    public virtual ICollection<RecognitionReaction> Reactions { get; set; } = new List<RecognitionReaction>();

    // Computed properties

    /// <summary>
    /// Whether this recognition has a badge.
    /// </summary>
    [NotMapped]
    public bool HasBadge => !string.IsNullOrWhiteSpace(BadgeType);

    /// <summary>
    /// A truncated preview of the message.
    /// </summary>
    [NotMapped]
    public string MessagePreview => Message.Length > 100 ? Message[..97] + "..." : Message;
}
