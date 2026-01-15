using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Key deliverable within a project.
    /// Maps to Supabase 'milestones' table.
    /// </summary>
    [Table("milestones")]
    public class Milestone
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Project this milestone belongs to.
        /// Maps to: project_id UUID NOT NULL
        /// </summary>
        [Column("project_id")]
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Milestone title.
        /// Maps to: title VARCHAR(200) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the milestone.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Target date for completion.
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
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent project.
        /// </summary>
        [NotMapped]
        public Project? Project { get; set; }

        #endregion
    }
}
