using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ProCohere.Avalonia.Interfaces;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Clean business service for help system operations.
/// Orchestrates repository and search services following single responsibility principle.
/// </summary>
public class HelpService
{
    private static readonly Lazy<HelpService> _instance = new(() => new HelpService());
    public static HelpService Instance => _instance.Value;
    
    private readonly IHelpContentRepository _contentRepository;
    private readonly IHelpSearchService _searchService;
    private readonly ConcurrentDictionary<string, string> _contentCache;
    private readonly Dictionary<string, HelpTopic> _topicsCache;
    private bool _isInitialized = false;
    
    // Constructor for dependency injection (future)
    public HelpService(IHelpContentRepository? contentRepository = null, IHelpSearchService? searchService = null)
    {
        _contentRepository = contentRepository ?? new HelpContentRepository();
        _searchService = searchService ?? new HelpSearchService();
        _contentCache = new ConcurrentDictionary<string, string>();
        _topicsCache = new Dictionary<string, HelpTopic>();
    }
    
    private HelpService() : this(null, null) { }
    
    /// <summary>
    /// Initializes the help service by loading topics.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        try
        {
            await LoadTopicsAsync();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpService] Initialization failed: {ex.Message}");
            throw new InvalidOperationException("Failed to initialize help system", ex);
        }
    }
    
    /// <summary>
    /// Gets a help topic by its ID with content loaded.
    /// </summary>
    public async Task<HelpTopic?> GetTopicAsync(string topicId)
    {
        await EnsureInitializedAsync();
        
        if (!_topicsCache.TryGetValue(topicId, out var topic))
            return null;
            
        // Load content if not already loaded
        if (string.IsNullOrEmpty(topic.Content) && !string.IsNullOrEmpty(topic.FilePath))
        {
            topic.Content = await LoadTopicContentAsync(topic.FilePath);
        }
        
        return topic;
    }
    
    /// <summary>
    /// Searches for help topics by query string.
    /// </summary>
    public async Task<List<HelpTopic>> SearchTopicsAsync(string query)
    {
        await EnsureInitializedAsync();
        var results = await _searchService.SearchAsync(query, _topicsCache.Values);
        return results.ToList();
    }
    
    /// <summary>
    /// Gets help topics for a specific UI context.
    /// </summary>
    public async Task<List<HelpTopic>> GetTopicsForContextAsync(object? context)
    {
        await EnsureInitializedAsync();
        var results = await _searchService.GetContextTopicsAsync(context, _topicsCache.Values);
        return results.ToList();
    }
    
    /// <summary>
    /// Gets the best help topic for the current context.
    /// </summary>
    public async Task<HelpTopic?> GetContextHelpAsync(object? context = null)
    {
        var topics = await GetTopicsForContextAsync(context);
        return topics.FirstOrDefault() ?? await GetTopicAsync("overview");
    }
    
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
    }
    
    private async Task LoadTopicsAsync()
    {
        try
        {
            var topics = await _contentRepository.LoadTopicsAsync();
            
            _topicsCache.Clear();
            foreach (var topic in topics)
            {
                _topicsCache[topic.Id] = topic;
            }
            
            // Create default content if no topics were loaded
            if (!_topicsCache.Any())
            {
                await _contentRepository.CreateDefaultContentAsync();
                await LoadTopicsAsync(); // Retry loading after creating defaults
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpService] Failed to load topics: {ex.Message}");
            throw;
        }
    }
    
    private async Task<string> LoadTopicContentAsync(string filePath)
    {
        if (_contentCache.TryGetValue(filePath, out var cached))
            return cached;
        
        try
        {
            var content = await _contentRepository.LoadTopicContentAsync(filePath);
            if (!string.IsNullOrEmpty(content))
            {
                _contentCache[filePath] = content;
            }
            return content;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpService] Failed to load content from {filePath}: {ex.Message}");
            return string.Empty;
        }
    }
}