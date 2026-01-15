using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Services.Data;
using Tracker.Services.Data.Repositories;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages application data and provides a single source of truth for all data collections.
    /// Uses ObservableCollection to enable automatic UI updates when data changes.
    /// ViewModels should bind directly to these collections rather than maintaining their own copies.
    /// 
    /// Data access uses Dapper repositories against Supabase PostgreSQL.
    /// </summary>
    public class TrackerDataManager
    {
        #region Fields

        private readonly Logging.ILogger _logger = LoggingManager.GetComponentLogger(nameof(TrackerDataManager));
        private bool _initialized;

        // Dapper connection factory - singleton for the app lifetime
        private IDapperConnectionFactory? _connectionFactory;

        // Observable collections - THE single source of truth
        private readonly ObservableCollection<TeamMember> _teamMembers = new();
        private readonly ObservableCollection<Meeting> _meetings = new();
        private readonly ObservableCollection<Project> _projects = new();
        private readonly ObservableCollection<TrackerTask> _tasks = new();
        private readonly ObservableCollection<Metric> _metrics = new();
        private readonly ObservableCollection<Goal> _strategicGoals = new();
        private readonly ObservableCollection<Feedback> _feedbacks = new();
        private readonly ObservableCollection<DevelopmentGoal> _developmentGoals = new();
        private readonly ObservableCollection<QuickNote> _quickNotes = new();
        private readonly ObservableCollection<PulseSurvey> _pulseSurveys = new();

        // Read-only wrappers for external access
        private readonly ReadOnlyObservableCollection<TeamMember> _teamMembersReadOnly;
        private readonly ReadOnlyObservableCollection<Meeting> _meetingsReadOnly;
        private readonly ReadOnlyObservableCollection<Project> _projectsReadOnly;
        private readonly ReadOnlyObservableCollection<TrackerTask> _tasksReadOnly;
        private readonly ReadOnlyObservableCollection<Metric> _metricsReadOnly;
        private readonly ReadOnlyObservableCollection<Goal> _strategicGoalsReadOnly;
        private readonly ReadOnlyObservableCollection<Feedback> _feedbacksReadOnly;
        private readonly ReadOnlyObservableCollection<DevelopmentGoal> _developmentGoalsReadOnly;
        private readonly ReadOnlyObservableCollection<QuickNote> _quickNotesReadOnly;
        private readonly ReadOnlyObservableCollection<PulseSurvey> _pulseSurveysReadOnly;

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
        private bool _pulseSurveysLoaded;

        // Lock objects for thread safety
        private readonly object _teamMembersLock = new();
        private readonly object _meetingsLock = new();
        private readonly object _projectsLock = new();
        private readonly object _tasksLock = new();
        private readonly object _metricsLock = new();
        private readonly object _strategicGoalsLock = new();
        private readonly object _feedbacksLock = new();
        private readonly object _developmentGoalsLock = new();
        private readonly object _quickNotesLock = new();
        private readonly object _pulseSurveysLock = new();

        #endregion

        #region Singleton Instance

        private static readonly Lazy<TrackerDataManager> _lazyInstance =
            new(() => new TrackerDataManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static TrackerDataManager Instance => _lazyInstance.Value;

        private TrackerDataManager()
        {
            _teamMembersReadOnly = new ReadOnlyObservableCollection<TeamMember>(_teamMembers);
            _meetingsReadOnly = new ReadOnlyObservableCollection<Meeting>(_meetings);
            _projectsReadOnly = new ReadOnlyObservableCollection<Project>(_projects);
            _tasksReadOnly = new ReadOnlyObservableCollection<TrackerTask>(_tasks);
            _metricsReadOnly = new ReadOnlyObservableCollection<Metric>(_metrics);
            _strategicGoalsReadOnly = new ReadOnlyObservableCollection<Goal>(_strategicGoals);
            _feedbacksReadOnly = new ReadOnlyObservableCollection<Feedback>(_feedbacks);
            _developmentGoalsReadOnly = new ReadOnlyObservableCollection<DevelopmentGoal>(_developmentGoals);
            _quickNotesReadOnly = new ReadOnlyObservableCollection<QuickNote>(_quickNotes);
            _pulseSurveysReadOnly = new ReadOnlyObservableCollection<PulseSurvey>(_pulseSurveys);
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (_initialized) return;
            
            try
            {
                _connectionFactory = new DapperConnectionFactory();
                _initialized = true;
                _logger.Info("TrackerDataManager initialized with Dapper");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize TrackerDataManager: {0}", ex.Message);
            }
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
                _pulseSurveys.Clear();
            });

            ResetAllLoadFlags();
        }

        public void InvalidateAllCaches()
        {
            _logger.Debug("Invalidating all caches");
            ResetAllLoadFlags();

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
                _pulseSurveys.Clear();
            });
        }

        private void ResetAllLoadFlags()
        {
            _teamMembersLoaded = false;
            _meetingsLoaded = false;
            _projectsLoaded = false;
            _tasksLoaded = false;
            _metricsLoaded = false;
            _strategicGoalsLoaded = false;
            _feedbacksLoaded = false;
            _developmentGoalsLoaded = false;
            _quickNotesLoaded = false;
            _pulseSurveysLoaded = false;
        }

        #endregion

        #region Public Properties

        public ReadOnlyObservableCollection<TeamMember> TeamMembers => _teamMembersReadOnly;
        public ReadOnlyObservableCollection<Meeting> Meetings => _meetingsReadOnly;
        public ReadOnlyObservableCollection<Meeting> OneOnOneMeetings => _meetingsReadOnly;
        public ReadOnlyObservableCollection<Project> Projects => _projectsReadOnly;
        public ReadOnlyObservableCollection<TrackerTask> Tasks => _tasksReadOnly;
        public ReadOnlyObservableCollection<Goal> StrategicGoals => _strategicGoalsReadOnly;
        public ReadOnlyObservableCollection<Metric> Metrics => _metricsReadOnly;
        public ReadOnlyObservableCollection<Feedback> Feedbacks => _feedbacksReadOnly;
        public ReadOnlyObservableCollection<DevelopmentGoal> DevelopmentGoals => _developmentGoalsReadOnly;
        public ReadOnlyObservableCollection<QuickNote> QuickNotes => _quickNotesReadOnly;
        public ReadOnlyObservableCollection<PulseSurvey> PulseSurveys => _pulseSurveysReadOnly;

        #endregion

        #region Helper Methods

        private void RunOnUiThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
                action();
            else
                Application.Current.Dispatcher.Invoke(action);
        }

        private void ReplaceCollectionItems<T>(ObservableCollection<T> collection, IEnumerable<T> newItems, object lockObj)
        {
            lock (lockObj)
            {
                RunOnUiThread(() =>
                {
                    collection.Clear();
                    foreach (var item in newItems)
                        collection.Add(item);
                });
            }
        }

        private async Task RefreshAllAndNotifyAsync()
        {
            _logger.Debug("Refreshing all data after mutation");
            await RefreshAllDataAsync();
            DataMessenger.SendRefreshAll();
        }

        private IDapperConnectionFactory GetConnectionFactory()
        {
            if (_connectionFactory == null)
                Initialize();
            return _connectionFactory!;
        }

        private Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>()
        {
            return Microsoft.Extensions.Logging.LoggerFactory
                .Create(builder => builder.AddDebug())
                .CreateLogger<T>();
        }

        #endregion

        #region Team Member Methods

        public async Task<ReadOnlyObservableCollection<TeamMember>> GetTeamData()
        {
            if (!_teamMembersLoaded)
            {
                _logger.Debug("Loading team members from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetTeamData called but OrganizationContext.OrganizationId is not set");
                    return _teamMembersReadOnly;
                }

                var repo = new TeamMemberRepository(GetConnectionFactory(), CreateLogger<TeamMemberRepository>());
                var members = await repo.GetActiveByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_teamMembers, members, _teamMembersLock);
                _teamMembersLoaded = true;
                _logger.Debug("Loaded {0} team members", _teamMembers.Count);
            }
            return _teamMembersReadOnly;
        }

        public async Task<bool> AddTeamMember(TeamMember teamMember)
        {
            var repo = new TeamMemberRepository(GetConnectionFactory(), CreateLogger<TeamMemberRepository>());
            var created = await repo.CreateAsync(teamMember);
            if (created != null)
            {
                teamMember.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateTeamMember(TeamMember teamMember)
        {
            var repo = new TeamMemberRepository(GetConnectionFactory(), CreateLogger<TeamMemberRepository>());
            var success = await repo.UpdateAsync(teamMember);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteTeamMember(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new TeamMemberRepository(GetConnectionFactory(), CreateLogger<TeamMemberRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Meeting Methods

        public async Task<ReadOnlyObservableCollection<Meeting>> GetOneOnOneMeetings()
        {
            if (!_meetingsLoaded)
            {
                _logger.Debug("Loading meetings from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetOneOnOneMeetings called but OrganizationContext.OrganizationId is not set");
                    return _meetingsReadOnly;
                }

                var repo = new MeetingRepository(GetConnectionFactory(), CreateLogger<MeetingRepository>());
                var meetings = await repo.GetByOrganizationAsync(orgId.Value);
                
                // Filter to OneOnOne type
                var oneOnOnes = meetings.Where(m => m.MeetingTypeString == "one_on_one" || m.Type == MeetingType.OneOnOne);
                ReplaceCollectionItems(_meetings, oneOnOnes, _meetingsLock);
                _meetingsLoaded = true;
                _logger.Debug("Loaded {0} meetings", _meetings.Count);
            }
            return _meetingsReadOnly;
        }

        public async Task<int> AddOneOnOneMeeting(Meeting meeting, Guid? teamMemberId = null)
        {
            meeting.Type = MeetingType.OneOnOne;
            var repo = new MeetingRepository(GetConnectionFactory(), CreateLogger<MeetingRepository>());
            var created = await repo.CreateAsync(meeting);
            if (created != null)
            {
                meeting.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return 1;
            }
            return 0;
        }

        public async Task<bool> UpdateOneOnOneMeeting(Meeting meeting)
        {
            var repo = new MeetingRepository(GetConnectionFactory(), CreateLogger<MeetingRepository>());
            var success = await repo.UpdateAsync(meeting);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteOneOnOneMeeting(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new MeetingRepository(GetConnectionFactory(), CreateLogger<MeetingRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Project Methods

        public async Task<ReadOnlyObservableCollection<Project>> GetProjects()
        {
            if (!_projectsLoaded)
            {
                _logger.Debug("Loading projects from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetProjects called but OrganizationContext.OrganizationId is not set");
                    return _projectsReadOnly;
                }

                var repo = new ProjectRepository(GetConnectionFactory(), CreateLogger<ProjectRepository>());
                var projects = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_projects, projects, _projectsLock);
                _projectsLoaded = true;
                _logger.Debug("Loaded {0} projects", _projects.Count);
            }
            return _projectsReadOnly;
        }

        public async Task<Guid> AddProject(Project project)
        {
            var repo = new ProjectRepository(GetConnectionFactory(), CreateLogger<ProjectRepository>());
            var created = await repo.CreateAsync(project);
            if (created != null)
            {
                project.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return created.Id;
            }
            return Guid.Empty;
        }

        public async Task<bool> UpdateProject(Project project)
        {
            var repo = new ProjectRepository(GetConnectionFactory(), CreateLogger<ProjectRepository>());
            var success = await repo.UpdateAsync(project);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteProject(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new ProjectRepository(GetConnectionFactory(), CreateLogger<ProjectRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Task Methods

        public async Task<ReadOnlyObservableCollection<TrackerTask>> GetTasks()
        {
            if (!_tasksLoaded)
            {
                _logger.Debug("Loading tasks from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetTasks called but OrganizationContext.OrganizationId is not set");
                    return _tasksReadOnly;
                }

                var repo = new TaskRepository(GetConnectionFactory(), CreateLogger<TaskRepository>());
                var tasks = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_tasks, tasks, _tasksLock);
                _tasksLoaded = true;
                _logger.Debug("Loaded {0} tasks", _tasks.Count);
            }
            return _tasksReadOnly;
        }

        public async Task<Guid> AddTask(TrackerTask task)
        {
            var repo = new TaskRepository(GetConnectionFactory(), CreateLogger<TaskRepository>());
            var created = await repo.CreateAsync(task);
            if (created != null)
            {
                task.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return created.Id;
            }
            return Guid.Empty;
        }

        public async Task<bool> UpdateTask(TrackerTask task)
        {
            var repo = new TaskRepository(GetConnectionFactory(), CreateLogger<TaskRepository>());
            var success = await repo.UpdateAsync(task);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteTask(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new TaskRepository(GetConnectionFactory(), CreateLogger<TaskRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Strategic Goal Methods

        public async Task<ReadOnlyObservableCollection<Goal>> GetStrategicGoals()
        {
            if (!_strategicGoalsLoaded)
            {
                _logger.Debug("Loading strategic goals from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetStrategicGoals called but OrganizationContext.OrganizationId is not set");
                    return _strategicGoalsReadOnly;
                }

                var repo = new GoalRepository(GetConnectionFactory(), CreateLogger<GoalRepository>());
                var goals = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_strategicGoals, goals, _strategicGoalsLock);
                _strategicGoalsLoaded = true;
                _logger.Debug("Loaded {0} strategic goals", _strategicGoals.Count);
            }
            return _strategicGoalsReadOnly;
        }

        public async Task<Guid> AddStrategicGoal(Goal goal)
        {
            var repo = new GoalRepository(GetConnectionFactory(), CreateLogger<GoalRepository>());
            var created = await repo.CreateAsync(goal);
            if (created != null)
            {
                goal.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return created.Id;
            }
            return Guid.Empty;
        }

        public async Task<bool> UpdateStrategicGoal(Goal goal)
        {
            var repo = new GoalRepository(GetConnectionFactory(), CreateLogger<GoalRepository>());
            var success = await repo.UpdateAsync(goal);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteStrategicGoal(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new GoalRepository(GetConnectionFactory(), CreateLogger<GoalRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Metric Methods

        public async Task<ReadOnlyObservableCollection<Metric>> GetMetrics()
        {
            if (!_metricsLoaded)
            {
                _logger.Debug("Loading metrics from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetMetrics called but OrganizationContext.OrganizationId is not set");
                    return _metricsReadOnly;
                }

                var repo = new MetricRepository(GetConnectionFactory(), CreateLogger<MetricRepository>());
                var metrics = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_metrics, metrics, _metricsLock);
                _metricsLoaded = true;
                _logger.Debug("Loaded {0} metrics", _metrics.Count);
            }
            return _metricsReadOnly;
        }

        public async Task<Guid> AddMetric(Metric metric)
        {
            var repo = new MetricRepository(GetConnectionFactory(), CreateLogger<MetricRepository>());
            var created = await repo.CreateAsync(metric);
            if (created != null)
            {
                metric.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return created.Id;
            }
            return Guid.Empty;
        }

        public async Task<bool> UpdateMetric(Metric metric)
        {
            var repo = new MetricRepository(GetConnectionFactory(), CreateLogger<MetricRepository>());
            var success = await repo.UpdateAsync(metric);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task<bool> DeleteMetric(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new MetricRepository(GetConnectionFactory(), CreateLogger<MetricRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        #endregion

        #region Feedback Methods

        public async Task<ReadOnlyObservableCollection<Feedback>> GetFeedbacks()
        {
            if (!_feedbacksLoaded)
            {
                _logger.Debug("Loading feedbacks from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetFeedbacks called but OrganizationContext.OrganizationId is not set");
                    return _feedbacksReadOnly;
                }

                var repo = new FeedbackRepository(GetConnectionFactory(), CreateLogger<FeedbackRepository>());
                var feedbacks = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_feedbacks, feedbacks, _feedbacksLock);
                _feedbacksLoaded = true;
                _logger.Debug("Loaded {0} feedbacks", _feedbacks.Count);
            }
            return _feedbacksReadOnly;
        }

        public async Task<int> AddFeedback(Feedback feedback)
        {
            var repo = new FeedbackRepository(GetConnectionFactory(), CreateLogger<FeedbackRepository>());
            var created = await repo.CreateAsync(feedback);
            if (created != null)
            {
                feedback.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return 1;
            }
            return 0;
        }

        public async Task<bool> UpdateFeedback(Feedback feedback)
        {
            var repo = new FeedbackRepository(GetConnectionFactory(), CreateLogger<FeedbackRepository>());
            var success = await repo.UpdateAsync(feedback);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task DeleteFeedbackAsync(Feedback feedback)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new FeedbackRepository(GetConnectionFactory(), CreateLogger<FeedbackRepository>());
            await repo.DeleteAsync(feedback.Id, userId);
            await RefreshAllAndNotifyAsync();
        }

        #endregion

        #region Development Goal Methods

        public async Task<ReadOnlyObservableCollection<DevelopmentGoal>> GetGoals()
        {
            if (!_developmentGoalsLoaded)
            {
                _logger.Debug("Loading development goals from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetGoals called but OrganizationContext.OrganizationId is not set");
                    return _developmentGoalsReadOnly;
                }

                var repo = new DevelopmentGoalRepository(GetConnectionFactory(), CreateLogger<DevelopmentGoalRepository>());
                var goals = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_developmentGoals, goals, _developmentGoalsLock);
                _developmentGoalsLoaded = true;
                _logger.Debug("Loaded {0} development goals", _developmentGoals.Count);
            }
            return _developmentGoalsReadOnly;
        }

        public async Task<Guid> AddGoal(DevelopmentGoal goal)
        {
            var repo = new DevelopmentGoalRepository(GetConnectionFactory(), CreateLogger<DevelopmentGoalRepository>());
            var created = await repo.CreateAsync(goal);
            if (created != null)
            {
                goal.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return created.Id;
            }
            return Guid.Empty;
        }

        public async Task<bool> UpdateGoal(DevelopmentGoal goal)
        {
            var repo = new DevelopmentGoalRepository(GetConnectionFactory(), CreateLogger<DevelopmentGoalRepository>());
            var success = await repo.UpdateAsync(goal);
            if (success)
                await RefreshAllAndNotifyAsync();
            return success;
        }

        public async Task DeleteGoalAsync(DevelopmentGoal goal)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new DevelopmentGoalRepository(GetConnectionFactory(), CreateLogger<DevelopmentGoalRepository>());
            await repo.DeleteAsync(goal.Id, userId);
            await RefreshAllAndNotifyAsync();
        }

        #endregion

        #region QuickNote Methods

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

                var repo = new QuickNoteRepository(GetConnectionFactory(), CreateLogger<QuickNoteRepository>());
                var notes = await repo.GetByUserAsync(userId.Value);
                
                ReplaceCollectionItems(_quickNotes, notes, _quickNotesLock);
                _quickNotesLoaded = true;
                _logger.Debug("Loaded {0} quick notes", _quickNotes.Count);
            }
            return _quickNotesReadOnly;
        }

        public async Task<int> AddQuickNote(QuickNote note)
        {
            var repo = new QuickNoteRepository(GetConnectionFactory(), CreateLogger<QuickNoteRepository>());
            var created = await repo.CreateAsync(note);
            if (created != null)
            {
                note.Id = created.Id;
                await RefreshAllAndNotifyAsync();
                return 1;
            }
            return 0;
        }

        #endregion

        #region PulseSurvey Methods

        public async Task<ReadOnlyObservableCollection<PulseSurvey>> GetPulseSurveys()
        {
            if (!_pulseSurveysLoaded)
            {
                _logger.Debug("Loading pulse surveys from database");

                var orgId = OrganizationContext.Current.OrganizationIdOrNull;
                if (!orgId.HasValue)
                {
                    _logger.Warn("GetPulseSurveys called but OrganizationContext.OrganizationId is not set");
                    return _pulseSurveysReadOnly;
                }

                var repo = new PulseSurveyRepository(GetConnectionFactory(), CreateLogger<PulseSurveyRepository>());
                var surveys = await repo.GetByOrganizationAsync(orgId.Value);
                
                ReplaceCollectionItems(_pulseSurveys, surveys, _pulseSurveysLock);
                _pulseSurveysLoaded = true;
                _logger.Debug("Loaded {0} pulse surveys", _pulseSurveys.Count);
            }
            return _pulseSurveysReadOnly;
        }

        public async Task<int> AddPulseSurvey(PulseSurvey survey)
        {
            var repo = new PulseSurveyRepository(GetConnectionFactory(), CreateLogger<PulseSurveyRepository>());
            var created = await repo.CreateAsync(survey);
            if (created != null)
            {
                survey.Id = created.Id;
                await RefreshPulseSurveysAsync();
                return 1;
            }
            return 0;
        }

        public async Task<bool> UpdatePulseSurvey(PulseSurvey survey)
        {
            var repo = new PulseSurveyRepository(GetConnectionFactory(), CreateLogger<PulseSurveyRepository>());
            var success = await repo.UpdateAsync(survey);
            if (success)
                await RefreshPulseSurveysAsync();
            return success;
        }

        public async Task<bool> DeletePulseSurvey(Guid id)
        {
            var userId = OrganizationContext.Current.UserIdOrNull ?? Guid.Empty;
            var repo = new PulseSurveyRepository(GetConnectionFactory(), CreateLogger<PulseSurveyRepository>());
            var success = await repo.DeleteAsync(id, userId);
            if (success)
                await RefreshPulseSurveysAsync();
            return success;
        }

        #endregion

        #region TaskCollection Methods

        public async Task<List<TaskCollection>> GetTaskCollections()
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
            {
                _logger.Warn("GetTaskCollections called but OrganizationContext.OrganizationId is not set");
                return new List<TaskCollection>();
            }

            // TODO: Create TaskCollectionRepository if needed
            // For now, return empty list
            return new List<TaskCollection>();
        }

        #endregion

        #region Refresh Methods

        public async Task RefreshAllDataAsync()
        {
            _logger.Info("Force refreshing all data from database");
            InvalidateAllCaches();

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

        public async Task RefreshTeamMembersAsync() { _teamMembersLoaded = false; await GetTeamData(); }
        public async Task RefreshOneOnOnesAsync() { _meetingsLoaded = false; await GetOneOnOneMeetings(); }
        public async Task RefreshTasksAsync() { _tasksLoaded = false; await GetTasks(); }
        public async Task RefreshStrategicGoalsAsync() { _strategicGoalsLoaded = false; await GetStrategicGoals(); }
        public async Task RefreshKPIsAsync() { _metricsLoaded = false; await GetMetrics(); }
        public async Task RefreshProjectsAsync() { _projectsLoaded = false; await GetProjects(); }
        public async Task RefreshFeedbacksAsync() { _feedbacksLoaded = false; await GetFeedbacks(); }
        public async Task RefreshGoalsAsync() { _developmentGoalsLoaded = false; await GetGoals(); }
        public async Task RefreshQuickNotesAsync() { _quickNotesLoaded = false; await GetQuickNotes(); }
        public async Task RefreshPulseSurveysAsync() { _pulseSurveysLoaded = false; await GetPulseSurveys(); }

        #endregion
    }
}
