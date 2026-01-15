using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Target (formerly Key Result) - a measurable outcome for a Goal.
    /// Maps to Supabase 'targets' table.
    /// Progress is calculated as: (CurrentValue - StartingValue) / (TargetValue - StartingValue) × 100
    /// </summary>
    [Table("targets")]
    public class Target
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent goal this target belongs to.
        /// Maps to: goal_id UUID NOT NULL
        /// </summary>
        [Column("goal_id")]
        public Guid GoalId { get; set; }

        /// <summary>
        /// Target title - what we're measuring.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Extended description.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Target value to achieve.
        /// Maps to: target_value NUMERIC NOT NULL
        /// </summary>
        [Column("target_value")]
        public decimal TargetValue { get; set; }

        /// <summary>
        /// Current value (manual or calculated from measurables).
        /// Maps to: current_value NUMERIC NOT NULL DEFAULT 0
        /// </summary>
        [Column("current_value")]
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Starting baseline value.
        /// Maps to: starting_value NUMERIC NOT NULL DEFAULT 0
        /// </summary>
        [Column("starting_value")]
        public decimal StartingValue { get; set; }

        /// <summary>
        /// Unit of measurement (%, points, hours, $, etc.).
        /// Maps to: unit VARCHAR(50) NULL
        /// </summary>
        [Column("unit")]
        [MaxLength(50)]
        public string? Unit { get; set; }

        /// <summary>
        /// Weight for weighted progress calculation.
        /// Maps to: weight NUMERIC NOT NULL DEFAULT 1.0
        /// </summary>
        [Column("weight")]
        public decimal Weight { get; set; } = 1.0m;

        /// <summary>
        /// Current status (stored as string).
        /// Maps to: status goal_status (enum) NOT NULL DEFAULT 'not_started'
        /// </summary>
        [Column("status")]
        [MaxLength(50)]
        public string StatusString { get; set; } = "not_started";

        /// <summary>
        /// Status as enum.
        /// </summary>
        [NotMapped]
        public GoalStatus Status
        {
            get => Enum.TryParse<GoalStatus>(StatusString, true, out var result) ? result : GoalStatus.NotStarted;
            set => StatusString = value.ToString().ToLowerInvariant().Replace("_", "");
        }

        /// <summary>
        /// Sort order within the goal.
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

        #region Navigation Properties

        /// <summary>
        /// Parent goal.
        /// </summary>
        [NotMapped]
        public Goal? Goal { get; set; }

        /// <summary>
        /// Linked measurable sources that feed this target.
        /// </summary>
        [NotMapped]
        public List<TargetMeasurable> Measurables { get; set; } = new();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Progress percentage (0-100+).
        /// </summary>
        [NotMapped]
        public decimal Progress
        {
            get
            {
                var range = TargetValue - StartingValue;
                if (range == 0) return TargetValue == CurrentValue ? 100m : 0m;
                
                var progress = (CurrentValue - StartingValue) / range * 100m;
                return Math.Max(0, Math.Min(progress, 100m));
            }
        }

        /// <summary>
        /// Is target complete (progress >= 100%)?
        /// </summary>
        [NotMapped]
        public bool IsComplete => Progress >= 100m;

        /// <summary>
        /// Remaining value to achieve target.
        /// </summary>
        [NotMapped]
        public decimal Remaining => TargetValue - CurrentValue;

        /// <summary>
        /// Direction of the target (GreaterOrEqual, LessOrEqual).
        /// Defaults to GreaterOrEqual (success when value >= target).
        /// </summary>
        [NotMapped]
        public Common.Enums.TargetDirectionEnum TargetDirection { get; set; } = Common.Enums.TargetDirectionEnum.GreaterOrEqual;

        #endregion
    }
}
