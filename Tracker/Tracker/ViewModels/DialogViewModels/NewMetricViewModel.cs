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
    /// ViewModel for creating and editing Metrics.
    /// 
    /// Metrics are standalone quantitative measures that can be:
    /// - Linked to Targets via IMeasurable interface
    /// - Linked to other Metrics (composite Metrics)
    /// - Used for data sources
    /// 
    /// Key responsibilities:
    /// - Expose Metric properties for data binding
    /// - Provide team member selection for Metric ownership
    /// - Handle target direction configuration (greater/less than)
    /// - Handle Metric creation and updates via commands
    /// - Track property changes for edit mode
    /// </summary>
    public class NewMetricViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly Metric _metric;
        private readonly bool _inEditMode;

        private ICommand? _addMetricCommand;
        private ICommand? _updateMetricCommand;

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<EnumWrapper<MetricTargetDirection>> _targetDirections = new();
        private ObservableCollection<EnumWrapper<MetricFrequency>> _frequencies = new();

        private TeamMember? _selectedOwner;
        private EnumWrapper<MetricTargetDirection>? _selectedTargetDirection;
        private EnumWrapper<MetricFrequency>? _selectedFrequency;

        private readonly Dictionary<string, object> _changedProperties = new();

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the NewMetricViewModel.
        /// </summary>
        /// <param name="callback">Optional callback to invoke when dialog closes.</param>
        /// <param name="metric">The Metric data to edit or a new Metric instance.</param>
        /// <param name="edit">True if editing an existing Metric, false for new Metric.</param>
        public NewMetricViewModel(Action? callback, Metric metric, bool edit = false) : base(callback)
        {
            _metric = metric;
            _inEditMode = edit;

            // Set defaults for new Metrics
            if (!_inEditMode)
            {
                _metric.LastUpdatedAt = DateTime.Now;
                _metric.CurrentValue = 0;
                _metric.TargetValue = 100;
                _metric.TargetDirection = MetricTargetDirection.HigherIsBetter;
                _metric.Frequency = MetricFrequency.Monthly;
            }

            LoadEnums();
            LoadTeamMembers();
        }

        /// <summary>
        /// Cleans up resources when the ViewModel is disposed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            _teamMembers.Clear();
            _targetDirections.Clear();
            _frequencies.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a new Metric to the database.
        /// </summary>
        public ICommand AddMetricCommand => _addMetricCommand ??=
            new TrackerCommand(AddMetricExecuted, CanExecuteAddMetric);

        /// <summary>
        /// Command to update an existing Metric in the database.
        /// </summary>
        public ICommand UpdateMetricCommand => _updateMetricCommand ??=
            new TrackerCommand(UpdateMetricExecuted, CanExecuteUpdateMetric);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying Metric data model.
        /// </summary>
        public Metric Data => _metric;

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
        public ObservableCollection<EnumWrapper<MetricTargetDirection>> TargetDirections => _targetDirections;

        /// <summary>
        /// Gets the collection of available frequencies.
        /// </summary>
        public ObservableCollection<EnumWrapper<MetricFrequency>> Frequencies => _frequencies;

        /// <summary>
        /// Gets or sets the selected owner for the Metric.
        /// </summary>
        public TeamMember? SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                _selectedOwner = value;
                if (value != null)
                {
                    _metric.Owner = value;
                    UpdateChangedValues("@OwnerId", value.Id);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected target direction (how to compare value to target).
        /// </summary>
        public EnumWrapper<MetricTargetDirection>? SelectedTargetDirection
        {
            get => _selectedTargetDirection;
            set
            {
                _selectedTargetDirection = value;
                if (value != null)
                {
                    _metric.TargetDirection = value.EnumValue;
                    UpdateChangedValues("@TargetDirection", value.EnumValue);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected frequency.
        /// </summary>
        public EnumWrapper<MetricFrequency>? SelectedFrequency
        {
            get => _selectedFrequency;
            set
            {
                _selectedFrequency = value;
                if (value != null)
                {
                    _metric.Frequency = value.EnumValue;
                    UpdateChangedValues("@Frequency", value.EnumValue);
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the Metric name.
        /// </summary>
        public string Name
        {
            get => _metric.Name;
            set
            {
                _metric.Name = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Name", value);
            }
        }

        /// <summary>
        /// Gets or sets the Metric description.
        /// </summary>
        public string? Description
        {
            get => _metric.Description;
            set
            {
                _metric.Description = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Description", value);
            }
        }

        /// <summary>
        /// Gets or sets the Metric unit (e.g., %, $, count).
        /// </summary>
        public string? Unit
        {
            get => _metric.Unit;
            set
            {
                _metric.Unit = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Unit", value);
            }
        }

        /// <summary>
        /// Gets or sets the Metric category.
        /// </summary>
        public string? Category
        {
            get => _metric.Category;
            set
            {
                _metric.Category = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Category", value);
            }
        }

        /// <summary>
        /// Gets or sets the current value of the Metric.
        /// </summary>
        public decimal Value
        {
            get => _metric.CurrentValue;
            set
            {
                _metric.CurrentValue = value;
                _metric.LastUpdatedAt = DateTime.Now; // Auto-update timestamp when value changes
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                RaisePropertyChanged(nameof(LastUpdated));
                UpdateChangedValues("@Value", value);
            }
        }

        /// <summary>
        /// Gets or sets the target value for the Metric.
        /// </summary>
        public decimal? TargetValue
        {
            get => _metric.TargetValue;
            set
            {
                _metric.TargetValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@TargetValue", value);
            }
        }

        /// <summary>
        /// Gets the calculated Metric status (OnTarget, CloseToTarget, OffTarget) - read-only.
        /// </summary>
        public GoalStatus Status => _metric.Status;

        /// <summary>
        /// Gets the last updated timestamp.
        /// </summary>
        public DateTime? LastUpdated => _metric.LastUpdatedAt;

        /// <summary>
        /// Gets the percentage complete towards target (0-100+).
        /// </summary>
        public decimal PercentComplete => _metric.Progress;

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads enum values for dropdown selections.
        /// </summary>
        private void LoadEnums()
        {
            _targetDirections.Clear();
            foreach (MetricTargetDirection direction in Enum.GetValues(typeof(MetricTargetDirection)))
            {
                _targetDirections.Add(new EnumWrapper<MetricTargetDirection>(direction));
            }

            _frequencies.Clear();
            foreach (MetricFrequency freq in Enum.GetValues(typeof(MetricFrequency)))
            {
                _frequencies.Add(new EnumWrapper<MetricFrequency>(freq));
            }

            // Set selected values
            _selectedTargetDirection = _targetDirections.FirstOrDefault(d => d.EnumValue == _metric.TargetDirection);
            _selectedFrequency = _frequencies.FirstOrDefault(f => f.EnumValue == _metric.Frequency);
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
            if (_inEditMode && _metric.Owner?.Id != Guid.Empty)
            {
                _selectedOwner = _teamMembers.FirstOrDefault(t => t.Id == _metric.Owner.Id);
            }
        }

        /// <summary>
        /// Determines whether a new Metric can be added.
        /// Requires at least a name and an owner.
        /// </summary>
        private bool CanExecuteAddMetric(object? obj)
        {
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (SelectedOwner == null) return false;
            return true;
        }

        /// <summary>
        /// Executes the add Metric command - saves the new Metric to the database.
        /// </summary>
        private async void AddMetricExecuted(object? parameter)
        {
            var id = await TrackerDataManager.Instance.AddMetric(_metric);
            if (id != Guid.Empty)
            {
                _metric.Id = id;
                NotificationManager.Instance.ShowSuccess("Metric Created", $"Metric '{Name}' has been created.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to create Metric.");
            }

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        /// <summary>
        /// Determines whether the Metric can be updated.
        /// Requires at least one property to have changed.
        /// </summary>
        private bool CanExecuteUpdateMetric(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        /// <summary>
        /// Executes the update Metric command - saves changes to the database.
        /// </summary>
        private async void UpdateMetricExecuted(object? parameter)
        {
            var success = await TrackerDataManager.Instance.UpdateMetric(_metric);
            if (success)
            {
                NotificationManager.Instance.ShowSuccess("Metric Updated", $"Metric '{Name}' has been updated.");
            }
            else
            {
                NotificationManager.Instance.ShowError("Error", "Failed to update Metric.");
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
