using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing Goals.
    /// 
    /// Targets are now separate entities that belong to Goals.
    /// Targets can optionally link to Metrics, Projects, or TaskCollections via IMeasurable.
    /// 
    /// Key responsibilities:
    /// - Expose Goal properties for data binding
    /// - Provide team member selection for Goal ownership
    /// - Manage Targets (inline editing within Goal)
    /// - Handle Goal creation and updates via commands
    /// - Track property changes for edit mode
    /// </summary>
    public class NewGoalViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly Goal _goal;
        private readonly bool _inEditMode;

        private ICommand? _addGoalCommand;
        private ICommand? _updateGoalCommand;
        private ICommand? _addTargetCommand;
        private ICommand? _removeTargetCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<Project> _availableProjects = new();
        private ObservableCollection<Target> _targets = new();
        private ObservableCollection<EnumWrapper<TimePeriodEnum>> _timePeriods = new();

        private TeamMember? _selectedOwner;
        private Project? _selectedProject;
        private Target? _selectedTarget;
        private EnumWrapper<TimePeriodEnum>? _selectedTimePeriod;

        private readonly Dictionary<string, object> _changedProperties = new();

        // New Target input fields
        private string _newTargetTitle = string.Empty;
        private decimal _newTargetValue = 100;
        private string _newTargetUnit = "%";

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewGoalViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="goal">The Goal data to edit or a new Goal instance.</param>
        /// <param name="edit">True if editing an existing Goal, false for new Goal.</param>
        public NewGoalViewModel(Action? callback, Goal goal, bool edit = false) : base(callback)
        {
            _goal = goal;
            _inEditMode = edit;

            // Set defaults for new Goals
            if (!_inEditMode)
            {
                _goal.StartDate = DateTime.Now;
                _goal.EndDate = DateTime.Now.AddMonths(3); // Default quarter duration
                _goal.TimePeriod = TimePeriodEnum.Q1;
                _goal.Year = DateTime.Now.Year;
            }

            LoadEnums();
            LoadTeamMembers();
            LoadProjects();
            LoadTargets();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            _availableProjects.Clear();
            _targets.Clear();
            _timePeriods.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new Goal to the database.
        /// </summary>
        public ICommand AddGoalCommand => _addGoalCommand ??=
            new TrackerCommand(AddGoalExecuted, CanExecuteAddGoal);

        /// <summary>
        /// Command to update an existing Goal in the database.
        /// </summary>
        public ICommand UpdateGoalCommand => _updateGoalCommand ??=
            new TrackerCommand(UpdateGoalExecuted, CanExecuteUpdateGoal);

        /// <summary>
        /// Command to add a new Target to this Goal.
        /// </summary>
        public ICommand AddTargetCommand => _addTargetCommand ??=
            new TrackerCommand(AddTargetExecuted, CanExecuteAddTarget);

        /// <summary>
        /// Command to remove a Target from this Goal.
        /// </summary>
        public ICommand RemoveTargetCommand => _removeTargetCommand ??=
            new TrackerCommand(RemoveTargetExecuted, CanExecuteRemoveTarget);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying Goal data model.
        /// </summary>
        public Goal Data => _goal;

        /// <summary>
        /// Gets whether the ViewModel is in edit mode (true) or add mode (false).
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of available team members for owner selection.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        /// <summary>
        /// Gets the collection of available projects to link this Goal to.
        /// </summary>
        public ObservableCollection<Project> AvailableProjects => _availableProjects;

        /// <summary>
        /// Gets the collection of Targets for this Goal.
        /// </summary>
        public ObservableCollection<Target> Targets => _targets;

        /// <summary>
        /// Gets the collection of available time periods.
        /// </summary>
        public ObservableCollection<EnumWrapper<TimePeriodEnum>> TimePeriods => _timePeriods;

        /// <summary>
        /// Gets or sets the selected owner for the Goal.
        /// </summary>
        public TeamMember? SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                _selectedOwner = value;
                if (value != null)
                {
                    _goal.Owner = value;
                    UpdateChangedValues("@OwnerId", value.Id);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected project that this Goal is linked to.
        /// </summary>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                _goal.ProjectId = value?.Id;
                UpdateChangedValues("@ProjectId", _goal.ProjectId);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected time period.
        /// </summary>
        public EnumWrapper<TimePeriodEnum>? SelectedTimePeriod
        {
            get => _selectedTimePeriod;
            set
            {
                _selectedTimePeriod = value;
                if (value != null)
                {
                    _goal.TimePeriod = value.EnumValue;
                    UpdateChangedValues("@TimePeriod", value.EnumValue);
                    UpdateDatesFromTimePeriod();
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected Target in the list.
        /// </summary>
        public Target? SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                _selectedTarget = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the title for a new Target.
        /// </summary>
        public string NewTargetTitle
        {
            get => _newTargetTitle;
            set
            {
                _newTargetTitle = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the target value for a new Target.
        /// </summary>
        public decimal NewTargetValue
        {
            get => _newTargetValue;
            set
            {
                _newTargetValue = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the unit for a new Target.
        /// </summary>
        public string NewTargetUnit
        {
            get => _newTargetUnit;
            set
            {
                _newTargetUnit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the Goal title (the objective statement).
        /// </summary>
        public string Title
        {
            get => _goal.Title;
            set
            {
                _goal.Title = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Title", value);
            }
        }

        /// <summary>
        /// Gets or sets the Goal description.
        /// </summary>
        public string? Description
        {
            get => _goal.Description;
            set
            {
                _goal.Description = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Description", value);
            }
        }

        /// <summary>
        /// Gets or sets the year for the Goal.
        /// </summary>
        public int Year
        {
            get => _goal.Year;
            set
            {
                _goal.Year = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Year", value);
                UpdateDatesFromTimePeriod();
            }
        }

        /// <summary>
        /// Gets or sets the Goal start date.
        /// </summary>
        public DateTime StartDate
        {
            get => _goal.StartDate;
            set
            {
                _goal.StartDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StartDateDisplay));
                UpdateChangedValues("@StartDate", value);
            }
        }

        /// <summary>
        /// Gets or sets the start date as a formatted display string.
        /// </summary>
        public string StartDateDisplay
        {
            get => _goal.StartDate == DateTime.MinValue ? "MM/DD/YYYY" : _goal.StartDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _goal.StartDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(StartDate));
                    UpdateChangedValues("@StartDate", date);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Goal end date.
        /// </summary>
        public DateTime EndDate
        {
            get => _goal.EndDate;
            set
            {
                _goal.EndDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EndDateDisplay));
                UpdateChangedValues("@EndDate", value);
            }
        }

        /// <summary>
        /// Gets or sets the end date as a formatted display string.
        /// </summary>
        public string EndDateDisplay
        {
            get => _goal.EndDate == DateTime.MinValue ? "MM/DD/YYYY" : _goal.EndDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _goal.EndDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(EndDate));
                    UpdateChangedValues("@EndDate", date);
                }
            }
        }

        /// <summary>
        /// Gets the calculated Goal status (OnTrack, AtRisk, OffTrack) based on Targets - read-only.
        /// </summary>
        public GoalStatus Status => _goal.Status;

        /// <summary>
        /// Gets the completion percentage based on Targets - read-only.
        /// </summary>
        public decimal CompletionPercentage => _goal.EffectiveProgress;

        /// <summary>
        /// Gets the time period display string.
        /// </summary>
        public string TimePeriodDisplay => $"{_goal.TimePeriod} {_goal.Year}";

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads enum values for dropdown selections.
        /// </summary>
        private void LoadEnums()
        {
            _timePeriods.Clear();
            foreach (TimePeriodEnum period in Enum.GetValues(typeof(TimePeriodEnum)))
            {
                _timePeriods.Add(new EnumWrapper<TimePeriodEnum>(period));
            }

            _selectedTimePeriod = _timePeriods.FirstOrDefault(p => p.EnumValue == _goal.TimePeriod);
        }

        /// <summary>
        /// Loads available team members for owner selection.
        /// </summary>
        private void LoadTeamMembers()
        {
            _teamMembers.Clear();
            var members = TrackerDataManager.Instance.TeamMembers;
            if (members != null)
            {
                foreach (var member in members.Where(m => m.IsActive))
                {
                    _teamMembers.Add(member);
                }
            }

            // Set selected owner if editing
            if (_inEditMode && _goal.Owner?.Id != Guid.Empty)
            {
                _selectedOwner = _teamMembers.FirstOrDefault(t => t.Id == _goal.Owner.Id);
            }
        }

        /// <summary>
        /// Loads available projects for linking.
        /// </summary>
        private void LoadProjects()
        {
            _availableProjects.Clear();
            var projects = TrackerDataManager.Instance.Projects;
            if (projects != null)
            {
                foreach (var project in projects)
                {
                    _availableProjects.Add(project);
                }
            }

            // Set selected project if editing and linked
            if (_inEditMode && _goal.ProjectId.HasValue && _goal.ProjectId != Guid.Empty)
            {
                _selectedProject = _availableProjects.FirstOrDefault(p => p.Id == _goal.ProjectId);
            }
        }

        /// <summary>
        /// Loads Targets already linked to this Goal.
        /// </summary>
        private void LoadTargets()
        {
            _targets.Clear();
            if (_goal.Targets != null)
            {
                foreach (var target in _goal.Targets)
                {
                    _targets.Add(target);
                }
            }
        }

        /// <summary>
        /// Updates start/end dates based on selected time period.
        /// </summary>
        private void UpdateDatesFromTimePeriod()
        {
            if (_selectedTimePeriod == null) return;

            var year = _goal.Year;
            switch (_selectedTimePeriod.EnumValue)
            {
                case TimePeriodEnum.Q1:
                    StartDate = new DateTime(year, 1, 1);
                    EndDate = new DateTime(year, 3, 31);
                    break;
                case TimePeriodEnum.Q2:
                    StartDate = new DateTime(year, 4, 1);
                    EndDate = new DateTime(year, 6, 30);
                    break;
                case TimePeriodEnum.Q3:
                    StartDate = new DateTime(year, 7, 1);
                    EndDate = new DateTime(year, 9, 30);
                    break;
                case TimePeriodEnum.Q4:
                    StartDate = new DateTime(year, 10, 1);
                    EndDate = new DateTime(year, 12, 31);
                    break;
                case TimePeriodEnum.Annual:
                    StartDate = new DateTime(year, 1, 1);
                    EndDate = new DateTime(year, 12, 31);
                    break;
                // Custom keeps whatever dates are set
            }
        }

        /// <summary>
        /// Determines whether a new Goal can be added.
        /// Requires at least a title and an owner.
        /// </summary>
        private bool CanExecuteAddGoal(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Title)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add Goal command - saves the new Goal to the database.
        /// </summary>
        private async void AddGoalExecuted(object? parameter)
        {
            // Copy targets to the data model
            _goal.Targets = _targets.ToList();

            var id = await TrackerDataManager.Instance.AddStrategicGoal(_goal);
            if (id != Guid.Empty)
            {
                _goal.Id = id;
                NotificationManager.Instance.ShowSuccess("Goal Created", $"Goal '{Title}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create Goal.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the Goal can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateGoal(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update Goal command - saves changes to the database.
        /// </summary>
        private async void UpdateGoalExecuted(object? parameter)
        {
            // Copy targets to the data model
            _goal.Targets = _targets.ToList();

            var success = await TrackerDataManager.Instance.UpdateStrategicGoal(_goal);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("Goal Updated", $"Goal '{Title}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update Goal.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether a Target can be added.
        /// Requires at least a title.
        /// </summary>
        private bool CanExecuteAddTarget(object? obj)
        {
            return !string.IsNullOrWhiteSpace(NewTargetTitle);
        }

        /// <summary>
        /// Adds a new Target to this Goal.
        /// </summary>
        private void AddTargetExecuted(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(NewTargetTitle)) return;

            var target = new Target
            {
                Title = NewTargetTitle,
                TargetValue = NewTargetValue,
                CurrentValue = 0,
                StartingValue = 0,
                Unit = NewTargetUnit,
                Weight = 1.0m,
                SortOrder = _targets.Count
            };

            _targets.Add(target);

            // Track the change
            UpdateChangedValues("@Targets", _targets.Count);

            // Update status properties
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(CompletionPercentage));

            // Clear input fields
            NewTargetTitle = string.Empty;
            NewTargetValue = 100;
            NewTargetUnit = "%";
        }

        /// <summary>
        /// Determines whether a Target can be removed.
        /// Requires a Target to be selected.
        /// </summary>
        private bool CanExecuteRemoveTarget(object? obj)
        {
            return SelectedTarget != null;
        }

        /// <summary>
        /// Removes the selected Target from this Goal.
        /// </summary>
        private void RemoveTargetExecuted(object? parameter)
        {
            if (SelectedTarget == null) return;

            _targets.Remove(SelectedTarget);

            // Track the change
            UpdateChangedValues("@Targets", _targets.Count);

            // Update status properties
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(CompletionPercentage));

            SelectedTarget = null;
        }

        /// <summary>
        /// Tracks property changes for enabling/disabling the update command.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The new value.</param>
        private void UpdateChangedValues(string key, object? value)
        {
            if (value == null)
            {
                _changedProperties.Remove(key);
            }
            else
            {
                _changedProperties[key] = value;
            }
        }

        #endregion
    }
}
