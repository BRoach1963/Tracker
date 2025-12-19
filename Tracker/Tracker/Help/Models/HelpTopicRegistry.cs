using System.IO;
using System.Text.Json;
using Tracker.Logging;

namespace Tracker.Help.Models
{
    /// <summary>
    /// Registry that maps help topic IDs to file paths and maintains the table of contents.
    /// </summary>
    public class HelpTopicRegistry
    {
        private static readonly Lazy<HelpTopicRegistry> _instance = 
            new(() => new HelpTopicRegistry(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static HelpTopicRegistry Instance => _instance.Value;

        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _topicPaths = new();
        private readonly string _helpBasePath;
        private HelpTocEntry? _tableOfContents;
        private bool _isInitialized;

        /// <summary>
        /// Default topic shown when no context is found.
        /// </summary>
        public string DefaultTopicId { get; set; } = "getting-started/overview";

        /// <summary>
        /// Gets the table of contents.
        /// </summary>
        public HelpTocEntry? TableOfContents => _tableOfContents;

        private HelpTopicRegistry()
        {
            _logger = LoggingManager.GetComponentLogger("HelpRegistry");
            _helpBasePath = GetHelpBasePath();
        }

        /// <summary>
        /// Initializes the registry by scanning for help files and loading TOC.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _logger.Info("Initializing help topic registry from: {0}", _helpBasePath);

                // Load table of contents if exists
                LoadTableOfContents();

                // Scan for markdown files
                ScanHelpFiles();

                _isInitialized = true;
                _logger.Info("Help registry initialized with {0} topics", _topicPaths.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize help registry");
            }
        }

        /// <summary>
        /// Gets the file path for a topic ID.
        /// </summary>
        public string? GetTopicPath(string topicId)
        {
            if (!_isInitialized) Initialize();

            if (_topicPaths.TryGetValue(NormalizeTopicId(topicId), out var path))
                return path;

            // Try with .md extension variations
            var possiblePaths = new[]
            {
                Path.Combine(_helpBasePath, $"{topicId}.md"),
                Path.Combine(_helpBasePath, topicId, "index.md"),
                Path.Combine(_helpBasePath, $"{topicId}/index.md")
            };

            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    _topicPaths[NormalizeTopicId(topicId)] = possiblePath;
                    return possiblePath;
                }
            }

            _logger.Warn("Help topic not found: {0}", topicId);
            return null;
        }

        /// <summary>
        /// Checks if a topic exists.
        /// </summary>
        public bool TopicExists(string topicId)
        {
            return GetTopicPath(topicId) != null;
        }

        /// <summary>
        /// Gets all registered topic IDs.
        /// </summary>
        public IEnumerable<string> GetAllTopicIds()
        {
            if (!_isInitialized) Initialize();
            return _topicPaths.Keys;
        }

        /// <summary>
        /// Registers a topic manually (for dynamic topics).
        /// </summary>
        public void RegisterTopic(string topicId, string filePath)
        {
            _topicPaths[NormalizeTopicId(topicId)] = filePath;
        }

        private void LoadTableOfContents()
        {
            var tocPath = Path.Combine(_helpBasePath, "toc.json");
            if (!File.Exists(tocPath))
            {
                _logger.Info("No toc.json found, will auto-generate from folder structure");
                _tableOfContents = GenerateTableOfContents();
                return;
            }

            try
            {
                var json = File.ReadAllText(tocPath);
                _tableOfContents = JsonSerializer.Deserialize<HelpTocEntry>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load toc.json");
                _tableOfContents = GenerateTableOfContents();
            }
        }

        private HelpTocEntry GenerateTableOfContents()
        {
            var root = new HelpTocEntry
            {
                Title = "Tracker Help",
                Icon = "📖",
                IsExpanded = true,
                Children = new List<HelpTocEntry>()
            };

            // Define the expected structure
            var categories = new[]
            {
                ("getting-started", "Getting Started", "🚀"),
                ("features", "Features", "✨"),
                ("dialogs", "Dialogs", "🖼️"),
                ("reference", "Reference", "📚")
            };

            foreach (var (folder, title, icon) in categories)
            {
                var categoryPath = Path.Combine(_helpBasePath, folder);
                if (!Directory.Exists(categoryPath)) continue;

                var categoryEntry = new HelpTocEntry
                {
                    Title = title,
                    Icon = icon,
                    IsExpanded = folder == "getting-started",
                    Children = new List<HelpTocEntry>()
                };

                foreach (var file in Directory.GetFiles(categoryPath, "*.md"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var topicId = $"{folder}/{fileName}";
                    var displayTitle = ExtractTitleFromFile(file) ?? ToTitleCase(fileName);

                    categoryEntry.Children.Add(new HelpTocEntry
                    {
                        Title = displayTitle,
                        TopicId = topicId
                    });
                }

                if (categoryEntry.Children.Count > 0)
                {
                    root.Children.Add(categoryEntry);
                }
            }

            return root;
        }

        private void ScanHelpFiles()
        {
            if (!Directory.Exists(_helpBasePath))
            {
                _logger.Warn("Help base path does not exist: {0}", _helpBasePath);
                return;
            }

            foreach (var file in Directory.GetFiles(_helpBasePath, "*.md", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(_helpBasePath, file);
                var topicId = NormalizeTopicId(relativePath.Replace(".md", "").Replace("\\", "/"));
                _topicPaths[topicId] = file;
            }
        }

        private string? ExtractTitleFromFile(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                var firstLine = reader.ReadLine();
                if (firstLine?.StartsWith("# ") == true)
                {
                    return firstLine.Substring(2).Trim();
                }
            }
            catch { }
            return null;
        }

        private static string NormalizeTopicId(string topicId)
        {
            return topicId.ToLowerInvariant()
                .Replace("\\", "/")
                .TrimStart('/')
                .TrimEnd('/');
        }

        private static string ToTitleCase(string fileName)
        {
            return string.Join(" ", fileName.Split('-', '_')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        private static string GetHelpBasePath()
        {
            // Try multiple locations
            var possiblePaths = new[]
            {
                // Development path
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Help"),
                // Installed path
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help"),
                // Relative to executable
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Help")
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            // Default to first option (will be created if needed)
            return possiblePaths[0];
        }
    }
}

