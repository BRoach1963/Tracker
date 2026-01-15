using System;
using System.Collections.Generic;

namespace Tracker.DataModels
{
    /// <summary>
    /// [FUTURE FEATURE] Offline Sync / Change Tracking
    /// 
    /// Purpose: Track local changes made while offline so they can be synced
    /// to Supabase when connectivity is restored. Enables conflict detection
    /// and resolution for multi-device scenarios.
    /// 
    /// Status: NOT YET IMPLEMENTED - Model placeholder for future offline capability.
    /// No corresponding Supabase table exists yet. Do not add Dapper attributes
    /// until the feature is implemented and table is created.
    /// 
    /// Future Use Cases:
    /// - Mobile app working offline
    /// - Poor connectivity environments  
    /// - Optimistic UI updates with background sync
    /// - Conflict resolution when same entity edited on multiple devices
    /// </summary>
    public class ChangeTrackingEntry
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
