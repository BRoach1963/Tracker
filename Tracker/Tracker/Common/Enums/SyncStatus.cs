namespace Tracker.Common.Enums
{
    /// <summary>
    /// Sync status for offline support.
    /// Maps to PostgreSQL sync_status enum.
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// Data is fully synchronized with server.
        /// </summary>
        Synced,

        /// <summary>
        /// Local changes pending upload to server.
        /// </summary>
        Pending,

        /// <summary>
        /// Conflict detected between local and server versions.
        /// </summary>
        Conflict
    }
}
