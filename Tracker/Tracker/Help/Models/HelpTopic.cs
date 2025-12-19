namespace Tracker.Help.Models
{
    /// <summary>
    /// Represents a help topic loaded from a markdown file.
    /// </summary>
    public class HelpTopic
    {
        /// <summary>
        /// Unique identifier for this topic (e.g., "features/team-members").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display title extracted from the markdown H1 or metadata.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Brief description/summary of the topic.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Raw markdown content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Parent topic ID for breadcrumb navigation.
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// Related topic IDs for "See Also" section.
        /// </summary>
        public List<string> RelatedTopics { get; set; } = new();

        /// <summary>
        /// Keywords for search indexing.
        /// </summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// Section anchors within the document (H2/H3 headers).
        /// </summary>
        public List<HelpSection> Sections { get; set; } = new();

        /// <summary>
        /// When this topic was last loaded.
        /// </summary>
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// File path this topic was loaded from.
        /// </summary>
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// Represents a section (H2/H3) within a help topic.
    /// </summary>
    public class HelpSection
    {
        /// <summary>
        /// Section anchor ID (slug from header text).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Section title (header text).
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Nesting level (2 for H2, 3 for H3, etc.).
        /// </summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// Result from a help search query.
    /// </summary>
    public class HelpSearchResult
    {
        /// <summary>
        /// The topic that matched.
        /// </summary>
        public string TopicId { get; set; } = string.Empty;

        /// <summary>
        /// Topic title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Matching section within the topic (if applicable).
        /// </summary>
        public string? SectionId { get; set; }

        /// <summary>
        /// Text snippet showing the match context.
        /// </summary>
        public string Snippet { get; set; } = string.Empty;

        /// <summary>
        /// Relevance score (higher = more relevant).
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// Context information passed when requesting help.
    /// </summary>
    public class HelpContext
    {
        /// <summary>
        /// The topic ID to display.
        /// </summary>
        public string TopicId { get; set; } = string.Empty;

        /// <summary>
        /// Optional section to scroll to.
        /// </summary>
        public string? Section { get; set; }

        /// <summary>
        /// Source element type name (for debugging/logging).
        /// </summary>
        public string? SourceElement { get; set; }

        /// <summary>
        /// Whether this came from F1 key or direct navigation.
        /// </summary>
        public bool IsContextual { get; set; }
    }

    /// <summary>
    /// Entry in the table of contents.
    /// </summary>
    public class HelpTocEntry
    {
        /// <summary>
        /// Display title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Topic ID (null for folder/category entries).
        /// </summary>
        public string? TopicId { get; set; }

        /// <summary>
        /// Icon identifier (for display).
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Child entries (for hierarchical display).
        /// </summary>
        public List<HelpTocEntry> Children { get; set; } = new();

        /// <summary>
        /// Whether this entry is expanded by default.
        /// </summary>
        public bool IsExpanded { get; set; }
    }
}

