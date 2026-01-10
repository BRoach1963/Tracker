using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a team within an organization (Engineering, Sales, Legal, etc.).
    /// Teams group team members and can have a designated lead.
    /// </summary>
    [Table("teams")]
    public class Team : AuditableEntity
    {
        /// <summary>
        /// Primary key - UUID for PostgreSQL compatibility.
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The organization this team belongs to.
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Team name (e.g., "Engineering", "Sales", "Legal").
        /// </summary>
        [Column("name")]
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional team description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Hex color for UI display (e.g., "#FF5733").
        /// </summary>
        [Column("color")]
        [MaxLength(7)]
        public string? Color { get; set; }

        /// <summary>
        /// The user who leads this team (optional).
        /// </summary>
        [Column("lead_user_id")]
        public Guid? LeadUserId { get; set; }

        /// <summary>
        /// Whether the team is currently active.
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        #region Navigation Properties

        /// <summary>
        /// The organization this team belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team lead (user).
        /// </summary>
        public User? Lead { get; set; }

        /// <summary>
        /// Team memberships (which team members belong to this team).
        /// </summary>
        public ICollection<TeamMembership> Memberships { get; set; } = new List<TeamMembership>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Number of active members in this team.
        /// </summary>
        [NotMapped]
        public int MemberCount => Memberships?.Count(m => m.LeftAt == null) ?? 0;

        /// <summary>
        /// Display name with member count.
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{Name} ({MemberCount})";

        #endregion
    }
}
