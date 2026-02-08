using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services.AI;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Type of entity for search results.
/// </summary>
public enum SearchEntityType
{
    TeamMember,
    Meeting,
    Task,
    Goal,
    Target,
    Metric,
    Note,
    Feedback,
    Project,
    Kudos
}

/// <summary>
/// Result from a global search operation.
/// </summary>
public sealed record GlobalSearchResult(
    SearchEntityType EntityType,
    string TypeDisplay,
    string Title,
    string Description,
    string Icon,
    Guid EntityId,
    double Relevance,
    DateTime? Date,
    bool IsSemanticResult,
    object? Entity
);

/// <summary>
/// Search options for customizing search behavior.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>Maximum number of results to return.</summary>
    public int MaxResults { get; init; } = 50;
    
    /// <summary>Entity types to include (null = all).</summary>
    public SearchEntityType[]? EntityTypes { get; init; }
    
    /// <summary>Whether to include semantic/vector search results.</summary>
    public bool IncludeSemanticResults { get; init; } = true;
    
    /// <summary>Minimum text match length to consider.</summary>
    public int MinQueryLength { get; init; } = 2;
    
    /// <summary>Minimum similarity for semantic results.</summary>
    public double MinSemanticSimilarity { get; init; } = 0.5;
    
    /// <summary>Maximum semantic results to fetch.</summary>
    public int MaxSemanticResults { get; init; } = 10;
}

/// <summary>
/// Provides global search functionality across all entities.
/// Combines fast text search with optional semantic/vector search.
/// Thread-safe singleton with performance optimizations.
/// </summary>
public sealed class GlobalSearchService
{
    #region Singleton
    
    private static readonly Lazy<GlobalSearchService> _instance =
        new(() => new GlobalSearchService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    /// <summary>Gets the singleton instance.</summary>
    public static GlobalSearchService Instance => _instance.Value;
    
    #endregion
    
    #region Constants
    
    private static readonly Dictionary<SearchEntityType, string> EntityIcons = new()
    {
        [SearchEntityType.TeamMember] = "👤",
        [SearchEntityType.Meeting] = "📅",
        [SearchEntityType.Task] = "✅",
        [SearchEntityType.Goal] = "🎯",
        [SearchEntityType.Target] = "🎯",
        [SearchEntityType.Metric] = "📊",
        [SearchEntityType.Note] = "📝",
        [SearchEntityType.Feedback] = "💬",
        [SearchEntityType.Project] = "📁",
        [SearchEntityType.Kudos] = "⭐"
    };
    
    private static readonly Dictionary<SearchEntityType, string> EntityTypeDisplays = new()
    {
        [SearchEntityType.TeamMember] = "Team Member",
        [SearchEntityType.Meeting] = "Meeting",
        [SearchEntityType.Task] = "Task",
        [SearchEntityType.Goal] = "Goal",
        [SearchEntityType.Target] = "Target",
        [SearchEntityType.Metric] = "Metric",
        [SearchEntityType.Note] = "Note",
        [SearchEntityType.Feedback] = "Feedback",
        [SearchEntityType.Project] = "Project",
        [SearchEntityType.Kudos] = "Kudos"
    };
    
    #endregion
    
    #region Properties
    
    /// <summary>Last error message if an operation failed.</summary>
    public string? LastError { get; private set; }
    
    #endregion
    
    #region Constructor
    
    private GlobalSearchService() { }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Searches across all entities for the given query.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="options">Search options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results ordered by relevance.</returns>
    public async Task<List<GlobalSearchResult>> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new SearchOptions();
        LastError = null;
        
        if (string.IsNullOrWhiteSpace(query) || query.Length < options.MinQueryLength)
        {
            return new List<GlobalSearchResult>();
        }
        
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var results = new List<GlobalSearchResult>();
        
        try
        {
            // Run text search and semantic search in parallel
            var textSearchTask = PerformTextSearchAsync(normalizedQuery, options, cancellationToken);
            
            Task<List<GlobalSearchResult>> semanticSearchTask = options.IncludeSemanticResults && VectorSearchService.Instance.IsAvailable
                ? PerformSemanticSearchAsync(query, options, cancellationToken)
                : Task.FromResult(new List<GlobalSearchResult>());
            
            await Task.WhenAll(textSearchTask, semanticSearchTask);
            
            var textResults = await textSearchTask;
            var semanticResults = await semanticSearchTask;
            
            // Merge and deduplicate results
            results = MergeResults(textResults, semanticResults, options.MaxResults);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LastError = $"Search failed: {ex.Message}";
            Debug.WriteLine($"[GlobalSearch] Error: {ex.Message}");
        }
        
        return results;
    }
    
    /// <summary>
    /// Gets recent items across all entity types.
    /// </summary>
    /// <param name="count">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent items.</returns>
    public async Task<List<GlobalSearchResult>> GetRecentItemsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GlobalSearchResult>();
        
        try
        {
            var data = await DashboardService.Instance.LoadDashboardDataAsync();
            
            // Get recent meetings
            results.AddRange(data.Meetings
                .OrderByDescending(m => m.ScheduledAt)
                .Take(3)
                .Select(m => CreateResult(SearchEntityType.Meeting, m.Id, 
                    m.Title ?? $"Meeting with {m.TeamMemberName ?? "team"}", 
                    m.Description ?? string.Empty, 
                    m.ScheduledAt, 0.5, m)));
            
            // Get recent tasks
            results.AddRange(data.Tasks
                .OrderByDescending(t => t.UpdatedAt)
                .Take(3)
                .Select(t => CreateResult(SearchEntityType.Task, t.Id,
                    t.Title ?? "Untitled Task",
                    $"Due: {t.DueDate?.ToString("MMM dd") ?? "No due date"}",
                    t.DueDate, 0.5, t)));
            
            // Get recent goals
            results.AddRange(data.Goals
                .OrderByDescending(g => g.UpdatedAt)
                .Take(2)
                .Select(g => CreateResult(SearchEntityType.Goal, g.Id,
                    g.Title,
                    g.Description ?? string.Empty,
                    g.DueDate, 0.5, g)));
            
            // Get recent feedback
            results.AddRange(data.Feedback
                .OrderByDescending(f => f.CreatedAt)
                .Take(2)
                .Select(f => CreateResult(SearchEntityType.Feedback, f.Id,
                    Truncate(f.Content ?? "Feedback", 50),
                    f.TypeDisplay ?? "Feedback",
                    f.CreatedAt, 0.5, f)));
            
            // Sort by date and take requested count
            results = results
                .OrderByDescending(r => r.Date)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GlobalSearch] Recent items error: {ex.Message}");
        }
        
        return results;
    }
    
    #endregion
    
    #region Private Methods - Text Search
    
    private async Task<List<GlobalSearchResult>> PerformTextSearchAsync(
        string query,
        SearchOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<GlobalSearchResult>();
        
        // Load cached dashboard data (fast - already loaded in most cases)
        var data = await DashboardService.Instance.LoadDashboardDataAsync();
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Search each entity type in parallel
        var searchTasks = new List<Task<List<GlobalSearchResult>>>();
        
        if (ShouldSearch(options, SearchEntityType.TeamMember))
            searchTasks.Add(Task.Run(() => SearchTeamMembers(data.TeamMembers, query), cancellationToken));
        
        if (ShouldSearch(options, SearchEntityType.Meeting))
            searchTasks.Add(Task.Run(() => SearchMeetings(data.Meetings, query), cancellationToken));
        
        if (ShouldSearch(options, SearchEntityType.Task))
            searchTasks.Add(Task.Run(() => SearchTasks(data.Tasks, query), cancellationToken));
        
        if (ShouldSearch(options, SearchEntityType.Goal))
            searchTasks.Add(Task.Run(() => SearchGoals(data.Goals, query), cancellationToken));
        
        if (ShouldSearch(options, SearchEntityType.Feedback))
            searchTasks.Add(Task.Run(() => SearchFeedback(data.Feedback, query), cancellationToken));
        
        await Task.WhenAll(searchTasks);
        
        foreach (var task in searchTasks)
        {
            results.AddRange(await task);
        }
        
        return results;
    }
    
    private static bool ShouldSearch(SearchOptions options, SearchEntityType type)
    {
        return options.EntityTypes == null || options.EntityTypes.Contains(type);
    }
    
    private List<GlobalSearchResult> SearchTeamMembers(IEnumerable<TeamMemberDetail> members, string query)
    {
        return members
            .Where(m => 
                (m.FirstName?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.LastName?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.DisplayName?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.Email?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.JobTitle?.ToLowerInvariant().Contains(query) ?? false))
            .Select(m => CreateResult(
                SearchEntityType.TeamMember,
                m.Id,
                m.FullName,
                m.JobTitle ?? m.Email,
                null,
                CalculateRelevance(query, m.FullName, m.Email),
                m))
            .ToList();
    }
    
    private List<GlobalSearchResult> SearchMeetings(IEnumerable<MeetingDetail> meetings, string query)
    {
        return meetings
            .Where(m =>
                (m.TeamMemberName?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.Description?.ToLowerInvariant().Contains(query) ?? false) ||
                (m.Title?.ToLowerInvariant().Contains(query) ?? false))
            .Select(m => CreateResult(
                SearchEntityType.Meeting,
                m.Id,
                m.Title ?? $"Meeting with {m.TeamMemberName ?? "team"}",
                m.Description ?? string.Empty,
                m.ScheduledAt,
                CalculateRelevance(query, m.Title, m.TeamMemberName),
                m))
            .ToList();
    }
    
    private List<GlobalSearchResult> SearchTasks(IEnumerable<TaskDetail> tasks, string query)
    {
        return tasks
            .Where(t =>
                (t.Title?.ToLowerInvariant().Contains(query) ?? false) ||
                (t.Description?.ToLowerInvariant().Contains(query) ?? false))
            .Select(t => CreateResult(
                SearchEntityType.Task,
                t.Id,
                t.Title ?? "Untitled Task",
                $"Due: {t.DueDate?.ToString("MMM dd") ?? "No date"} | {t.StatusDisplay}",
                t.DueDate,
                CalculateRelevance(query, t.Title, t.Description),
                t))
            .ToList();
    }
    
    private List<GlobalSearchResult> SearchGoals(IEnumerable<GoalDetail> goals, string query)
    {
        return goals
            .Where(g =>
                (g.Title?.ToLowerInvariant().Contains(query) ?? false) ||
                (g.Description?.ToLowerInvariant().Contains(query) ?? false))
            .Select(g => CreateResult(
                SearchEntityType.Goal,
                g.Id,
                g.Title,
                g.Description ?? string.Empty,
                g.DueDate,
                CalculateRelevance(query, g.Title, g.Description),
                g))
            .ToList();
    }
    
    private List<GlobalSearchResult> SearchFeedback(IEnumerable<FeedbackDetail> feedback, string query)
    {
        return feedback
            .Where(f =>
                (f.Content?.ToLowerInvariant().Contains(query) ?? false) ||
                (f.Title?.ToLowerInvariant().Contains(query) ?? false))
            .Select(f => CreateResult(
                SearchEntityType.Feedback,
                f.Id,
                Truncate(f.Title ?? f.Content ?? "Feedback", 60),
                f.TypeDisplay ?? "Feedback",
                f.CreatedAt,
                CalculateRelevance(query, f.Title, f.Content),
                f))
            .ToList();
    }
    
    #endregion
    
    #region Private Methods - Semantic Search
    
    private async Task<List<GlobalSearchResult>> PerformSemanticSearchAsync(
        string query,
        SearchOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<GlobalSearchResult>();
        
        try
        {
            // Convert entity type filter to string array for vector search
            string[]? entityTypeFilter = null;
            if (options.EntityTypes != null)
            {
                entityTypeFilter = options.EntityTypes
                    .Select(EntityTypeToVectorType)
                    .Where(t => t != null)
                    .Cast<string>()
                    .ToArray();
            }
            
            var vectorResults = await VectorSearchService.Instance.SearchAsync(
                query,
                options.MaxSemanticResults,
                entityTypeFilter,
                options.MinSemanticSimilarity,
                cancellationToken);
            
            foreach (var vr in vectorResults)
            {
                var entityType = VectorTypeToEntityType(vr.EntityType);
                if (entityType == null) continue;
                
                results.Add(new GlobalSearchResult(
                    entityType.Value,
                    EntityTypeDisplays.GetValueOrDefault(entityType.Value, vr.EntityType),
                    Truncate(vr.ContentPreview ?? vr.Content ?? "Match", 60),
                    $"Semantic match ({vr.Similarity:P0})",
                    EntityIcons.GetValueOrDefault(entityType.Value, "🔍"),
                    vr.EntityId,
                    vr.Similarity,
                    null,
                    true,
                    null
                ));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GlobalSearch] Semantic search error: {ex.Message}");
            // Don't fail the whole search if semantic fails
        }
        
        return results;
    }
    
    private static string? EntityTypeToVectorType(SearchEntityType type)
    {
        return type switch
        {
            SearchEntityType.Note => "note",
            SearchEntityType.Feedback => "feedback",
            SearchEntityType.Meeting => "meeting_note",
            SearchEntityType.Goal => "goal",
            SearchEntityType.Task => "task",
            _ => null
        };
    }
    
    private static SearchEntityType? VectorTypeToEntityType(string vectorType)
    {
        return vectorType switch
        {
            "note" => SearchEntityType.Note,
            "feedback" => SearchEntityType.Feedback,
            "meeting_note" => SearchEntityType.Meeting,
            "goal" => SearchEntityType.Goal,
            "task" => SearchEntityType.Task,
            _ => null
        };
    }
    
    #endregion
    
    #region Private Methods - Utilities
    
    private List<GlobalSearchResult> MergeResults(
        List<GlobalSearchResult> textResults,
        List<GlobalSearchResult> semanticResults,
        int maxResults)
    {
        // Combine results, deduplicating by EntityId
        var combined = new Dictionary<Guid, GlobalSearchResult>();
        
        // Add text results first (they have higher confidence)
        foreach (var result in textResults)
        {
            if (!combined.ContainsKey(result.EntityId))
            {
                combined[result.EntityId] = result;
            }
        }
        
        // Add semantic results, boosting score if already found via text
        foreach (var result in semanticResults)
        {
            if (combined.TryGetValue(result.EntityId, out var existing))
            {
                // Boost existing result with semantic confirmation
                combined[result.EntityId] = existing with
                {
                    Relevance = Math.Min(1.0, existing.Relevance + (result.Relevance * 0.2))
                };
            }
            else
            {
                combined[result.EntityId] = result;
            }
        }
        
        // Sort by relevance, then by date
        return combined.Values
            .OrderByDescending(r => r.Relevance)
            .ThenByDescending(r => r.Date)
            .Take(maxResults)
            .ToList();
    }
    
    private static GlobalSearchResult CreateResult(
        SearchEntityType type,
        Guid id,
        string title,
        string description,
        DateTime? date,
        double relevance,
        object? entity)
    {
        return new GlobalSearchResult(
            type,
            EntityTypeDisplays.GetValueOrDefault(type, type.ToString()),
            title,
            description,
            EntityIcons.GetValueOrDefault(type, "📄"),
            id,
            relevance,
            date,
            false,
            entity
        );
    }
    
    private static double CalculateRelevance(string query, params string?[] fields)
    {
        var score = 0.0;
        var queryLower = query.ToLowerInvariant();
        
        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field)) continue;
            
            var fieldLower = field.ToLowerInvariant();
            
            // Exact match in field
            if (fieldLower.Equals(queryLower))
            {
                score = Math.Max(score, 1.0);
            }
            // Starts with query
            else if (fieldLower.StartsWith(queryLower))
            {
                score = Math.Max(score, 0.9);
            }
            // Word starts with query
            else if (fieldLower.Split(' ').Any(w => w.StartsWith(queryLower)))
            {
                score = Math.Max(score, 0.8);
            }
            // Contains query
            else if (fieldLower.Contains(queryLower))
            {
                score = Math.Max(score, 0.6);
            }
        }
        
        return score;
    }
    
    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;
        
        return text[..(maxLength - 3)] + "...";
    }
    
    #endregion
}
