using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Tracks the history of manager assignments for team members.
    /// 
    /// When a team member's manager changes (reorg, promotion, manager departure),
    /// a new record is created with the start date, and the previous record's
    /// end date is populated.
    /// 
    /// This enables:
    /// - Historical reporting (who managed whom during a period)
    /// - Data continuity when team members move between managers
    /// - Audit trail for organizational changes
    /// 
    /// The current manager relationship has EndDate = null.
    /// Maps to: manager_history (9 columns)
    /// NOTE: This table does NOT follow the standard soft delete pattern.
    /// </summary>
    [Table("manager_history")]
    public class ManagerHistory
    {
        /// <summary>
        /// Primary key - UUID.
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The organization this history record belongs to.
        /// Required for RLS filtering.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The team member whose manager assignment is being tracked.
        /// Maps to: team_member_id UUID NOT NULL
        /// </summary>
        [Column("team_member_id")]
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// The manager (user) who was assigned to the team member.
        /// Maps to: manager_user_id UUID NOT NULL
        /// </summary>
        [Column("manager_user_id")]
        public Guid ManagerUserId { get; set; }

        /// <summary>
        /// Date when this manager assignment started.
        /// Maps to: start_date DATE NOT NULL DEFAULT CURRENT_DATE
        /// </summary>
        [Column("start_date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Date when this manager assignment ended.
        /// Null indicates this is the current manager.
        /// Maps to: end_date DATE NULL
        /// </summary>
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Reason for the manager change.
        /// Examples: "reorg", "promotion", "manager_departure", "initial_assignment", "transfer"
        /// Maps to: change_reason VARCHAR(500) NULL
        /// </summary>
        [Column("change_reason")]
        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        /// <summary>
        /// When this record was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this record.
        /// Maps to: created_by UUID NULL
        /// </summary>
        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The organization this record belongs to.
        /// </summary>
        [NotMapped]
        public Organization? Organization { get; set; }

        /// <summary>
        /// The team member whose manager changed.
        /// </summary>
        [NotMapped]
        public TeamMember? TeamMember { get; set; }

        /// <summary>
        /// The manager (user) who was assigned.
        /// </summary>
        [NotMapped]
        public User? Manager { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether this is the current manager assignment.
        /// </summary>
        [NotMapped]
        public bool IsCurrent => EndDate == null;

        /// <summary>
        /// Duration of this manager assignment.
        /// </summary>
        [NotMapped]
        public TimeSpan Duration => (EndDate ?? DateTime.Today) - StartDate;

        /// <summary>
        /// Duration in months.
        /// </summary>
        [NotMapped]
        public int DurationMonths => (int)(Duration.TotalDays / 30.44);

        #endregion
    }
}
