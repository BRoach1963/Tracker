using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Interfaces;
using Tracker.Managers;
using Tracker.MockData;

namespace Tracker.ViewModels
{
    /// <summary>
    /// Main ViewModel for the Tracker application.
    /// Manages all entity collections and their CRUD operations.
    /// </summary>
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

        // Selected items
        private TeamMember? _teamMember;
        private TeamMemberWrapper? _selectedTeamMemberWrapper;
        private ITask? _selectedTask;
        private Project? _selectedProject;
        private ObjectiveKeyResult? _selectedOkr;
        private KeyPerformanceIndicator? _selectedKpi;
        private OneOnOne? _selectedOneOnOne;

        // Team Member commands
        private ICommand? _editTeamMemberCommand;
        private ICommand? _deleteTeamMemberCommand;
        private ICommand? _addTeamMemberOneOnOneCommand;

        // Task commands
        private ICommand? _editTaskCommand;
        private ICommand? _deleteTaskCommand;

        // Project commands
        private ICommand? _editProjectCommand;
        private ICommand? _deleteProjectCommand;

        // OKR commands
        private ICommand? _editOkrCommand;
        private ICommand? _deleteOkrCommand;

        // KPI commands
        private ICommand? _editKpiCommand;
        private ICommand? _deleteKpiCommand;

        // OneOnOne commands
        private ICommand? _editOneOnOneCommand;
        private ICommand? _deleteOneOnOneCommand;

        #endregion

        #region Ctor

        public TrackerMainViewModel()
        {
            // Don't load data here - wait for window to be loaded
            // Data will be loaded in MainWindow.Loaded event
            SubscribeToMessages();
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeToMessages();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands - Team Members

        public ICommand TeamMemberEditCommand => _editTeamMemberCommand ??=
            new TrackerCommand(EditTeamMemberExecuted, CanEditTeamMemberExecute);

        public ICommand TeamMemberDeleteCommand => _deleteTeamMemberCommand ??=
            new TrackerCommand(DeleteTeamMemberExecuted, CanDeleteTeamMember);

        public ICommand AddTeamMemberOneOnOneCommand => _addTeamMemberOneOnOneCommand ??=
            new TrackerCommand(AddTeamMemberOneOnOneExecuted, CanExecuteAddTeamMemberOneOnOne);

        #endregion

        #region Commands - Tasks

        public ICommand EditTaskCommand => _editTaskCommand ??=
            new TrackerCommand(EditTaskExecuted, CanEditTask);

        public ICommand DeleteTaskCommand => _deleteTaskCommand ??=
            new TrackerCommand(DeleteTaskExecuted, CanDeleteTask);

        #endregion

        #region Commands - Projects

        public ICommand EditProjectCommand => _editProjectCommand ??=
            new TrackerCommand(EditProjectExecuted, CanEditProject);

        public ICommand DeleteProjectCommand => _deleteProjectCommand ??=
            new TrackerCommand(DeleteProjectExecuted, CanDeleteProject);

        #endregion

        #region Commands - OKRs

        public ICommand EditOkrCommand => _editOkrCommand ??=
            new TrackerCommand(EditOkrExecuted, CanEditOkr);

        public ICommand DeleteOkrCommand => _deleteOkrCommand ??=
            new TrackerCommand(DeleteOkrExecuted, CanDeleteOkr);

        #endregion

        #region Commands - KPIs

        public ICommand EditKpiCommand => _editKpiCommand ??=
            new TrackerCommand(EditKpiExecuted, CanEditKpi);

        public ICommand DeleteKpiCommand => _deleteKpiCommand ??=
            new TrackerCommand(DeleteKpiExecuted, CanDeleteKpi);

        #endregion

        #region Commands - OneOnOnes

        public ICommand EditOneOnOneCommand => _editOneOnOneCommand ??=
            new TrackerCommand(EditOneOnOneExecuted, CanEditOneOnOne);

        public ICommand DeleteOneOnOneCommand => _deleteOneOnOneCommand ??=
            new TrackerCommand(DeleteOneOnOneExecuted, CanDeleteOneOnOne);

        #endregion

        #region Public Properties - Collections

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;
        public ObservableCollection<OneOnOne> SelectedTeamMemberOneOnOneCollection => _selectedTeamMemberOneOnOneCollection;
        public ObservableCollection<OneOnOne> OneOnOnes => _oneOnOnes;
        public ObservableCollection<ITask> Tasks => _tasks;
        public ObservableCollection<ObjectiveKeyResult> ObjectiveKeyResults => _okrs;
        public ObservableCollection<KeyPerformanceIndicator> KeyPerformanceIndicators => _kpis;
        public ObservableCollection<Project> Projects => _projects;

        #endregion

        #region Public Properties - Selected Items

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

        public ITask? SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                RaisePropertyChanged();
            }
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                RaisePropertyChanged();
            }
        }

        public ObjectiveKeyResult? SelectedOkr
        {
            get => _selectedOkr;
            set
            {
                _selectedOkr = value;
                RaisePropertyChanged();
            }
        }

        public KeyPerformanceIndicator? SelectedKpi
        {
            get => _selectedKpi;
            set
            {
                _selectedKpi = value;
                RaisePropertyChanged();
            }
        }

        public OneOnOne? SelectedOneOnOne
        {
            get => _selectedOneOnOne;
            set
            {
                _selectedOneOnOne = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods - Data Loading

        private async void GetData()
        {
            await RefreshAllDataAsync();
        }

        /// <summary>
        /// Refreshes all data from the database and updates the UI.
        /// Can be called externally to refresh the UI after data changes.
        /// </summary>
        public async Task RefreshAllDataAsync()
        {
            // Load data from database
            var team = await TrackerDataManager.Instance.GetTeamData();
            var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes();
            var tasks = await TrackerDataManager.Instance.GetTasks();
            var kpis = await TrackerDataManager.Instance.GetKPIs();
            var okrs = await TrackerDataManager.Instance.GetOKRs();
            var projects = await TrackerDataManager.Instance.GetProjects();

            // Update collections on UI thread - use same approach as DashboardViewModel
            if (App.Current.Dispatcher.CheckAccess())
            {
                // Already on UI thread
                _teamMembers = new ObservableCollection<TeamMember>(team);
                _oneOnOnes = new ObservableCollection<OneOnOne>(oneOnOnes);
                _tasks = new ObservableCollection<ITask>(tasks.Cast<ITask>());
                _kpis = new ObservableCollection<KeyPerformanceIndicator>(kpis);
                _okrs = new ObservableCollection<ObjectiveKeyResult>(okrs);
                _projects = new ObservableCollection<Project>(projects);
                
                RaisePropertyChanged(nameof(TeamMembers));
                RaisePropertyChanged(nameof(OneOnOnes));
                RaisePropertyChanged(nameof(Tasks));
                RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                RaisePropertyChanged(nameof(ObjectiveKeyResults));
                RaisePropertyChanged(nameof(Projects));
                
                // Refresh selected team member's 1:1 collection if one is selected
                if (_selectedTeamMemberWrapper != null)
                {
                    SetTeamMemberOneOnOneCollection();
                }
            }
            else
            {
                // Need to dispatch to UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    _teamMembers = new ObservableCollection<TeamMember>(team);
                    _oneOnOnes = new ObservableCollection<OneOnOne>(oneOnOnes);
                    _tasks = new ObservableCollection<ITask>(tasks.Cast<ITask>());
                    _kpis = new ObservableCollection<KeyPerformanceIndicator>(kpis);
                    _okrs = new ObservableCollection<ObjectiveKeyResult>(okrs);
                    _projects = new ObservableCollection<Project>(projects);
                    
                    RaisePropertyChanged(nameof(TeamMembers));
                    RaisePropertyChanged(nameof(OneOnOnes));
                    RaisePropertyChanged(nameof(Tasks));
                    RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                    RaisePropertyChanged(nameof(ObjectiveKeyResults));
                    RaisePropertyChanged(nameof(Projects));
                    
                    // Refresh selected team member's 1:1 collection if one is selected
                    if (_selectedTeamMemberWrapper != null)
                    {
                        SetTeamMemberOneOnOneCollection();
                    }
                });
            }
        }

        private async void RefreshData(PropertyChangedEnum changedProperty)
        {
            // If All is specified, refresh everything
            if (changedProperty == PropertyChangedEnum.All)
            {
                await RefreshAllDataAsync();
                return;
            }

            switch (changedProperty)
            {
                case PropertyChangedEnum.TeamMembers:
                    var team = await TrackerDataManager.Instance.GetTeamData().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedTeamMemberWrapper = null;
                        _teamMembers.Clear();
                        foreach (var member in team)
                        {
                            _teamMembers.Add(member);
                        }
                    });
                    break;
                case PropertyChangedEnum.OneOnOnes:
                    var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _oneOnOnes.Clear();
                        foreach (var meeting in oneOnOnes)
                        {
                            _oneOnOnes.Add(meeting);
                        }
                        if (_selectedTeamMemberWrapper != null)
                        {
                            SetTeamMemberOneOnOneCollection();
                        }
                    });
                    break;
                case PropertyChangedEnum.Tasks:
                    var tasks = await TrackerDataManager.Instance.GetTasks().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _tasks.Clear();
                        foreach (var task in tasks.Cast<ITask>())
                        {
                            _tasks.Add(task);
                        }
                    });
                    break;
                case PropertyChangedEnum.Projects:
                    var projects = await TrackerDataManager.Instance.GetProjects().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _projects.Clear();
                        foreach (var project in projects)
                        {
                            _projects.Add(project);
                        }
                    });
                    break;
                case PropertyChangedEnum.OKRs:
                    var okrs = await TrackerDataManager.Instance.GetOKRs().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _okrs.Clear();
                        foreach (var okr in okrs)
                        {
                            _okrs.Add(okr);
                        }
                    });
                    break;
                case PropertyChangedEnum.KPIs:
                    var kpis = await TrackerDataManager.Instance.GetKPIs().ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _kpis.Clear();
                        foreach (var kpi in kpis)
                        {
                            _kpis.Add(kpi);
                        }
                    });
                    break;
            }
        }

        #endregion

        #region Private Methods - Team Members

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

        private async void DeleteTeamMemberExecuted(object? parameter)
        {
            if (parameter is TeamMemberWrapper wrapper)
            {
                var owner = Application.Current.MainWindow;
                var result = MessageBoxHelper.Show(
                    $"Are you sure you want to delete {wrapper.Data.FirstName} {wrapper.Data.LastName}?\n\n" +
                    "This will also remove all associated 1:1 meetings and related data.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    owner);

                if (result != MessageBoxResult.Yes)
                    return;

                var success = await TrackerDataManager.Instance.DeleteTeamMember(wrapper.Data.Id);
                
                if (success)
                {
                    NotificationManager.Instance.ShowSuccess("Deleted", $"{wrapper.Data.FirstName} {wrapper.Data.LastName} has been removed.");
                }
                else
                {
                    NotificationManager.Instance.ShowError("Error", "Failed to delete team member.");
                }
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

        private bool CanExecuteAddTeamMemberOneOnOne(object? obj)
        {
            return _selectedTeamMemberWrapper != null;
        }

        private void AddTeamMemberOneOnOneExecuted(object? parameter)
        {
            if (parameter is TeamMemberWrapper wrapper)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOneOnOne, true, () =>
                {
                    RaisePropertyChanged(nameof(SelectedTeamMemberWrapper));
                }, wrapper.Data);
            }
        }

        #endregion

        #region Private Methods - Tasks

        private bool CanEditTask(object? parameter)
        {
            return parameter is ITask || _selectedTask != null;
        }

        private void EditTaskExecuted(object? parameter)
        {
            var task = parameter as ITask ?? _selectedTask;
            if (task != null)
            {
                // TODO: Launch edit task dialog when edit mode is supported
                DialogManager.Instance.LaunchDialogByType(DialogType.AddTask, true, () =>
                {
                    RaisePropertyChanged(nameof(Tasks));
                }, task);
            }
        }

        private bool CanDeleteTask(object? parameter)
        {
            return parameter is ITask || _selectedTask != null;
        }

        private async void DeleteTaskExecuted(object? parameter)
        {
            var task = parameter as ITask ?? _selectedTask;
            if (task == null) return;

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this task?\n\n\"{task.Description}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection (database delete would go here when implemented)
            var taskToRemove = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (taskToRemove != null)
            {
                _tasks.Remove(taskToRemove);
                NotificationManager.Instance.ShowSuccess("Deleted", "Task has been removed.");
                RaisePropertyChanged(nameof(Tasks));
            }
        }

        #endregion

        #region Private Methods - Projects

        private bool CanEditProject(object? parameter)
        {
            return parameter is Project || _selectedProject != null;
        }

        private void EditProjectExecuted(object? parameter)
        {
            var project = parameter as Project ?? _selectedProject;
            if (project != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddProject, true, () =>
                {
                    RaisePropertyChanged(nameof(Projects));
                }, project);
            }
        }

        private bool CanDeleteProject(object? parameter)
        {
            return parameter is Project || _selectedProject != null;
        }

        private async void DeleteProjectExecuted(object? parameter)
        {
            var project = parameter as Project ?? _selectedProject;
            if (project == null) return;

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete the project \"{project.Name}\"?\n\n" +
                "This will also remove all associated tasks, OKRs, and KPIs.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDataManager.Instance.DeleteProject(project.ID);
            
            if (success)
            {
                _projects.Remove(project);
                NotificationManager.Instance.ShowSuccess("Deleted", $"Project \"{project.Name}\" has been removed.");
                RaisePropertyChanged(nameof(Projects));
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to delete project.");
            }
        }

        #endregion

        #region Private Methods - OKRs

        private bool CanEditOkr(object? parameter)
        {
            return parameter is ObjectiveKeyResult || _selectedOkr != null;
        }

        private void EditOkrExecuted(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? _selectedOkr;
            if (okr != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOKR, true, () =>
                {
                    RaisePropertyChanged(nameof(ObjectiveKeyResults));
                }, okr);
            }
        }

        private bool CanDeleteOkr(object? parameter)
        {
            return parameter is ObjectiveKeyResult || _selectedOkr != null;
        }

        private async void DeleteOkrExecuted(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? _selectedOkr;
            if (okr == null) return;

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this OKR?\n\n\"{okr.Title}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection
            _okrs.Remove(okr);
            NotificationManager.Instance.ShowSuccess("Deleted", "OKR has been removed.");
            RaisePropertyChanged(nameof(ObjectiveKeyResults));
        }

        #endregion

        #region Private Methods - KPIs

        private bool CanEditKpi(object? parameter)
        {
            return parameter is KeyPerformanceIndicator || _selectedKpi != null;
        }

        private void EditKpiExecuted(object? parameter)
        {
            var kpi = parameter as KeyPerformanceIndicator ?? _selectedKpi;
            if (kpi != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddKPI, true, () =>
                {
                    RaisePropertyChanged(nameof(KeyPerformanceIndicators));
                }, kpi);
            }
        }

        private bool CanDeleteKpi(object? parameter)
        {
            return parameter is KeyPerformanceIndicator || _selectedKpi != null;
        }

        private async void DeleteKpiExecuted(object? parameter)
        {
            var kpi = parameter as KeyPerformanceIndicator ?? _selectedKpi;
            if (kpi == null) return;

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this KPI?\n\n\"{kpi.Name}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            // Remove from local collection
            _kpis.Remove(kpi);
            NotificationManager.Instance.ShowSuccess("Deleted", "KPI has been removed.");
            RaisePropertyChanged(nameof(KeyPerformanceIndicators));
        }

        #endregion

        #region Private Methods - OneOnOnes

        private bool CanEditOneOnOne(object? parameter)
        {
            return parameter is OneOnOne || _selectedOneOnOne != null;
        }

        private void EditOneOnOneExecuted(object? parameter)
        {
            var oneOnOne = parameter as OneOnOne ?? _selectedOneOnOne;
            if (oneOnOne != null)
            {
                DialogManager.Instance.LaunchDialogByType(DialogType.AddOneOnOne, true, () =>
                {
                    RaisePropertyChanged(nameof(OneOnOnes));
                    SetTeamMemberOneOnOneCollection();
                }, oneOnOne);
            }
        }

        private bool CanDeleteOneOnOne(object? parameter)
        {
            return parameter is OneOnOne || _selectedOneOnOne != null;
        }

        private async void DeleteOneOnOneExecuted(object? parameter)
        {
            var oneOnOne = parameter as OneOnOne ?? _selectedOneOnOne;
            if (oneOnOne == null) return;

            var owner = Application.Current.MainWindow;
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete this 1:1 meeting?\n\n\"{oneOnOne.Description}\" with {oneOnOne.TeamMemberName}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                owner);

            if (result != MessageBoxResult.Yes)
                return;

            var success = await TrackerDataManager.Instance.DeleteOneOnOne(oneOnOne.Id);
            
            if (success)
            {
                _oneOnOnes.Remove(oneOnOne);
                _selectedTeamMemberOneOnOneCollection.Remove(oneOnOne);
                NotificationManager.Instance.ShowSuccess("Deleted", "1:1 meeting has been removed.");
                RaisePropertyChanged(nameof(OneOnOnes));
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to delete 1:1 meeting.");
            }
        }

        #endregion

        #region Private Methods - Messaging

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
