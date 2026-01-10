using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents the many-to-many relationship between teams and team members.
    /// A team member can belong to multiple teams, and a team can have multiple members.
    /// </summary>
    [Table("team_memberships")]
    public class TeamMembership
    {
        /// <summary>
        /// Primary key - UUID for PostgreSQL compatibility.
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The team this membership is for.
        /// </summary>
        [Column("team_id")]
        public Guid TeamId { get; set; }

        /// <summary>
        /// The team member in this membership.
        /// </summary>
        [Column("team_member_id")]
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Whether this member is the lead of the team.
        /// </summary>
        [Column("is_lead")]
        public bool IsLead { get; set; } = false;

        /// <summary>
        /// When the member joined the team.
        /// </summary>
        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the member left the team (null = still active).
        /// </summary>
        [Column("left_at")]
        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// When this record was created.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this membership.
        /// </summary>
        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The team.
        /// </summary>
        public Team? Team { get; set; }

        /// <summary>
        /// The team member.
        /// </summary>
        public TeamMember? TeamMember { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether this membership is currently active.
        /// </summary>
        [NotMapped]
        public bool IsActive => LeftAt == null;

        /// <summary>
        /// Duration of membership (or current duration if still active).
        /// </summary>
        [NotMapped]
        public TimeSpan Duration => (LeftAt ?? DateTime.UtcNow) - JoinedAt;

        #endregion
    }
}
