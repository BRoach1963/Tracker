using System;
using System.Collections.Generic;

namespace Tracker.DataModels
{
    /// <summary>
    /// Synchronization tracking for changes.
    /// Used for offline sync and change tracking.
    /// Maps to Supabase 'change_tracking_entries' table.
    /// </summary>
    public class ChangeTrackingEntry : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this change belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// User who made the change.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Entity type that was changed (Meeting, Task, Goal, etc.).
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the entity that changed.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Type of change (insert, update, delete).
        /// </summary>
        public string ChangeType { get; set; } = "update";

        /// <summary>
        /// JSON representation of the change.
        /// </summary>
        public string? ChangeData { get; set; }

        /// <summary>
        /// When the change was made.
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Has this been synced to the server?
        /// </summary>
        public bool IsSynced { get; set; } = false;

        /// <summary>
        /// When it was synced.
        /// </summary>
        public DateTime? SyncedAt { get; set; }
    }
}
