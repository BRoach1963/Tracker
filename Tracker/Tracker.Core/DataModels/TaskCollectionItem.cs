using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Links a task to a TaskCollection.
    /// A task can belong to multiple collections.
    /// Maps to Supabase 'task_collection_items' table.
    /// </summary>
    [Table("task_collection_items")]
    public class TaskCollectionItem
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// FK to the parent TaskCollection.
        /// Maps to: collection_id UUID NOT NULL
        /// </summary>
        [Column("collection_id")]
        public Guid CollectionId { get; set; }

        /// <summary>
        /// FK to the TrackerTask.
        /// Maps to: task_id UUID NOT NULL
        /// </summary>
        [Column("task_id")]
        public Guid TaskId { get; set; }

        /// <summary>
        /// Organization ID for RLS (denormalized from collection).
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Sort order within the collection.
        /// Maps to: sort_order INT NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// When created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Navigation property to the parent collection.
        /// </summary>
        [NotMapped]
        public TaskCollection? Collection { get; set; }

        /// <summary>
        /// Navigation property to the task.
        /// </summary>
        [NotMapped]
        public TrackerTask? Task { get; set; }

        #endregion
    }
}


