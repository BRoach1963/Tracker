using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ProCohere.Avalonia.Attributes;
using ProCohere.Avalonia.Interfaces;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for searching and filtering help topics.
/// Separated from content storage for single responsibility.
/// </summary>
public class HelpSearchService : IHelpSearchService
{
    public Task<IEnumerable<HelpTopic>> SearchAsync(string query, IEnumerable<HelpTopic> topics)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            var sortedTopics = topics.OrderBy(t => t.Title);
            return Task.FromResult<IEnumerable<HelpTopic>>(sortedTopics);
        }
        
        var queryLower = query.ToLowerInvariant();
        var results = topics
            .Select(topic => new { Topic = topic, Score = CalculateRelevanceScore(topic, queryLower) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Topic.Priority)
            .ThenBy(x => x.Topic.Title)
            .Select(x => x.Topic);
            
        return Task.FromResult<IEnumerable<HelpTopic>>(results);
    }
    
    public Task<IEnumerable<HelpTopic>> GetContextTopicsAsync(object? context, IEnumerable<HelpTopic> topics)
    {
        var contextName = GetContextName(context);
        if (string.IsNullOrEmpty(contextName))
        {
            return Task.FromResult(Enumerable.Empty<HelpTopic>());
        }
        
        var contextTopics = topics
            .Where(t => t.IsContextSensitive && t.Contexts.Contains(contextName))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Title);
            
        return Task.FromResult<IEnumerable<HelpTopic>>(contextTopics);
    }
    
    private static int CalculateRelevanceScore(HelpTopic topic, string queryLower)
    {
        var score = 0;
        
        // Title match (highest score)
        if (topic.Title.ToLowerInvariant().Contains(queryLower))
            score += 100;
        
        // Keyword match
        foreach (var keyword in topic.Keywords)
        {
            if (keyword.ToLowerInvariant().Contains(queryLower))
                score += 50;
        }
        
        // Category match
        if (topic.Category.ToLowerInvariant().Contains(queryLower))
            score += 30;
        
        // Content match (if loaded)
        if (!string.IsNullOrEmpty(topic.Content) && 
            topic.Content.ToLowerInvariant().Contains(queryLower))
            score += 20;
        
        // Add priority bonus
        score += topic.Priority;
        
        return score;
    }
    
    private static string GetContextName(object? context)
    {
        if (context == null) return string.Empty;
        
        var type = context.GetType();
        
        // Check for HelpContextAttribute on the class
        var helpAttribute = type.GetCustomAttribute<HelpContextAttribute>();
        if (helpAttribute != null)
        {
            return helpAttribute.ContextName ?? type.Name;
        }
        
        // Default to type name
        return type.Name;
    }
}