using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents public recognition/kudos given from one team member to another.
    /// Recognition is a form of public praise that acknowledges contributions.
    /// Maps to Supabase 'recognition' table (16 columns).
    /// </summary>
    [Table("recognition")]
    public class Kudos : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Unique identifier (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this recognition belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The team member who gave this recognition.
        /// Maps to: from_team_member_id UUID NOT NULL
        /// </summary>
        [Column("from_team_member_id")]
        public Guid FromTeamMemberId { get; set; }

        /// <summary>
        /// The team member who received this recognition.
        /// Maps to: to_team_member_id UUID NOT NULL
        /// </summary>
        [Column("to_team_member_id")]
        public Guid ToTeamMemberId { get; set; }

        /// <summary>
        /// Project this recognition relates to.
        /// Maps to: project_id UUID NULL
        /// </summary>
        [Column("project_id")]
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Goal this recognition relates to.
        /// Maps to: goal_id UUID NULL
        /// </summary>
        [Column("goal_id")]
        public Guid? GoalId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Recognition title/headline.
        /// Maps to: title VARCHAR(200) NOT NULL
        /// </summary>
        [Column("title")]
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The recognition message content.
        /// Maps to: message TEXT NOT NULL
        /// </summary>
        [Column("message")]
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Badge type: team_player, innovator, customer_focus, leader, mentor, etc.
        /// Maps to: badge_type VARCHAR(100) NULL
        /// </summary>
        [Column("badge_type")]
        [MaxLength(100)]
        public string? BadgeType { get; set; }

        /// <summary>
        /// Company values this recognition acknowledges (JSONB array).
        /// Maps to: company_values JSONB NULL
        /// </summary>
        [Column("company_values")]
        public string? CompanyValuesJson { get; set; }

        #endregion

        #region Flags

        /// <summary>
        /// Whether this recognition is public to the organization.
        /// Maps to: is_public BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_public")]
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Count of reactions/emoji responses to this recognition.
        /// Maps to: reactions_count INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("reactions_count")]
        public int ReactionsCount { get; set; } = 0;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// The organization this recognition belongs to.
        /// </summary>
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member who gave this recognition.
        /// </summary>
        public TeamMember? FromTeamMember { get; set; }

        /// <summary>
        /// The team member who received this recognition.
        /// </summary>
        public TeamMember? ToTeamMember { get; set; }

        /// <summary>
        /// Project this recognition relates to.
        /// </summary>
        public Project? Project { get; set; }

        /// <summary>
        /// Goal this recognition relates to.
        /// </summary>
        public Goal? Goal { get; set; }

        #endregion
    }
}
