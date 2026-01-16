using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.Core.DataModels;

namespace Tracker.Core.Data.Repositories
{
    /// <summary>
    /// Repository for TaskCollection entity.
    /// Provides data access for all task collection-related operations.
    /// 
    /// This is the ONLY place that queries the 'task_collections' and 'task_collection_items' tables.
    /// ViewModels and Services NEVER query the database directly - they use this repository.
    /// 
    /// TaskCollections group tasks together for progress tracking and can be linked to Targets/Metrics.
    /// </summary>
    public interface ITaskCollectionRepository
    {
        /// <summary>
        /// Get a task collection by ID.
        /// </summary>
        Task<TaskCollection?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all task collections for an organization.
        /// </summary>
        Task<IEnumerable<TaskCollection>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get a collection with its items (tasks) loaded.
        /// </summary>
        Task<TaskCollection?> GetWithItemsAsync(Guid id);

        /// <summary>
        /// Get a collection with items and full task details loaded.
        /// </summary>
        Task<TaskCollection?> GetWithItemsAndTasksAsync(Guid id);

        /// <summary>
        /// Create a new task collection.
        /// </summary>
        Task<TaskCollection?> CreateAsync(TaskCollection collection);

        /// <summary>
        /// Update a task collection.
        /// </summary>
        Task<bool> UpdateAsync(TaskCollection collection);

        /// <summary>
        /// Delete a task collection (hard delete - also removes items).
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Get collection items for a collection.
        /// </summary>
        Task<IEnumerable<TaskCollectionItem>> GetCollectionItemsAsync(Guid collectionId);

        /// <summary>
        /// Add a task to a collection.
        /// </summary>
        Task<TaskCollectionItem?> AddTaskToCollectionAsync(Guid collectionId, Guid taskId, Guid organizationId, int sortOrder = 0);

        /// <summary>
        /// Remove a task from a collection.
        /// </summary>
        Task<bool> RemoveTaskFromCollectionAsync(Guid collectionId, Guid taskId);

        /// <summary>
        /// Remove a collection item by its ID.
        /// </summary>
        Task<bool> DeleteCollectionItemAsync(Guid itemId);

        /// <summary>
        /// Update sort order for collection items.
        /// </summary>
        Task<bool> UpdateItemSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates);

        /// <summary>
        /// Check if a task is in a collection.
        /// </summary>
        Task<bool> IsTaskInCollectionAsync(Guid collectionId, Guid taskId);

        /// <summary>
        /// Get all collections containing a specific task.
        /// </summary>
        Task<IEnumerable<TaskCollection>> GetCollectionsForTaskAsync(Guid taskId);

        /// <summary>
        /// Get progress stats for a collection (completed/total).
        /// </summary>
        Task<(int Completed, int Total)> GetCollectionProgressAsync(Guid collectionId);
    }

    public class TaskCollectionRepository : ITaskCollectionRepository
    {
        private readonly IDapperConnectionFactory _connectionFactory;
        private readonly ILogger<TaskCollectionRepository> _logger;

        public TaskCollectionRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<TaskCollectionRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<TaskCollection?> GetByIdAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM task_collections
                    WHERE id = @Id";

                return await connection.QueryFirstOrDefaultAsync<TaskCollection>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task collection {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TaskCollection>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM task_collections
                    WHERE organization_id = @OrgId
                    ORDER BY name";

                return await connection.QueryAsync<TaskCollection>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task collections for organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<TaskCollection?> GetWithItemsAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();

                // Get the collection
                const string collectionSql = @"
                    SELECT * FROM task_collections
                    WHERE id = @Id";

                var collection = await connection.QueryFirstOrDefaultAsync<TaskCollection>(collectionSql, new { Id = id });

                if (collection != null)
                {
                    // Get the items
                    var items = await GetCollectionItemsAsync(id);
                    collection.Items = items.ToList();
                }

                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task collection with items {Id}", id);
                throw;
            }
        }

        public async Task<TaskCollection?> GetWithItemsAndTasksAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();

                // Get the collection
                const string collectionSql = @"
                    SELECT * FROM task_collections
                    WHERE id = @Id";

                var collection = await connection.QueryFirstOrDefaultAsync<TaskCollection>(collectionSql, new { Id = id });

                if (collection != null)
                {
                    // Get items with tasks in one query
                    const string itemsSql = @"
                        SELECT 
                            tci.id, tci.collection_id, tci.task_id, tci.organization_id, tci.sort_order, tci.created_at,
                            t.id, t.organization_id, t.title, t.description, t.status, t.priority, 
                            t.due_date, t.completed_at, t.owner_team_member_id, t.project_id,
                            t.created_at, t.updated_at, t.is_deleted
                        FROM task_collection_items tci
                        INNER JOIN tasks t ON tci.task_id = t.id
                        WHERE tci.collection_id = @CollectionId AND t.is_deleted = false
                        ORDER BY tci.sort_order, tci.created_at";

                    var items = await connection.QueryAsync<TaskCollectionItem, TrackerTask, TaskCollectionItem>(
                        itemsSql,
                        (item, task) =>
                        {
                            item.Task = task;
                            return item;
                        },
                        new { CollectionId = id },
                        splitOn: "id");

                    collection.Items = items.ToList();
                }

                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task collection with items and tasks {Id}", id);
                throw;
            }
        }

        public async Task<TaskCollection?> CreateAsync(TaskCollection collection)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO task_collections 
                        (organization_id, name, description, query_config)
                    VALUES 
                        (@OrganizationId, @Name, @Description, @QueryConfig::jsonb)
                    RETURNING *";

                return await connection.QueryFirstOrDefaultAsync<TaskCollection>(sql, collection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task collection {Name}", collection.Name);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(TaskCollection collection)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE task_collections
                    SET name = @Name, 
                        description = @Description,
                        query_config = @QueryConfig::jsonb,
                        updated_at = now()
                    WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, collection);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task collection {Id}", collection.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();

                // Delete items first (foreign key constraint)
                const string deleteItemsSql = "DELETE FROM task_collection_items WHERE collection_id = @Id";
                await connection.ExecuteAsync(deleteItemsSql, new { Id = id });

                // Delete the collection
                const string deleteCollectionSql = "DELETE FROM task_collections WHERE id = @Id";
                var rows = await connection.ExecuteAsync(deleteCollectionSql, new { Id = id });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task collection {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TaskCollectionItem>> GetCollectionItemsAsync(Guid collectionId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM task_collection_items
                    WHERE collection_id = @CollectionId
                    ORDER BY sort_order, created_at";

                return await connection.QueryAsync<TaskCollectionItem>(sql, new { CollectionId = collectionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collection items for collection {CollectionId}", collectionId);
                throw;
            }
        }

        public async Task<TaskCollectionItem?> AddTaskToCollectionAsync(Guid collectionId, Guid taskId, Guid organizationId, int sortOrder = 0)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO task_collection_items 
                        (collection_id, task_id, organization_id, sort_order)
                    VALUES 
                        (@CollectionId, @TaskId, @OrganizationId, @SortOrder)
                    RETURNING *";

                return await connection.QueryFirstOrDefaultAsync<TaskCollectionItem>(sql, new
                {
                    CollectionId = collectionId,
                    TaskId = taskId,
                    OrganizationId = organizationId,
                    SortOrder = sortOrder
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding task {TaskId} to collection {CollectionId}", taskId, collectionId);
                throw;
            }
        }

        public async Task<bool> RemoveTaskFromCollectionAsync(Guid collectionId, Guid taskId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    DELETE FROM task_collection_items 
                    WHERE collection_id = @CollectionId AND task_id = @TaskId";

                var rows = await connection.ExecuteAsync(sql, new { CollectionId = collectionId, TaskId = taskId });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing task {TaskId} from collection {CollectionId}", taskId, collectionId);
                throw;
            }
        }

        public async Task<bool> DeleteCollectionItemAsync(Guid itemId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = "DELETE FROM task_collection_items WHERE id = @Id";

                var rows = await connection.ExecuteAsync(sql, new { Id = itemId });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting collection item {ItemId}", itemId);
                throw;
            }
        }

        public async Task<bool> UpdateItemSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> updates)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    UPDATE task_collection_items
                    SET sort_order = @SortOrder
                    WHERE id = @Id";

                foreach (var (id, sortOrder) in updates)
                {
                    await connection.ExecuteAsync(sql, new { Id = id, SortOrder = sortOrder });
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sort order for collection items");
                throw;
            }
        }

        public async Task<bool> IsTaskInCollectionAsync(Guid collectionId, Guid taskId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT EXISTS (
                        SELECT 1 FROM task_collection_items 
                        WHERE collection_id = @CollectionId AND task_id = @TaskId
                    )";

                return await connection.ExecuteScalarAsync<bool>(sql, new { CollectionId = collectionId, TaskId = taskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if task {TaskId} is in collection {CollectionId}", taskId, collectionId);
                throw;
            }
        }

        public async Task<IEnumerable<TaskCollection>> GetCollectionsForTaskAsync(Guid taskId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT tc.* FROM task_collections tc
                    INNER JOIN task_collection_items tci ON tc.id = tci.collection_id
                    WHERE tci.task_id = @TaskId
                    ORDER BY tc.name";

                return await connection.QueryAsync<TaskCollection>(sql, new { TaskId = taskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collections for task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<(int Completed, int Total)> GetCollectionProgressAsync(Guid collectionId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT 
                        COUNT(*) FILTER (WHERE t.status = 'completed') as completed,
                        COUNT(*) as total
                    FROM task_collection_items tci
                    INNER JOIN tasks t ON tci.task_id = t.id
                    WHERE tci.collection_id = @CollectionId AND t.is_deleted = false";

                var result = await connection.QueryFirstAsync<dynamic>(sql, new { CollectionId = collectionId });
                return ((int)result.completed, (int)result.total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for collection {CollectionId}", collectionId);
                throw;
            }
        }
    }
}
