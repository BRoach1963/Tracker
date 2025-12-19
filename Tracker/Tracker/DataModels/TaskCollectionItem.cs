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
        public int Id { get; set; }

        /// <summary>
        /// FK to the parent TaskCollection.
        /// </summary>
        public int CollectionId { get; set; }

        /// <summary>
        /// FK to the IndividualTask.
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
        public IndividualTask? Task { get; set; }
    }
}


