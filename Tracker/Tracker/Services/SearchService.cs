using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services
{
    /// <summary>
    /// Result from a global search operation.
    /// </summary>
    public class SearchResult
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        /// <summary>
        /// Entity ID for non-TeamMember entities (int-based IDs).
        /// </summary>
        public int EntityId { get; set; }
        /// <summary>
        /// Entity ID for TeamMember entities (Guid-based ID).
        /// </summary>
        public Guid? GuidEntityId { get; set; }
        public DateTime? Date { get; set; }
        public object? Entity { get; set; }
    }

    /// <summary>
    /// Provides global search functionality across all entities.
    /// </summary>
    public class SearchService : ISearchService
    {
        #region Singleton

        private static readonly Lazy<SearchService> _lazyInstance = 
            new(() => new SearchService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of SearchService.
        /// </summary>
        public static SearchService Instance => _lazyInstance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;

        #endregion

        #region Constructor

        private SearchService()
        {
            _logger = LoggingManager.GetComponentLogger("SearchService");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches across all entities for the given query.
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(string query, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<SearchResult>();

            var results = new List<SearchResult>();
            query = query.ToLowerInvariant();

            try
            {
                // Search in parallel
                var teamMembersTask = SearchTeamMembersAsync(query);
                var meetingsTask = SearchMeetingsAsync(query);
                var projectsTask = SearchProjectsAsync(query);
                var tasksTask = SearchTasksAsync(query);
                var goalsTask = SearchGoalsAsync(query);
                var targetsTask = SearchTargetsAsync(query);
                var metricsTask = SearchMetricsAsync(query);
                var notesTask = SearchNotesAsync(query);
                var feedbackTask = SearchFeedbackAsync(query);

                await Task.WhenAll(
                    teamMembersTask, meetingsTask, projectsTask, tasksTask,
                    goalsTask, targetsTask, metricsTask, notesTask, feedbackTask
                );

                results.AddRange(await teamMembersTask);
                results.AddRange(await meetingsTask);
                results.AddRange(await projectsTask);
                results.AddRange(await tasksTask);
                results.AddRange(await goalsTask);
                results.AddRange(await targetsTask);
                results.AddRange(await metricsTask);
                results.AddRange(await notesTask);
                results.AddRange(await feedbackTask);

                // Sort by relevance (exact matches first, then by date)
                results = results
                    .OrderByDescending(r => r.Title.ToLowerInvariant().StartsWith(query))
                    .ThenByDescending(r => r.Title.ToLowerInvariant().Contains(query))
                    .ThenByDescending(r => r.Date)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error during search");
            }

            return results;
        }

        /// <summary>
        /// Gets recent items across all entity types.
        /// </summary>
        public async Task<List<SearchResult>> GetRecentItemsAsync(int count = 10)
        {
            var results = new List<SearchResult>();

            try
            {
                // Get recent from each category using TrackerDataManager as single source of truth
                var meetings = await TrackerDataManager.Instance.GetOneOnOneMeetings();
                results.AddRange(meetings.Take(3).Select(m => new SearchResult
                {
                    Type = "1:1 Meeting",
                    Title = $"1:1 with {m.Report?.FullName ?? "Unknown"}",
                    Description = m.Description ?? string.Empty,
                    Icon = "📅",
                    GuidEntityId = m.Id,
                    Date = m.ScheduledAt,
                    Entity = m
                }));

                var tasks = await TrackerDataManager.Instance.GetTasks();
                results.AddRange(tasks.Take(3).Select(t => new SearchResult
                {
                    Type = "Task",
                    Title = t.Title,
                    Description = $"Assigned to: {t.Owner?.FullName ?? "Unassigned"}",
                    Icon = "✅",
                    GuidEntityId = t.Id,
                    Date = t.DueDate,
                    Entity = t
                }));

                var notes = await TrackerDataManager.Instance.GetQuickNotes();
                results.AddRange(notes.Take(3).Select(n => new SearchResult
                {
                    Type = "Note",
                    Title = n.Preview,
                    Description = n.CategoryDisplay,
                    Icon = "📝",
                    EntityId = n.Id,
                    Date = n.CreatedAt,
                    Entity = n
                }));

                results = results.OrderByDescending(r => r.Date).Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting recent items");
            }

            return results;
        }

        #endregion

        #region Private Search Methods

        private async Task<List<SearchResult>> SearchTeamMembersAsync(string query)
        {
            var results = new List<SearchResult>();
            var members = await TrackerDataManager.Instance.GetTeamData();

            foreach (var m in members.Where(m =>
                m.FirstName.ToLowerInvariant().Contains(query) ||
                m.LastName.ToLowerInvariant().Contains(query) ||
                m.Email.ToLowerInvariant().Contains(query) ||
                m.JobTitle.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Team Member",
                    Title = $"{m.FirstName} {m.LastName}",
                    Description = m.JobTitle ?? string.Empty,
                    Icon = "👤",
                    GuidEntityId = m.Id,
                    Entity = m
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchMeetingsAsync(string query)
        {
            var results = new List<SearchResult>();
            var meetings = await TrackerDataManager.Instance.GetOneOnOneMeetings();

            foreach (var m in meetings.Where(m =>
                (m.Report?.FullName ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (m.Description ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (m.Notes ?? string.Empty).ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "1:1 Meeting",
                    Title = $"1:1 with {m.Report?.FullName ?? "Unknown"}",
                    Description = m.Description ?? string.Empty,
                    Icon = "📅",
                    GuidEntityId = m.Id,
                    Date = m.ScheduledAt,
                    Entity = m
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchProjectsAsync(string query)
        {
            var results = new List<SearchResult>();
            var projects = await TrackerDataManager.Instance.GetProjects();

            foreach (var p in projects.Where(p =>
                p.Name.ToLowerInvariant().Contains(query) ||
                (p.Description ?? string.Empty).ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Project",
                    Title = p.Name,
                    Description = p.Description ?? string.Empty,
                    Icon = "📁",
                    GuidEntityId = p.Id,
                    Date = p.StartDate,
                    Entity = p
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchTasksAsync(string query)
        {
            var results = new List<SearchResult>();
            var tasks = await TrackerDataManager.Instance.GetTasks();

            foreach (var t in tasks.Where(t =>
                (t.Title ?? t.Description ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (t.Owner?.FullName ?? string.Empty).ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Task",
                    Title = t.Title ?? t.Description ?? "Untitled Task",
                    Description = $"Due: {t.DueDate:MMM dd} | {t.Owner?.FullName ?? "Unassigned"}",
                    Icon = "✅",
                    GuidEntityId = t.Id,
                    Date = t.DueDate,
                    Entity = t
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchGoalsAsync(string query)
        {
            var results = new List<SearchResult>();
            var goals = await TrackerDataManager.Instance.GetGoals();

            foreach (var g in goals.Where(g =>
                g.Title.ToLowerInvariant().Contains(query) ||
                g.Description.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Goal",
                    Title = g.Title,
                    Description = g.Description,
                    Icon = "🎯",
                    GuidEntityId = g.Id,
                    Date = g.TargetDate,
                    Entity = g
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchTargetsAsync(string query)
        {
            var results = new List<SearchResult>();
            var goals = await TrackerDataManager.Instance.GetStrategicGoals();
            var allTargets = goals.SelectMany(g => g.Targets ?? new List<Target>()).ToList();

            foreach (var t in allTargets.Where(t =>
                (t.Title ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (t.Description ?? string.Empty).ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Target",
                    Title = t.Title ?? "Untitled Target",
                    Description = t.Description ?? string.Empty,
                    Icon = "🎯",
                    GuidEntityId = t.Id,
                    Entity = t
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchMetricsAsync(string query)
        {
            var results = new List<SearchResult>();

            // Metrics search is not yet implemented.
            // This will be wired to MetricRepository / TrackerDataManager metrics once available.
            return results;
        }

        private async Task<List<SearchResult>> SearchNotesAsync(string query)
        {
            var results = new List<SearchResult>();
            var notes = await TrackerDataManager.Instance.GetQuickNotes();

            foreach (var n in notes.Where(n =>
                n.Content.ToLowerInvariant().Contains(query) ||
                n.Tags.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Note",
                    Title = n.Preview,
                    Description = n.CategoryDisplay,
                    Icon = "📝",
                    EntityId = n.Id,
                    Date = n.CreatedAt,
                    Entity = n
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchFeedbackAsync(string query)
        {
            var results = new List<SearchResult>();
            var members = await TrackerDataManager.Instance.GetTeamData();
            var feedbacks = await TrackerDataManager.Instance.GetFeedbacks();

            foreach (var f in feedbacks.Where(f =>
                (f.Content ?? string.Empty).ToLowerInvariant().Contains(query)))
            {
                var member = members.FirstOrDefault(m => m.Id == f.ToTeamMemberId);
                results.Add(new SearchResult
                {
                    Type = "Feedback",
                    Title = f.Content?.Length > 50 ? f.Content.Substring(0, 50) + "..." : f.Content ?? "Feedback",
                    Description = $"For {member?.FirstName ?? "Unknown"} - {f.FeedbackType}",
                    Icon = "💬",
                    GuidEntityId = f.Id,
                    Date = f.CreatedAt,
                    Entity = f
                });
            }

            return results;
        }

        #endregion
    }
}

