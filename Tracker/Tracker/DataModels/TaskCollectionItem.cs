namespace Tracker.DataModels
{
    /// <summary>
    /// Links a task to a TaskCollection.
    /// A task can belong to multiple collections.
    /// </summary>
    public class TaskCollectionItem : AuditableEntity
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The organization this item belongs to.
        /// Null for legacy local-only databases (migration compatibility).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// FK to the parent TaskCollection.
        /// </summary>
        public int CollectionId { get; set; }

        /// <summary>
        /// FK to the TrackerTask.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Sort order within the collection.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Navigation property to the parent collection.
        /// </summary>
        public TaskCollection? Collection { get; set; }

        /// <summary>
        /// Navigation property to the task.
        /// </summary>
        public TrackerTask? Task { get; set; }
    }
}


