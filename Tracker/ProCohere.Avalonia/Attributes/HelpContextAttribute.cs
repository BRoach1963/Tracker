using System;

namespace ProCohere.Avalonia.Attributes;

/// <summary>
/// Marks a UI element with a help context for F1 context-sensitive help.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
public class HelpContextAttribute : Attribute
{
    /// <summary>
    /// The help topic ID to show when F1 is pressed in this context.
    /// </summary>
    public string TopicId { get; }
    
    /// <summary>
    /// Optional context name for this UI element (used for grouping related help contexts).
    /// </summary>
    public string? ContextName { get; set; }
    
    /// <summary>
    /// Priority for this context if multiple contexts are found (higher wins).
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Creates a new help context attribute.
    /// </summary>
    /// <param name="topicId">The help topic ID to associate with this context</param>
    public HelpContextAttribute(string topicId)
    {
        TopicId = topicId ?? throw new ArgumentNullException(nameof(topicId));
    }
}