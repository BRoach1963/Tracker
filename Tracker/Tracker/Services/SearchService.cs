using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;

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
        public int EntityId { get; set; }
        public DateTime? Date { get; set; }
        public object? Entity { get; set; }
    }

    /// <summary>
    /// Provides global search functionality across all entities.
    /// </summary>
    public class SearchService
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
                var oneOnOnesTask = SearchOneOnOnesAsync(query);
                var projectsTask = SearchProjectsAsync(query);
                var tasksTask = SearchTasksAsync(query);
                var okrsTask = SearchOkrsAsync(query);
                var kpisTask = SearchKpisAsync(query);
                var notesTask = SearchNotesAsync(query);
                var goalsTask = SearchGoalsAsync(query);
                var feedbackTask = SearchFeedbackAsync(query);

                await Task.WhenAll(
                    teamMembersTask, oneOnOnesTask, projectsTask, tasksTask,
                    okrsTask, kpisTask, notesTask, goalsTask, feedbackTask
                );

                results.AddRange(await teamMembersTask);
                results.AddRange(await oneOnOnesTask);
                results.AddRange(await projectsTask);
                results.AddRange(await tasksTask);
                results.AddRange(await okrsTask);
                results.AddRange(await kpisTask);
                results.AddRange(await notesTask);
                results.AddRange(await goalsTask);
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
                // Get recent from each category
                var oneOnOnes = await TrackerDbManager.Instance.GetOneOnOnesAsync();
                results.AddRange(oneOnOnes.Take(3).Select(o => new SearchResult
                {
                    Type = "1:1 Meeting",
                    Title = $"1:1 with {o.TeamMemberName}",
                    Description = o.Description,
                    Icon = "📅",
                    EntityId = o.Id,
                    Date = o.Date,
                    Entity = o
                }));

                var tasks = await TrackerDbManager.Instance.GetTasksAsync();
                results.AddRange(tasks.Take(3).Select(t => new SearchResult
                {
                    Type = "Task",
                    Title = t.Description,
                    Description = $"Assigned to: {t.OwnerName}",
                    Icon = "✅",
                    EntityId = t.Id,
                    Date = t.DueDate,
                    Entity = t
                }));

                var notes = await TrackerDbManager.Instance.GetQuickNotesAsync();
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
            var members = await TrackerDbManager.Instance.GetTeamMembersAsync();

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
                    Description = m.JobTitle,
                    Icon = "👤",
                    EntityId = m.Id,
                    Entity = m
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchOneOnOnesAsync(string query)
        {
            var results = new List<SearchResult>();
            var meetings = await TrackerDbManager.Instance.GetOneOnOnesAsync();

            foreach (var m in meetings.Where(m =>
                m.TeamMemberName.ToLowerInvariant().Contains(query) ||
                m.Description.ToLowerInvariant().Contains(query) ||
                m.Notes.ToLowerInvariant().Contains(query) ||
                m.Agenda.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "1:1 Meeting",
                    Title = $"1:1 with {m.TeamMemberName}",
                    Description = m.Description,
                    Icon = "📅",
                    EntityId = m.Id,
                    Date = m.Date,
                    Entity = m
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchProjectsAsync(string query)
        {
            var results = new List<SearchResult>();
            var projects = await TrackerDbManager.Instance.GetProjectsAsync();

            foreach (var p in projects.Where(p =>
                p.Name.ToLowerInvariant().Contains(query) ||
                p.Description.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Project",
                    Title = p.Name,
                    Description = p.Description,
                    Icon = "📁",
                    EntityId = p.ID,
                    Date = p.StartDate,
                    Entity = p
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchTasksAsync(string query)
        {
            var results = new List<SearchResult>();
            var tasks = await TrackerDbManager.Instance.GetTasksAsync();

            foreach (var t in tasks.Where(t =>
                t.Description.ToLowerInvariant().Contains(query) ||
                t.OwnerName.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "Task",
                    Title = t.Description,
                    Description = $"Due: {t.DueDate:MMM dd} | {t.OwnerName}",
                    Icon = "✅",
                    EntityId = t.Id,
                    Date = t.DueDate,
                    Entity = t
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchOkrsAsync(string query)
        {
            var results = new List<SearchResult>();
            var okrs = await TrackerDbManager.Instance.GetOKRsAsync();

            foreach (var o in okrs.Where(o =>
                o.Title.ToLowerInvariant().Contains(query) ||
                o.Description.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "OKR",
                    Title = o.Title,
                    Description = o.Description,
                    Icon = "🎯",
                    EntityId = o.ObjectiveId,
                    Date = o.EndDate,
                    Entity = o
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchKpisAsync(string query)
        {
            var results = new List<SearchResult>();
            var kpis = await TrackerDbManager.Instance.GetKPIsAsync();

            foreach (var k in kpis.Where(k =>
                k.Name.ToLowerInvariant().Contains(query) ||
                k.Description.ToLowerInvariant().Contains(query)))
            {
                results.Add(new SearchResult
                {
                    Type = "KPI",
                    Title = k.Name,
                    Description = $"Value: {k.Value} / Target: {k.TargetValue}",
                    Icon = "📊",
                    EntityId = k.KpiId,
                    Entity = k
                });
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchNotesAsync(string query)
        {
            var results = new List<SearchResult>();
            var notes = await TrackerDbManager.Instance.GetQuickNotesAsync(includeArchived: true);

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

        private async Task<List<SearchResult>> SearchGoalsAsync(string query)
        {
            var results = new List<SearchResult>();
            var members = await TrackerDbManager.Instance.GetTeamMembersAsync();

            foreach (var member in members)
            {
                var goals = await TrackerDbManager.Instance.GetGoalsForTeamMemberAsync(member.Id);
                foreach (var g in goals.Where(g =>
                    g.Title.ToLowerInvariant().Contains(query) ||
                    g.Description.ToLowerInvariant().Contains(query)))
                {
                    results.Add(new SearchResult
                    {
                        Type = "Goal",
                        Title = g.Title,
                        Description = $"{member.FirstName}'s goal - {g.Category}",
                        Icon = "🏆",
                        EntityId = g.Id,
                        Date = g.TargetDate,
                        Entity = g
                    });
                }
            }

            return results;
        }

        private async Task<List<SearchResult>> SearchFeedbackAsync(string query)
        {
            var results = new List<SearchResult>();
            var members = await TrackerDbManager.Instance.GetTeamMembersAsync();

            foreach (var member in members)
            {
                var feedbacks = await TrackerDbManager.Instance.GetFeedbackForTeamMemberAsync(member.Id);
                foreach (var f in feedbacks.Where(f =>
                    f.Title.ToLowerInvariant().Contains(query) ||
                    f.Content.ToLowerInvariant().Contains(query)))
                {
                    results.Add(new SearchResult
                    {
                        Type = "Feedback",
                        Title = f.Title,
                        Description = $"For {member.FirstName} - {f.Type}",
                        Icon = "💬",
                        EntityId = f.Id,
                        Date = f.Date,
                        Entity = f
                    });
                }
            }

            return results;
        }

        #endregion
    }
}

