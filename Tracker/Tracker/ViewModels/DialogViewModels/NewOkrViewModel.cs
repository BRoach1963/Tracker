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
    /// ViewModel for creating and editing Objectives and Key Results (OKRs).
    /// 
    /// Key Results are now separate entities (not KPIs) that belong to OKRs.
    /// Key Results can optionally link to KPIs, Projects, or TaskCollections via IMeasurable.
    /// 
    /// Key responsibilities:
    /// - Expose OKR properties for data binding
    /// - Provide team member selection for OKR ownership
    /// - Manage Key Results (inline editing within OKR)
    /// - Handle OKR creation and updates via commands
    /// - Track property changes for edit mode
    /// </summary>
    public class NewOkrViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ObjectiveKeyResult _data;
        private readonly bool _inEditMode;

        private ICommand? _addOkrCommand;
        private ICommand? _updateOkrCommand;
        private ICommand? _addKeyResultCommand;
        private ICommand? _removeKeyResultCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<Project> _availableProjects = new();
        private ObservableCollection<KeyResult> _keyResults = new();
        private ObservableCollection<EnumWrapper<TimePeriodEnum>> _timePeriods = new();

        private TeamMember? _selectedOwner;
        private Project? _selectedProject;
        private KeyResult? _selectedKeyResult;
        private EnumWrapper<TimePeriodEnum>? _selectedTimePeriod;

        private readonly Dictionary<string, object> _changedProperties = new();

        // New Key Result input fields
        private string _newKrTitle = string.Empty;
        private decimal _newKrTargetValue = 100;
        private string _newKrUnit = "%";

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewOkrViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="data">The OKR data to edit or a new OKR instance.</param>
        /// <param name="edit">True if editing an existing OKR, false for new OKR.</param>
        public NewOkrViewModel(Action? callback, ObjectiveKeyResult data, bool edit = false) : base(callback)
        {
            _data = data;
            _inEditMode = edit;

            // Set defaults for new OKRs
            if (!_inEditMode)
            {
                _data.StartDate = DateTime.Now;
                _data.EndDate = DateTime.Now.AddMonths(3); // Default quarter duration
                _data.TimePeriod = TimePeriodEnum.Q1;
                _data.Year = DateTime.Now.Year;
            }

            LoadEnums();
            LoadTeamMembers();
            LoadProjects();
            LoadKeyResults();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            _availableProjects.Clear();
            _keyResults.Clear();
            _timePeriods.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new OKR to the database.
        /// </summary>
        public ICommand AddOkrCommand => _addOkrCommand ??=
            new TrackerCommand(AddOkrExecuted, CanExecuteAddOkr);

        /// <summary>
        /// Command to update an existing OKR in the database.
        /// </summary>
        public ICommand UpdateOkrCommand => _updateOkrCommand ??=
            new TrackerCommand(UpdateOkrExecuted, CanExecuteUpdateOkr);

        /// <summary>
        /// Command to add a new Key Result to this OKR.
        /// </summary>
        public ICommand AddKeyResultCommand => _addKeyResultCommand ??=
            new TrackerCommand(AddKeyResultExecuted, CanExecuteAddKeyResult);

        /// <summary>
        /// Command to remove a Key Result from this OKR.
        /// </summary>
        public ICommand RemoveKeyResultCommand => _removeKeyResultCommand ??=
            new TrackerCommand(RemoveKeyResultExecuted, CanExecuteRemoveKeyResult);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying OKR data model.
        /// </summary>
        public ObjectiveKeyResult Data => _data;

        /// <summary>
        /// Gets whether the ViewModel is in edit mode (true) or add mode (false).
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of available team members for owner selection.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        /// <summary>
        /// Gets the collection of available projects to link this OKR to.
        /// </summary>
        public ObservableCollection<Project> AvailableProjects => _availableProjects;

        /// <summary>
        /// Gets the collection of Key Results for this OKR.
        /// </summary>
        public ObservableCollection<KeyResult> KeyResults => _keyResults;

        /// <summary>
        /// Gets the collection of available time periods.
        /// </summary>
        public ObservableCollection<EnumWrapper<TimePeriodEnum>> TimePeriods => _timePeriods;

        /// <summary>
        /// Gets or sets the selected owner for the OKR.
        /// </summary>
        public TeamMember? SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                _selectedOwner = value;
                if (value != null)
                {
                    _data.Owner = value;
                    UpdateChangedValues("@OwnerId", value.Id);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected project that this OKR is linked to.
        /// </summary>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value;
                _data.ProjectId = value?.ID;
                UpdateChangedValues("@ProjectId", _data.ProjectId);
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
                    _data.TimePeriod = value.EnumValue;
                    UpdateChangedValues("@TimePeriod", value.EnumValue);
                    UpdateDatesFromTimePeriod();
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected Key Result in the list.
        /// </summary>
        public KeyResult? SelectedKeyResult
        {
            get => _selectedKeyResult;
            set
            {
                _selectedKeyResult = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the title for a new Key Result.
        /// </summary>
        public string NewKrTitle
        {
            get => _newKrTitle;
            set
            {
                _newKrTitle = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the target value for a new Key Result.
        /// </summary>
        public decimal NewKrTargetValue
        {
            get => _newKrTargetValue;
            set
            {
                _newKrTargetValue = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the unit for a new Key Result.
        /// </summary>
        public string NewKrUnit
        {
            get => _newKrUnit;
            set
            {
                _newKrUnit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the OKR title (the objective statement).
        /// </summary>
        public string Title
        {
            get => _data.Title;
            set
            {
                _data.Title = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Title", value);
            }
        }

        /// <summary>
        /// Gets or sets the OKR description.
        /// </summary>
        public string Description
        {
            get => _data.Description;
            set
            {
                _data.Description = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Description", value);
            }
        }

        /// <summary>
        /// Gets or sets the year for the OKR.
        /// </summary>
        public int Year
        {
            get => _data.Year;
            set
            {
                _data.Year = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Year", value);
                UpdateDatesFromTimePeriod();
            }
        }

        /// <summary>
        /// Gets or sets the OKR start date.
        /// </summary>
        public DateTime StartDate
        {
            get => _data.StartDate;
            set
            {
                _data.StartDate = value;
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
            get => _data.StartDate == DateTime.MinValue ? "MM/DD/YYYY" : _data.StartDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _data.StartDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(StartDate));
                    UpdateChangedValues("@StartDate", date);
                }
            }
        }

        /// <summary>
        /// Gets or sets the OKR end date.
        /// </summary>
        public DateTime EndDate
        {
            get => _data.EndDate;
            set
            {
                _data.EndDate = value;
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
            get => _data.EndDate == DateTime.MinValue ? "MM/DD/YYYY" : _data.EndDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParse(value, out DateTime date))
                {
                    _data.EndDate = date;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(EndDate));
                    UpdateChangedValues("@EndDate", date);
                }
            }
        }

        /// <summary>
        /// Gets the calculated OKR status (OnTrack, AtRisk, OffTrack) based on Key Results - read-only.
        /// </summary>
        public ObjectiveStatusEnum Status => _data.Status;

        /// <summary>
        /// Gets the completion percentage based on Key Results - read-only.
        /// </summary>
        public double CompletionPercentage => _data.CompletionPercentage;

        /// <summary>
        /// Gets the time period display string.
        /// </summary>
        public string TimePeriodDisplay => _data.TimePeriodDisplay;

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

            _selectedTimePeriod = _timePeriods.FirstOrDefault(p => p.EnumValue == _data.TimePeriod);
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
            if (_inEditMode && _data.Owner?.Id != Guid.Empty)
            {
                _selectedOwner = _teamMembers.FirstOrDefault(t => t.Id == _data.Owner.Id);
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
            if (_inEditMode && _data.ProjectId.HasValue && _data.ProjectId > 0)
            {
                _selectedProject = _availableProjects.FirstOrDefault(p => p.ID == _data.ProjectId);
            }
        }

        /// <summary>
        /// Loads Key Results already linked to this OKR.
        /// </summary>
        private void LoadKeyResults()
        {
            _keyResults.Clear();
            if (_data.KeyResults != null)
            {
                foreach (var kr in _data.KeyResults)
                {
                    _keyResults.Add(kr);
                }
            }
        }

        /// <summary>
        /// Updates start/end dates based on selected time period.
        /// </summary>
        private void UpdateDatesFromTimePeriod()
        {
            if (_selectedTimePeriod == null) return;

            var year = _data.Year;
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
        /// Determines whether a new OKR can be added.
        /// Requires at least a title and an owner.
        /// </summary>
        private bool CanExecuteAddOkr(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Title)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add OKR command - saves the new OKR to the database.
        /// </summary>
        private async void AddOkrExecuted(object? parameter)
        {
            // Copy key results to the data model
            _data.KeyResults = _keyResults.ToList();

            var id = await TrackerDataManager.Instance.AddOKR(_data);
            if (id > 0)
            {
                _data.ObjectiveId = id;
                NotificationManager.Instance.ShowSuccess("OKR Created", $"Objective '{Title}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create OKR.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the OKR can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateOkr(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update OKR command - saves changes to the database.
        /// </summary>
        private async void UpdateOkrExecuted(object? parameter)
        {
            // Copy key results to the data model
            _data.KeyResults = _keyResults.ToList();

            var success = await TrackerDataManager.Instance.UpdateOKR(_data);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("OKR Updated", $"Objective '{Title}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update OKR.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether a Key Result can be added.
        /// Requires at least a title.
        /// </summary>
        private bool CanExecuteAddKeyResult(object? obj)
        {
            return !string.IsNullOrWhiteSpace(NewKrTitle);
        }

        /// <summary>
        /// Adds a new Key Result to this OKR.
        /// </summary>
        private void AddKeyResultExecuted(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(NewKrTitle)) return;

            var kr = new KeyResult
            {
                Title = NewKrTitle,
                TargetValue = NewKrTargetValue,
                CurrentValue = 0,
                StartingValue = 0,
                Unit = NewKrUnit,
                Weight = 1.0m,
                SortOrder = _keyResults.Count,
                TargetDirection = TargetDirectionEnum.GreaterOrEqual
            };

            _keyResults.Add(kr);

            // Track the change
            UpdateChangedValues("@KeyResults", _keyResults.Count);

            // Update status properties
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(CompletionPercentage));

            // Clear input fields
            NewKrTitle = string.Empty;
            NewKrTargetValue = 100;
            NewKrUnit = "%";
        }

        /// <summary>
        /// Determines whether a Key Result can be removed.
        /// Requires a Key Result to be selected.
        /// </summary>
        private bool CanExecuteRemoveKeyResult(object? obj)
        {
            return SelectedKeyResult != null;
        }

        /// <summary>
        /// Removes the selected Key Result from this OKR.
        /// </summary>
        private void RemoveKeyResultExecuted(object? parameter)
        {
            if (SelectedKeyResult == null) return;

            _keyResults.Remove(SelectedKeyResult);

            // Track the change
            UpdateChangedValues("@KeyResults", _keyResults.Count);

            // Update status properties
            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(CompletionPercentage));

            SelectedKeyResult = null;
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
