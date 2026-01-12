using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Services;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages application data and provides a single source of truth for all data collections.
    /// Uses ObservableCollection to enable automatic UI updates when data changes.
    /// ViewModels should bind directly to these collections rather than maintaining their own copies.
    /// </summary>
    public class TrackerDataManager
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(TrackerDataManager));
        private bool _initialized;

        // Observable collections - THE single source of truth
        private readonly ObservableCollection<TeamMember> _teamMembers = new();
        private readonly ObservableCollection<Meeting> _meetings = new();
        private readonly ObservableCollection<Project> _projects = new();
        private readonly ObservableCollection<TrackerTask> _tasks = new();
        private readonly ObservableCollection<Metric> _metrics = new();
        // Strategic goals (formerly "OKRs")
        private readonly ObservableCollection<Goal> _strategicGoals = new();
        private readonly ObservableCollection<Feedback> _feedbacks = new();
        private readonly ObservableCollection<DevelopmentGoal> _developmentGoals = new();
        private readonly ObservableCollection<QuickNote> _quickNotes = new();

        // Specialized data collections (isolated - no cross-dependencies with core data)
        private readonly ObservableCollection<PulseSurvey> _pulseSurveys = new();
        private readonly ObservableCollection<ReviewTemplate> _reviewTemplates = new();
        private readonly ObservableCollection<PerformanceReviewCycle> _reviewCycles = new();

        // Read-only wrappers for external access (prevents external modification)
        private readonly ReadOnlyObservableCollection<TeamMember> _teamMembersReadOnly;
        private readonly ReadOnlyObservableCollection<Meeting> _meetingsReadOnly;
        private readonly ReadOnlyObservableCollection<Project> _projectsReadOnly;
        private readonly ReadOnlyObservableCollection<TrackerTask> _tasksReadOnly;
        private readonly ReadOnlyObservableCollection<Metric> _metricsReadOnly;
        private readonly ReadOnlyObservableCollection<Goal> _strategicGoalsReadOnly;
        private readonly ReadOnlyObservableCollection<Feedback> _feedbacksReadOnly;
        private readonly ReadOnlyObservableCollection<DevelopmentGoal> _developmentGoalsReadOnly;
        private readonly ReadOnlyObservableCollection<QuickNote> _quickNotesReadOnly;

        // Read-only wrappers for specialized data
        private readonly ReadOnlyObservableCollection<PulseSurvey> _pulseSurveysReadOnly;
        private readonly ReadOnlyObservableCollection<ReviewTemplate> _reviewTemplatesReadOnly;
        private readonly ReadOnlyObservableCollection<PerformanceReviewCycle> _reviewCyclesReadOnly;

        // Track if initial load has been done for each collection
        private bool _teamMembersLoaded;
        private bool _meetingsLoaded;
        private bool _projectsLoaded;
        private bool _tasksLoaded;
        private bool _metricsLoaded;
        private bool _strategicGoalsLoaded;
        private bool _feedbacksLoaded;
        private bool _developmentGoalsLoaded;
        private bool _quickNotesLoaded;

        // Track load status for specialized data
        private bool _pulseSurveysLoaded;
        private bool _reviewTemplatesLoaded;
        private bool _reviewCyclesLoaded;

        // Lock objects for thread safety during collection updates
        private readonly object _teamMembersLock = new();
        private readonly object _meetingsLock = new();
        private readonly object _projectsLock = new();
        private readonly object _tasksLock = new();
        private readonly object _metricsLock = new();
        private readonly object _strategicGoalsLock = new();
        private readonly object _feedbacksLock = new();
        private readonly object _developmentGoalsLock = new();
        private readonly object _quickNotesLock = new();

        // Lock objects for specialized data
        private readonly object _pulseSurveysLock = new();
        private readonly object _reviewTemplatesLock = new();
        private readonly object _reviewCyclesLock = new();

        #endregion

        #region Singleton Instance

        private static readonly Lazy<TrackerDataManager> _lazyInstance =
            new(() => new TrackerDataManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of TrackerDataManager.
        /// </summary>
        public static TrackerDataManager Instance => _lazyInstance.Value;

        private TrackerDataManager()
        {
            // Initialize read-only wrappers
            _teamMembersReadOnly = new ReadOnlyObservableCollection<TeamMember>(_teamMembers);
            _meetingsReadOnly = new ReadOnlyObservableCollection<Meeting>(_meetings);
            _projectsReadOnly = new ReadOnlyObservableCollection<Project>(_projects);
            _tasksReadOnly = new ReadOnlyObservableCollection<TrackerTask>(_tasks);
            _metricsReadOnly = new ReadOnlyObservableCollection<Metric>(_metrics);
            _strategicGoalsReadOnly = new ReadOnlyObservableCollection<Goal>(_strategicGoals);
            _feedbacksReadOnly = new ReadOnlyObservableCollection<Feedback>(_feedbacks);
            _developmentGoalsReadOnly = new ReadOnlyObservableCollection<DevelopmentGoal>(_developmentGoals);
            _quickNotesReadOnly = new ReadOnlyObservableCollection<QuickNote>(_quickNotes);

            // Initialize read-only wrappers for specialized data
            _pulseSurveysReadOnly = new ReadOnlyObservableCollection<PulseSurvey>(_pulseSurveys);
            _reviewTemplatesReadOnly = new ReadOnlyObservableCollection<ReviewTemplate>(_reviewTemplates);
            _reviewCyclesReadOnly = new ReadOnlyObservableCollection<PerformanceReviewCycle>(_reviewCycles);
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public void Shutdown()
        {
            RunOnUiThread(() =>
            {
                _teamMembers.Clear();
                _meetings.Clear();
                _projects.Clear();
                _tasks.Clear();
                _metrics.Clear();
                _strategicGoals.Clear();
                _feedbacks.Clear();
                _developmentGoals.Clear();
                _quickNotes.Clear();

                // Clear specialized data
                _pulseSurveys.Clear();
                _reviewTemplates.Clear();
                _reviewCycles.Clear();
            });

            // Reset load flags
            _teamMembersLoaded = false;
            _meetingsLoaded = false;
            _projectsLoaded = false;
            _tasksLoaded = false;
            _metricsLoaded = false;
            _strategicGoalsLoaded = false;
            _feedbacksLoaded = false;
            _developmentGoalsLoaded = false;
            _quickNotesLoaded = false;

            // Reset specialized data flags
            _pulseSurveysLoaded = false;
            _reviewTemplatesLoaded = false;
            _reviewCyclesLoaded = false;
        }

        /// <summary>
        /// Invalidates all caches, forcing a fresh load from database on next access.
        /// Call this after login to ensure fresh data for the new user.
        /// </summary>
        public void InvalidateAllCaches()
        {
            _logger.Debug("Invalidating all caches");

            _teamMembersLoaded = false;
            _meetingsLoaded = false;
            _projectsLoaded = false;
            _tasksLoaded = false;
            _metricsLoaded = false;
            _strategicGoalsLoaded = false;
            _feedbacksLoaded = false;
            _developmentGoalsLoaded = false;
            _quickNotesLoaded = false;

            // Invalidate specialized data caches
            _pulseSurveysLoaded = false;
            _reviewTemplatesLoaded = false;
            _reviewCyclesLoaded = false;

            // Clear collections to free memory and ensure stale data isn't shown
            RunOnUiThread(() =>
            {
                _teamMembers.Clear();
                _meetings.Clear();
                _projects.Clear();
                _tasks.Clear();
                _metrics.Clear();
                _strategicGoals.Clear();
                _feedbacks.Clear();
                _developmentGoals.Clear();
                _quickNotes.Clear();

                // Clear specialized data
                _pulseSurveys.Clear();
                _reviewTemplates.Clear();
                _reviewCycles.Clear();
            });
        }

        #endregion

        #region Public Properties - Single Source of Truth

        /// <summary>
        /// Gets the read-only collection of team members. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<TeamMember> TeamMembers => _teamMembersReadOnly;

        /// <summary>
        /// Gets the read-only collection of meetings. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Meeting> Meetings => _meetingsReadOnly;

        /// <summary>
        /// Gets the read-only collection of one-on-one meetings (MeetingType.OneOnOne).
        /// This is currently an alias over the Meetings collection and is kept for
        /// backwards compatibility with existing ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Meeting> OneOnOneMeetings => _meetingsReadOnly;

        /// <summary>
        /// Gets the read-only collection of projects. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Project> Projects => _projectsReadOnly;

        /// <summary>
        /// Gets the read-only collection of tasks. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<TrackerTask> Tasks => _tasksReadOnly;

        /// <summary>
        /// Gets the read-only collection of strategic goals (formerly "OKRs").
        /// </summary>
        public ReadOnlyObservableCollection<Goal> StrategicGoals => _strategicGoalsReadOnly;

        /// <summary>
        /// Gets the read-only collection of metrics. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Metric> Metrics => _metricsReadOnly;

        /// <summary>
        /// Gets the read-only collection of feedbacks. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Feedback> Feedbacks => _feedbacksReadOnly;

        /// <summary>
        /// Gets the read-only collection of development goals. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<DevelopmentGoal> DevelopmentGoals => _developmentGoalsReadOnly;

        /// <summary>
        /// Gets the read-only collection of quick notes. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<QuickNote> QuickNotes => _quickNotesReadOnly;

        #endregion

        #region Public Properties - Specialized Data (Isolated)

        /// <summary>
        /// Gets the read-only collection of pulse surveys. Bind directly to this in ViewModels.
        /// This data is isolated - CRUD operations only refresh this collection.
        /// </summary>
        public ReadOnlyObservableCollection<PulseSurvey> PulseSurveys => _pulseSurveysReadOnly;

        /// <summary>
        /// Gets the read-only collection of review templates. Bind directly to this in ViewModels.
        /// This data is isolated - CRUD operations only refresh this collection.
        /// </summary>
        public ReadOnlyObservableCollection<ReviewTemplate> ReviewTemplates => _reviewTemplatesReadOnly;

        /// <summary>
        /// Gets the read-only collection of review cycles. Bind directly to this in ViewModels.
        /// This data is isolated - CRUD operations only refresh this collection.
        /// </summary>
        public ReadOnlyObservableCollection<PerformanceReviewCycle> ReviewCycles => _reviewCyclesReadOnly;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Runs an action on the UI thread. Required because ObservableCollection 
        /// must be modified on the same thread that created it (UI thread).
        /// </summary>
        private void RunOnUiThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                // No dispatcher available (unit tests or before app starts)
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Replaces all items in a collection on the UI thread.
        /// </summary>
        private void ReplaceCollectionItems<T>(ObservableCollection<T> collection, IEnumerable<T> newItems, object lockObj)
        {
            lock (lockObj)
            {
                RunOnUiThread(() =>
                {
                    collection.Clear();
                    foreach (var item in newItems)
                    {
                        collection.Add(item);
                    }
                });
            }
        }

        /// <summary>
        /// Refreshes all data from database and notifies all ViewModels.
        /// Call this after ANY CRUD operation to ensure all screens stay in sync.
        /// </summary>
        private async Task RefreshAllAndNotifyAsync()
        {
            _logger.Debug("Refreshing all data after mutation");
            await RefreshAllDataAsync();
            DataMessenger.SendRefreshAll();
        }

        #endregion

        #region Team Member Methods

        /// <summary>
        /// Ensures team members are loaded and returns the collection.
        /// The returned collection IS the single source of truth - bind to it directly.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<TeamMember>> GetTeamData()
        {
            if (!_teamMembersLoaded)
            {
                _logger.Debug("Loading team members from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetTeamData called but OrganizationContext.UserId is not set");
                    return _teamMembersReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var teamMemberRepository = new TeamMemberRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var members = await teamMemberRepository.GetTeamMembersAsync();
                ReplaceCollectionItems(_teamMembers, members ?? new List<TeamMember>(), _teamMembersLock);
                _teamMembersLoaded = true;
                _logger.Debug("Loaded {0} team members", _teamMembers.Count);
            }
            return _teamMembersReadOnly;
        }

        public async Task<bool> AddTeamMember(TeamMember teamMember)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddTeamMember called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var teamMemberRepository = new TeamMemberRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await teamMemberRepository.AddTeamMemberAsync(teamMember);
            if (id != Guid.Empty)
            {
                teamMember.Id = id;
                await RefreshAllAndNotifyAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateTeamMember(TeamMember teamMember)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateTeamMember called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var teamMemberRepository = new TeamMemberRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await teamMemberRepository.UpdateTeamMemberAsync(teamMember);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteTeamMember(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteTeamMember called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var teamMemberRepository = new TeamMemberRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await teamMemberRepository.DeleteTeamMemberAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region OneOnOne Methods

        /// <summary>
        /// Ensures meetings of type OneOnOne are loaded and returns the collection (as Meeting entities).
        /// </summary>
        public async Task<ReadOnlyObservableCollection<Meeting>> GetOneOnOneMeetings()
        {
            if (!_meetingsLoaded)
            {
                _logger.Debug("Loading meetings of type OneOnOne from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetOneOnOneMeetings called but OrganizationContext.UserId is not set");
                    return _meetingsReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var meetingRepository = new MeetingRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var oneOnOneMeetings = await meetingRepository.GetMeetingsByTypeAsync(MeetingType.OneOnOne);
                ReplaceCollectionItems(_meetings, oneOnOneMeetings, _meetingsLock);
                _meetingsLoaded = true;
                _logger.Debug("Loaded {0} meetings of type OneOnOne", _meetings.Count);
            }

            return _meetingsReadOnly;
        }

        /// <summary>
        /// Adds a new meeting of type OneOnOne using the unified Meeting model.
        /// Returns 1 on success and 0 on failure (legacy convention).
        /// </summary>
        public async Task<int> AddOneOnOneMeeting(Meeting meeting, Guid? teamMemberId = null)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddOneOnOneMeeting called but OrganizationContext.UserId is not set");
                return 0;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var meetingRepository = new MeetingRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            // Ensure correct meeting type
            meeting.Type = MeetingType.OneOnOne;

            var meetingId = await meetingRepository.AddMeetingAsync(meeting, teamMemberId);

            if (meetingId != Guid.Empty)
            {
                meeting.Id = meetingId;

                // Create meeting reminder if enabled
                await Services.ReminderService.Instance.CreateMeetingReminderAsync(meeting);

                await RefreshAllAndNotifyAsync();
                return 1;
            }

            _logger.Warn("AddOneOnOne failed to create meeting");
            return 0;
        }

        /// <summary>
        /// Updates an existing meeting of type OneOnOne using the unified Meeting model.
        /// </summary>
        public async Task<bool> UpdateOneOnOneMeeting(Meeting meeting)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateOneOnOneMeeting called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var meetingRepository = new MeetingRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await meetingRepository.UpdateMeetingAsync(meeting);
            if (success)
            {
                // Update meeting reminder if date/time changed
                await Services.ReminderService.Instance.CreateMeetingReminderAsync(meeting);

                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        /// <summary>
        /// Deletes a meeting of type OneOnOne by its Meeting.Id (Guid).
        /// </summary>
        public async Task<bool> DeleteOneOnOneMeeting(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteOneOnOneMeeting called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var meetingRepository = new MeetingRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await meetingRepository.DeleteMeetingAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        #endregion

        #region Project Methods

        /// <summary>
        /// Ensures projects are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<Project>> GetProjects()
        {
            if (!_projectsLoaded)
            {
                _logger.Debug("Loading projects from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetProjects called but OrganizationContext.UserId is not set");
                    return _projectsReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var projectRepository = new ProjectRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var projects = await projectRepository.GetProjectsAsync();
                ReplaceCollectionItems(_projects, projects ?? new List<Project>(), _projectsLock);
                _projectsLoaded = true;
                _logger.Debug("Loaded {0} projects", _projects.Count);
            }
            return _projectsReadOnly;
        }

        public async Task<Guid> AddProject(Project project)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddProject called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var projectRepository = new ProjectRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await projectRepository.AddProjectAsync(project);
            if (id != Guid.Empty)
            {
                project.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateProject(Project project)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateProject called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var projectRepository = new ProjectRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await projectRepository.UpdateProjectAsync(project);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteProject(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteProject called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var projectRepository = new ProjectRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await projectRepository.DeleteProjectAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region Task Methods

        /// <summary>
        /// Ensures tasks are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<TrackerTask>> GetTasks()
        {
            if (!_tasksLoaded)
            {
                _logger.Debug("Loading tasks from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetTasks called but OrganizationContext.UserId is not set");
                    return _tasksReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var taskRepository = new TrackerTaskRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var tasks = await taskRepository.GetTasksAsync();

                // Populate meeting counts for all tasks in a single batch query (prevents N+1 problem)
                if (tasks != null && tasks.Count > 0)
                {
                    var taskIds = tasks.Select(t => t.Id).ToList();
                    var meetingCounts = await taskRepository.GetTaskMeetingCountsAsync(taskIds);

                    foreach (var task in tasks)
                    {
                        if (meetingCounts.TryGetValue(task.Id, out var count))
                        {
                            task.MeetingCount = count;
                        }
                    }
                }

                ReplaceCollectionItems(_tasks, tasks ?? new List<TrackerTask>(), _tasksLock);
                _tasksLoaded = true;
                _logger.Debug("Loaded {0} tasks", _tasks.Count);
            }

            return _tasksReadOnly;
        }

        /// <summary>
        /// Adds a new task.
        /// </summary>
        public async Task<Guid> AddTask(TrackerTask task)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddTask called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var taskRepository = new TrackerTaskRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await taskRepository.AddTaskAsync(task);
            if (id != Guid.Empty)
            {
                task.Id = id;
                await RefreshAllAndNotifyAsync();
            }

            return id;
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        public async Task<bool> UpdateTask(TrackerTask task)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateTask called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var taskRepository = new TrackerTaskRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await taskRepository.UpdateTaskAsync(task);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        /// <summary>
        /// Deletes a task by ID.
        /// </summary>
        public async Task<bool> DeleteTask(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteTask called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var taskRepository = new TrackerTaskRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await taskRepository.DeleteTaskAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        #endregion

        #region Strategic Goal Methods

        /// <summary>
        /// Ensures strategic goals are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<Goal>> GetStrategicGoals()
        {
            if (!_strategicGoalsLoaded)
            {
                _logger.Debug("Loading strategic goals from database");
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetOKRs called but OrganizationContext.UserId is not set");
                    return _strategicGoalsReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var goalRepository = new GoalRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var goals = await goalRepository.GetGoalsAsync();

                ReplaceCollectionItems(_strategicGoals, goals ?? new List<Goal>(), _strategicGoalsLock);
                _strategicGoalsLoaded = true;
                _logger.Debug("Loaded {0} strategic goals", _strategicGoals.Count);
            }

            return _strategicGoalsReadOnly;
        }

        public async Task<Guid> AddStrategicGoal(Goal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddStrategicGoal called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new GoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await goalRepository.AddGoalAsync(goal);
            if (id != Guid.Empty)
            {
                goal.Id = id;
                await RefreshAllAndNotifyAsync();
            }

            return id;
        }

        public async Task<bool> UpdateStrategicGoal(Goal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateStrategicGoal called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new GoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await goalRepository.UpdateGoalAsync(goal);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteStrategicGoal(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
            _logger.Warn("DeleteStrategicGoal called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new GoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await goalRepository.DeleteGoalAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region Metric Methods

        /// <summary>
        /// Ensures metrics are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<Metric>> GetMetrics()
        {
            if (!_metricsLoaded)
            {
                _logger.Debug("Loading metrics from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetMetrics called but OrganizationContext.UserId is not set");
                    return _metricsReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var metricRepository = new MetricRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var metrics = await metricRepository.GetMetricsAsync();

                ReplaceCollectionItems(_metrics, metrics ?? new List<Metric>(), _metricsLock);
                _metricsLoaded = true;
                _logger.Debug("Loaded {0} metrics", _metrics.Count);
            }

            return _metricsReadOnly;
        }

        /// <summary>
        /// Adds a new metric.
        /// </summary>
        public async Task<Guid> AddMetric(Metric metric)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddMetric called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var metricRepository = new MetricRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await metricRepository.AddMetricAsync(metric);
            if (id != Guid.Empty)
            {
                metric.Id = id;
                await RefreshAllAndNotifyAsync();
            }

            return id;
        }

        /// <summary>
        /// Updates an existing metric.
        /// </summary>
        public async Task<bool> UpdateMetric(Metric metric)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateMetric called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var metricRepository = new MetricRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await metricRepository.UpdateMetricAsync(metric);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        /// <summary>
        /// Deletes a metric by its Id.
        /// </summary>
        public async Task<bool> DeleteMetric(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteMetric called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var metricRepository = new MetricRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await metricRepository.DeleteMetricAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }

            return success;
        }

        #endregion

        #region Feedback Methods

        /// <summary>
        /// Ensures feedbacks are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<Feedback>> GetFeedbacks()
        {
            if (!_feedbacksLoaded)
            {
                _logger.Debug("Loading feedbacks from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetFeedbacks called but OrganizationContext.UserId is not set");
                    return _feedbacksReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var feedbackRepository = new FeedbackRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var feedbacks = await feedbackRepository.GetAllFeedbackAsync();
                ReplaceCollectionItems(_feedbacks, feedbacks ?? new List<Feedback>(), _feedbacksLock);
                _feedbacksLoaded = true;
                _logger.Debug("Loaded {0} feedbacks", _feedbacks.Count);
            }
            return _feedbacksReadOnly;
        }

        public async Task<int> AddFeedback(Feedback feedback)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddFeedback called but OrganizationContext.UserId is not set");
                return 0;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var feedbackRepository = new FeedbackRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await feedbackRepository.AddFeedbackAsync(feedback);
            if (id != Guid.Empty)
            {
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateFeedback(Feedback feedback)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateFeedback called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var feedbackRepository = new FeedbackRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await feedbackRepository.UpdateFeedbackAsync(feedback);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task DeleteFeedbackAsync(Feedback feedback)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteFeedbackAsync called but OrganizationContext.UserId is not set");
                return;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            await using var context = contextFactory.CreateContext();

            var existing = await context.Feedbacks
                .Where(f => !f.IsDeleted && f.Id == feedback.Id)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                _logger.Warn("DeleteFeedbackAsync: Feedback with Id {0} not found", feedback.Id);
                return;
            }

            context.Feedbacks.Remove(existing);
            await context.SaveChangesAsync();
            await RefreshAllAndNotifyAsync();
        }

        #endregion

        #region Goal Methods

        /// <summary>
        /// Ensures goals are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<DevelopmentGoal>> GetGoals()
        {
            if (!_developmentGoalsLoaded)
            {
                _logger.Debug("Loading goals from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetGoals called but OrganizationContext.UserId is not set");
                    return _developmentGoalsReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var goalRepository = new DevelopmentGoalRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var goals = await goalRepository.GetAllDevelopmentGoalsAsync();
                ReplaceCollectionItems(_developmentGoals, goals ?? new List<DevelopmentGoal>(), _developmentGoalsLock);
                _developmentGoalsLoaded = true;
                _logger.Debug("Loaded {0} goals", _developmentGoals.Count);
            }
            return _developmentGoalsReadOnly;
        }

        public async Task<Guid> AddGoal(DevelopmentGoal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddGoal called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new DevelopmentGoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await goalRepository.AddDevelopmentGoalAsync(goal);
            if (id != Guid.Empty)
            {
                goal.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateGoal(DevelopmentGoal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateGoal called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new DevelopmentGoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await goalRepository.UpdateDevelopmentGoalAsync(goal);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task DeleteGoalAsync(DevelopmentGoal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteGoalAsync called but OrganizationContext.UserId is not set");
                return;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var goalRepository = new DevelopmentGoalRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await goalRepository.DeleteDevelopmentGoalAsync(goal.Id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
        }

        #endregion

        #region QuickNote Methods

        /// <summary>
        /// Ensures quick notes are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<QuickNote>> GetQuickNotes()
        {
            if (!_quickNotesLoaded)
            {
                _logger.Debug("Loading quick notes from database");

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetQuickNotes called but OrganizationContext.UserId is not set");
                    return _quickNotesReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var quickNoteRepository = new QuickNoteRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var notes = await quickNoteRepository.GetQuickNotesAsync();
                ReplaceCollectionItems(_quickNotes, notes ?? new List<QuickNote>(), _quickNotesLock);
                _quickNotesLoaded = true;
                _logger.Debug("Loaded {0} quick notes", _quickNotes.Count);
            }
            return _quickNotesReadOnly;
        }

        public async Task<int> AddQuickNote(QuickNote note)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddQuickNote called but OrganizationContext.UserId is not set");
                return 0;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var quickNoteRepository = new QuickNoteRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await quickNoteRepository.AddQuickNoteAsync(note);
            if (id > 0)
            {
                note.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        #endregion

        #region PulseSurvey Methods (Isolated Refresh)

        /// <summary>
        /// Ensures pulse surveys are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<PulseSurvey>> GetPulseSurveys()
        {
            if (!_pulseSurveysLoaded)
            {
                _logger.Debug("Loading pulse surveys from database");
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetPulseSurveys called but OrganizationContext.UserId is not set");
                    return _pulseSurveysReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var pulseSurveyRepository = new PulseSurveyRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var surveys = await pulseSurveyRepository.GetPulseSurveysAsync();
                ReplaceCollectionItems(_pulseSurveys, surveys ?? new List<PulseSurvey>(), _pulseSurveysLock);
                _pulseSurveysLoaded = true;
                _logger.Debug("Loaded {0} pulse surveys", _pulseSurveys.Count);
            }
            return _pulseSurveysReadOnly;
        }

        public async Task<int> AddPulseSurvey(PulseSurvey survey)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddPulseSurvey called but OrganizationContext.UserId is not set");
                return 0;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var pulseSurveyRepository = new PulseSurveyRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await pulseSurveyRepository.AddPulseSurveyAsync(survey);
            if (id > 0)
            {
                survey.Id = id;
                await RefreshPulseSurveysAsync();
            }
            return id;
        }

        public async Task<bool> UpdatePulseSurvey(PulseSurvey survey)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdatePulseSurvey called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var pulseSurveyRepository = new PulseSurveyRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await pulseSurveyRepository.UpdatePulseSurveyAsync(survey);
            if (success)
            {
                await RefreshPulseSurveysAsync();
            }
            return success;
        }

        public async Task<bool> DeletePulseSurvey(int id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeletePulseSurvey called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var pulseSurveyRepository = new PulseSurveyRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await pulseSurveyRepository.DeletePulseSurveyAsync(id);
            if (success)
            {
                await RefreshPulseSurveysAsync();
            }
            return success;
        }

        #endregion

        #region ReviewTemplate Methods (Isolated Refresh)

        /// <summary>
        /// Ensures review templates are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<ReviewTemplate>> GetReviewTemplates()
        {
            if (!_reviewTemplatesLoaded)
            {
                _logger.Debug("Loading review templates from database");
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetReviewTemplates called but OrganizationContext.UserId is not set");
                    return _reviewTemplatesReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var reviewTemplateRepository = new ReviewTemplateRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var templates = await reviewTemplateRepository.GetReviewTemplatesAsync();
                ReplaceCollectionItems(_reviewTemplates, templates ?? new List<ReviewTemplate>(), _reviewTemplatesLock);
                _reviewTemplatesLoaded = true;
                _logger.Debug("Loaded {0} review templates", _reviewTemplates.Count);
            }
            return _reviewTemplatesReadOnly;
        }

        public async Task<Guid> AddReviewTemplate(ReviewTemplate template)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddReviewTemplate called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewTemplateRepository = new ReviewTemplateRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await reviewTemplateRepository.AddReviewTemplateAsync(template);
            if (id != Guid.Empty)
            {
                await RefreshReviewTemplatesAsync();
            }
            return id;
        }

        public async Task<bool> UpdateReviewTemplate(ReviewTemplate template)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateReviewTemplate called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewTemplateRepository = new ReviewTemplateRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await reviewTemplateRepository.UpdateReviewTemplateAsync(template);
            if (success)
            {
                await RefreshReviewTemplatesAsync();
            }
            return success;
        }

        public async Task<bool> DeleteReviewTemplate(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteReviewTemplate called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewTemplateRepository = new ReviewTemplateRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await reviewTemplateRepository.DeleteReviewTemplateAsync(id);
            if (success)
            {
                await RefreshReviewTemplatesAsync();
            }
            return success;
        }

        #endregion

        #region ReviewCycle Methods (Isolated Refresh)

        /// <summary>
        /// Ensures review cycles are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<PerformanceReviewCycle>> GetReviewCycles()
        {
            if (!_reviewCyclesLoaded)
            {
                _logger.Debug("Loading review cycles from database");
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    _logger.Warn("GetReviewCycles called but OrganizationContext.UserId is not set");
                    return _reviewCyclesReadOnly;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                using var context = contextFactory.CreateContext();

                var reviewCycleRepository = new ReviewCycleRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var cycles = await reviewCycleRepository.GetReviewCyclesAsync();
                ReplaceCollectionItems(_reviewCycles, cycles ?? new List<PerformanceReviewCycle>(), _reviewCyclesLock);
                _reviewCyclesLoaded = true;
                _logger.Debug("Loaded {0} review cycles", _reviewCycles.Count);
            }
            return _reviewCyclesReadOnly;
        }

        public async Task<Guid> AddReviewCycle(PerformanceReviewCycle cycle)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("AddReviewCycle called but OrganizationContext.UserId is not set");
                return Guid.Empty;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewCycleRepository = new ReviewCycleRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var id = await reviewCycleRepository.AddReviewCycleAsync(cycle);
            if (id != Guid.Empty)
            {
                await RefreshReviewCyclesAsync();
            }
            return id;
        }

        public async Task<bool> UpdateReviewCycle(PerformanceReviewCycle cycle)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("UpdateReviewCycle called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewCycleRepository = new ReviewCycleRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await reviewCycleRepository.UpdateReviewCycleAsync(cycle);
            if (success)
            {
                await RefreshReviewCyclesAsync();
            }
            return success;
        }

        public async Task<bool> DeleteReviewCycle(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                _logger.Warn("DeleteReviewCycle called but OrganizationContext.UserId is not set");
                return false;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            var reviewCycleRepository = new ReviewCycleRepository(
                context,
                userId.Value,
                () => contextFactory.CreateContext());

            var success = await reviewCycleRepository.DeleteReviewCycleAsync(id);
            if (success)
            {
                await RefreshReviewCyclesAsync();
            }
            return success;
        }

        /// <summary>
        /// Creates performance reviews for all team members in a cycle.
        /// TODO: Implement properly via repository pattern
        /// </summary>
        public async Task<int> CreateReviewsForCycleAsync(Guid cycleId)
        {
            _logger.Warn("CreateReviewsForCycleAsync not yet implemented for repository pattern");
            await Task.CompletedTask;
            return 0;
        }

        /// <summary>
        /// Shares a review with the team member.
        /// TODO: Implement properly via repository pattern
        /// </summary>
        public async Task<bool> ShareReviewAsync(Guid reviewId)
        {
            _logger.Warn("ShareReviewAsync not yet implemented for repository pattern");
            await Task.CompletedTask;
            return false;
        }

        #endregion

        #region TaskCollection Methods

        public async Task<List<TaskCollection>> GetTaskCollections()
        {
            var organizationId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!organizationId.HasValue)
            {
                _logger.Warn("GetTaskCollections called but OrganizationContext.OrganizationId is not set");
                return new List<TaskCollection>();
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            using var context = contextFactory.CreateContext();

            return await context.TaskCollections
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.OrganizationId == organizationId.Value)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        #endregion

        #region Refresh Methods

        /// <summary>
        /// Forces a refresh of all data from the database.
        /// Use sparingly - prefer letting the cache work.
        /// </summary>
        public async Task RefreshAllDataAsync()
        {
            _logger.Info("Force refreshing all data from database");
            
            // Mark all as needing reload
            InvalidateAllCaches();

            // Load all data in parallel
            await Task.WhenAll(
                GetTeamData(),
                GetOneOnOneMeetings(),
                GetTasks(),
                GetStrategicGoals(),
                GetMetrics(),
                GetProjects(),
                GetFeedbacks(),
                GetGoals()
            );

            _logger.Info("All data refreshed: {0} team members, {1} tasks, {2} strategic goals",
                _teamMembers.Count, _tasks.Count, _strategicGoals.Count);
        }

        /// <summary>
        /// Refreshes a specific data type from the database.
        /// </summary>
        public async Task RefreshDataTypeAsync(DataChangeType type)
        {
            switch (type)
            {
                case DataChangeType.TeamMembers:
                    _teamMembersLoaded = false;
                    await GetTeamData();
                    break;
                case DataChangeType.OneOnOnes:
                    _meetingsLoaded = false;
                    await GetOneOnOneMeetings();
                    break;
                case DataChangeType.Tasks:
                    _tasksLoaded = false;
                    await GetTasks();
                    break;
                case DataChangeType.OKRs:
                    _strategicGoalsLoaded = false;
                    await GetStrategicGoals();
                    break;
                case DataChangeType.KPIs:
                    // Legacy change type name; now refreshes metrics
                    _metricsLoaded = false;
                    await GetMetrics();
                    break;
                case DataChangeType.Projects:
                    _projectsLoaded = false;
                    await GetProjects();
                    break;
                case DataChangeType.Feedback:
                    _feedbacksLoaded = false;
                    await GetFeedbacks();
                    break;
                case DataChangeType.Goals:
                    _developmentGoalsLoaded = false;
                    await GetGoals();
                    break;
                case DataChangeType.QuickNotes:
                    _quickNotesLoaded = false;
                    await GetQuickNotes();
                    break;
            }
        }

        /// <summary>
        /// Refreshes team members from the database.
        /// </summary>
        public async Task RefreshTeamMembersAsync()
        {
            _teamMembersLoaded = false;
            await GetTeamData();
        }

        /// <summary>
        /// Refreshes 1:1 meetings from the database.
        /// </summary>
        public async Task RefreshOneOnOnesAsync()
        {
            _meetingsLoaded = false;
            await GetOneOnOneMeetings();
        }

        /// <summary>
        /// Refreshes tasks from the database.
        /// </summary>
        public async Task RefreshTasksAsync()
        {
            _tasksLoaded = false;
            await GetTasks();
        }

        /// <summary>
        /// Refreshes strategic goals from the database.
        /// </summary>
        public async Task RefreshStrategicGoalsAsync()
        {
            _strategicGoalsLoaded = false;
            await GetStrategicGoals();
        }

        /// <summary>
        /// Refreshes metrics from the database (legacy name: KPIs).
        /// </summary>
        public async Task RefreshKPIsAsync()
        {
            _metricsLoaded = false;
            await GetMetrics();
        }

        /// <summary>
        /// Refreshes projects from the database.
        /// </summary>
        public async Task RefreshProjectsAsync()
        {
            _projectsLoaded = false;
            await GetProjects();
        }

        /// <summary>
        /// Refreshes feedbacks from the database.
        /// </summary>
        public async Task RefreshFeedbacksAsync()
        {
            _feedbacksLoaded = false;
            await GetFeedbacks();
        }

        /// <summary>
        /// Refreshes goals from the database.
        /// </summary>
        public async Task RefreshGoalsAsync()
        {
            _developmentGoalsLoaded = false;
            await GetGoals();
        }

        /// <summary>
        /// Refreshes quick notes from the database.
        /// </summary>
        public async Task RefreshQuickNotesAsync()
        {
            _quickNotesLoaded = false;
            await GetQuickNotes();
        }

        /// <summary>
        /// Refreshes pulse surveys from the database.
        /// This is an isolated refresh - does not trigger RefreshAll since no dependencies exist.
        /// </summary>
        public async Task RefreshPulseSurveysAsync()
        {
            _pulseSurveysLoaded = false;
            await GetPulseSurveys();
            _logger.Debug("Pulse surveys refreshed");
        }

        /// <summary>
        /// Refreshes review templates from the database.
        /// This is an isolated refresh - does not trigger RefreshAll since no dependencies exist.
        /// </summary>
        public async Task RefreshReviewTemplatesAsync()
        {
            _reviewTemplatesLoaded = false;
            await GetReviewTemplates();
            _logger.Debug("Review templates refreshed");
        }

        /// <summary>
        /// Refreshes review cycles from the database.
        /// This is an isolated refresh - does not trigger RefreshAll since no dependencies exist.
        /// </summary>
        public async Task RefreshReviewCyclesAsync()
        {
            _reviewCyclesLoaded = false;
            await GetReviewCycles();
            _logger.Debug("Review cycles refreshed");
        }

        #endregion
    }
}
