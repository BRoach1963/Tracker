using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Links a Target to a measurable source (metric, project, task collection).
    /// Maps to Supabase 'target_measurables' table.
    /// This is a polymorphic association - MeasurableType determines which entity MeasurableId references.
    /// </summary>
    [Table("target_measurables")]
    public class TargetMeasurable
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent target this measurable belongs to.
        /// Maps to: target_id UUID NOT NULL
        /// </summary>
        [Column("target_id")]
        public Guid TargetId { get; set; }

        /// <summary>
        /// Type of measurable entity (metric, project, task_collection).
        /// Maps to: measurable_type VARCHAR(50) NOT NULL
        /// </summary>
        [Column("measurable_type")]
        [MaxLength(50)]
        public string MeasurableType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the measurable entity.
        /// Maps to: measurable_id UUID NOT NULL
        /// </summary>
        [Column("measurable_id")]
        public Guid MeasurableId { get; set; }

        /// <summary>
        /// How to aggregate values from this source (stored as string).
        /// Maps to: aggregation_type VARCHAR(50) NOT NULL DEFAULT 'latest'
        /// </summary>
        [Column("aggregation_type")]
        [MaxLength(50)]
        public string AggregationTypeString { get; set; } = "latest";

        /// <summary>
        /// Aggregation type as enum.
        /// </summary>
        [NotMapped]
        public AggregationTypeEnum AggregationType
        {
            get => Enum.TryParse<AggregationTypeEnum>(AggregationTypeString, true, out var result) ? result : AggregationTypeEnum.Latest;
            set => AggregationTypeString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        [NotMapped]
        public bool IsDeleted { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Parent target.
        /// </summary>
        [NotMapped]
        public Target? Target { get; set; }

        #endregion

        #region Runtime Properties (Not Persisted)

        /// <summary>
        /// Display name for the linked measurable (resolved at runtime).
        /// </summary>
        [NotMapped]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Current progress of the linked measurable (resolved at runtime).
        /// </summary>
        [NotMapped]
        public decimal? CurrentProgress { get; set; }

        #endregion
    }
}
