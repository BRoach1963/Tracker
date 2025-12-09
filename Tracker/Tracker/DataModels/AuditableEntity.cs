namespace Tracker.DataModels
{
    /// <summary>
    /// Base class for all entities that need audit tracking.
    /// Provides LastModified and ModifiedBy fields for change tracking and future sync capabilities.
    /// </summary>
    public abstract class AuditableEntity
    {
        /// <summary>
        /// The date and time this record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user who created this record.
        /// </summary>
        public string CreatedBy { get; set; } = Environment.UserName;

        /// <summary>
        /// The date and time this record was last modified.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The user who last modified this record.
        /// </summary>
        public string LastModifiedBy { get; set; } = Environment.UserName;

        /// <summary>
        /// Row version for optimistic concurrency control.
        /// Also useful for sync conflict detection.
        /// </summary>
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Flag indicating if this record has been soft-deleted.
        /// Useful for sync scenarios where we need to track deletions.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// The date and time this record was deleted (if soft-deleted).
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// The user who deleted this record (if soft-deleted).
        /// </summary>
        public string? DeletedBy { get; set; }
    }
}

