using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Base class for entity indexers - provides common functionality
    /// </summary>
    public abstract class EntityIndexerBase
    {
        protected readonly ILogger _logger;
        protected int _indexedCount = 0;

        protected EntityIndexerBase(string componentName)
        {
            _logger = LoggingManager.GetComponentLogger(componentName);
        }

        /// <summary>
        /// Creates a vector embedding and stores it
        /// </summary>
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

                // Store in vector database
                await VectorStore.Instance.AddAsync(id, embedding, content, metadata);
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
