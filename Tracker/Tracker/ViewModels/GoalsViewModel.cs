using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
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
    /// ViewModel for the Goals page with 3-panel layout.
    /// 
    /// Features:
    /// - Left panel: Goal cards with search/filter
    /// - Top-right panel: Targets for selected Goal
    /// - Bottom-right panel: Target details with linked measurables
    /// - Full CRUD + Duplication support
    /// - Predictive analytics for trajectory visualization
    /// 
    /// Usage:
    /// Bind to GoalsControl.DataContext
    /// </summary>
    public class GoalsViewModel : BaseViewModel, IDisposable
    {
        #region Fields

        private readonly ILogger _logger;
        private ObservableCollection<Goal> _okrs = new();
        private ObservableCollection<Goal> _filteredOkrs = new();
        private Goal? _selectedOkr;
        private Target? _selectedKeyResult;
        private string _searchText = string.Empty;
        private GoalStatus? _statusFilter;
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

        public GoalsViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("GoalsViewModel");
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
            
            if (info.RefreshAll || info.Includes(DataChangeType.Goals) || info.Includes(DataChangeType.OKRs))
            {
                _logger.Info("Refreshing strategic goals due to data change");
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
        public ObservableCollection<Goal> Okrs
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
        public ObservableCollection<Goal> FilteredOkrs
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
        public Goal? SelectedOkr
        {
            get => _selectedOkr;
            set
            {
                _selectedOkr = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedOkr));
                RaisePropertyChanged(nameof(SelectedOkrKeyResults));
                
                // Clear KR selection when OKR changes
                SelectedKeyResult = value?.Targets?.FirstOrDefault();
                
                // Load predictive analytics for the selected OKR
                _ = LoadSelectedOkrAnalyticsAsync();
            }
        }

        /// <summary>
        /// Currently selected Key Result.
        /// </summary>
        public Target? SelectedKeyResult
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
        public GoalStatus? StatusFilter
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
        public ObservableCollection<Target>? SelectedOkrKeyResults =>
            SelectedOkr?.Targets != null 
                ? new ObservableCollection<Target>(SelectedOkr.Targets.OrderBy(kr => kr.SortOrder)) 
                : null;

        #endregion

        #region Statistics Properties

        /// <summary>
        /// Count of On Track OKRs (from full list, not filtered).
        /// </summary>
        public int OnTrackCount => _okrs.Count(o => o.Status == GoalStatus.OnTrack);

        /// <summary>
        /// Count of At Risk OKRs.
        /// </summary>
        public int AtRiskCount => _okrs.Count(o => o.Status == GoalStatus.AtRisk);

        /// <summary>
        /// Count of Off Track OKRs.
        /// </summary>
        public int OffTrackCount => _okrs.Count(o => o.Status == GoalStatus.OffTrack);

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

        private bool CanExecuteOkrAction(object? parameter) => SelectedOkr != null || parameter is Goal;
        private bool CanExecuteKrAction(object? parameter) => SelectedKeyResult != null || parameter is Target;

        private void ExecuteAddOkr(object? parameter)
        {
            // Launch Add OKR dialog
            DialogCommands.LaunchDialogCommand.Execute(DialogType.AddOKR);
        }

        private void ExecuteEditOkr(object? parameter)
        {
            var okr = parameter as Goal ?? SelectedOkr;
            if (okr == null) return;

            // Launch Edit OKR dialog
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.EditOKR, okr));
        }

        private async void ExecuteDuplicateOkr(object? parameter)
        {
            var okr = parameter as Goal ?? SelectedOkr;
            if (okr == null) return;

            try
            {
                // Create duplicate following design rules
                var duplicate = new Goal
                {
                    Title = $"Copy of {okr.Title}",
                    Description = okr.Description,
                    Owner = okr.Owner,
                    TimePeriod = okr.TimePeriod,
                    Year = DateTime.Now.Year,
                    // Set dates to current quarter
                    StartDate = GetCurrentQuarterStart(),
                    EndDate = GetCurrentQuarterEnd(),
                    // Targets are NOT copied - start fresh
                    Targets = new List<Target>()
                };

                var id = await TrackerDataManager.Instance.AddStrategicGoal(duplicate);
                if (id != Guid.Empty)
                {
                    await LoadDataAsync();
                    SelectedOkr = _okrs.FirstOrDefault(o => o.Id == id);
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
            var okr = parameter as Goal ?? SelectedOkr;
            if (okr == null) return;

            if (!MessageBoxHelper.ConfirmDelete(okr.Title, "OKR", "This will also delete all associated Key Results."))
                return;

            try
            {
                var success = await TrackerDataManager.Instance.DeleteStrategicGoal(okr.Id);
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
            var kr = parameter as Target ?? SelectedKeyResult;
            if (kr == null) return;

            // Launch Edit Key Result dialog
            DialogCommands.LaunchDialogCommand.Execute(new EditDialogParameter(DialogType.EditKeyResult, kr));
        }

        private async void ExecuteDuplicateKeyResult(object? parameter)
        {
            var kr = parameter as Target ?? SelectedKeyResult;
            if (kr == null || SelectedOkr == null) return;

            try
            {
                // Create duplicate following design rules
                var duplicate = new Target
                {
                    GoalId = kr.GoalId,
                    Title = $"{kr.Title} (Copy)",
                    Description = kr.Description,
                    TargetValue = kr.TargetValue,
                    StartingValue = kr.StartingValue,
                    CurrentValue = kr.StartingValue, // Reset to starting value
                    Unit = kr.Unit,
                    Weight = kr.Weight,
                    SortOrder = (SelectedOkr.Targets?.Max(k => k.SortOrder) ?? 0) + 1,
                    // Measurables are NOT copied
                    Measurables = new List<TargetMeasurable>()
                };

                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    NotificationManager.Instance.ShowError("Error", "User context not available.");
                    return;
                }

                var connectionFactory = new Services.Data.DapperConnectionFactory();
                var targetRepository = new Services.Data.Repositories.TargetRepository(
                    connectionFactory, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.Data.Repositories.TargetRepository>.Instance);
                
                var created = await targetRepository.CreateAsync(duplicate);
                var id = created?.Id ?? Guid.Empty;
                if (id != Guid.Empty)
                {
                    await LoadDataAsync();

                    // Re-select the OKR to refresh KRs
                    var reloadedOkr = _okrs.FirstOrDefault(o => o.Id == SelectedOkr.Id);
                    SelectedOkr = reloadedOkr;
                    SelectedKeyResult = reloadedOkr?.Targets?.FirstOrDefault(k => k.Id == id);

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
            var kr = parameter as Target ?? SelectedKeyResult;
            if (kr == null) return;

            if (!MessageBoxHelper.ConfirmDelete(kr.Title, "Key Result"))
                return;

            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue)
                {
                    NotificationManager.Instance.ShowError("Error", "User context not available.");
                    return;
                }

                var connectionFactory = new Services.Data.DapperConnectionFactory();
                var targetRepository = new Services.Data.Repositories.TargetRepository(
                    connectionFactory, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.Data.Repositories.TargetRepository>.Instance);
                
                var success = await targetRepository.DeleteAsync(kr.Id, userId.Value);
                if (success)
                {
                    await LoadDataAsync();

                    // Re-select the OKR to refresh KRs
                    var okrId = SelectedOkr?.Id;
                    if (okrId.HasValue)
                    {
                        SelectedOkr = _okrs.FirstOrDefault(o => o.Id == okrId.Value);
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
            if (parameter is not TargetMeasurable measurable) return;

            try
            {
                // TODO: Need to implement delete target measurable repository method
                // For now, log and show a message
                _logger.Warn("Delete target measurable not yet implemented for Id: {0}", measurable.Id);
                NotificationManager.Instance.ShowWarning("Not Implemented", "Removing measurable links is not yet implemented.");
                
                // Once implemented, refresh:
                // await LoadDataAsync();
                // var krId = SelectedKeyResult?.Id;
                // var okrId = SelectedOkr?.Id;
                // if (okrId.HasValue)
                // {
                //     SelectedOkr = _okrs.FirstOrDefault(o => o.Id == okrId.Value);
                //     if (krId.HasValue)
                //     {
                //         SelectedKeyResult = SelectedOkr?.Targets?.FirstOrDefault(k => k.Id == krId.Value);
                //     }
                // }
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
        /// Loads strategic goals from the database and resolves measurable display properties.
        /// </summary>
        public async Task LoadDataAsync()
        {
            _logger.Debug("LoadDataAsync started");
            IsLoading = true;
            try
            {
                var okrs = await TrackerDataManager.Instance.GetStrategicGoals();
                _logger.Info("Loaded {0} strategic goals from database", okrs.Count);
                
                // Resolve measurable display properties for each Key Result
                await ResolveMeasurableDisplayPropertiesAsync(okrs);
                
                // Sort by year descending, then period, then title
                var sortedOkrs = okrs
                    .OrderByDescending(o => o.Year)
                    .ThenByDescending(o => o.TimePeriod)
                    .ThenBy(o => o.Title)
                    .ToList();

                Okrs = new ObservableCollection<Goal>(sortedOkrs);
                _logger.Debug("Okrs property set with {0} items, FilteredOkrs has {1} items", 
                    _okrs.Count, _filteredOkrs.Count);
                
                // Restore selection if possible
                if (SelectedOkr != null)
                {
                    var reselect = _okrs.FirstOrDefault(o => o.Id == SelectedOkr.Id);
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
        /// This requires looking up the actual Metric/Project/TaskCollection entities.
        /// </summary>
        private async Task ResolveMeasurableDisplayPropertiesAsync(IEnumerable<Goal> okrs)
        {
            // Load lookup dictionaries for each measurable type via TrackerDataManager
            var metricList = await TrackerDataManager.Instance.GetMetrics();
            var metrics = metricList.ToDictionary(k => k.Id);
            
            var projectList = await TrackerDataManager.Instance.GetProjects();
            var projects = projectList.ToDictionary(p => p.Id);
            
            // TaskCollection uses int Id, so we can't easily map it with Guid MeasurableId
            // TODO: Update TaskCollection to use Guid Id in the future
            var taskCollectionList = await TrackerDataManager.Instance.GetTaskCollections();
            
            // Resolve each measurable
            foreach (var okr in okrs)
            {
                foreach (var kr in okr.Targets ?? new List<Target>())
                {
                    foreach (var measurable in kr.Measurables ?? new List<TargetMeasurable>())
                    {
                        switch (measurable.MeasurableType)
                        {
                            case "metric":
                                if (metrics.TryGetValue(measurable.MeasurableId, out var metric))
                                {
                                    measurable.DisplayName = metric.Name;
                                    measurable.CurrentProgress = metric.Progress;
                                }
                                break;
                                
                            case "project":
                                if (projects.TryGetValue(measurable.MeasurableId, out var project))
                                {
                                    measurable.DisplayName = project.Name;
                                    measurable.CurrentProgress = project.ProgressPercent;
                                }
                                break;
                                
                            case "task_collection":
                                // TaskCollection uses int Id, lookup by name fallback for now
                                var tc = taskCollectionList.FirstOrDefault(t => t.Name == measurable.DisplayName);
                                if (tc != null)
                                {
                                    measurable.DisplayName = tc.DisplayName;
                                    measurable.CurrentProgress = tc.Progress;
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
                    o.Targets?.Any(kr => kr.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase)) == true);
            }

            // Apply status filter
            if (StatusFilter.HasValue)
            {
                filtered = filtered.Where(o => o.Status == StatusFilter.Value);
            }

            FilteredOkrs = new ObservableCollection<Goal>(filtered);
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
        public void SelectOkr(Goal? okr)
        {
            SelectedOkr = okr;
        }

        /// <summary>
        /// Public method to select a Key Result (for external callers).
        /// </summary>
        public void SelectKeyResult(Target? kr)
        {
            SelectedKeyResult = kr;
        }

        /// <summary>
        /// Sets the status filter.
        /// </summary>
        public void SetStatusFilter(GoalStatus? status)
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
                // TODO: PredictiveAnalyticsViewModel.LoadForOkrAsync expects int id, but Goal.Id is now Guid
                // Need to update PredictiveAnalyticsViewModel to accept Guid IDs
                _logger.Debug("LoadSelectedOkrAnalyticsAsync: Skipping analytics (needs migration to Guid)");
                SelectedOkrAnalytics = null;
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
                // TODO: PredictiveAnalyticsViewModel.LoadForKeyResultAsync expects int id, but Target.Id is now Guid
                // Need to update PredictiveAnalyticsViewModel to accept Guid IDs
                _logger.Debug("LoadSelectedKrAnalyticsAsync: Skipping analytics (needs migration to Guid)");
                SelectedKrAnalytics = null;
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

