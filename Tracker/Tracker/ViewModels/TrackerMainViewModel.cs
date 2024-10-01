using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Interfaces;
using Tracker.Managers;
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
        private ObservableCollection<OneOnOne> _selectedTeamMemberOneOnOneCollection = new();

        private TeamMember? _teamMember;
        private TeamMemberWrapper? _selectedTeamMemberWrapper;

        private ICommand? _editTeamMemberCommand;
        private ICommand? _deleteTeamMemberCommand;

        #endregion

        #region Ctor

        public TrackerMainViewModel()
        {
            GetData();
            SubscribeToMessages();
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeToMessages();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        public ICommand TeamMemberEditCommand => _editTeamMemberCommand ??=
            new TrackerCommand(EditTeamMemberExecuted, CanEditTeamMemberExecute);

        public ICommand TeamMemberDeleteCommand => _deleteTeamMemberCommand ??=
            new TrackerCommand(DeleteTeamMemberExecuted, CanDeleteTeamMember);

        #endregion

        #region Public Properties

        public TeamMember? SelectedTeamMember
        {
            get => _teamMember;
            set
            {
                _teamMember = value;
                SelectedTeamMemberWrapper = new TeamMemberWrapper(_teamMember);
                if (_teamMember != null) SetTeamMemberOneOnOneCollection();
                RaisePropertyChanged();
            }
        }

        public TeamMemberWrapper? SelectedTeamMemberWrapper
        {
            get => _selectedTeamMemberWrapper;
            set
            {
                _selectedTeamMemberWrapper = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        public ObservableCollection<OneOnOne> SelectedTeamMemberOneOnOneCollection => _selectedTeamMemberOneOnOneCollection;

        public ObservableCollection<OneOnOne> OneOnOnes => _oneOnOnes;

        public ObservableCollection<ITask> Tasks => _tasks;

        public ObservableCollection<ObjectiveKeyResult> ObjectiveKeyResults => _okrs;

        public ObservableCollection<KeyPerformanceIndicator> KeyPerformanceIndicators => _kpis;

        public ObservableCollection<Project> Projects => _projects;


        #endregion

        #region Private Methods

        private async void GetData()
        {
            var team = await TrackerDataManager.Instance.GetTeamData().ConfigureAwait(true);
            App.Current.Dispatcher.BeginInvoke(() =>
            {
                _teamMembers = new ObservableCollection<TeamMember>(team);
                _oneOnOnes = MockOneOnOnes.GetMockOneOnOneData(team);
                _tasks = MockTasks.GetMockTaskData(team);
                _kpis = MockKpIs.GetMockKpiData(team);
                _okrs = MockOkRs.GetMockOkrData(team);
                _projects = MockProjects.GetMockProjects(team);
                RaisePropertyChanged(nameof(TeamMembers));
                RaisePropertyChanged(nameof(OneOnOnes));
                RaisePropertyChanged(nameof(Tasks));
                RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                RaisePropertyChanged(nameof(ObjectiveKeyResults));
                RaisePropertyChanged(nameof(Projects));

            });
           
        }

        private async void RefreshData(PropertyChangedEnum changedProperty)
        {
            switch (changedProperty)
            {
                case PropertyChangedEnum.TeamMembers:
                    _teamMembers.Clear();

                    var team = await TrackerDataManager.Instance.GetTeamData().ConfigureAwait(true);
                    App.Current.Dispatcher.BeginInvoke(() =>
                    {
                        SelectedTeamMemberWrapper = null;
                        _teamMembers = new ObservableCollection<TeamMember>(team);
                        RaisePropertyChanged(nameof(TeamMembers));
                    });
                    break;
            }
        }

        private bool CanEditTeamMemberExecute(object? parameter)
        {
            return _selectedTeamMemberWrapper != null;
        }

        private void EditTeamMemberExecuted(object? parameter)
        {
            if (parameter is TeamMemberWrapper wrapper)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.EditTeamMember, true, () =>
                {
                    RaisePropertyChanged(nameof(SelectedTeamMemberWrapper));
                }, wrapper.Data);
            }
        }

        private bool CanDeleteTeamMember(object? parameter)
        {
            return _selectedTeamMemberWrapper != null;
        }

        private void DeleteTeamMemberExecuted(object? parameter)
        {
            if (parameter is TeamMemberWrapper wrapper)
            {
                // Get Confirmation from User

                TrackerDataManager.Instance.DeleteTeamMember(wrapper.Data.Id); 
            }
        }

        private void SetTeamMemberOneOnOneCollection()
        {
            _selectedTeamMemberOneOnOneCollection.Clear();
            foreach (var meeting in _oneOnOnes.Where(x => _teamMember != null && x.TeamMember.Id == _teamMember.Id))
            {
                _selectedTeamMemberOneOnOneCollection.Add(meeting);
            }
        }

        private void SubscribeToMessages()
        {
            Messenger.Subscribe<PropertyChangedMessage>(HandlePropertyChangedMessage);
        }

        private void UnsubscribeToMessages()
        {
            Messenger.Unsubscribe<PropertyChangedMessage>(HandlePropertyChangedMessage);
        }

        private void HandlePropertyChangedMessage(PropertyChangedMessage message)
        {
            if (message.RefreshData)
            {
                RefreshData(message.ChangedProperty);
            }
        }

        #endregion
    }
}
