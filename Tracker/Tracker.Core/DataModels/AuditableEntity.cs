using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Base class for all database entities that require audit tracking.
    /// Aligned with Supabase PostgreSQL schema patterns.
    /// 
    /// This class provides standardized fields for:
    /// - Creation tracking (created_at)
    /// - Modification tracking (updated_at)
    /// - Soft delete support (is_deleted, deleted_at, deleted_by)
    /// 
    /// Supabase Schema Pattern:
    /// Most tables include: created_at, updated_at, is_deleted, deleted_at, deleted_by
    /// Some tables also have: created_by (uuid FK to users)
    /// 
    /// Soft Delete Pattern:
    /// Instead of physically removing records from the database, entities are marked
    /// with IsDeleted = true. This preserves data for audit trails and enables
    /// potential recovery. Dapper queries should filter: WHERE is_deleted = false
    /// 
    /// Usage:
    /// <code>
    /// public class TeamMember : AuditableEntity
    /// {
    ///     public Guid Id { get; set; }
    ///     public string Name { get; set; }
    ///     // ... other properties
    /// }
    /// </code>
    /// </summary>
    public abstract class AuditableEntity
    {
        #region Timestamp Tracking

        /// <summary>
        /// The UTC date and time when this record was first created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The UTC date and time when this record was last modified.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Soft Delete Support

        /// <summary>
        /// Indicates whether this record has been soft-deleted.
        /// Maps to: is_deleted BOOLEAN NOT NULL DEFAULT false
        /// 
        /// When true, the record is considered "deleted" for business purposes
        /// but remains in the database for audit trails and potential recovery.
        /// Dapper queries should filter: WHERE is_deleted = false
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// The UTC date and time when this record was soft-deleted.
        /// Maps to: deleted_at TIMESTAMPTZ NULL
        /// Null if the record has not been deleted.
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// The user who deleted this record (UUID FK to users).
        /// Maps to: deleted_by UUID NULL
        /// Null if the record has not been deleted.
        /// </summary>
        [Column("deleted_by")]
        public Guid? DeletedBy { get; set; }

        #endregion
    }
}
