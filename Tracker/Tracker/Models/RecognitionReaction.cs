using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.DataModels;

namespace Tracker.Models;

/// <summary>
/// Represents a reaction to a recognition post.
/// Maps to Supabase recognition_reactions table.
/// </summary>
public class RecognitionReaction
{
    /// <summary>
    /// Unique identifier for this reaction.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Recognition this reaction is for.
    /// </summary>
    [Required]
    public Guid RecognitionId { get; set; }

    /// <summary>
    /// Team member who reacted.
    /// </summary>
    [Required]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// Type of reaction (like, celebrate, support, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReactionType { get; set; } = "like";

    /// <summary>
    /// When this reaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RecognitionId))]
    public virtual Recognition? Recognition { get; set; }

    [ForeignKey(nameof(TeamMemberId))]
    public virtual TeamMember? TeamMember { get; set; }
}
