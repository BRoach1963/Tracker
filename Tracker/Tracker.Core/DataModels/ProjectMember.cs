using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Team member assignment to a project.
    /// Maps to Supabase 'project_members' table.
    /// </summary>
    [Table("project_members")]
    public class ProjectMember
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Project this member is assigned to.
        /// Maps to: project_id UUID NOT NULL
        /// </summary>
        [Column("project_id")]
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Team member assigned to the project.
        /// Maps to: team_member_id UUID NOT NULL
        /// </summary>
        [Column("team_member_id")]
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Role on the project (owner, contributor, reviewer).
        /// Maps to: role VARCHAR(100) NULL
        /// </summary>
        [Column("role")]
        [MaxLength(100)]
        public string? Role { get; set; }

        /// <summary>
        /// When the member joined the project.
        /// Maps to: joined_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent project.
        /// </summary>
        [NotMapped]
        public Project? Project { get; set; }

        /// <summary>
        /// Team member.
        /// </summary>
        [NotMapped]
        public TeamMember? TeamMember { get; set; }

        #endregion
    }
}
