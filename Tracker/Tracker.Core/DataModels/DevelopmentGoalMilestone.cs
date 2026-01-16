using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// A milestone within a development goal.
    /// Maps to Supabase 'development_goal_milestones' table (11 columns).
    /// Note: This table does NOT have organization_id or soft delete - inherits minimally from AuditableEntity.
    /// </summary>
    [Table("development_goal_milestones")]
    public class DevelopmentGoalMilestone : AuditableEntity
    {
        #region Primary Key & Foreign Keys

        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent development goal.
        /// Maps to: goal_id UUID NOT NULL
        /// </summary>
        [Column("goal_id")]
        public Guid GoalId { get; set; }

        #endregion

        #region Content

        /// <summary>
        /// Milestone title.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Additional notes.
        /// Maps to: notes TEXT NULL
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        #endregion

        #region Status & Dates

        /// <summary>
        /// Target date for this milestone.
        /// Maps to: target_date DATE NULL
        /// </summary>
        [Column("target_date")]
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// When the milestone was completed.
        /// Maps to: completed_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Current status: not_started, in_progress, completed, cancelled.
        /// Maps to: status milestone_status (enum) NOT NULL DEFAULT 'not_started'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "not_started";

        /// <summary>
        /// Sort order for display.
        /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Parent development goal.
        /// </summary>
        public DevelopmentGoal? Goal { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether milestone is completed.
        /// </summary>
        [NotMapped]
        public bool IsCompleted => Status == "completed" || CompletedAt.HasValue;

        #endregion
    }
}
