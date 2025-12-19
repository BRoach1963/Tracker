using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Interfaces;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for creating and editing Key Results.
    /// Key Results belong to OKRs and can have linked Measurables (KPIs, Projects, TaskCollections).
    /// </summary>
    public class KeyResultViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly KeyResult _data;
        private readonly int _okrId;
        private readonly bool _inEditMode;

        private ICommand? _saveCommand;
        private ICommand? _addMeasurableCommand;
        private ICommand? _removeMeasurableCommand;

        private ObservableCollection<EnumWrapper<TargetDirectionEnum>> _targetDirections = new();
        private EnumWrapper<TargetDirectionEnum>? _selectedTargetDirection;

        private ObservableCollection<KeyResultMeasurable> _measurables = new();
        private KeyResultMeasurable? _selectedMeasurable;

        private readonly Dictionary<string, object> _changedProperties = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of KeyResultViewModel.
        /// </summary>
        /// <param name="callback">Callback when dialog closes.</param>
        /// <param name="data">The Key Result to edit or new instance.</param>
        /// <param name="okrId">The parent OKR ID.</param>
        /// <param name="edit">True if editing existing Key Result.</param>
        public KeyResultViewModel(Action? callback, KeyResult data, int okrId, bool edit = false) : base(callback)
        {
            _data = data;
            _okrId = okrId;
            _inEditMode = edit;

            if (!_inEditMode)
            {
                _data.OkrId = okrId;
                _data.TargetValue = 100;
                _data.StartingValue = 0;
                _data.CurrentValue = 0;
                _data.Unit = "%";
                _data.Weight = 1.0m;
                _data.TargetDirection = TargetDirectionEnum.GreaterOrEqual;
            }

            LoadEnums();
            LoadMeasurables();
        }

        protected override void Dispose(bool disposing)
        {
            _targetDirections.Clear();
            _measurables.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to save the Key Result.
        /// </summary>
        public ICommand SaveCommand => _saveCommand ??=
            new TrackerCommand(SaveExecuted, CanSave);

        /// <summary>
        /// Command to add a measurable to this Key Result.
        /// </summary>
        public ICommand AddMeasurableCommand => _addMeasurableCommand ??=
            new TrackerCommand(AddMeasurableExecuted);

        /// <summary>
        /// Command to remove a measurable from this Key Result.
        /// </summary>
        public ICommand RemoveMeasurableCommand => _removeMeasurableCommand ??=
            new TrackerCommand(RemoveMeasurableExecuted, CanRemoveMeasurable);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the underlying Key Result data.
        /// </summary>
        public KeyResult Data => _data;

        /// <summary>
        /// Gets whether in edit mode.
        /// </summary>
        public bool InEditMode => _inEditMode;

        /// <summary>
        /// Gets the collection of target direction options.
        /// </summary>
        public ObservableCollection<EnumWrapper<TargetDirectionEnum>> TargetDirections => _targetDirections;

        /// <summary>
        /// Gets or sets the selected target direction.
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
        /// Gets the collection of linked measurables.
        /// </summary>
        public ObservableCollection<KeyResultMeasurable> Measurables => _measurables;

        /// <summary>
        /// Gets or sets the selected measurable.
        /// </summary>
        public KeyResultMeasurable? SelectedMeasurable
        {
            get => _selectedMeasurable;
            set
            {
                _selectedMeasurable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the Key Result title.
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
        /// Gets or sets the Key Result description.
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
        /// Gets or sets the starting value.
        /// </summary>
        public decimal StartingValue
        {
            get => _data.StartingValue;
            set
            {
                _data.StartingValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@StartingValue", value);
            }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public decimal CurrentValue
        {
            get => _data.CurrentValue;
            set
            {
                _data.CurrentValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@CurrentValue", value);
            }
        }

        /// <summary>
        /// Gets or sets the target value.
        /// </summary>
        public decimal TargetValue
        {
            get => _data.TargetValue;
            set
            {
                _data.TargetValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Progress));
                RaisePropertyChanged(nameof(Status));
                UpdateChangedValues("@TargetValue", value);
            }
        }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        public string Unit
        {
            get => _data.Unit;
            set
            {
                _data.Unit = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Unit", value);
            }
        }

        /// <summary>
        /// Gets or sets the weight for averaging.
        /// </summary>
        public decimal Weight
        {
            get => _data.Weight;
            set
            {
                _data.Weight = value;
                RaisePropertyChanged();
                UpdateChangedValues("@Weight", value);
            }
        }

        /// <summary>
        /// Gets the calculated progress.
        /// </summary>
        public decimal Progress => _data.Progress;

        /// <summary>
        /// Gets the calculated status.
        /// </summary>
        public KpiStatusEnum Status => _data.Status;

        #endregion

        #region Private Methods

        private void LoadEnums()
        {
            _targetDirections.Clear();
            foreach (TargetDirectionEnum direction in Enum.GetValues(typeof(TargetDirectionEnum)))
            {
                _targetDirections.Add(new EnumWrapper<TargetDirectionEnum>(direction));
            }
            _selectedTargetDirection = _targetDirections.FirstOrDefault(d => d.EnumValue == _data.TargetDirection);
        }

        private void LoadMeasurables()
        {
            _measurables.Clear();
            if (_data.Measurables != null)
            {
                foreach (var m in _data.Measurables)
                {
                    _measurables.Add(m);
                }
            }
        }

        private bool CanSave(object? obj)
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private void SaveExecuted(object? parameter)
        {
            _data.Measurables = _measurables.ToList();

            // Key Results are saved through the parent OKR
            // This dialog just prepares the data
            NotificationManager.Instance.ShowSuccess(
                _inEditMode ? "Key Result Updated" : "Key Result Created", 
                $"'{Title}' will be saved with the OKR.");

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        private void AddMeasurableExecuted(object? parameter)
        {
            // Launch the AddMeasurable dialog to select a KPI, Project, or TaskCollection
            DialogManager.Instance.LaunchDialogByType(
                Common.Enums.DialogType.AddMeasurable,
                true,
                () =>
                {
                    // Refresh measurables list after dialog closes
                    LoadMeasurables();
                    RaisePropertyChanged(nameof(Measurables));
                },
                _data);
        }

        private bool CanRemoveMeasurable(object? obj)
        {
            return SelectedMeasurable != null;
        }

        private void RemoveMeasurableExecuted(object? parameter)
        {
            if (SelectedMeasurable == null) return;

            _measurables.Remove(SelectedMeasurable);
            UpdateChangedValues("@Measurables", _measurables.Count);
            SelectedMeasurable = null;
        }

        private void UpdateChangedValues(string key, object? value)
        {
            if (value == null)
                _changedProperties.Remove(key);
            else
                _changedProperties[key] = value;
        }

        #endregion
    }
}


