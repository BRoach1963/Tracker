using System.IO;
using System.Text.RegularExpressions;
using Tracker.Logging;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Indexes help documentation by chunking .md files and creating embeddings.
    /// Run once at startup to populate the vector store.
    /// </summary>
    public class DocumentIndexer
    {
        #region Constants

        // Chunk settings - balance between context and token limits
        private const int MaxChunkSize = 500;      // chars per chunk
        private const int ChunkOverlap = 50;       // overlap between chunks for context
        private const int MinChunkSize = 100;      // minimum chars to create a chunk

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly EmbeddingService _embeddingService;
        private readonly VectorStore _vectorStore;

        #endregion

        #region Singleton

        private static readonly Lazy<DocumentIndexer> _instance =
            new(() => new DocumentIndexer(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static DocumentIndexer Instance => _instance.Value;

        #endregion

        #region Constructor

        private DocumentIndexer()
        {
            _logger = LoggingManager.GetComponentLogger("DocIndexer");
            _embeddingService = EmbeddingService.Instance;
            _vectorStore = VectorStore.Instance;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if documentation needs to be indexed and indexes if necessary.
        /// </summary>
        public async Task<bool> EnsureIndexedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var existingDocs = await _vectorStore.GetIndexedDocumentsAsync();
                var helpPath = GetHelpPath();

                if (!Directory.Exists(helpPath))
                {
                    _logger.Warn("Help documentation path not found: {0}", helpPath);
                    return false;
                }

                var mdFiles = Directory.GetFiles(helpPath, "*.md", SearchOption.AllDirectories);
                
                // Check if we need to re-index (simple check: count mismatch)
                if (existingDocs.Count >= mdFiles.Length && existingDocs.Count > 0)
                {
                    _logger.Info("Documentation already indexed ({0} documents)", existingDocs.Count);
                    return true;
                }

                _logger.Info("Indexing {0} documentation files...", mdFiles.Length);
                return await IndexAllDocumentsAsync(mdFiles, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error ensuring index");
                return false;
            }
        }

        /// <summary>
        /// Forces a complete re-index of all documentation.
        /// </summary>
        public async Task<bool> ReindexAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Starting full re-index...");
                await _vectorStore.ClearAllAsync();

                var helpPath = GetHelpPath();
                if (!Directory.Exists(helpPath))
                {
                    _logger.Warn("Help path not found: {0}", helpPath);
                    return false;
                }

                var mdFiles = Directory.GetFiles(helpPath, "*.md", SearchOption.AllDirectories);
                return await IndexAllDocumentsAsync(mdFiles, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during reindex");
                return false;
            }
        }

        /// <summary>
        /// Gets statistics about the current index.
        /// </summary>
        public async Task<IndexStats> GetStatsAsync()
        {
            var chunkCount = await _vectorStore.GetChunkCountAsync();
            var docIds = await _vectorStore.GetIndexedDocumentsAsync();

            return new IndexStats
            {
                DocumentCount = docIds.Count,
                ChunkCount = chunkCount,
                IndexedDocuments = docIds
            };
        }

        #endregion

        #region Private Methods

        private string GetHelpPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Help");
        }

        private async Task<bool> IndexAllDocumentsAsync(string[] filePaths, CancellationToken cancellationToken)
        {
            var allChunks = new List<(string DocId, int Index, string Content)>();
            var totalChunks = 0;

            // Step 1: Chunk all documents
            foreach (var filePath in filePaths)
            {
                try
                {
                    var docId = GetDocId(filePath);
                    var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var chunks = ChunkDocument(content, docId);

                    foreach (var chunk in chunks)
                    {
                        allChunks.Add((docId, chunk.Index, chunk.Content));
                    }

                    totalChunks += chunks.Count;
                    _logger.Debug("Chunked {0}: {1} chunks", docId, chunks.Count);
                }
                catch (Exception ex)
                {
                    _logger.Warn("Error chunking {0}: {1}", filePath, ex.Message);
                }
            }

            _logger.Info("Total chunks to embed: {0}", totalChunks);

            // Step 2: Generate embeddings in batches
            var textsToEmbed = allChunks.Select(c => c.Content).ToList();
            var embeddings = await _embeddingService.GetEmbeddingsBatchAsync(textsToEmbed, cancellationToken);

            // Step 3: Store in vector database
            var chunksToStore = new List<(string DocId, int ChunkIndex, string Content, float[] Embedding, string? Metadata)>();

            for (int i = 0; i < allChunks.Count; i++)
            {
                var embedding = embeddings[i];
                if (embedding == null)
                {
                    _logger.Warn("Failed to get embedding for chunk {0}", i);
                    continue;
                }

                chunksToStore.Add((
                    allChunks[i].DocId,
                    allChunks[i].Index,
                    allChunks[i].Content,
                    embedding,
                    null
                ));
            }

            if (chunksToStore.Count > 0)
            {
                await _vectorStore.StoreBatchAsync(chunksToStore);
            }

            _logger.Info("Indexing complete: {0} chunks stored", chunksToStore.Count);
            return chunksToStore.Count > 0;
        }

        private string GetDocId(string filePath)
        {
            // Create a readable doc ID from the file path
            // e.g., "Resources/Help/features/okrs.md" -> "features/okrs"
            var helpPath = GetHelpPath();
            var relativePath = Path.GetRelativePath(helpPath, filePath);
            return Path.ChangeExtension(relativePath, null).Replace('\\', '/');
        }

        private List<(int Index, string Content)> ChunkDocument(string content, string docId)
        {
            var chunks = new List<(int Index, string Content)>();

            // Clean up the content
            content = CleanMarkdown(content);

            // Split by headers first for semantic chunking
            var sections = SplitByHeaders(content);

            int chunkIndex = 0;
            foreach (var section in sections)
            {
                // If section is small enough, keep it as one chunk
                if (section.Length <= MaxChunkSize)
                {
                    if (section.Length >= MinChunkSize)
                    {
                        chunks.Add((chunkIndex++, section.Trim()));
                    }
                    continue;
                }

                // Split large sections by paragraphs/sentences
                var subChunks = SplitIntoChunks(section);
                foreach (var subChunk in subChunks)
                {
                    if (subChunk.Length >= MinChunkSize)
                    {
                        chunks.Add((chunkIndex++, subChunk.Trim()));
                    }
                }
            }

            return chunks;
        }

        private string CleanMarkdown(string content)
        {
            // Remove code blocks (keep the content concept but not formatting)
            content = Regex.Replace(content, @"```[\s\S]*?```", " [code example] ");
            
            // Remove inline code backticks
            content = Regex.Replace(content, @"`([^`]+)`", "$1");
            
            // Remove image links
            content = Regex.Replace(content, @"!\[.*?\]\(.*?\)", "");
            
            // Convert links to just text
            content = Regex.Replace(content, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            
            // Remove excessive whitespace
            content = Regex.Replace(content, @"\n{3,}", "\n\n");
            content = Regex.Replace(content, @"[ \t]+", " ");

            return content;
        }

        private List<string> SplitByHeaders(string content)
        {
            var sections = new List<string>();
            
            // Split on markdown headers (# ## ### etc.)
            var headerPattern = new Regex(@"(?=^#{1,3}\s)", RegexOptions.Multiline);
            var parts = headerPattern.Split(content);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    sections.Add(trimmed);
                }
            }

            return sections.Count > 0 ? sections : new List<string> { content };
        }

        private List<string> SplitIntoChunks(string text)
        {
            var chunks = new List<string>();
            
            // Split by paragraphs first
            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            var currentChunk = "";
            foreach (var para in paragraphs)
            {
                if (currentChunk.Length + para.Length <= MaxChunkSize)
                {
                    currentChunk += (currentChunk.Length > 0 ? "\n\n" : "") + para;
                }
                else
                {
                    if (currentChunk.Length >= MinChunkSize)
                    {
                        chunks.Add(currentChunk);
                    }
                    currentChunk = para;
                }
            }

            if (currentChunk.Length >= MinChunkSize)
            {
                chunks.Add(currentChunk);
            }

            return chunks;
        }

        #endregion
    }

    /// <summary>
    /// Statistics about the document index.
    /// </summary>
    public class IndexStats
    {
        public int DocumentCount { get; set; }
        public int ChunkCount { get; set; }
        public List<string> IndexedDocuments { get; set; } = new();
    }
}

