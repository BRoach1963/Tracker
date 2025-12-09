namespace Tracker.DataModels
{
    /// <summary>
    /// Base class for all database entities that require audit tracking.
    /// 
    /// This class provides standardized fields for:
    /// - Creation tracking (when and by whom a record was created)
    /// - Modification tracking (when and by whom a record was last changed)
    /// - Soft delete support (records are marked deleted rather than removed)
    /// - Concurrency control (RowVersion for optimistic locking)
    /// 
    /// All audit fields are automatically populated by TrackerDbContext.SaveChanges()
    /// based on the entity's state (Added, Modified, or Deleted).
    /// 
    /// Soft Delete Pattern:
    /// Instead of physically removing records from the database, entities are marked
    /// with IsDeleted = true. This preserves data for audit trails and enables
    /// potential recovery. Most queries should filter on IsDeleted = false.
    /// 
    /// Future Sync Support:
    /// These fields, combined with the ChangeTrackingEntry table, enable future
    /// offline sync scenarios where changes made locally can be synchronized
    /// with a central SQL Server when connectivity is restored.
    /// 
    /// Usage:
    /// <code>
    /// public class TeamMember : AuditableEntity
    /// {
    ///     public int Id { get; set; }
    ///     public string Name { get; set; }
    ///     // ... other properties
    /// }
    /// </code>
    /// </summary>
    public abstract class AuditableEntity
    {
        #region Creation Tracking

        /// <summary>
        /// The UTC date and time when this record was first created.
        /// Automatically set by SaveChanges() when the entity is added.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The username of the person who created this record.
        /// Defaults to the current Windows username.
        /// </summary>
        public string CreatedBy { get; set; } = Environment.UserName;

        #endregion

        #region Modification Tracking

        /// <summary>
        /// The UTC date and time when this record was last modified.
        /// Updated automatically by SaveChanges() whenever the entity is changed.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The username of the person who last modified this record.
        /// Updated automatically on every save.
        /// </summary>
        public string LastModifiedBy { get; set; } = Environment.UserName;

        #endregion

        #region Concurrency Control

        /// <summary>
        /// Row version for optimistic concurrency control.
        /// 
        /// On SQL Server, this is a ROWVERSION/TIMESTAMP that changes automatically
        /// with each update. EF Core uses this to detect concurrent modifications:
        /// if the version in the database differs from the version in memory,
        /// a DbUpdateConcurrencyException is thrown.
        /// 
        /// On SQLite, this field is not used for concurrency (SQLite doesn't support
        /// ROWVERSION), but it's included for schema compatibility.
        /// </summary>
        public byte[]? RowVersion { get; set; }

        #endregion

        #region Soft Delete Support

        /// <summary>
        /// Indicates whether this record has been soft-deleted.
        /// 
        /// When true, the record is considered "deleted" for business purposes
        /// but remains in the database for audit trails and potential recovery.
        /// 
        /// Most queries should include a filter: .Where(e => !e.IsDeleted)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// The UTC date and time when this record was soft-deleted.
        /// Null if the record has not been deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// The username of the person who deleted this record.
        /// Null if the record has not been deleted.
        /// </summary>
        public string? DeletedBy { get; set; }

        #endregion
    }
}
