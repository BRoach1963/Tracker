namespace Tracker.DataModels
{
    /// <summary>
    /// Tracks changes made while offline for future sync with SQL Server.
    /// </summary>
    public class ChangeTrackingEntry
    {
        public int Id { get; set; }

        /// <summary>
        /// The type of entity that was changed (e.g., "TeamMember", "Project")
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The primary key of the changed entity.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The type of change: Insert, Update, Delete
        /// </summary>
        public ChangeType ChangeType { get; set; }

        /// <summary>
        /// JSON serialization of the entity at the time of change.
        /// </summary>
        public string EntityJson { get; set; } = string.Empty;

        /// <summary>
        /// When this change was made.
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who made this change.
        /// </summary>
        public string ChangedBy { get; set; } = Environment.UserName;

        /// <summary>
        /// Has this change been synced to the server?
        /// </summary>
        public bool IsSynced { get; set; } = false;

        /// <summary>
        /// When this change was synced (if synced).
        /// </summary>
        public DateTime? SyncedAt { get; set; }

        /// <summary>
        /// Any error message if sync failed.
        /// </summary>
        public string? SyncError { get; set; }
    }

    public enum ChangeType
    {
        Insert = 1,
        Update = 2,
        Delete = 3
    }
}

