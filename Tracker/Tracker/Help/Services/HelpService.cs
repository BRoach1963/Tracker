using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Tracker.Help.Attributes;
using Tracker.Help.Models;
using Tracker.Help.Views;
using Tracker.Logging;

namespace Tracker.Help.Services
{
    /// <summary>
    /// Main service for the help system. Provides context-sensitive help,
    /// topic loading, and search functionality.
    /// </summary>
    public class HelpService
    {
        #region Singleton

        private static readonly Lazy<HelpService> _instance = 
            new(() => new HelpService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of HelpService.
        /// </summary>
        public static HelpService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private readonly LruCache<string, HelpTopic> _topicCache;
        private readonly HelpTopicRegistry _registry;
        private bool _isInitialized;

        /// <summary>
        /// Maximum number of topics to keep in memory.
        /// </summary>
        private const int MAX_CACHED_TOPICS = 20;

        #endregion

        #region Events

        /// <summary>
        /// Fired when help should be displayed.
        /// </summary>
        public event EventHandler<HelpContext>? HelpRequested;

        /// <summary>
        /// Fired when help window should close.
        /// </summary>
        public event EventHandler? HelpClosed;

        #endregion

        #region Constructor

        private HelpService()
        {
            _logger = LoggingManager.GetComponentLogger("HelpService");
            _topicCache = new LruCache<string, HelpTopic>(MAX_CACHED_TOPICS);
            _registry = HelpTopicRegistry.Instance;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the help service.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _logger.Info("Initializing HelpService");
            _registry.Initialize();
            _isInitialized = true;
        }

        /// <summary>
        /// Shows context-sensitive help based on the currently focused element.
        /// </summary>
        /// <param name="focusedElement">The element that has focus (or null for general help).</param>
        public void ShowContextHelp(DependencyObject? focusedElement = null)
        {
            if (!_isInitialized) Initialize();

            var context = ResolveHelpContext(focusedElement);
            
            _logger.Info("Showing help for context: {0} (section: {1})", 
                context.TopicId, context.Section ?? "none");

            // Show the help window (reuses existing or creates new)
            Application.Current?.Dispatcher.Invoke(() =>
            {
                HelpWindow.ShowForContext(context);
            });

            HelpRequested?.Invoke(this, context);
        }

        /// <summary>
        /// Shows help for a specific topic ID.
        /// </summary>
        /// <param name="topicId">The topic to display.</param>
        /// <param name="section">Optional section anchor within the topic.</param>
        public void ShowTopic(string topicId, string? section = null)
        {
            if (!_isInitialized) Initialize();

            var context = new HelpContext
            {
                TopicId = topicId,
                Section = section,
                IsContextual = false
            };

            _logger.Info("Showing topic: {0}", topicId);
            HelpRequested?.Invoke(this, context);
        }

        /// <summary>
        /// Loads a help topic by ID.
        /// </summary>
        /// <param name="topicId">The topic identifier.</param>
        /// <returns>The loaded topic, or null if not found.</returns>
        public async Task<HelpTopic?> GetTopicAsync(string topicId)
        {
            if (!_isInitialized) Initialize();

            // Check cache first
            if (_topicCache.TryGet(topicId, out var cached))
            {
                return cached;
            }

            // Load from file
            var filePath = _registry.GetTopicPath(topicId);
            if (filePath == null)
            {
                _logger.Warn("Topic not found: {0}", topicId);
                return null;
            }

            try
            {
                var topic = await LoadTopicFromFileAsync(topicId, filePath);
                if (topic != null)
                {
                    _topicCache.Add(topicId, topic);
                }
                return topic;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load topic: {0}", topicId);
                return null;
            }
        }

        /// <summary>
        /// Synchronous version of GetTopicAsync for simpler use cases.
        /// </summary>
        public HelpTopic? GetTopic(string topicId)
        {
            return GetTopicAsync(topicId).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Searches help topics for matching content.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <returns>List of matching results.</returns>
        public async Task<List<HelpSearchResult>> SearchAsync(string query)
        {
            if (!_isInitialized) Initialize();

            if (string.IsNullOrWhiteSpace(query))
                return new List<HelpSearchResult>();

            var results = new List<HelpSearchResult>();
            var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var topicId in _registry.GetAllTopicIds())
            {
                try
                {
                    var topic = await GetTopicAsync(topicId);
                    if (topic == null) continue;

                    var score = CalculateSearchScore(topic, queryTerms);
                    if (score > 0)
                    {
                        results.Add(new HelpSearchResult
                        {
                            TopicId = topicId,
                            Title = topic.Title,
                            Snippet = ExtractSnippet(topic.Content, queryTerms),
                            Score = score
                        });
                    }
                }
                catch
                {
                    // Skip topics that fail to load during search
                }
            }

            return results.OrderByDescending(r => r.Score).Take(20).ToList();
        }

        /// <summary>
        /// Gets the table of contents.
        /// </summary>
        public HelpTocEntry? GetTableOfContents()
        {
            if (!_isInitialized) Initialize();
            return _registry.TableOfContents;
        }

        /// <summary>
        /// Clears the topic cache.
        /// </summary>
        public void ClearCache()
        {
            _topicCache.Clear();
            _logger.Info("Help topic cache cleared");
        }

        /// <summary>
        /// Closes the help display.
        /// </summary>
        public void CloseHelp()
        {
            HelpClosed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resolves the help context by walking up the visual tree looking for 
        /// attached properties first, then HelpContext attributes.
        /// </summary>
        private HelpContext ResolveHelpContext(DependencyObject? element)
        {
            var context = new HelpContext
            {
                TopicId = _registry.DefaultTopicId,
                IsContextual = true
            };

            if (element == null)
            {
                // No focused element - try to find the active window
                element = Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive);
            }

            // Walk up the visual tree looking for help context
            // Priority: 1) Attached properties, 2) HelpContext attributes
            var current = element;
            string? foundTopicId = null;
            string? foundSection = null;
            string? foundSourceElement = null;

            while (current != null)
            {
                // First, check for attached properties (most specific)
                var attachedTopicId = HelpProperties.GetTopicId(current);
                var attachedSection = HelpProperties.GetSection(current);

                if (!string.IsNullOrEmpty(attachedTopicId))
                {
                    // Found attached property - use it
                    foundTopicId = attachedTopicId;
                    foundSection = attachedSection;
                    foundSourceElement = GetElementIdentifier(current);
                    _logger.Info("Found attached help context on {0}: {1}/{2}", 
                        foundSourceElement, attachedTopicId, attachedSection ?? "none");
                    break;
                }

                // If we only have a section attached, look for topic higher up
                if (!string.IsNullOrEmpty(attachedSection) && foundSection == null)
                {
                    foundSection = attachedSection;
                    foundSourceElement = GetElementIdentifier(current);
                }

                // Check for HelpContext attribute on the class
                var helpAttr = current.GetType().GetCustomAttribute<HelpContextAttribute>();
                if (helpAttr != null)
                {
                    foundTopicId = helpAttr.TopicId;
                    // Only use attribute's section if we haven't found a more specific one
                    if (foundSection == null)
                    {
                        foundSection = helpAttr.Section;
                    }
                    if (foundSourceElement == null)
                    {
                        foundSourceElement = current.GetType().Name;
                    }
                    break;
                }

                // Try to get parent
                current = GetParent(current);
            }

            // Apply what we found
            if (!string.IsNullOrEmpty(foundTopicId))
            {
                context.TopicId = foundTopicId;
                context.Section = foundSection;
                context.SourceElement = foundSourceElement;
            }
            // If still default, check if we're in a known window type
            else if (element != null)
            {
                context.TopicId = InferTopicFromElementType(element);
            }

            return context;
        }

        /// <summary>
        /// Gets a human-readable identifier for a UI element.
        /// </summary>
        private string GetElementIdentifier(DependencyObject element)
        {
            if (element is FrameworkElement fe)
            {
                if (!string.IsNullOrEmpty(fe.Name))
                    return $"{element.GetType().Name}({fe.Name})";
            }
            return element.GetType().Name;
        }

        private DependencyObject? GetParent(DependencyObject element)
        {
            try
            {
                // Try visual tree first
                var parent = VisualTreeHelper.GetParent(element);
                if (parent != null) return parent;

                // Try logical tree
                if (element is FrameworkElement fe)
                    return fe.Parent;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string InferTopicFromElementType(DependencyObject element)
        {
            // Map common control/window names to topics
            var typeName = element.GetType().Name.ToLowerInvariant();

            return typeName switch
            {
                "mainwindow" => "getting-started/overview",
                "dashboardcontrol" => "features/dashboard",
                "teammemberscontrol" => "features/team-members",
                "oneononescontrol" => "features/one-on-ones",
                "taskscontrol" => "features/tasks",
                "projectscontrol" => "features/projects",
                "okrscontrol" => "features/okrs",
                "kpiscontrol" => "features/kpis",
                "addteammemberdialog" => "dialogs/add-team-member",
                "addoneononedialog" => "dialogs/add-one-on-one",
                "addtaskdialog" or "newtaskdialog" => "dialogs/add-task",
                "addprojectdialog" or "newprojectdialog" => "dialogs/add-project",
                "settingscontrol" => "dialogs/settings",
                _ => _registry.DefaultTopicId
            };
        }

        private async Task<HelpTopic?> LoadTopicFromFileAsync(string topicId, string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var content = await File.ReadAllTextAsync(filePath);
            
            var topic = new HelpTopic
            {
                Id = topicId,
                Content = content,
                FilePath = filePath,
                LoadedAt = DateTime.UtcNow
            };

            // Extract title from first H1
            var titleMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
            topic.Title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : ToTitleCase(topicId);

            // Extract sections (H2 and H3)
            var sectionMatches = Regex.Matches(content, @"^(#{2,3})\s+(.+)$", RegexOptions.Multiline);
            foreach (Match match in sectionMatches)
            {
                topic.Sections.Add(new HelpSection
                {
                    Level = match.Groups[1].Value.Length,
                    Title = match.Groups[2].Value.Trim(),
                    Id = ToSlug(match.Groups[2].Value)
                });
            }

            // Extract description (first paragraph after title)
            var descMatch = Regex.Match(content, @"^#\s+.+\r?\n\r?\n(.+?)(?:\r?\n\r?\n|$)", RegexOptions.Singleline);
            if (descMatch.Success)
            {
                topic.Description = descMatch.Groups[1].Value.Trim();
            }

            return topic;
        }

        private double CalculateSearchScore(HelpTopic topic, string[] queryTerms)
        {
            double score = 0;
            var titleLower = topic.Title.ToLowerInvariant();
            var contentLower = topic.Content.ToLowerInvariant();

            foreach (var term in queryTerms)
            {
                // Title match (high weight)
                if (titleLower.Contains(term))
                    score += 10;

                // Content match (lower weight)
                var contentMatches = Regex.Matches(contentLower, Regex.Escape(term)).Count;
                score += Math.Min(contentMatches, 5); // Cap at 5 to prevent spam

                // Keyword match (if implemented)
                if (topic.Keywords.Any(k => k.ToLowerInvariant().Contains(term)))
                    score += 5;
            }

            return score;
        }

        private string ExtractSnippet(string content, string[] queryTerms)
        {
            // Remove markdown formatting for cleaner snippet
            var cleanContent = Regex.Replace(content, @"[#*_`\[\]]", "");
            cleanContent = Regex.Replace(cleanContent, @"\r?\n", " ");

            // Find the first occurrence of any query term
            var contentLower = cleanContent.ToLowerInvariant();
            var firstMatchIndex = -1;

            foreach (var term in queryTerms)
            {
                var index = contentLower.IndexOf(term);
                if (index >= 0 && (firstMatchIndex < 0 || index < firstMatchIndex))
                {
                    firstMatchIndex = index;
                }
            }

            if (firstMatchIndex < 0)
            {
                // No match found, return beginning
                return cleanContent.Length > 150 
                    ? cleanContent.Substring(0, 147) + "..." 
                    : cleanContent;
            }

            // Extract snippet around the match
            var start = Math.Max(0, firstMatchIndex - 50);
            var length = Math.Min(150, cleanContent.Length - start);
            var snippet = cleanContent.Substring(start, length);

            if (start > 0) snippet = "..." + snippet;
            if (start + length < cleanContent.Length) snippet += "...";

            return snippet.Trim();
        }

        private static string ToTitleCase(string text)
        {
            var parts = text.Split('/', '-', '_');
            var lastPart = parts.LastOrDefault() ?? text;
            return string.Join(" ", lastPart.Split('-', '_')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        private static string ToSlug(string text)
        {
            return Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        }

        #endregion
    }
}

