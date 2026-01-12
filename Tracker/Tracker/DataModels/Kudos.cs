using System;
using System.Collections.Generic;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents public recognition/kudos given from one team member to another.
    /// Recognition is a form of public praise that acknowledges contributions.
    /// Maps to Supabase 'recognition' table.
    /// </summary>
    public class Kudos : AuditableEntity
    {
        /// <summary>
        /// Unique identifier (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this recognition belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member who gave this recognition.
        /// </summary>
        public Guid FromTeamMemberId { get; set; }
        public TeamMember? FromTeamMember { get; set; }

        /// <summary>
        /// The team member who received this recognition.
        /// </summary>
        public Guid ToTeamMemberId { get; set; }
        public TeamMember? ToTeamMember { get; set; }

        /// <summary>
        /// Recognition title/headline.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The recognition message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Badge type: team_player, innovator, customer_focus, leader, mentor, etc.
        /// </summary>
        public string? BadgeType { get; set; }

        /// <summary>
        /// Company values this recognition acknowledges (JSONB array of strings).
        /// </summary>
        public List<string>? CompanyValues { get; set; }

        /// <summary>
        /// Whether this recognition is public to the organization.
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Count of reactions/emoji responses to this recognition.
        /// </summary>
        public int ReactionsCount { get; set; } = 0;

        /// <summary>
        /// When this recognition was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this recognition was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this recognition is deleted (soft delete).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// When this recognition was deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted this recognition.
        /// </summary>
        public Guid? DeletedBy { get; set; }
    }
}
