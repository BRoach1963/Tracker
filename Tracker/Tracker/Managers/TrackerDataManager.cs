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

        #endregion

        #region Singleton Instance

        private static TrackerDataManager? _instance;
        private static readonly object SyncRoot = new();

        public static TrackerDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        _instance ??= new TrackerDataManager();
                    }
                }
                return _instance;
            }
        }

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
            _teamMembers?.Clear();
            _teamMembers = await TrackerDbManager.Instance!.GetTeamMembersAsync();
            return _teamMembers;
        }

        public async Task<bool> AddTeamMember(TeamMember teamMember)
        {
            var id = await TrackerDbManager.Instance!.AddTeamMemberAsync(teamMember);
            if (id > 0)
            {
                teamMember.Id = id;
                _teamMembers?.Add(teamMember);
                
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
            _oneOnOnes?.Clear();
            _oneOnOnes = await TrackerDbManager.Instance!.GetOneOnOnesAsync();
            return _oneOnOnes;
        }

        public async Task<int> AddOneOnOne(OneOnOne oneOnOne)
        {
            var id = await TrackerDbManager.Instance!.AddOneOnOneAsync(oneOnOne);
            if (id > 0)
            {
                oneOnOne.Id = id;
                _oneOnOnes?.Add(oneOnOne);
                
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
            _projects?.Clear();
            _projects = await TrackerDbManager.Instance!.GetProjectsAsync();
            return _projects;
        }

        public async Task<int> AddProject(Project project)
        {
            var id = await TrackerDbManager.Instance!.AddProjectAsync(project);
            if (id > 0)
            {
                project.ID = id;
                _projects?.Add(project);
            }
            return id;
        }

        public async Task<bool> UpdateProject(Project project)
        {
            return await TrackerDbManager.Instance!.UpdateProjectAsync(project);
        }

        public async Task<bool> DeleteProject(int id)
        {
            var success = await TrackerDbManager.Instance!.DeleteProjectAsync(id);
            if (success)
            {
                _projects?.RemoveAll(p => p.ID == id);
            }
            return success;
        }

        #endregion

        #region Task Methods

        public async Task<List<IndividualTask>> GetTasks()
        {
            _tasks?.Clear();
            _tasks = await TrackerDbManager.Instance!.GetTasksAsync();
            return _tasks;
        }

        public async Task<int> AddTask(IndividualTask task)
        {
            return await TrackerDbManager.Instance!.AddTaskAsync(task);
        }

        public async Task<bool> UpdateTask(IndividualTask task)
        {
            return await TrackerDbManager.Instance!.UpdateTaskAsync(task);
        }

        #endregion

        #region OKR Methods

        public async Task<List<ObjectiveKeyResult>> GetOKRs()
        {
            _okrs?.Clear();
            _okrs = await TrackerDbManager.Instance!.GetOKRsAsync();
            return _okrs;
        }

        public async Task<int> AddOKR(ObjectiveKeyResult okr)
        {
            return await TrackerDbManager.Instance!.AddOKRAsync(okr);
        }

        public async Task<bool> UpdateOKR(ObjectiveKeyResult okr)
        {
            return await TrackerDbManager.Instance!.UpdateOKRAsync(okr);
        }

        #endregion

        #region KPI Methods

        public async Task<List<KeyPerformanceIndicator>> GetKPIs()
        {
            _kpis?.Clear();
            _kpis = await TrackerDbManager.Instance!.GetKPIsAsync();
            return _kpis;
        }

        public async Task<int> AddKPI(KeyPerformanceIndicator kpi)
        {
            return await TrackerDbManager.Instance!.AddKPIAsync(kpi);
        }

        public async Task<bool> UpdateKPI(KeyPerformanceIndicator kpi)
        {
            return await TrackerDbManager.Instance!.UpdateKPIAsync(kpi);
        }

        #endregion
    }
}
