using System;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Represents a help topic with content and metadata for the help system.
/// </summary>
public class HelpTopic
{
    /// <summary>
    /// Unique identifier for the help topic.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display title of the help topic.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Markdown content for the help topic.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Keywords for searching this help topic.
    /// </summary>
    public List<string> Keywords { get; set; } = new();
    
    /// <summary>
    /// Category this help topic belongs to (e.g., "Projects", "Goals", "Teams").
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Priority for search results (higher is more important).
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Related topic IDs for "See Also" links.
    /// </summary>
    public List<string> RelatedTopics { get; set; } = new();
    
    /// <summary>
    /// File path to the markdown content (for lazy loading).
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// When this topic was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this is a context-sensitive topic (appears for specific UI contexts).
    /// </summary>
    public bool IsContextSensitive { get; set; }
    
    /// <summary>
    /// UI contexts where this topic is relevant (e.g., "ProjectsView", "GoalsDialog").
    /// </summary>
    public List<string> Contexts { get; set; } = new();
}