namespace Tracker.DataModels
{
    /// <summary>
    /// Records changes made to entities for future offline synchronization.
    /// 
    /// This table supports the planned offline sync feature where users can:
    /// 1. Configure SQL Server as their primary database
    /// 2. Work offline using a local SQLite cache when disconnected
    /// 3. Sync their offline changes back to SQL Server when reconnected
    /// 
    /// How It Works:
    /// When running in offline mode, every insert/update/delete is recorded here
    /// as a ChangeTrackingEntry with a JSON snapshot of the entity state.
    /// 
    /// When connectivity is restored, the sync process:
    /// 1. Reads all entries where IsSynced = false
    /// 2. Applies changes to SQL Server in chronological order
    /// 3. Handles conflicts using the configured strategy (server wins / last write wins)
    /// 4. Marks entries as synced or records sync errors
    /// 
    /// Example Entry:
    /// <code>
    /// {
    ///     EntityType: "TeamMember",
    ///     EntityId: 42,
    ///     ChangeType: Update,
    ///     EntityJson: "{\"Id\":42,\"FirstName\":\"John\",...}",
    ///     ChangedAt: "2024-01-15T10:30:00Z",
    ///     ChangedBy: "jdoe",
    ///     IsSynced: false
    /// }
    /// </code>
    /// 
    /// Note: This feature is designed for future implementation. The infrastructure
    /// is in place, but the sync logic itself is not yet implemented.
    /// </summary>
    public class ChangeTrackingEntry
    {
        /// <summary>
        /// Primary key for the change tracking entry.
        /// Auto-incremented by the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The type name of the entity that was changed.
        /// Examples: "TeamMember", "Project", "OneOnOne"
        /// </summary>
        /// <remarks>
        /// This is stored as a string (not an enum) to allow for future entity types
        /// without requiring schema changes.
        /// </remarks>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The primary key of the entity that was changed.
        /// Combined with EntityType, this uniquely identifies the affected record.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The type of change: Insert, Update, or Delete.
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// JSON serialization of the entity at the time of the change.
        /// 
        /// For Insert/Update: Contains the new state of the entity.
        /// For Delete: Contains the state before deletion (for potential undo).
        /// </summary>
        /// <remarks>
        /// Using JSON allows the sync process to reconstruct the entity
        /// without needing to know its exact type at compile time.
        /// </remarks>
        public string EntityJson { get; set; } = string.Empty;

        /// <summary>
        /// The UTC timestamp when this change was made.
        /// Used for ordering changes during sync and conflict resolution.
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The username of the person who made this change.
        /// Used for audit logging and conflict resolution.
        /// </summary>
        public string ChangedBy { get; set; } = Environment.UserName;

        #region Sync Status

        /// <summary>
        /// Whether this change has been successfully synced to the server.
        /// 
        /// - false: Change is pending sync (needs to be sent to server)
        /// - true: Change has been successfully synced
        /// </summary>
        public bool IsSynced { get; set; } = false;

        /// <summary>
        /// The UTC timestamp when this change was synced to the server.
        /// Null if not yet synced.
        /// </summary>
        public DateTime? SyncedAt { get; set; }

        /// <summary>
        /// Error message if the sync attempt failed.
        /// Null if sync was successful or not yet attempted.
        /// </summary>
        /// <remarks>
        /// Common errors might include:
        /// - Conflict: Record was modified on server
        /// - Not found: Record was deleted on server
        /// - Permission denied: User lacks permission
        /// </remarks>
        public string? SyncError { get; set; }

        #endregion
    }

    /// <summary>
    /// Types of data changes that can be tracked.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>A new record was inserted.</summary>
        Insert = 1,

        /// <summary>An existing record was updated.</summary>
        Update = 2,

        /// <summary>A record was deleted (soft or hard).</summary>
        Delete = 3
    }
}
