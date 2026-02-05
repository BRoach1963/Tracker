using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces;

/// <summary>
/// Interface for help content repository operations.
/// Separates data access from business logic.
/// </summary>
public interface IHelpContentRepository
{
    /// <summary>
    /// Loads all available help topics from storage.
    /// </summary>
    Task<IEnumerable<HelpTopic>> LoadTopicsAsync();
    
    /// <summary>
    /// Loads the content for a specific help topic.
    /// </summary>
    Task<string> LoadTopicContentAsync(string filePath);
    
    /// <summary>
    /// Saves help topic index to storage.
    /// </summary>
    Task SaveTopicIndexAsync(IEnumerable<HelpTopic> topics);
    
    /// <summary>
    /// Creates default help content if none exists.
    /// </summary>
    Task CreateDefaultContentAsync();
}

/// <summary>
/// Interface for help search operations.
/// Separates search logic from content management.
/// </summary>
public interface IHelpSearchService  
{
    /// <summary>
    /// Searches help topics by query with relevance scoring.
    /// </summary>
    Task<IEnumerable<HelpTopic>> SearchAsync(string query, IEnumerable<HelpTopic> topics);
    
    /// <summary>
    /// Gets help topics relevant to a specific UI context.
    /// </summary>
    Task<IEnumerable<HelpTopic>> GetContextTopicsAsync(object? context, IEnumerable<HelpTopic> topics);
}

/// <summary>
/// Interface for help window factory.
/// Enables testable view creation without MVVM violations.
/// </summary>
public interface IHelpWindowFactory
{
    /// <summary>
    /// Creates and shows a help window with the specified topic.
    /// </summary>
    Task ShowHelpWindowAsync(string? initialTopicId = null);
}