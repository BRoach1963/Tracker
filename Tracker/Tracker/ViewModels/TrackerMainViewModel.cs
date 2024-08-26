using System.Collections.ObjectModel;
using Tracker.DataModels;
using Tracker.Interfaces;
using Tracker.MockData;

namespace Tracker.ViewModels
{
    public class TrackerMainViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<OneOnOne> _oneOnOnes = new();
        private ObservableCollection<ITask> _tasks = new();
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private ObservableCollection<Project> _projects = new();

        #endregion

        #region Ctor

        public TrackerMainViewModel()
        {
            GetData();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        public ObservableCollection<OneOnOne> OneOnOnes => _oneOnOnes;

        public ObservableCollection<ITask> Tasks => _tasks;

        public ObservableCollection<ObjectiveKeyResult> ObjectiveKeyResults => _okrs;

        public ObservableCollection<KeyPerformanceIndicator> KeyPerformanceIndicators => _kpis;

        public ObservableCollection<Project> Projects => _projects;


        #endregion

        #region Private Methods

        private void GetData()
        {
            _teamMembers = MockTeamMemberData.GetMockTeamMemberData();
            _oneOnOnes = MockOneOnOnes.GetMockOneOnOneData();
            _tasks = MockTasks.GetMockTaskData();
        }

        #endregion
    }
}
