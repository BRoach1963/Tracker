using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// A milestone within a Goal.
    /// Maps to: goal_milestones (10 columns)
    /// NOTE: This table does NOT have soft delete columns - just timestamps.
    /// </summary>
    [Table("goal_milestones")]
    public class GoalMilestone
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent goal this milestone belongs to.
        /// Maps to: goal_id UUID NOT NULL
        /// </summary>
        [Column("goal_id")]
        public Guid GoalId { get; set; }

        /// <summary>
        /// Milestone title.
        /// Maps to: title VARCHAR(200) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Target date for this milestone.
        /// Maps to: target_date DATE NOT NULL
        /// </summary>
        [Column("target_date")]
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// When the milestone was completed.
        /// Maps to: completed_date DATE NULL
        /// </summary>
        [Column("completed_date")]
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Whether the milestone is completed.
        /// Maps to: is_completed BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_completed")]
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Sort order for display.
        /// Maps to: sort_order INT4 NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// When the milestone was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the milestone was last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Navigation to the parent goal.
        /// </summary>
        [NotMapped]
        public Goal? Goal { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Is the milestone overdue?
        /// </summary>
        [NotMapped]
        public bool IsOverdue => !IsCompleted && TargetDate < DateTime.Today;

        /// <summary>
        /// Days until target date (negative if overdue).
        /// </summary>
        [NotMapped]
        public int DaysRemaining => (int)(TargetDate - DateTime.Today).TotalDays;

        #endregion
    }
}
