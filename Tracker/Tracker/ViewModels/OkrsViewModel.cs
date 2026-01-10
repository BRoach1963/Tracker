using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Interfaces;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Analytics;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the OKRs page with 3-panel layout.
    /// 
    /// Features:
    /// - Left panel: OKR cards with search/filter
    /// - Top-right panel: Key Results for selected OKR
    /// - Bottom-right panel: KR details with linked measurables
    /// - Full CRUD + Duplication support
    /// - Predictive analytics for trajectory visualization
    /// 
    /// Usage:
    /// Bind to OkrsControl.DataContext
    /// </summary>
    public class OkrsViewModel : BaseViewModel, IDisposable
    {
        #region Fields

        private readonly ILogger _logger;
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private ObservableCollection<ObjectiveKeyResult> _filteredOkrs = new();
        private ObjectiveKeyResult? _selectedOkr;
        private KeyResult? _selectedKeyResult;
        private string _searchText = string.Empty;
        private ObjectiveStatusEnum? _statusFilter;
        private bool _isLoading;
        private PredictiveAnalyticsViewModel? _selectedOkrAnalytics;
        private PredictiveAnalyticsViewModel? _selectedKrAnalytics;

        // Commands
        private ICommand? _addOkrCommand;
        private ICommand? _editOkrCommand;
        private ICommand? _duplicateOkrCommand;
        private ICommand? _deleteOkrCommand;
        private ICommand? _addKeyResultCommand;
        private ICommand? _editKeyResultCommand;
        private ICommand? _duplicateKeyResultCommand;
        private ICommand? _deleteKeyResultCommand;
        private ICommand? _addMeasurableCommand;
        private ICommand? _removeMeasurableCommand;
        private ICommand? _refreshCommand;

        #endregion

        #region Constructor

        public OkrsViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("OkrsViewModel");
            // Don't load data in constructor - wait for Loaded event
            // Data will be loaded asynchronously to avoid blocking UI

            // Subscribe to data change messages
            DataMessenger.Register(this, OnDataChanged);
        }

        #endregion

        #region IDisposable

        public new void Dispose()
        {
            DataMessenger.Unregister(this);
        }

        #endregion

        #region Message Handlers

        private void OnDataChanged(DataChangeInfo info)
        {
            _logger.Debug("OnDataChanged received. RefreshAll={0}, Types={1}", 
                info.RefreshAll, string.Join(",", info.ChangedTypes));
            
            if (info.RefreshAll || info.Includes(DataChangeType.OKRs))
            {
                _logger.Info("Refreshing OKRs due to data change");
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// All OKRs from the database.
        /// </summary>
        public ObservableCollection<ObjectiveKeyResult> Okrs
        {
            get => _okrs;
            set
            {
                _okrs = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        /// <summary>
        /// Filtered OKRs based on search and status filter.
        /// </summary>
        public ObservableCollection<ObjectiveKeyResult> FilteredOkrs
        {
            get => _filteredOkrs;
            set
            {
                _filteredOkrs = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(OnTrackCount));
                RaisePropertyChanged(nameof(AtRiskCount));
                RaisePropertyChanged(nameof(OffTrackCount));
                RaisePropertyChanged(nameof(TotalCount));
            }
        }

        /// <summary>
        /// Currently selected OKR.
        /// </summary>
        public ObjectiveKeyResult? SelectedOkr
        {
            get => _selectedOkr;
            set
            {
                _selectedOkr = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedOkr));
                RaisePropertyChanged(nameof(SelectedOkrKeyResults));
                
                // Clear KR selection when OKR changes
                SelectedKeyResult = value?.KeyResults?.FirstOrDefault();
                
                // Load predictive analytics for the selected OKR
                _ = LoadSelectedOkrAnalyticsAsync();
            }
        }

        /// <summary>
        /// Currently selected Key Result.
        /// </summary>
        public KeyResult? SelectedKeyResult
        {
            get => _selectedKeyResult;
            set
            {
                _selectedKeyResult = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedKeyResult));
                
                // Load predictive analytics for the selected Key Result
                _ = LoadSelectedKrAnalyticsAsync();
            }
        }

        /// <summary>
        /// Predictive analytics for the selected OKR.
        /// </summary>
        public PredictiveAnalyticsViewModel? SelectedOkrAnalytics
        {
            get => _selectedOkrAnalytics;
            private set
            {
                _selectedOkrAnalytics = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Predictive analytics for the selected Key Result.
        /// </summary>
        public PredictiveAnalyticsViewModel? SelectedKrAnalytics
        {
            get => _selectedKrAnalytics;
            private set
            {
                _selectedKrAnalytics = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Search text for filtering OKRs.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        /// <summary>
        /// Status filter for OKRs.
        /// </summary>
        public ObjectiveStatusEnum? StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        /// <summary>
        /// Whether data is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether an OKR is currently selected.
        /// </summary>
        public bool HasSelectedOkr => SelectedOkr != null;

        /// <summary>
        /// Whether a Key Result is currently selected.
        /// </summary>
        public bool HasSelectedKeyResult => SelectedKeyResult != null;

        /// <summary>
        /// Key Results for the selected OKR.
        /// </summary>
        public ObservableCollection<KeyResult>? SelectedOkrKeyResults =>
            SelectedOkr?.KeyResults != null 
                ? new ObservableCollection<KeyResult>(SelectedOkr.KeyResults.OrderBy(kr => kr.SortOrder)) 
                : null;

        #endregion

        #region Statistics Properties

        /// <summary>
        /// Count of On Track OKRs (from full list, not filtered).
        /// </summary>
        public int OnTrackCount => _okrs.Count(o => o.Status == ObjectiveStatusEnum.OnTrack);

        /// <summary>
        /// Count of At Risk OKRs.
        /// </summary>
        public int AtRiskCount => _okrs.Count(o => o.Status == ObjectiveStatusEnum.AtRisk);

        /// <summary>
        /// Count of Off Track OKRs.
        /// </summary>
        public int OffTrackCount => _okrs.Count(o => o.Status == ObjectiveStatusEnum.OffTrack);

        /// <summary>
        /// Total OKR count.
        /// </summary>
        public int TotalCount => _okrs.Count;

        #endregion

        #region Commands

        public ICommand AddOkrCommand => _addOkrCommand ??= new TrackerCommand(ExecuteAddOkr);
        public ICommand EditOkrCommand => _editOkrCommand ??= new TrackerCommand(ExecuteEditOkr, CanExecuteOkrAction);
        public ICommand DuplicateOkrCommand => _duplicateOkrCommand ??= new TrackerCommand(ExecuteDuplicateOkr, CanExecuteOkrAction);
        public ICommand DeleteOkrCommand => _deleteOkrCommand ??= new TrackerCommand(ExecuteDeleteOkr, CanExecuteOkrAction);
        public ICommand AddKeyResultCommand => _addKeyResultCommand ??= new TrackerCommand(ExecuteAddKeyResult, CanExecuteOkrAction);
        public ICommand EditKeyResultCommand => _editKeyResultCommand ??= new TrackerCommand(ExecuteEditKeyResult, CanExecuteKrAction);
        public ICommand DuplicateKeyResultCommand => _duplicateKeyResultCommand ??= new TrackerCommand(ExecuteDuplicateKeyResult, CanExecuteKrAction);
        public ICommand DeleteKeyResultCommand => _deleteKeyResultCommand ??= new TrackerCommand(ExecuteDeleteKeyResult, CanExecuteKrAction);
        public ICommand AddMeasurableCommand => _addMeasurableCommand ??= new TrackerCommand(ExecuteAddMeasurable, CanExecuteKrAction);
        public ICommand RemoveMeasurableCommand => _removeMeasurableCommand ??= new TrackerCommand(ExecuteRemoveMeasurable);
        public ICommand RefreshCommand => _refreshCommand ??= new TrackerCommand(async _ => await LoadDataAsync());

        #endregion

        #region Command Implementations

        private bool CanExecuteOkrAction(object? parameter) => SelectedOkr != null || parameter is ObjectiveKeyResult;
        private bool CanExecuteKrAction(object? parameter) => SelectedKeyResult != null || parameter is KeyResult;

        private void ExecuteAddOkr(object? parameter)
        {
            // Launch Add OKR dialog
            DialogCommands.LaunchDialogCommand.Execute(DialogType.AddOKR);
        }

        private void ExecuteEditOkr(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? SelectedOkr;
            if (okr == null) return;

            // Launch Edit OKR dialog
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.EditOKR, okr));
        }

        private async void ExecuteDuplicateOkr(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? SelectedOkr;
            if (okr == null) return;

            try
            {
                // Create duplicate following design rules
                var duplicate = new ObjectiveKeyResult
                {
                    Title = $"Copy of {okr.Title}",
                    Description = okr.Description,
                    Owner = okr.Owner,
                    TimePeriod = okr.TimePeriod,
                    Year = DateTime.Now.Year,
                    // Set dates to current quarter
                    StartDate = GetCurrentQuarterStart(),
                    EndDate = GetCurrentQuarterEnd(),
                    // Key Results are NOT copied - start fresh
                    KeyResults = new List<KeyResult>()
                };

                var id = await TrackerDbManager.Instance!.AddOKRAsync(duplicate);
                if (id > 0)
                {
                    await LoadDataAsync();
                    SelectedOkr = _okrs.FirstOrDefault(o => o.ObjectiveId == id);
                    NotificationManager.Instance.ShowSuccess("OKR Duplicated", $"'{duplicate.Title}' created successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to duplicate OKR");
                NotificationManager.Instance.ShowError("Error", "Failed to duplicate OKR.");
            }
        }

        private async void ExecuteDeleteOkr(object? parameter)
        {
            var okr = parameter as ObjectiveKeyResult ?? SelectedOkr;
            if (okr == null) return;

            if (!MessageBoxHelper.ConfirmDelete(okr.Title, "OKR", "This will also delete all associated Key Results."))
                return;

            try
            {
                var success = await TrackerDbManager.Instance!.DeleteOKRAsync(okr.ObjectiveId);
                if (success)
                {
                    await LoadDataAsync();
                    SelectedOkr = null;
                    NotificationManager.Instance.ShowSuccess("OKR Deleted", $"'{okr.Title}' has been deleted.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to delete OKR");
                NotificationManager.Instance.ShowError("Error", "Failed to delete OKR.");
            }
        }

        private void ExecuteAddKeyResult(object? parameter)
        {
            if (SelectedOkr == null) return;

            // Launch Add Key Result dialog (TODO: create this dialog)
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.AddKeyResult, SelectedOkr));
        }

        private void ExecuteEditKeyResult(object? parameter)
        {
            var kr = parameter as KeyResult ?? SelectedKeyResult;
            if (kr == null) return;

            // Launch Edit Key Result dialog
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.EditKeyResult, kr));
        }

        private async void ExecuteDuplicateKeyResult(object? parameter)
        {
            var kr = parameter as KeyResult ?? SelectedKeyResult;
            if (kr == null || SelectedOkr == null) return;

            try
            {
                // Create duplicate following design rules
                var duplicate = new KeyResult
                {
                    OkrId = kr.OkrId,
                    Title = $"{kr.Title} (Copy)",
                    Description = kr.Description,
                    TargetValue = kr.TargetValue,
                    StartingValue = kr.StartingValue,
                    CurrentValue = kr.StartingValue, // Reset to starting value
                    Unit = kr.Unit,
                    Weight = kr.Weight,
                    TargetDirection = kr.TargetDirection,
                    SortOrder = (SelectedOkr.KeyResults?.Max(k => k.SortOrder) ?? 0) + 1,
                    // Measurables are NOT copied
                    Measurables = new List<KeyResultMeasurable>()
                };

                var id = await TrackerDbManager.Instance!.AddKeyResultAsync(duplicate);
                if (id > 0)
                {
                    await LoadDataAsync();

                    // Re-select the OKR to refresh KRs
                    var reloadedOkr = _okrs.FirstOrDefault(o => o.ObjectiveId == SelectedOkr.ObjectiveId);
                    SelectedOkr = reloadedOkr;
                    SelectedKeyResult = reloadedOkr?.KeyResults?.FirstOrDefault(k => k.Id == id);

                    NotificationManager.Instance.ShowSuccess("Key Result Duplicated", $"'{duplicate.Title}' created.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to duplicate Key Result");
                NotificationManager.Instance.ShowError("Error", "Failed to duplicate Key Result.");
            }
        }

        private async void ExecuteDeleteKeyResult(object? parameter)
        {
            var kr = parameter as KeyResult ?? SelectedKeyResult;
            if (kr == null) return;

            if (!MessageBoxHelper.ConfirmDelete(kr.Title, "Key Result"))
                return;

            try
            {
                var success = await TrackerDbManager.Instance!.DeleteKeyResultAsync(kr.Id);
                if (success)
                {
                    await LoadDataAsync();

                    // Re-select the OKR to refresh KRs
                    var okrId = SelectedOkr?.ObjectiveId;
                    if (okrId.HasValue)
                    {
                        SelectedOkr = _okrs.FirstOrDefault(o => o.ObjectiveId == okrId.Value);
                    }

                    NotificationManager.Instance.ShowSuccess("Key Result Deleted", $"'{kr.Title}' has been deleted.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to delete Key Result");
                NotificationManager.Instance.ShowError("Error", "Failed to delete Key Result.");
            }
        }

        private void ExecuteAddMeasurable(object? parameter)
        {
            if (SelectedKeyResult == null) return;

            // Launch Add Measurable dialog (TODO: create this dialog)
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.AddMeasurable, SelectedKeyResult));
        }

        private async void ExecuteRemoveMeasurable(object? parameter)
        {
            if (parameter is not KeyResultMeasurable measurable) return;

            try
            {
                var success = await TrackerDbManager.Instance!.DeleteKeyResultMeasurableAsync(measurable.Id);
                if (success)
                {
                    await LoadDataAsync();

                    // Refresh selection
                    var krId = SelectedKeyResult?.Id;
                    var okrId = SelectedOkr?.ObjectiveId;
                    if (okrId.HasValue)
                    {
                        SelectedOkr = _okrs.FirstOrDefault(o => o.ObjectiveId == okrId.Value);
                        if (krId.HasValue)
                        {
                            SelectedKeyResult = SelectedOkr?.KeyResults?.FirstOrDefault(k => k.Id == krId.Value);
                        }
                    }

                    NotificationManager.Instance.ShowSuccess("Measurable Removed", "Link has been removed.");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to remove measurable");
                NotificationManager.Instance.ShowError("Error", "Failed to remove measurable link.");
            }
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// Loads OKRs from the database and resolves measurable display properties.
        /// </summary>
        public async Task LoadDataAsync()
        {
            _logger.Debug("LoadDataAsync started");
            IsLoading = true;
            try
            {
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                _logger.Info("Loaded {0} OKRs from database", okrs.Count);
                
                // Resolve measurable display properties for each Key Result
                await ResolveMeasurableDisplayPropertiesAsync(okrs);
                
                // Sort by year descending, then period, then title
                var sortedOkrs = okrs
                    .OrderByDescending(o => o.Year)
                    .ThenByDescending(o => o.TimePeriod)
                    .ThenBy(o => o.Title)
                    .ToList();

                Okrs = new ObservableCollection<ObjectiveKeyResult>(sortedOkrs);
                _logger.Debug("Okrs property set with {0} items, FilteredOkrs has {1} items", 
                    _okrs.Count, _filteredOkrs.Count);
                
                // Restore selection if possible
                if (SelectedOkr != null)
                {
                    var reselect = _okrs.FirstOrDefault(o => o.ObjectiveId == SelectedOkr.ObjectiveId);
                    SelectedOkr = reselect;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load OKRs");
                NotificationManager.Instance.ShowError("Error", "Failed to load OKRs from database.");
            }
            finally
            {
                IsLoading = false;
                _logger.Debug("LoadDataAsync completed");
            }
        }
        
        /// <summary>
        /// Resolves the DisplayName, CurrentProgress, and CurrentDisplayValue for each measurable.
        /// This requires looking up the actual KPI/Project/TaskCollection entities.
        /// </summary>
        private async Task ResolveMeasurableDisplayPropertiesAsync(IEnumerable<ObjectiveKeyResult> okrs)
        {
            // Load lookup dictionaries for each measurable type via TrackerDataManager
            var kpiList = await TrackerDataManager.Instance.GetKPIs();
            var kpis = kpiList.ToDictionary(k => k.KpiId);
            
            var projectList = await TrackerDataManager.Instance.GetProjects();
            var projects = projectList.ToDictionary(p => p.ID);
            
            var taskCollectionList = await TrackerDataManager.Instance.GetTaskCollections();
            var taskCollections = taskCollectionList.ToDictionary(tc => tc.Id);
            
            // Resolve each measurable
            foreach (var okr in okrs)
            {
                foreach (var kr in okr.KeyResults ?? new List<KeyResult>())
                {
                    foreach (var measurable in kr.Measurables ?? new List<KeyResultMeasurable>())
                    {
                        switch (measurable.MeasurableType)
                        {
                            case MeasurableType.Metric:
                                if (kpis.TryGetValue(measurable.MeasurableId, out var kpi))
                                {
                                    measurable.DisplayName = kpi.DisplayName;
                                    measurable.CurrentProgress = kpi.Progress;
                                    measurable.CurrentDisplayValue = kpi.DisplayValue;
                                }
                                break;
                                
                            case MeasurableType.Project:
                                if (projects.TryGetValue(measurable.MeasurableId, out var project))
                                {
                                    measurable.DisplayName = project.DisplayName;
                                    measurable.CurrentProgress = project.Progress;
                                    measurable.CurrentDisplayValue = project.DisplayValue;
                                }
                                break;
                                
                            case MeasurableType.TaskCollection:
                                if (taskCollections.TryGetValue(measurable.MeasurableId, out var tc))
                                {
                                    measurable.DisplayName = tc.DisplayName;
                                    measurable.CurrentProgress = tc.Progress;
                                    measurable.CurrentDisplayValue = tc.DisplayValue;
                                }
                                break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Filtering

        private void ApplyFilters()
        {
            var filtered = _okrs.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(o =>
                    o.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    o.Description.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    o.Owner?.FullName?.Contains(search, StringComparison.InvariantCultureIgnoreCase) == true ||
                    o.KeyResults?.Any(kr => kr.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase)) == true);
            }

            // Apply status filter
            if (StatusFilter.HasValue)
            {
                filtered = filtered.Where(o => o.Status == StatusFilter.Value);
            }

            FilteredOkrs = new ObservableCollection<ObjectiveKeyResult>(filtered);
        }

        #endregion

        #region Helper Methods

        private static DateTime GetCurrentQuarterStart()
        {
            var now = DateTime.Today;
            var quarter = (now.Month - 1) / 3 + 1;
            return new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
        }

        private static DateTime GetCurrentQuarterEnd()
        {
            var start = GetCurrentQuarterStart();
            return start.AddMonths(3).AddDays(-1);
        }

        /// <summary>
        /// Public method to select an OKR (for external callers).
        /// </summary>
        public void SelectOkr(ObjectiveKeyResult? okr)
        {
            SelectedOkr = okr;
        }

        /// <summary>
        /// Public method to select a Key Result (for external callers).
        /// </summary>
        public void SelectKeyResult(KeyResult? kr)
        {
            SelectedKeyResult = kr;
        }

        /// <summary>
        /// Sets the status filter.
        /// </summary>
        public void SetStatusFilter(ObjectiveStatusEnum? status)
        {
            StatusFilter = status;
        }

        #endregion

        #region Predictive Analytics

        private async Task LoadSelectedOkrAnalyticsAsync()
        {
            if (SelectedOkr == null)
            {
                SelectedOkrAnalytics = null;
                return;
            }

            try
            {
                var analytics = new PredictiveAnalyticsViewModel();
                await analytics.LoadForOkrAsync(
                    SelectedOkr.ObjectiveId,
                    SelectedOkr.Title,
                    SelectedOkr.StartDate,
                    SelectedOkr.EndDate);
                SelectedOkrAnalytics = analytics;
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to load OKR analytics: {0}", ex.Message);
                SelectedOkrAnalytics = null;
            }
        }

        private async Task LoadSelectedKrAnalyticsAsync()
        {
            if (SelectedKeyResult == null)
            {
                SelectedKrAnalytics = null;
                return;
            }

            try
            {
                var analytics = new PredictiveAnalyticsViewModel();
                // Use parent OKR dates for the Key Result
                var parentOkr = SelectedOkr;
                await analytics.LoadForKeyResultAsync(
                    SelectedKeyResult.Id,
                    SelectedKeyResult.Title,
                    parentOkr?.StartDate,
                    parentOkr?.EndDate);
                SelectedKrAnalytics = analytics;
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to load KR analytics: {0}", ex.Message);
                SelectedKrAnalytics = null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for passing edit dialog parameters.
    /// </summary>
    public class EditDialogParameter
    {
        public DialogType DialogType { get; }
        public object Entity { get; }

        public EditDialogParameter(DialogType dialogType, object entity)
        {
            DialogType = dialogType;
            Entity = entity;
        }
    }
}

