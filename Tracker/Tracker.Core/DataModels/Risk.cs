using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Core.Common.Enums;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// A risk that can be attached to projects, goals, tasks, or metrics.
    /// Maps to Supabase 'risks' table.
    /// Uses polymorphic association via entity_type and entity_id.
    /// </summary>
    [Table("risks")]
    public class Risk
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this risk belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Type of entity this risk is attached to.
        /// Maps to: entity_type VARCHAR(50) NOT NULL
        /// Values: project, goal, task, metric
        /// </summary>
        [Column("entity_type")]
        [MaxLength(50)]
        public string EntityType { get; set; } = "project";

        /// <summary>
        /// ID of the related entity.
        /// Maps to: entity_id UUID NOT NULL
        /// </summary>
        [Column("entity_id")]
        public Guid EntityId { get; set; }

        /// <summary>
        /// Short name for the risk.
        /// Maps to: name VARCHAR(200) NOT NULL
        /// </summary>
        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the risk.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Overall severity (stored as string).
        /// Maps to: severity risk_severity (enum) NOT NULL DEFAULT 'medium'
        /// </summary>
        [Column("severity")]
        [MaxLength(50)]
        public string SeverityString { get; set; } = "medium";

        /// <summary>
        /// Severity as enum.
        /// </summary>
        [NotMapped]
        public RiskLevelEnum Severity
        {
            get => SeverityString switch
            {
                "low" => RiskLevelEnum.Low,
                "high" => RiskLevelEnum.High,
                "critical" => RiskLevelEnum.Critical,
                _ => RiskLevelEnum.Medium
            };
            set => SeverityString = value switch
            {
                RiskLevelEnum.Low => "low",
                RiskLevelEnum.High => "high",
                RiskLevelEnum.Critical => "critical",
                _ => "medium"
            };
        }

        /// <summary>
        /// Likelihood of occurrence.
        /// Maps to: probability VARCHAR(50) DEFAULT 'possible'
        /// Values: unlikely, possible, likely, almost_certain
        /// </summary>
        [Column("probability")]
        [MaxLength(50)]
        public string Probability { get; set; } = "possible";

        /// <summary>
        /// Impact if the risk occurs.
        /// Maps to: impact VARCHAR(50) DEFAULT 'moderate'
        /// Values: minimal, moderate, significant, severe
        /// </summary>
        [Column("impact")]
        [MaxLength(50)]
        public string Impact { get; set; } = "moderate";

        /// <summary>
        /// Current status (stored as string).
        /// Maps to: status risk_status (enum) NOT NULL DEFAULT 'identified'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = "identified";

        /// <summary>
        /// What we're doing to address the risk.
        /// Maps to: mitigation_strategy TEXT NULL
        /// </summary>
        [Column("mitigation_strategy")]
        public string? MitigationStrategy { get; set; }

        /// <summary>
        /// Plan if the risk materializes.
        /// Maps to: contingency_plan TEXT NULL
        /// </summary>
        [Column("contingency_plan")]
        public string? ContingencyPlan { get; set; }

        /// <summary>
        /// Team member responsible for managing this risk.
        /// Maps to: owner_team_member_id UUID NULL
        /// </summary>
        [Column("owner_team_member_id")]
        public Guid? OwnerTeamMemberId { get; set; }

        /// <summary>
        /// When the risk was identified.
        /// Maps to: identified_date DATE NOT NULL DEFAULT CURRENT_DATE
        /// </summary>
        [Column("identified_date")]
        public DateTime IdentifiedDate { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Target date for resolving the risk.
        /// Maps to: target_resolution_date DATE NULL
        /// </summary>
        [Column("target_resolution_date")]
        public DateTime? TargetResolutionDate { get; set; }

        /// <summary>
        /// When the risk was resolved.
        /// Maps to: resolved_date DATE NULL
        /// </summary>
        [Column("resolved_date")]
        public DateTime? ResolvedDate { get; set; }

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
        /// User who created this risk.
        /// Maps to: created_by_user_id UUID NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid? CreatedByUserId { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// Maps to: is_deleted BOOLEAN NOT NULL DEFAULT false
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// When soft deleted.
        /// Maps to: deleted_at TIMESTAMPTZ NULL
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted this risk.
        /// Maps to: deleted_by UUID NULL
        /// </summary>
        [Column("deleted_by")]
        public Guid? DeletedBy { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Owner team member.
        /// </summary>
        [NotMapped]
        public TeamMember? Owner { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Whether the risk has been mitigated/resolved.
        /// </summary>
        [NotMapped]
        public bool IsMitigated => Status == "resolved" || Status == "accepted";

        /// <summary>
        /// Display string for status.
        /// </summary>
        [NotMapped]
        public string StatusDisplay => Status switch
        {
            "identified" => "Identified",
            "assessing" => "Assessing",
            "mitigating" => "Mitigating",
            "monitoring" => "Monitoring",
            "resolved" => "Resolved",
            "accepted" => "Accepted",
            _ => Status
        };

        #endregion
    }
}
