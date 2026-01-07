using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Base class for entity indexers - provides common functionality using Template Method pattern.
    /// Subclasses only need to implement entity-specific logic.
    /// 
    /// Supports both legacy VectorStore (SQLite) and new IVectorStore implementations
    /// (PostgreSQL, SQL Server) for vector storage.
    /// </summary>
    public abstract class EntityIndexerBase
    {
        protected readonly ILogger _logger;
        protected int _indexedCount = 0;
        private IVectorStore? _vectorStore;
        
        /// <summary>
        /// The name of the entity type for logging (e.g., "tasks", "meetings")
        /// </summary>
        protected abstract string EntityTypeName { get; }

        /// <summary>
        /// The entity type identifier used in IVectorStore (e.g., "TeamMember", "Meeting")
        /// </summary>
        protected virtual string EntityTypeId => EntityTypeName;

        protected EntityIndexerBase(string componentName)
        {
            _logger = LoggingManager.GetComponentLogger(componentName);
        }

        /// <summary>
        /// Sets a custom IVectorStore to use instead of the legacy singleton.
        /// Call this before IndexAllAsync to use the new multi-tenant vector store.
        /// </summary>
        public void SetVectorStore(IVectorStore vectorStore)
        {
            _vectorStore = vectorStore;
        }

        /// <summary>
        /// Gets whether a custom vector store is configured.
        /// </summary>
        protected bool HasCustomVectorStore => _vectorStore != null;

        /// <summary>
        /// Template method for indexing all entities of a type.
        /// Handles reset, logging, filtering, and exception handling.
        /// Subclasses implement FetchEntitiesAsync and IndexSingleEntityAsync.
        /// </summary>
        /// <param name="sinceTime">Only index entities created/modified after this time (null = all)</param>
        public async Task<int> IndexAllAsync(DateTime? sinceTime = null)
        {
            ResetCount();
            
            if (sinceTime == null)
                _logger.Info("Starting full {0} indexing...", EntityTypeName);
            else
                _logger.Info("Starting incremental {0} indexing since {1}...", EntityTypeName, sinceTime.Value.ToString("g"));

            try
            {
                // Fetch all entities (filtered by global query filters)
                var entities = await FetchEntitiesAsync();
                
                // Filter by modification time for incremental indexing
                if (sinceTime != null)
                {
                    entities = FilterByModificationTime(entities, sinceTime.Value);
                }

                // Index each entity
                foreach (var entity in entities)
                {
                    await IndexSingleEntityAsync(entity);
                }

                _logger.Info("Indexed {0} {1}", _indexedCount, EntityTypeName);
                return _indexedCount;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error indexing {0}", EntityTypeName);
                return _indexedCount;
            }
        }

        /// <summary>
        /// Fetches all entities to be indexed. Override in subclass.
        /// </summary>
        protected abstract Task<IEnumerable<object>> FetchEntitiesAsync();

        /// <summary>
        /// Filters entities by modification time for incremental indexing.
        /// Default implementation assumes entities have CreatedAt/LastModifiedAt properties.
        /// </summary>
        protected virtual IEnumerable<object> FilterByModificationTime(IEnumerable<object> entities, DateTime sinceTime)
        {
            return entities.Where(e =>
            {
                var type = e.GetType();
                var createdAt = type.GetProperty("CreatedAt")?.GetValue(e) as DateTime? ?? DateTime.MinValue;
                var modifiedAt = type.GetProperty("LastModifiedAt")?.GetValue(e) as DateTime? ?? DateTime.MinValue;
                return createdAt > sinceTime || modifiedAt > sinceTime;
            });
        }

        /// <summary>
        /// Indexes a single entity. Override in subclass to build content and metadata.
        /// </summary>
        protected abstract Task IndexSingleEntityAsync(object entity);

        /// <summary>
        /// Creates a vector embedding and stores it.
        /// Uses IVectorStore if configured, otherwise falls back to legacy VectorStore.
        /// </summary>
        /// <param name="id">Unique identifier for the entity</param>
        /// <param name="content">Text content to embed</param>
        /// <param name="metadata">Optional metadata dictionary</param>
        protected async Task<bool> IndexEntityAsync(string id, string content, Dictionary<string, object>? metadata = null)
        {
            try
            {
                // Get embedding for the content
                var embedding = await EmbeddingService.Instance.GetEmbeddingAsync(content);
                if (embedding == null)
                {
                    _logger.Warn("Failed to generate embedding for: {0}", id);
                    return false;
                }

                // Store in vector database - use new IVectorStore if configured
                if (_vectorStore != null)
                {
                    await _vectorStore.StoreAsync(
                        entityType: EntityTypeId,
                        entityId: id,
                        content: content,
                        embedding: embedding,
                        chunkIndex: 0,
                        metadata: metadata);
                }
                else
                {
                    // Legacy path - use singleton VectorStore
                    await VectorStore.Instance.AddAsync(id, embedding, content, metadata);
                }
                
                _indexedCount++;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn("Error indexing {0}: {1}", id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the count of indexed entities
        /// </summary>
        public int GetIndexedCount() => _indexedCount;

        /// <summary>
        /// Resets the counter
        /// </summary>
        public void ResetCount() => _indexedCount = 0;
    }
}
