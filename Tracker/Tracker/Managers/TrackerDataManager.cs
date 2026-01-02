using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;

namespace Tracker.Managers
{
    /// <summary>
    /// Manages application data and provides a caching layer over the database.
    /// </summary>
    public class TrackerDataManager
    {
        #region Fields

        private bool _initialized;
        private List<TeamMember>? _teamMembers = new();
        private List<OneOnOne>? _oneOnOnes = new();
        private List<Project>? _projects = new();
        private List<IndividualTask>? _tasks = new();
        private List<ObjectiveKeyResult>? _okrs = new();
        private List<KeyPerformanceIndicator>? _kpis = new();

        // Cache invalidation flags - track which caches need to be refreshed
        private bool _teamMembersInvalidated = true;
        private bool _oneOnOnesInvalidated = true;
        private bool _projectsInvalidated = true;
        private bool _tasksInvalidated = true;
        private bool _okrsInvalidated = true;
        private bool _kpisInvalidated = true;

        #endregion

        #region Singleton Instance

        private static readonly Lazy<TrackerDataManager> _lazyInstance = 
            new(() => new TrackerDataManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of TrackerDataManager.
        /// </summary>
        public static TrackerDataManager Instance => _lazyInstance.Value;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
        }

        public void Shutdown()
        {
            _teamMembers?.Clear();
            _oneOnOnes?.Clear();
            _projects?.Clear();
            _tasks?.Clear();
            _okrs?.Clear();
            _kpis?.Clear();

            // Reset invalidation flags
            _teamMembersInvalidated = true;
            _oneOnOnesInvalidated = true;
            _projectsInvalidated = true;
            _tasksInvalidated = true;
            _okrsInvalidated = true;
            _kpisInvalidated = true;

            _teamMembers = null;
            _oneOnOnes = null;
            _projects = null;
            _tasks = null;
            _okrs = null;
            _kpis = null;
        }

        #endregion

        #region Public Properties

        public List<TeamMember>? TeamMembers => _teamMembers;
        public List<OneOnOne>? OneOnOnes => _oneOnOnes;
        public List<Project>? Projects => _projects;
        public List<IndividualTask>? Tasks => _tasks;
        public List<ObjectiveKeyResult>? OKRs => _okrs;
        public List<KeyPerformanceIndicator>? KPIs => _kpis;

        #endregion

        #region Team Member Methods

        public async Task<List<TeamMember>> GetTeamData()
        {
            // Only refresh from database if cache is invalidated
            if (_teamMembersInvalidated)
            {
                _teamMembers = await TrackerDbManager.Instance!.GetTeamMembersAsync();
                _teamMembersInvalidated = false;
            }
            return _teamMembers;
        }

        public async Task<bool> AddTeamMember(TeamMember teamMember)
        {
            var id = await TrackerDbManager.Instance!.AddTeamMemberAsync(teamMember);
            if (id > 0)
            {
                teamMember.Id = id;
                _teamMembers?.Add(teamMember);

                // Mark cache as valid since we just added to it
                _teamMembersInvalidated = false;

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.TeamMembers,
                    RefreshData = true
                });
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateTeamMember(TeamMember teamMember)
        {
            var success = await TrackerDbManager.Instance!.UpdateTeamMemberAsync(teamMember);
            if (success)
            {
                // Update local cache
                var existing = _teamMembers?.FirstOrDefault(t => t.Id == teamMember.Id);
                if (existing != null)
                {
                    var index = _teamMembers!.IndexOf(existing);
                    _teamMembers[index] = teamMember;
                }

                // Mark cache as valid since we just updated it
                _teamMembersInvalidated = false;

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.TeamMembers,
                    RefreshData = true
                });
            }
            return success;
        }

        public async Task<bool> DeleteTeamMember(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteTeamMemberAsync(id);
            if (success)
            {
                _teamMembers?.RemoveAll(t => t.Id == id);

                // Mark cache as valid since we just updated it
                _teamMembersInvalidated = false;

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.TeamMembers,
                    RefreshData = true
                });
            }
            return success;
        }

        #endregion

        #region OneOnOne Methods

        public async Task<List<OneOnOne>> GetOneOnOnes()
        {
            // Only refresh from database if cache is invalidated
            if (_oneOnOnesInvalidated)
            {
                _oneOnOnes = await TrackerDbManager.Instance!.GetOneOnOnesAsync();
                _oneOnOnesInvalidated = false;
            }
            return _oneOnOnes;
        }

        public async Task<int> AddOneOnOne(OneOnOne oneOnOne, int? teamMemberId = null)
        {
            var id = await TrackerDbManager.Instance!.AddOneOnOneAsync(oneOnOne, teamMemberId);
            if (id > 0)
            {
                oneOnOne.Id = id;

                // If teamMemberId was provided, populate the TeamMember navigation property from cache
                if (teamMemberId.HasValue && oneOnOne.TeamMember == null)
                {
                    oneOnOne.TeamMember = _teamMembers?.FirstOrDefault(tm => tm.Id == teamMemberId.Value) ?? new TeamMember();
                }

                _oneOnOnes?.Add(oneOnOne);

                // Mark cache as valid since we just added to it
                _oneOnOnesInvalidated = false;

                // Create meeting reminder if enabled
                await Services.ReminderService.Instance.CreateMeetingReminderAsync(oneOnOne);

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.OneOnOnes,
                    RefreshData = true
                });
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

                // Mark cache as valid since we just updated it
                _oneOnOnesInvalidated = false;

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.OneOnOnes,
                    RefreshData = true
                });
            }
            return success;
        }

        public async Task<bool> DeleteOneOnOne(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteOneOnOneAsync(id);
            if (success)
            {
                _oneOnOnes?.RemoveAll(o => o.Id == id);

                // Mark cache as valid since we just updated it
                _oneOnOnesInvalidated = false;

                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.OneOnOnes,
                    RefreshData = true
                });
            }
            return success;
        }

        #endregion

        #region Project Methods

        public async Task<List<Project>> GetProjects()
        {
            // Only refresh from database if cache is invalidated
            if (_projectsInvalidated)
            {
                _projects = await TrackerDbManager.Instance!.GetProjectsAsync();
                _projectsInvalidated = false;
            }
            return _projects;
        }

        public async Task<int> AddProject(Project project)
        {
            var id = await TrackerDbManager.Instance!.AddProjectAsync(project);
            if (id > 0)
            {
                project.ID = id;
                _projects?.Add(project);

                // Mark cache as valid since we just added to it
                _projectsInvalidated = false;
            }
            return id;
        }

        public async Task<bool> UpdateProject(Project project)
        {
            var success = await TrackerDbManager.Instance!.UpdateProjectAsync(project);
            if (success)
            {
                // Update cache with the modified project
                var existing = _projects?.FirstOrDefault(p => p.ID == project.ID);
                if (existing != null)
                {
                    var index = _projects!.IndexOf(existing);
                    _projects[index] = project;
                }

                // Mark cache as valid since we just updated it
                _projectsInvalidated = false;

                // Notify subscribers of the change
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.Projects,
                    RefreshData = true
                });
            }
            return success;
        }

        public async Task<bool> DeleteProject(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteProjectAsync(id);
            if (success)
            {
                _projects?.RemoveAll(p => p.ID == id);

                // Mark cache as valid since we just updated it
                _projectsInvalidated = false;
            }
            return success;
        }

        #endregion

        #region Task Methods

        public async Task<List<IndividualTask>> GetTasks()
        {
            // Only refresh from database if cache is invalidated
            if (_tasksInvalidated)
            {
                _tasks = await TrackerDbManager.Instance!.GetTasksAsync();

                // Populate meeting counts for all tasks in a single batch query (prevents N+1 problem)
                if (_tasks != null && _tasks.Count > 0)
                {
                    var taskIds = _tasks.Select(t => t.Id).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetTaskMeetingCountsAsync(taskIds);

                    foreach (var task in _tasks)
                    {
                        task.MeetingCount = meetingCounts.TryGetValue(task.Id, out var count) ? count : 0;
                    }
                }

                _tasksInvalidated = false;
            }

            return _tasks;
        }

        public async Task<int> AddTask(IndividualTask task)
        {
            var id = await TrackerDbManager.Instance!.AddTaskAsync(task);
            if (id > 0)
            {
                // Mark cache as invalid since we added a new task
                // It will be refreshed on next GetTasks() call
                _tasksInvalidated = true;
            }
            return id;
        }

        public async Task<bool> UpdateTask(IndividualTask task)
        {
            var success = await TrackerDbManager.Instance!.UpdateTaskAsync(task);
            if (success)
            {
                // Update cache with the modified task
                var existing = _tasks?.FirstOrDefault(t => t.Id == task.Id);
                if (existing != null)
                {
                    var index = _tasks!.IndexOf(existing);
                    _tasks[index] = task;
                }

                // Mark cache as valid since we just updated it
                _tasksInvalidated = false;

                // Notify subscribers of the change
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.Tasks,
                    RefreshData = true
                });
            }
            return success;
        }

        #endregion

        #region OKR Methods

        public async Task<List<ObjectiveKeyResult>> GetOKRs()
        {
            // Only refresh from database if cache is invalidated
            if (_okrsInvalidated)
            {
                _okrs = await TrackerDbManager.Instance!.GetOKRsAsync();

                // Populate meeting counts for all OKRs in a single batch query (prevents N+1 problem)
                if (_okrs != null && _okrs.Count > 0)
                {
                    var okrIds = _okrs.Select(o => o.ObjectiveId).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetOkrMeetingCountsAsync(okrIds);

                    foreach (var okr in _okrs)
                    {
                        okr.MeetingCount = meetingCounts.TryGetValue(okr.ObjectiveId, out var count) ? count : 0;
                    }
                }

                _okrsInvalidated = false;
            }

            return _okrs;
        }

        public async Task<int> AddOKR(ObjectiveKeyResult okr)
        {
            var id = await TrackerDbManager.Instance!.AddOKRAsync(okr);
            if (id > 0)
            {
                // Mark cache as invalid since we added a new OKR
                // It will be refreshed on next GetOKRs() call
                _okrsInvalidated = true;
            }
            return id;
        }

        public async Task<bool> UpdateOKR(ObjectiveKeyResult okr)
        {
            var success = await TrackerDbManager.Instance!.UpdateOKRAsync(okr);
            if (success)
            {
                // Update cache with the modified OKR
                var existing = _okrs?.FirstOrDefault(o => o.ObjectiveId == okr.ObjectiveId);
                if (existing != null)
                {
                    var index = _okrs!.IndexOf(existing);
                    _okrs[index] = okr;
                }

                // Mark cache as valid since we just updated it
                _okrsInvalidated = false;

                // Notify subscribers of the change
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.OKRs,
                    RefreshData = true
                });
            }
            return success;
        }

        #endregion

        #region KPI Methods

        public async Task<List<KeyPerformanceIndicator>> GetKPIs()
        {
            // Only refresh from database if cache is invalidated
            if (_kpisInvalidated)
            {
                _kpis = await TrackerDbManager.Instance!.GetKPIsAsync();

                // Populate meeting counts for all KPIs in a single batch query (prevents N+1 problem)
                if (_kpis != null && _kpis.Count > 0)
                {
                    var kpiIds = _kpis.Select(k => k.KpiId).ToList();
                    var meetingCounts = await TrackerDbManager.Instance.GetKpiMeetingCountsAsync(kpiIds);

                    foreach (var kpi in _kpis)
                    {
                        kpi.MeetingCount = meetingCounts.TryGetValue(kpi.KpiId, out var count) ? count : 0;
                    }
                }

                _kpisInvalidated = false;
            }

            return _kpis;
        }

        public async Task<int> AddKPI(KeyPerformanceIndicator kpi)
        {
            var id = await TrackerDbManager.Instance!.AddKPIAsync(kpi);
            if (id > 0)
            {
                // Mark cache as invalid since we added a new KPI
                // It will be refreshed on next GetKPIs() call
                _kpisInvalidated = true;
            }
            return id;
        }

        public async Task<bool> UpdateKPI(KeyPerformanceIndicator kpi)
        {
            var success = await TrackerDbManager.Instance!.UpdateKPIAsync(kpi);
            if (success)
            {
                // Update cache with the modified KPI
                var existing = _kpis?.FirstOrDefault(k => k.KpiId == kpi.KpiId);
                if (existing != null)
                {
                    var index = _kpis!.IndexOf(existing);
                    _kpis[index] = kpi;
                }

                // Mark cache as valid since we just updated it
                _kpisInvalidated = false;

                // Notify subscribers of the change
                Messenger.Publish(new PropertyChangedMessage
                {
                    ChangedProperty = PropertyChangedEnum.KPIs,
                    RefreshData = true
                });
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

        #region Feedback Methods

        public async Task<List<Feedback>> GetFeedbacks()
        {
            return await TrackerDbManager.Instance!.GetAllFeedbackAsync();
        }

        public async Task<int> AddFeedback(Feedback feedback)
        {
            return await TrackerDbManager.Instance!.AddFeedbackAsync(feedback);
        }

        public async Task DeleteFeedbackAsync(Feedback feedback)
        {
            await TrackerDbManager.Instance!.DeleteFeedbackAsync(feedback.Id);
        }

        #endregion

        #region Goal Methods

        public async Task<List<IndividualGoal>> GetGoals()
        {
            return await TrackerDbManager.Instance!.GetAllGoalsAsync();
        }

        public async Task<int> AddGoal(IndividualGoal goal)
        {
            return await TrackerDbManager.Instance!.AddGoalAsync(goal);
        }

        public async Task DeleteGoalAsync(IndividualGoal goal)
        {
            await TrackerDbManager.Instance!.DeleteGoalAsync(goal.Id);
        }

        #endregion

        #region QuickNote Methods

        public async Task<List<QuickNote>> GetQuickNotes()
        {
            return await TrackerDbManager.Instance!.GetQuickNotesAsync();
        }

        public async Task<int> AddQuickNote(QuickNote note)
        {
            return await TrackerDbManager.Instance!.AddQuickNoteAsync(note);
        }

        #endregion
    }
}
