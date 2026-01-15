using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Tracks dependencies between projects (Project A depends on Project B completing first).
    /// Maps to Supabase 'project_dependencies' table.
    /// </summary>
    [Table("project_dependencies")]
    public class ProjectDependency
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this dependency belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The project that HAS the dependency (the dependent project).
        /// Maps to: dependent_project_id UUID NOT NULL
        /// </summary>
        [Column("dependent_project_id")]
        public Guid DependentProjectId { get; set; }

        /// <summary>
        /// The project that MUST complete first (the required/prerequisite project).
        /// Maps to: required_project_id UUID NOT NULL
        /// </summary>
        [Column("required_project_id")]
        public Guid RequiredProjectId { get; set; }

        /// <summary>
        /// Type of dependency relationship.
        /// Maps to: dependency_type VARCHAR(50) NOT NULL DEFAULT 'finish_to_start'
        /// Values: finish_to_start, start_to_start, finish_to_finish, start_to_finish
        /// </summary>
        [Column("dependency_type")]
        [MaxLength(50)]
        public string DependencyType { get; set; } = "finish_to_start";

        /// <summary>
        /// Description of the dependency.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Is this a hard dependency (blocking) or soft (informational)?
        /// Maps to: is_blocking BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_blocking")]
        public bool IsBlocking { get; set; } = true;

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

        /// <summary>
        /// User who created this dependency.
        /// Maps to: created_by_user_id UUID NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid? CreatedByUserId { get; set; }

        #region Navigation Properties

        /// <summary>
        /// The dependent project.
        /// </summary>
        [NotMapped]
        public Project? DependentProject { get; set; }

        /// <summary>
        /// The required/prerequisite project.
        /// </summary>
        [NotMapped]
        public Project? RequiredProject { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Display name for the dependency type.
        /// </summary>
        [NotMapped]
        public string DependencyTypeDisplay => DependencyType switch
        {
            "finish_to_start" => "Finish to Start",
            "start_to_start" => "Start to Start",
            "finish_to_finish" => "Finish to Finish",
            "start_to_finish" => "Start to Finish",
            _ => DependencyType
        };

        #endregion
    }
}
