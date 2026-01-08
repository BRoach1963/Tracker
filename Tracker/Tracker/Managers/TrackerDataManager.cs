using System.Collections.ObjectModel;
using System.Windows;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;

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
        private readonly ObservableCollection<OneOnOne> _oneOnOnes = new();
        private readonly ObservableCollection<Project> _projects = new();
        private readonly ObservableCollection<IndividualTask> _tasks = new();
        private readonly ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private readonly ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private readonly ObservableCollection<Feedback> _feedbacks = new();
        private readonly ObservableCollection<IndividualGoal> _goals = new();
        private readonly ObservableCollection<QuickNote> _quickNotes = new();

        // Specialized data collections (isolated - no cross-dependencies with core data)
        private readonly ObservableCollection<PulseSurvey> _pulseSurveys = new();
        private readonly ObservableCollection<ReviewTemplate> _reviewTemplates = new();
        private readonly ObservableCollection<PerformanceReviewCycle> _reviewCycles = new();

        // Read-only wrappers for external access (prevents external modification)
        private readonly ReadOnlyObservableCollection<TeamMember> _teamMembersReadOnly;
        private readonly ReadOnlyObservableCollection<OneOnOne> _oneOnOnesReadOnly;
        private readonly ReadOnlyObservableCollection<Project> _projectsReadOnly;
        private readonly ReadOnlyObservableCollection<IndividualTask> _tasksReadOnly;
        private readonly ReadOnlyObservableCollection<ObjectiveKeyResult> _okrsReadOnly;
        private readonly ReadOnlyObservableCollection<KeyPerformanceIndicator> _kpisReadOnly;
        private readonly ReadOnlyObservableCollection<Feedback> _feedbacksReadOnly;
        private readonly ReadOnlyObservableCollection<IndividualGoal> _goalsReadOnly;
        private readonly ReadOnlyObservableCollection<QuickNote> _quickNotesReadOnly;

        // Read-only wrappers for specialized data
        private readonly ReadOnlyObservableCollection<PulseSurvey> _pulseSurveysReadOnly;
        private readonly ReadOnlyObservableCollection<ReviewTemplate> _reviewTemplatesReadOnly;
        private readonly ReadOnlyObservableCollection<PerformanceReviewCycle> _reviewCyclesReadOnly;

        // Track if initial load has been done for each collection
        private bool _teamMembersLoaded;
        private bool _oneOnOnesLoaded;
        private bool _projectsLoaded;
        private bool _tasksLoaded;
        private bool _okrsLoaded;
        private bool _kpisLoaded;
        private bool _feedbacksLoaded;
        private bool _goalsLoaded;
        private bool _quickNotesLoaded;

        // Track load status for specialized data
        private bool _pulseSurveysLoaded;
        private bool _reviewTemplatesLoaded;
        private bool _reviewCyclesLoaded;

        // Lock objects for thread safety during collection updates
        private readonly object _teamMembersLock = new();
        private readonly object _oneOnOnesLock = new();
        private readonly object _projectsLock = new();
        private readonly object _tasksLock = new();
        private readonly object _okrsLock = new();
        private readonly object _kpisLock = new();
        private readonly object _feedbacksLock = new();
        private readonly object _goalsLock = new();
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
            _oneOnOnesReadOnly = new ReadOnlyObservableCollection<OneOnOne>(_oneOnOnes);
            _projectsReadOnly = new ReadOnlyObservableCollection<Project>(_projects);
            _tasksReadOnly = new ReadOnlyObservableCollection<IndividualTask>(_tasks);
            _okrsReadOnly = new ReadOnlyObservableCollection<ObjectiveKeyResult>(_okrs);
            _kpisReadOnly = new ReadOnlyObservableCollection<KeyPerformanceIndicator>(_kpis);
            _feedbacksReadOnly = new ReadOnlyObservableCollection<Feedback>(_feedbacks);
            _goalsReadOnly = new ReadOnlyObservableCollection<IndividualGoal>(_goals);
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
                _oneOnOnes.Clear();
                _projects.Clear();
                _tasks.Clear();
                _okrs.Clear();
                _kpis.Clear();
                _feedbacks.Clear();
                _goals.Clear();
                _quickNotes.Clear();

                // Clear specialized data
                _pulseSurveys.Clear();
                _reviewTemplates.Clear();
                _reviewCycles.Clear();
            });

            // Reset load flags
            _teamMembersLoaded = false;
            _oneOnOnesLoaded = false;
            _projectsLoaded = false;
            _tasksLoaded = false;
            _okrsLoaded = false;
            _kpisLoaded = false;
            _feedbacksLoaded = false;
            _goalsLoaded = false;
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
            _oneOnOnesLoaded = false;
            _projectsLoaded = false;
            _tasksLoaded = false;
            _okrsLoaded = false;
            _kpisLoaded = false;
            _feedbacksLoaded = false;
            _goalsLoaded = false;
            _quickNotesLoaded = false;

            // Invalidate specialized data caches
            _pulseSurveysLoaded = false;
            _reviewTemplatesLoaded = false;
            _reviewCyclesLoaded = false;

            // Clear collections to free memory and ensure stale data isn't shown
            RunOnUiThread(() =>
            {
                _teamMembers.Clear();
                _oneOnOnes.Clear();
                _projects.Clear();
                _tasks.Clear();
                _okrs.Clear();
                _kpis.Clear();
                _feedbacks.Clear();
                _goals.Clear();
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
        /// Gets the read-only collection of one-on-ones. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<OneOnOne> OneOnOnes => _oneOnOnesReadOnly;

        /// <summary>
        /// Gets the read-only collection of projects. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Project> Projects => _projectsReadOnly;

        /// <summary>
        /// Gets the read-only collection of tasks. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<IndividualTask> Tasks => _tasksReadOnly;

        /// <summary>
        /// Gets the read-only collection of OKRs. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<ObjectiveKeyResult> OKRs => _okrsReadOnly;

        /// <summary>
        /// Gets the read-only collection of KPIs. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<KeyPerformanceIndicator> KPIs => _kpisReadOnly;

        /// <summary>
        /// Gets the read-only collection of feedbacks. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<Feedback> Feedbacks => _feedbacksReadOnly;

        /// <summary>
        /// Gets the read-only collection of goals. Bind directly to this in ViewModels.
        /// </summary>
        public ReadOnlyObservableCollection<IndividualGoal> Goals => _goalsReadOnly;

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
                var members = await TrackerDbManager.Instance!.GetTeamMembersAsync();
                ReplaceCollectionItems(_teamMembers, members, _teamMembersLock);
                _teamMembersLoaded = true;
                _logger.Debug("Loaded {0} team members", _teamMembers.Count);
            }
            return _teamMembersReadOnly;
        }

        public async Task<bool> AddTeamMember(TeamMember teamMember)
        {
            var id = await TrackerDbManager.Instance!.AddTeamMemberAsync(teamMember);
            if (id > 0)
            {
                teamMember.Id = id;
                await RefreshAllAndNotifyAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateTeamMember(TeamMember teamMember)
        {
            var success = await TrackerDbManager.Instance!.UpdateTeamMemberAsync(teamMember);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteTeamMember(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteTeamMemberAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region OneOnOne Methods

        /// <summary>
        /// Ensures one-on-ones are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<OneOnOne>> GetOneOnOnes()
        {
            if (!_oneOnOnesLoaded)
            {
                _logger.Debug("Loading one-on-ones from database");
                var oneOnOnes = await TrackerDbManager.Instance!.GetOneOnOnesAsync();
                ReplaceCollectionItems(_oneOnOnes, oneOnOnes, _oneOnOnesLock);
                _oneOnOnesLoaded = true;
                _logger.Debug("Loaded {0} one-on-ones", _oneOnOnes.Count);
            }
            return _oneOnOnesReadOnly;
        }

        public async Task<int> AddOneOnOne(OneOnOne oneOnOne, int? teamMemberId = null)
        {
            var id = await TrackerDbManager.Instance!.AddOneOnOneAsync(oneOnOne, teamMemberId);
            if (id > 0)
            {
                oneOnOne.Id = id;

                // Create meeting reminder if enabled
                await Services.ReminderService.Instance.CreateMeetingReminderAsync(oneOnOne);

                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateOneOnOne(OneOnOne oneOnOne)
        {
            var success = await TrackerDbManager.Instance!.UpdateOneOnOneAsync(oneOnOne);
            if (success)
            {
                // Update meeting reminder if date/time changed
                await Services.ReminderService.Instance.CreateMeetingReminderAsync(oneOnOne);

                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteOneOnOne(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteOneOnOneAsync(id);
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
                var projects = await TrackerDbManager.Instance!.GetProjectsAsync();
                ReplaceCollectionItems(_projects, projects, _projectsLock);
                _projectsLoaded = true;
                _logger.Debug("Loaded {0} projects", _projects.Count);
            }
            return _projectsReadOnly;
        }

        public async Task<int> AddProject(Project project)
        {
            var id = await TrackerDbManager.Instance!.AddProjectAsync(project);
            if (id > 0)
            {
                project.ID = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateProject(Project project)
        {
            var success = await TrackerDbManager.Instance!.UpdateProjectAsync(project);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteProject(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteProjectAsync(id);
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
        public async Task<ReadOnlyObservableCollection<IndividualTask>> GetTasks()
        {
            if (!_tasksLoaded)
            {
                _logger.Debug("Loading tasks from database");
                var tasks = await TrackerDbManager.Instance!.GetTasksAsync();

                // Populate meeting counts for all tasks in a single batch query (prevents N+1 problem)
                if (tasks != null && tasks.Count > 0)
                {
                    var taskIds = tasks.Select(t => t.Id).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetTaskMeetingCountsAsync(taskIds);

                    foreach (var task in tasks)
                    {
                        task.MeetingCount = meetingCounts.TryGetValue(task.Id, out var count) ? count : 0;
                    }
                }

                ReplaceCollectionItems(_tasks, tasks ?? new List<IndividualTask>(), _tasksLock);
                _tasksLoaded = true;
                _logger.Debug("Loaded {0} tasks", _tasks.Count);
            }

            return _tasksReadOnly;
        }

        public async Task<int> AddTask(IndividualTask task)
        {
            var id = await TrackerDbManager.Instance!.AddTaskAsync(task);
            if (id > 0)
            {
                task.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateTask(IndividualTask task)
        {
            var success = await TrackerDbManager.Instance!.UpdateTaskAsync(task);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteTask(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteTaskAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region OKR Methods

        /// <summary>
        /// Ensures OKRs are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<ObjectiveKeyResult>> GetOKRs()
        {
            if (!_okrsLoaded)
            {
                _logger.Debug("Loading OKRs from database");
                var okrs = await TrackerDbManager.Instance!.GetOKRsAsync();

                // Populate meeting counts for all OKRs in a single batch query (prevents N+1 problem)
                if (okrs != null && okrs.Count > 0)
                {
                    var okrIds = okrs.Select(o => o.ObjectiveId).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetOkrMeetingCountsAsync(okrIds);

                    foreach (var okr in okrs)
                    {
                        okr.MeetingCount = meetingCounts.TryGetValue(okr.ObjectiveId, out var count) ? count : 0;
                    }
                }

                ReplaceCollectionItems(_okrs, okrs ?? new List<ObjectiveKeyResult>(), _okrsLock);
                _okrsLoaded = true;
                _logger.Debug("Loaded {0} OKRs", _okrs.Count);
            }

            return _okrsReadOnly;
        }

        public async Task<int> AddOKR(ObjectiveKeyResult okr)
        {
            var id = await TrackerDbManager.Instance!.AddOKRAsync(okr);
            if (id > 0)
            {
                okr.ObjectiveId = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateOKR(ObjectiveKeyResult okr)
        {
            var success = await TrackerDbManager.Instance!.UpdateOKRAsync(okr);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteOKR(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteOKRAsync(id);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        #endregion

        #region KPI Methods

        /// <summary>
        /// Ensures KPIs are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<KeyPerformanceIndicator>> GetKPIs()
        {
            if (!_kpisLoaded)
            {
                _logger.Debug("Loading KPIs from database");
                var kpis = await TrackerDbManager.Instance!.GetKPIsAsync();

                // Populate meeting counts for all KPIs in a single batch query (prevents N+1 problem)
                if (kpis != null && kpis.Count > 0)
                {
                    var kpiIds = kpis.Select(k => k.KpiId).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetKpiMeetingCountsAsync(kpiIds);

                    foreach (var kpi in kpis)
                    {
                        kpi.MeetingCount = meetingCounts.TryGetValue(kpi.KpiId, out var count) ? count : 0;
                    }
                }

                ReplaceCollectionItems(_kpis, kpis ?? new List<KeyPerformanceIndicator>(), _kpisLock);
                _kpisLoaded = true;
                _logger.Debug("Loaded {0} KPIs", _kpis.Count);
            }

            return _kpisReadOnly;
        }

        public async Task<int> AddKPI(KeyPerformanceIndicator kpi)
        {
            var id = await TrackerDbManager.Instance!.AddKPIAsync(kpi);
            if (id > 0)
            {
                kpi.KpiId = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task<bool> UpdateKPI(KeyPerformanceIndicator kpi)
        {
            var success = await TrackerDbManager.Instance!.UpdateKPIAsync(kpi);
            if (success)
            {
                await RefreshAllAndNotifyAsync();
            }
            return success;
        }

        public async Task<bool> DeleteKPI(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteKPIAsync(id);
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
                var feedbacks = await TrackerDbManager.Instance!.GetAllFeedbackAsync();
                ReplaceCollectionItems(_feedbacks, feedbacks, _feedbacksLock);
                _feedbacksLoaded = true;
                _logger.Debug("Loaded {0} feedbacks", _feedbacks.Count);
            }
            return _feedbacksReadOnly;
        }

        public async Task<int> AddFeedback(Feedback feedback)
        {
            var id = await TrackerDbManager.Instance!.AddFeedbackAsync(feedback);
            if (id > 0)
            {
                feedback.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task DeleteFeedbackAsync(Feedback feedback)
        {
            await TrackerDbManager.Instance!.DeleteFeedbackAsync(feedback.Id);
            await RefreshAllAndNotifyAsync();
        }

        #endregion

        #region Goal Methods

        /// <summary>
        /// Ensures goals are loaded and returns the collection.
        /// </summary>
        public async Task<ReadOnlyObservableCollection<IndividualGoal>> GetGoals()
        {
            if (!_goalsLoaded)
            {
                _logger.Debug("Loading goals from database");
                var goals = await TrackerDbManager.Instance!.GetAllGoalsAsync();
                ReplaceCollectionItems(_goals, goals, _goalsLock);
                _goalsLoaded = true;
                _logger.Debug("Loaded {0} goals", _goals.Count);
            }
            return _goalsReadOnly;
        }

        public async Task<int> AddGoal(IndividualGoal goal)
        {
            var id = await TrackerDbManager.Instance!.AddGoalAsync(goal);
            if (id > 0)
            {
                goal.Id = id;
                await RefreshAllAndNotifyAsync();
            }
            return id;
        }

        public async Task DeleteGoalAsync(IndividualGoal goal)
        {
            await TrackerDbManager.Instance!.DeleteGoalAsync(goal.Id);
            await RefreshAllAndNotifyAsync();
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
                var notes = await TrackerDbManager.Instance!.GetQuickNotesAsync();
                ReplaceCollectionItems(_quickNotes, notes, _quickNotesLock);
                _quickNotesLoaded = true;
                _logger.Debug("Loaded {0} quick notes", _quickNotes.Count);
            }
            return _quickNotesReadOnly;
        }

        public async Task<int> AddQuickNote(QuickNote note)
        {
            var id = await TrackerDbManager.Instance!.AddQuickNoteAsync(note);
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
                var surveys = await TrackerDbManager.Instance!.GetPulseSurveysAsync();
                ReplaceCollectionItems(_pulseSurveys, surveys, _pulseSurveysLock);
                _pulseSurveysLoaded = true;
                _logger.Debug("Loaded {0} pulse surveys", _pulseSurveys.Count);
            }
            return _pulseSurveysReadOnly;
        }

        public async Task<int> AddPulseSurvey(PulseSurvey survey)
        {
            var id = await TrackerDbManager.Instance!.AddPulseSurveyAsync(survey);
            if (id > 0)
            {
                survey.Id = id;
                await RefreshPulseSurveysAsync();
            }
            return id;
        }

        public async Task<bool> UpdatePulseSurvey(PulseSurvey survey)
        {
            var success = await TrackerDbManager.Instance!.UpdatePulseSurveyAsync(survey);
            if (success)
            {
                await RefreshPulseSurveysAsync();
            }
            return success;
        }

        public async Task<bool> DeletePulseSurvey(int id)
        {
            var success = await TrackerDbManager.Instance!.DeletePulseSurveyAsync(id);
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
                var templates = await TrackerDbManager.Instance!.GetReviewTemplatesAsync();
                ReplaceCollectionItems(_reviewTemplates, templates, _reviewTemplatesLock);
                _reviewTemplatesLoaded = true;
                _logger.Debug("Loaded {0} review templates", _reviewTemplates.Count);
            }
            return _reviewTemplatesReadOnly;
        }

        public async Task<int> AddReviewTemplate(ReviewTemplate template)
        {
            var id = await TrackerDbManager.Instance!.AddReviewTemplateAsync(template);
            if (id > 0)
            {
                template.Id = id;
                await RefreshReviewTemplatesAsync();
            }
            return id;
        }

        public async Task<bool> UpdateReviewTemplate(ReviewTemplate template)
        {
            var success = await TrackerDbManager.Instance!.UpdateReviewTemplateAsync(template);
            if (success)
            {
                await RefreshReviewTemplatesAsync();
            }
            return success;
        }

        public async Task<bool> DeleteReviewTemplate(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteReviewTemplateAsync(id);
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
                var cycles = await TrackerDbManager.Instance!.GetReviewCyclesAsync();
                ReplaceCollectionItems(_reviewCycles, cycles, _reviewCyclesLock);
                _reviewCyclesLoaded = true;
                _logger.Debug("Loaded {0} review cycles", _reviewCycles.Count);
            }
            return _reviewCyclesReadOnly;
        }

        public async Task<int> AddReviewCycle(PerformanceReviewCycle cycle)
        {
            var id = await TrackerDbManager.Instance!.AddReviewCycleAsync(cycle);
            if (id > 0)
            {
                cycle.Id = id;
                await RefreshReviewCyclesAsync();
            }
            return id;
        }

        public async Task<bool> UpdateReviewCycle(PerformanceReviewCycle cycle)
        {
            var success = await TrackerDbManager.Instance!.UpdateReviewCycleAsync(cycle);
            if (success)
            {
                await RefreshReviewCyclesAsync();
            }
            return success;
        }

        public async Task<bool> DeleteReviewCycle(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteReviewCycleAsync(id);
            if (success)
            {
                await RefreshReviewCyclesAsync();
            }
            return success;
        }

        #endregion

        #region TaskCollection Methods

        public async Task<List<TaskCollection>> GetTaskCollections()
        {
            return await TrackerDbManager.Instance!.GetTaskCollectionsAsync();
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
                GetOneOnOnes(),
                GetTasks(),
                GetOKRs(),
                GetKPIs(),
                GetProjects(),
                GetFeedbacks(),
                GetGoals()
            );

            _logger.Info("All data refreshed: {0} team members, {1} tasks, {2} OKRs",
                _teamMembers.Count, _tasks.Count, _okrs.Count);
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
                    _oneOnOnesLoaded = false;
                    await GetOneOnOnes();
                    break;
                case DataChangeType.Tasks:
                    _tasksLoaded = false;
                    await GetTasks();
                    break;
                case DataChangeType.OKRs:
                    _okrsLoaded = false;
                    await GetOKRs();
                    break;
                case DataChangeType.KPIs:
                    _kpisLoaded = false;
                    await GetKPIs();
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
                    _goalsLoaded = false;
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
            _oneOnOnesLoaded = false;
            await GetOneOnOnes();
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
        /// Refreshes OKRs from the database.
        /// </summary>
        public async Task RefreshOKRsAsync()
        {
            _okrsLoaded = false;
            await GetOKRs();
        }

        /// <summary>
        /// Refreshes KPIs from the database.
        /// </summary>
        public async Task RefreshKPIsAsync()
        {
            _kpisLoaded = false;
            await GetKPIs();
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
            _goalsLoaded = false;
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
