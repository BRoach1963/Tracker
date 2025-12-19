using System.Text;
using Tracker.Logging;
using Tracker.Services.AI;

namespace Tracker.Services
{
    /// <summary>
    /// Builds smart context for queries using semantic search on vectorized data
    /// </summary>
    public class SmartContextBuilder
    {
        private static readonly Lazy<SmartContextBuilder> _instance = 
            new(() => new SmartContextBuilder(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SmartContextBuilder Instance => _instance.Value;

        private readonly ILogger _logger;

        private SmartContextBuilder()
        {
            _logger = LoggingManager.GetComponentLogger("SmartContextBuilder");
        }

        /// <summary>
        /// Searches vectorized data for relevant context based on the question
        /// </summary>
        public async Task<string> GetDataContextForQueryAsync(string question, int topK = 5)
        {
            try
            {
                // Get embedding for the question
                var questionEmbedding = await EmbeddingService.Instance.GetEmbeddingAsync(question);
                if (questionEmbedding == null)
                {
                    _logger.Warn("Failed to embed question for data search");
                    return string.Empty;
                }

                // Search vectorized data (not documentation)
                var results = await VectorStore.Instance.SearchAsync(questionEmbedding, topK, minScore: 0.5f);

                if (results.Count == 0)
                {
                    _logger.Debug("No relevant data found for: {0}", question);
                    return string.Empty;
                }

                // Filter to only data entities (not documentation)
                // Metadata contains "type" field - check if it's NOT "documentation"
                var dataResults = results
                    .Where(r => !string.IsNullOrEmpty(r.Metadata) && 
                           r.Metadata.Contains("\"type\"") && 
                           !r.Metadata.Contains("\"type\":\"documentation\""))
                    .ToList();

                if (dataResults.Count == 0)
                {
                    return string.Empty;
                }

                // Build context from relevant data
                var sb = new StringBuilder();
                sb.AppendLine("Relevant data from your workspace:");
                sb.AppendLine();

                foreach (var result in dataResults.Take(5))
                {
                    sb.AppendLine(result.Content);
                    sb.AppendLine();
                }

                var context = sb.ToString();

                // Limit to reasonable size
                if (context.Length > 3000)
                {
                    context = context.Substring(0, 3000) + "\n[More data available...]";
                }

                _logger.Debug("Found {0} relevant data items ({1} chars)", dataResults.Count, context.Length);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting data context");
                return string.Empty;
            }
        }
    }
}
