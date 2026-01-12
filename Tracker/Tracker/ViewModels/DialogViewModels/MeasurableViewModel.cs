using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for selecting and linking a measurable (KPI, Project, TaskCollection) to a Target.
    /// </summary>
    public class MeasurableViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("MeasurableVM");
        private readonly Target _target;
        
        private ICommand? _addCommand;

        private ObservableCollection<MeasurableItemWrapper> _availableKpis = new();
        private ObservableCollection<MeasurableItemWrapper> _availableProjects = new();
        private ObservableCollection<MeasurableItemWrapper> _availableTaskCollections = new();

        private MeasurableItemWrapper? _selectedItem;
        private Interfaces.MeasurableType _selectedType = Interfaces.MeasurableType.Metric;
        private AggregationTypeEnum _selectedAggregation = AggregationTypeEnum.Latest;

        private ObservableCollection<EnumWrapper<AggregationTypeEnum>> _aggregationTypes = new();
        private EnumWrapper<AggregationTypeEnum>? _selectedAggregationType;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new MeasurableViewModel.
        /// </summary>
        /// <param name="callback">Callback when dialog closes.</param>
        /// <param name="target">The Target to add the measurable to.</param>
        public MeasurableViewModel(Action? callback, Target target) : base(callback)
        {
            _target = target;
            LoadEnums();
            LoadAvailableMeasurables();
        }

        protected override void Dispose(bool disposing)
        {
            _availableKpis.Clear();
            _availableProjects.Clear();
            _availableTaskCollections.Clear();
            _aggregationTypes.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add the selected measurable to the Target.
        /// </summary>
        public ICommand AddCommand => _addCommand ??=
            new TrackerCommand(AddExecuted, CanAdd);

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the available KPIs.
        /// </summary>
        public ObservableCollection<MeasurableItemWrapper> AvailableKpis => _availableKpis;

        /// <summary>
        /// Gets the available Projects.
        /// </summary>
        public ObservableCollection<MeasurableItemWrapper> AvailableProjects => _availableProjects;

        /// <summary>
        /// Gets the available Task Collections.
        /// </summary>
        public ObservableCollection<MeasurableItemWrapper> AvailableTaskCollections => _availableTaskCollections;

        /// <summary>
        /// Gets the available aggregation types.
        /// </summary>
        public ObservableCollection<EnumWrapper<AggregationTypeEnum>> AggregationTypes => _aggregationTypes;

        /// <summary>
        /// Gets or sets the selected measurable type.
        /// </summary>
        public Interfaces.MeasurableType SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CurrentItems));
            }
        }

        /// <summary>
        /// Gets the current items based on selected type.
        /// </summary>
        public ObservableCollection<MeasurableItemWrapper> CurrentItems
        {
            get
            {
                return SelectedType switch
                {
                    Interfaces.MeasurableType.Metric => _availableKpis,
                    Interfaces.MeasurableType.Project => _availableProjects,
                    Interfaces.MeasurableType.TaskCollection => _availableTaskCollections,
                    _ => _availableKpis
                };
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public MeasurableItemWrapper? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected aggregation type.
        /// </summary>
        public EnumWrapper<AggregationTypeEnum>? SelectedAggregationType
        {
            get => _selectedAggregationType;
            set
            {
                _selectedAggregationType = value;
                if (value != null)
                {
                    _selectedAggregation = value.EnumValue;
                }
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private void LoadEnums()
        {
            _aggregationTypes.Clear();
            foreach (AggregationTypeEnum aggType in Enum.GetValues(typeof(AggregationTypeEnum)))
            {
                _aggregationTypes.Add(new EnumWrapper<AggregationTypeEnum>(aggType));
            }
            _selectedAggregationType = _aggregationTypes.FirstOrDefault(a => a.EnumValue == _selectedAggregation);
        }

        private async void LoadAvailableMeasurables()
        {
            try
            {
                // Load KPIs
                var kpis = await TrackerDataManager.Instance.GetKPIs();
                _availableKpis.Clear();
                foreach (var kpi in kpis.Where(k => !k.IsDeleted))
                {
                    _availableKpis.Add(new MeasurableItemWrapper(kpi.KpiId, kpi.Name, Interfaces.MeasurableType.Metric));
                }

                // Load Projects
                var projects = await TrackerDataManager.Instance.GetProjects();
                _availableProjects.Clear();
                foreach (var project in projects.Where(p => !p.IsDeleted))
                {
                    _availableProjects.Add(new MeasurableItemWrapper(project.ID, project.Name, Interfaces.MeasurableType.Project));
                }

                // TaskCollections would be loaded similarly if the feature exists
                // For now, leave empty
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading measurables: {0}", ex.Message);
            }
        }

        private bool CanAdd(object? obj)
        {
            return SelectedItem != null;
        }

        private void AddExecuted(object? parameter)
        {
            if (SelectedItem == null) return;

            var measurable = new TargetMeasurable
            {
                TargetId = _target.Id,
                MeasurableType = SelectedItem.Type,
                MeasurableId = SelectedItem.Id,
                DisplayName = SelectedItem.Name,
                AggregationType = _selectedAggregation
            };

            _target.Measurables ??= new List<TargetMeasurable>();
            _target.Measurables.Add(measurable);

            NotificationManager.Instance.ShowSuccess("Measurable Added", $"'{SelectedItem.Name}' has been linked to the Target.");

            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        #endregion
    }

    /// <summary>
    /// Wrapper for displaying available measurables in the selection list.
    /// </summary>
    public class MeasurableItemWrapper
    {
        public int Id { get; }
        public string Name { get; }
        public Interfaces.MeasurableType Type { get; }

        public MeasurableItemWrapper(int id, string name, Interfaces.MeasurableType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}

