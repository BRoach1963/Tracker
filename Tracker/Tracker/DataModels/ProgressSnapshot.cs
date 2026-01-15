using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.DataModels
{
    /// <summary>
    /// Supported entity types for progress snapshots.
    /// </summary>
    public enum SnapshotEntityType
    {
        Goal = 0,
        Target = 1,
        Project = 2,
        Task = 3
    }

    /// <summary>
    /// Represents a point-in-time snapshot of progress for an entity.
    /// Maps to Supabase 'progress_snapshots' table.
    /// Used for trajectory analysis and trend visualization.
    /// </summary>
    [Table("progress_snapshots")]
    public class ProgressSnapshot
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this snapshot belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// The type of entity being tracked (goal, project, task, etc.).
        /// Maps to: entity_type VARCHAR(50) NOT NULL
        /// </summary>
        [Column("entity_type")]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the entity.
        /// Maps to: entity_id UUID NOT NULL
        /// </summary>
        [Column("entity_id")]
        public Guid EntityId { get; set; }

        /// <summary>
        /// The date of the snapshot.
        /// Maps to: snapshot_date DATE NOT NULL
        /// </summary>
        [Column("snapshot_date")]
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// Period type for the snapshot (stored as string).
        /// Maps to: period_type snapshot_period (enum) NOT NULL DEFAULT 'weekly'
        /// Values: daily, weekly, monthly, quarterly
        /// </summary>
        [Column("period_type")]
        [MaxLength(50)]
        public string PeriodType { get; set; } = "weekly";

        /// <summary>
        /// Metrics data as JSON.
        /// Maps to: metrics JSONB NOT NULL DEFAULT '{}'
        /// Contains progress, velocity, and other calculated values.
        /// </summary>
        [Column("metrics")]
        public string Metrics { get; set; } = "{}";

        /// <summary>
        /// Overall score for this snapshot period.
        /// Maps to: overall_score NUMERIC NULL
        /// </summary>
        [Column("overall_score")]
        public decimal? OverallScore { get; set; }

        /// <summary>
        /// Trend direction indicator.
        /// Maps to: trend_direction INT4 NULL
        /// Negative = declining, 0 = stable, Positive = improving
        /// </summary>
        [Column("trend_direction")]
        public int? TrendDirection { get; set; }

        /// <summary>
        /// When this snapshot was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
