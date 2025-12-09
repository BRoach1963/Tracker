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
    /// ViewModel for creating and editing Key Performance Indicators (KPIs).
    /// 
    /// Key responsibilities:
    /// - Expose KPI properties for data binding
    /// - Provide team member selection for KPI ownership
    /// - Handle target direction configuration (greater/less than)
    /// - Configure on-target and off-target thresholds
    /// - Handle KPI creation and updates via commands
    /// - Track property changes for edit mode
    /// 
    /// Usage:
    /// <code>
    /// var vm = new NewKpiViewModel(callback, new KeyPerformanceIndicator());
    /// var dialog = new AddKPI(vm);
    /// </code>
    /// </summary>
    public class NewKpiViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly KeyPerformanceIndicator _data;
        private readonly bool _inEditMode;

        private ICommand? _addKpiCommand;
        private ICommand? _updateKpiCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<EnumWrapper<TargetDirectionEnum>> _targetDirections = new();
        private ObservableCollection<ObjectiveKeyResult> _availableOkrs = new();

        private TeamMember? _selectedOwner;
        private EnumWrapper<TargetDirectionEnum>? _selectedTargetDirection;
        private ObjectiveKeyResult? _selectedOkr;

        private readonly Dictionary<string, object> _changedProperties = new();

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewKpiViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="data">The KPI data to edit or a new KPI instance.</param>
        /// <param name="edit">True if editing an existing KPI, false for new KPI.</param>
        public NewKpiViewModel(Action? callback, KeyPerformanceIndicator data, bool edit = false) : base(callback)
        {
            _data = data;
            _inEditMode = edit;

            // Set defaults for new KPIs
            if (!_inEditMode)
            {
                _data.LastUpdated = DateTime.Now;
                _data.Value = 0;
                _data.TargetValue = 100;
                _data.TargetDirection = TargetDirectionEnum.GreaterOrEqual;
                _data.OnTargetThresholdPercentage = 5.0;
                _data.OffTargetThresholdPercentage = 10.0;
            }

            LoadEnums();
            LoadTeamMembers();
            LoadOkrs();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            _targetDirections.Clear();
            _availableOkrs.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new KPI to the database.
        /// </summary>
        public ICommand AddKpiCommand => _addKpiCommand ??=
            new TrackerCommand(AddKpiExecuted, CanExecuteAddKpi);

        /// <summary>
        /// Command to update an existing KPI in the database.
        /// </summary>
        public ICommand UpdateKpiCommand => _updateKpiCommand ??=
            new TrackerCommand(UpdateKpiExecuted, CanExecuteUpdateKpi);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying KPI data model.
        /// </summary>
        public KeyPerformanceIndicator Data => _data;

        /// <summary>
        /// Gets whether the ViewModel is in edit mode (true) or add mode (false).
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of available team members for owner selection.
        /// </summary>
        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        /// <summary>
        /// Gets the collection of available target directions (GreaterOrEqual, LessOrEqual).
        /// </summary>
        public ObservableCollection<EnumWrapper<TargetDirectionEnum>> TargetDirections => _targetDirections;

        /// <summary>
        /// Gets the collection of available OKRs to link this KPI to.
        /// </summary>
        public ObservableCollection<ObjectiveKeyResult> AvailableOkrs => _availableOkrs;

        /// <summary>
        /// Gets or sets the selected owner for the KPI.
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
        /// Gets or sets the selected target direction (how to compare value to target).
        /// </summary>
        public EnumWrapper<TargetDirectionEnum>? SelectedTargetDirection
        {
            get => _selectedTargetDirection;
            set
            {
                _selectedTargetDirection = value;
                if (value != null)
                {
                    _data.TargetDirection = value.EnumValue;
                    UpdateChangedValues("@TargetDirection", value.EnumValue);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected OKR that this KPI is linked to.
        /// </summary>
        public ObjectiveKeyResult? SelectedOkr
        {
            get => _selectedOkr;
            set
            {
                _selectedOkr = value;
                _data.OkrId = value?.ObjectiveId ?? 0;
                UpdateChangedValues("@OkrId", _data.OkrId);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the KPI name.
        /// </summary>
        public string Name
        {
            get => _data.Name;
            set
            {
                _data.Name = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Name", value);
            }
        }

        /// <summary>
        /// Gets or sets the KPI description.
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
        /// Gets or sets the current value of the KPI.
        /// </summary>
        public double Value
        {
            get => double.IsNaN(_data.Value) ? 0 : _data.Value;
            set
            {
                _data.Value = value;
                _data.LastUpdated = DateTime.Now; // Auto-update timestamp when value changes
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                RaisePropertyChanged(nameof(LastUpdated));
                UpdateChangedValues("@Value", value);
            }
        }

        /// <summary>
        /// Gets or sets the target value for the KPI.
        /// </summary>
        public double TargetValue
        {
            get => double.IsNaN(_data.TargetValue) ? 0 : _data.TargetValue;
            set
            {
                _data.TargetValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@TargetValue", value);
            }
        }

        /// <summary>
        /// Gets the calculated KPI status (OnTarget, CloseToTarget, OffTarget) - read-only.
        /// </summary>
        public KpiStatusEnum Status => _data.Status;

        /// <summary>
        /// Gets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated => _data.LastUpdated;

        /// <summary>
        /// Gets or sets the on-target threshold percentage.
        /// Values within this percentage of the target are considered "on target".
        /// </summary>
        public double OnTargetThresholdPercentage
        {
            get => _data.OnTargetThresholdPercentage;
            set
            {
                _data.OnTargetThresholdPercentage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@OnTargetThresholdPercentage", value);
            }
        }

        /// <summary>
        /// Gets or sets the off-target threshold percentage.
        /// Values beyond this percentage from the target are considered "off target".
        /// </summary>
        public double OffTargetThresholdPercentage
        {
            get => _data.OffTargetThresholdPercentage;
            set
            {
                _data.OffTargetThresholdPercentage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@OffTargetThresholdPercentage", value);
            }
        }

        /// <summary>
        /// Gets or sets the on-target threshold in absolute units.
        /// </summary>
        public double OnTargetThresholdAbsolute
        {
            get => _data.OnTargetThresholdAbsolute;
            set
            {
                _data.OnTargetThresholdAbsolute = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@OnTargetThresholdAbsolute", value);
            }
        }

        /// <summary>
        /// Gets or sets the off-target threshold in absolute units.
        /// </summary>
        public double OffTargetThresholdAbsolute
        {
            get => _data.OffTargetThresholdAbsolute;
            set
            {
                _data.OffTargetThresholdAbsolute = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@OffTargetThresholdAbsolute", value);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads enum values for dropdown selections.
        /// </summary>
        private void LoadEnums()
        {
            _targetDirections.Clear();
            foreach (TargetDirectionEnum direction in Enum.GetValues(typeof(TargetDirectionEnum)))
            {
                _targetDirections.Add(new EnumWrapper<TargetDirectionEnum>(direction));
            }

            // Set selected target direction
            _selectedTargetDirection = _targetDirections.FirstOrDefault(d => d.EnumValue == _data.TargetDirection);
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
            if (_inEditMode && _data.Owner?.Id > 0)
            {
                _selectedOwner = _teamMembers.FirstOrDefault(t => t.Id == _data.Owner.Id);
            }
        }

        /// <summary>
        /// Loads available OKRs for linking.
        /// </summary>
        private void LoadOkrs()
        {
            _availableOkrs.Clear();
            var okrs = TrackerDataManager.Instance.OKRs;
            if (okrs != null)
            {
                foreach (var okr in okrs)
                {
                    _availableOkrs.Add(okr);
                }
            }

            // Set selected OKR if editing and linked
            if (_inEditMode && _data.OkrId > 0)
            {
                _selectedOkr = _availableOkrs.FirstOrDefault(o => o.ObjectiveId == _data.OkrId);
            }
        }

        /// <summary>
        /// Determines whether a new KPI can be added.
        /// Requires at least a name and an owner.
        /// </summary>
        private bool CanExecuteAddKpi(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add KPI command - saves the new KPI to the database.
        /// </summary>
        private async void AddKpiExecuted(object? parameter)
        {
            var id = await TrackerDataManager.Instance.AddKPI(_data);
            if (id > 0)
            {
                _data.KpiId = id;
                NotificationManager.Instance.ShowSuccess("KPI Created", $"KPI '{Name}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create KPI.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the KPI can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateKpi(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update KPI command - saves changes to the database.
        /// </summary>
        private async void UpdateKpiExecuted(object? parameter)
        {
            var success = await TrackerDataManager.Instance.UpdateKPI(_data);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("KPI Updated", $"KPI '{Name}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update KPI.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
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
