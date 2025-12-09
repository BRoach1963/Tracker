using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.Managers;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard view, providing summary statistics and chart data.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        #region Fields

        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<OneOnOne> _oneOnOnes = new();
        private ObservableCollection<IndividualTask> _tasks = new();
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private ObservableCollection<Project> _projects = new();

        // Summary statistics
        private int _totalTeamMembers;
        private int _totalOneOnOnes;
        private int _totalTasks;
        private int _completedTasks;
        private int _totalProjects;
        private int _totalOKRs;
        private int _totalKPIs;
        private int _upcomingMeetings;

        // Chart series
        private SeriesCollection _taskCompletionSeries;
        private SeriesCollection _okrProgressSeries;
        private SeriesCollection _kpiStatusSeries;
        private SeriesCollection _teamActivitySeries;
        private string[] _taskCompletionLabels;
        private string[] _okrProgressLabels;
        private string[] _kpiStatusLabels;
        private string[] _teamActivityLabels;

        private ICommand? _refreshCommand;

        #endregion

        #region Ctor

        public DashboardViewModel()
        {
            InitializeCharts();
            LoadData();
        }

        #endregion

        #region Properties - Summary Statistics

        public int TotalTeamMembers
        {
            get => _totalTeamMembers;
            set
            {
                _totalTeamMembers = value;
                RaisePropertyChanged();
            }
        }

        public int TotalOneOnOnes
        {
            get => _totalOneOnOnes;
            set
            {
                _totalOneOnOnes = value;
                RaisePropertyChanged();
            }
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set
            {
                _totalTasks = value;
                RaisePropertyChanged();
            }
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set
            {
                _completedTasks = value;
                RaisePropertyChanged();
            }
        }

        public int TotalProjects
        {
            get => _totalProjects;
            set
            {
                _totalProjects = value;
                RaisePropertyChanged();
            }
        }

        public int TotalOKRs
        {
            get => _totalOKRs;
            set
            {
                _totalOKRs = value;
                RaisePropertyChanged();
            }
        }

        public int TotalKPIs
        {
            get => _totalKPIs;
            set
            {
                _totalKPIs = value;
                RaisePropertyChanged();
            }
        }

        public int UpcomingMeetings
        {
            get => _upcomingMeetings;
            set
            {
                _upcomingMeetings = value;
                RaisePropertyChanged();
            }
        }

        public double TaskCompletionPercentage => TotalTasks > 0 
            ? Math.Round((CompletedTasks / (double)TotalTasks) * 100, 1) 
            : 0;

        #endregion

        #region Properties - Charts

        public SeriesCollection TaskCompletionSeries
        {
            get => _taskCompletionSeries;
            set
            {
                _taskCompletionSeries = value;
                RaisePropertyChanged();
            }
        }

        public SeriesCollection OkrProgressSeries
        {
            get => _okrProgressSeries;
            set
            {
                _okrProgressSeries = value;
                RaisePropertyChanged();
            }
        }

        public SeriesCollection KpiStatusSeries
        {
            get => _kpiStatusSeries;
            set
            {
                _kpiStatusSeries = value;
                RaisePropertyChanged();
            }
        }

        public SeriesCollection TeamActivitySeries
        {
            get => _teamActivitySeries;
            set
            {
                _teamActivitySeries = value;
                RaisePropertyChanged();
            }
        }

        public string[] TaskCompletionLabels
        {
            get => _taskCompletionLabels;
            set
            {
                _taskCompletionLabels = value;
                RaisePropertyChanged();
            }
        }

        public string[] OkrProgressLabels
        {
            get => _okrProgressLabels;
            set
            {
                _okrProgressLabels = value;
                RaisePropertyChanged();
            }
        }

        public string[] KpiStatusLabels
        {
            get => _kpiStatusLabels;
            set
            {
                _kpiStatusLabels = value;
                RaisePropertyChanged();
            }
        }

        public string[] TeamActivityLabels
        {
            get => _teamActivityLabels;
            set
            {
                _teamActivityLabels = value;
                RaisePropertyChanged();
            }
        }

        public Func<double, string> Formatter { get; set; } = value => value.ToString("N0");

        #endregion

        #region Commands

        public ICommand RefreshCommand => _refreshCommand ??= 
            new TrackerCommand(ExecuteRefresh, _ => true);

        #endregion

        #region Private Methods

        private void InitializeCharts()
        {
            TaskCompletionSeries = new SeriesCollection();
            OkrProgressSeries = new SeriesCollection();
            KpiStatusSeries = new SeriesCollection();
            TeamActivitySeries = new SeriesCollection();
        }

        private async void LoadData()
        {
            await RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            _teamMembers = new ObservableCollection<TeamMember>(await TrackerDataManager.Instance.GetTeamData());
            _oneOnOnes = new ObservableCollection<OneOnOne>(await TrackerDataManager.Instance.GetOneOnOnes());
            _tasks = new ObservableCollection<IndividualTask>(await TrackerDataManager.Instance.GetTasks());
            _okrs = new ObservableCollection<ObjectiveKeyResult>(await TrackerDataManager.Instance.GetOKRs());
            _kpis = new ObservableCollection<KeyPerformanceIndicator>(await TrackerDataManager.Instance.GetKPIs());
            _projects = new ObservableCollection<Project>(await TrackerDataManager.Instance.GetProjects());

            UpdateSummaryStatistics();
            UpdateCharts();
        }

        private void UpdateSummaryStatistics()
        {
            TotalTeamMembers = _teamMembers.Count;
            TotalOneOnOnes = _oneOnOnes.Count;
            TotalTasks = _tasks.Count;
            CompletedTasks = _tasks.Count(t => t.IsCompleted);
            TotalProjects = _projects.Count;
            TotalOKRs = _okrs.Count;
            TotalKPIs = _kpis.Count;
            UpcomingMeetings = _oneOnOnes.Count(m => m.Date >= DateTime.Today);
            
            RaisePropertyChanged(nameof(TaskCompletionPercentage));
        }

        private void UpdateCharts()
        {
            UpdateTaskCompletionChart();
            UpdateOkrProgressChart();
            UpdateKpiStatusChart();
            UpdateTeamActivityChart();
        }

        private void UpdateTaskCompletionChart()
        {
            var completed = _tasks.Count(t => t.IsCompleted);
            var incomplete = _tasks.Count - completed;

            TaskCompletionSeries.Clear();
            TaskCompletionSeries.Add(new PieSeries
            {
                Title = "Completed",
                Values = new ChartValues<ObservableValue> { new ObservableValue(completed) },
                DataLabels = true
            });
            TaskCompletionSeries.Add(new PieSeries
            {
                Title = "Incomplete",
                Values = new ChartValues<ObservableValue> { new ObservableValue(incomplete) },
                DataLabels = true
            });

            TaskCompletionLabels = new[] { "Completed", "Incomplete" };
        }

        private void UpdateOkrProgressChart()
        {
            var onTrack = _okrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OnTrack);
            var atRisk = _okrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.AtRisk);
            var offTrack = _okrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OffTrack);

            OkrProgressSeries.Clear();
            OkrProgressSeries.Add(new ColumnSeries
            {
                Title = "On Track",
                Values = new ChartValues<double> { onTrack }
            });
            OkrProgressSeries.Add(new ColumnSeries
            {
                Title = "At Risk",
                Values = new ChartValues<double> { atRisk }
            });
            OkrProgressSeries.Add(new ColumnSeries
            {
                Title = "Off Track",
                Values = new ChartValues<double> { offTrack }
            });

            OkrProgressLabels = new[] { "Status" };
        }

        private void UpdateKpiStatusChart()
        {
            var onTarget = _kpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget);
            var offTarget = _kpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.OffTarget);
            var closeToTarget = _kpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.CloseToTarget);

            KpiStatusSeries.Clear();
            KpiStatusSeries.Add(new PieSeries
            {
                Title = "On Target",
                Values = new ChartValues<ObservableValue> { new ObservableValue(onTarget) },
                DataLabels = true
            });
            KpiStatusSeries.Add(new PieSeries
            {
                Title = "Off Target",
                Values = new ChartValues<ObservableValue> { new ObservableValue(offTarget) },
                DataLabels = true
            });
            KpiStatusSeries.Add(new PieSeries
            {
                Title = "Close To Target",
                Values = new ChartValues<ObservableValue> { new ObservableValue(closeToTarget) },
                DataLabels = true
            });

            KpiStatusLabels = new[] { "On Target", "Off Target", "Close To Target" };
        }

        private void UpdateTeamActivityChart()
        {
            // Group 1:1s by month for the last 6 months
            var sixMonthsAgo = DateTime.Today.AddMonths(-6);
            var recentMeetings = _oneOnOnes
                .Where(m => m.Date >= sixMonthsAgo)
                .GroupBy(m => new { m.Date.Year, m.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .ToList();

            TeamActivitySeries.Clear();
            var values = new ChartValues<double>(recentMeetings.Select(m => (double)m.Count));
            TeamActivitySeries.Add(new LineSeries
            {
                Title = "1:1 Meetings",
                Values = values,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 8
            });

            TeamActivityLabels = recentMeetings.Select(m => m.Label).ToArray();
        }

        private void ExecuteRefresh(object? parameter)
        {
            RefreshDataAsync();
        }

        #endregion
    }
}

