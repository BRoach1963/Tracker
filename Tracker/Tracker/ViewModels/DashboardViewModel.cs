using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Analytics;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard view - Manager's Team Health command center.
    /// Provides actionable insights focused on what needs attention NOW.
    /// </summary>
    public class DashboardViewModel : BaseViewModel, IDisposable
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("DashboardVM");
        
        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<OneOnOne> _oneOnOnes = new();
        private ObservableCollection<IndividualTask> _tasks = new();
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        private ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private ObservableCollection<Project> _projects = new();
        private ObservableCollection<Feedback> _feedbacks = new();
        private ObservableCollection<IndividualGoal> _goals = new();

        // Summary statistics
        private int _totalTeamMembers;
        private int _totalTasks;
        private int _completedTasks;
        private int _upcomingMeetings;
        private int _activeOkrs;
        private int _kpisOnTarget;
        private int _totalKpis;

        // Manager-centric metrics
        private int _overdueMeetingsCount;
        private int _openActionItemsCount;
        private int _totalActionItems;
        private int _completedActionItems;
        private int _unresolvedConcernsCount;
        private int _goalsDueSoonCount;

        // KPI Card metrics
        private int _meetingCadencePercent;
        private int _actionItemCompletionPercent;
        private int _okrOnTrackPercent;

        // Chart series
        private SeriesCollection _taskCompletionSeries = new();
        private SeriesCollection _okrProgressSeries = new();
        private SeriesCollection _kpiStatusSeries = new();
        private string[] _okrProgressLabels = Array.Empty<string>();

        // Collections for display
        private ObservableCollection<OneOnOne> _upcomingMeetingsList = new();
        private ObservableCollection<TeamMemberCadenceInfo> _teamMemberCadence = new();
        private ObservableCollection<TeamMemberCadenceInfo> _overdueMeetingMembers = new();
        private ObservableCollection<MeetingTask> _recentActionItems = new();
        private ObservableCollection<AgendaItem> _recentConcerns = new();
        private ObservableCollection<IndividualGoal> _goalsDueSoon = new();
        private ObservableCollection<TeamHealthRow> _teamHealthRows = new();
        private ObservableCollection<TrajectoryAlertItem> _atRiskTrajectoryItems = new();

        private ICommand? _refreshCommand;
        private ICommand? _newOneOnOneCommand;

        // Color brushes
        private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly Brush AmberBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
        private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
        private static readonly Brush GrayBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));

        #endregion

        #region Ctor

        public DashboardViewModel()
        {
            InitializeCharts();
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
            
            // Refresh if any relevant data type changed
            if (info.RefreshAll ||
                info.Includes(DataChangeType.TeamMembers) ||
                info.Includes(DataChangeType.OneOnOnes) ||
                info.Includes(DataChangeType.Tasks) ||
                info.Includes(DataChangeType.Projects) ||
                info.Includes(DataChangeType.OKRs) ||
                info.Includes(DataChangeType.KPIs) ||
                info.Includes(DataChangeType.Goals) ||
                info.Includes(DataChangeType.Feedback))
            {
                _logger.Info("Refreshing dashboard data due to data change");
                // Refresh on UI thread
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await RefreshDataAsync();
                });
            }
        }

        #endregion

        #region Properties - Header

        public string CurrentUserName => UserSettingsManager.Instance.CurrentUser ?? "Manager";
        
        public string TodayDateFormatted => DateTime.Now.ToString("dddd, MMMM d, yyyy");

        public string TimeOfDayGreeting
        {
            get
            {
                var hour = DateTime.Now.Hour;
                if (hour < 12) return "morning";
                if (hour < 17) return "afternoon";
                return "evening";
            }
        }

        #endregion

        #region Properties - KPI Cards

        public int MeetingCadencePercent
        {
            get => _meetingCadencePercent;
            set { _meetingCadencePercent = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(MeetingCadenceColor)); RaisePropertyChanged(nameof(MeetingCadenceStatus)); }
        }

        public Brush MeetingCadenceColor => _meetingCadencePercent >= 80 ? GreenBrush : _meetingCadencePercent >= 50 ? AmberBrush : RedBrush;

        public string MeetingCadenceStatus => _meetingCadencePercent >= 80 ? "On Track" : _meetingCadencePercent >= 50 ? "Needs Attn" : "Critical";

        public int ActionItemCompletionPercent
        {
            get => _actionItemCompletionPercent;
            set { _actionItemCompletionPercent = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ActionItemColor)); }
        }

        public Brush ActionItemColor => _actionItemCompletionPercent >= 70 ? GreenBrush : _actionItemCompletionPercent >= 40 ? AmberBrush : RedBrush;

        public Brush TaskCompletionColor => TaskCompletionPercentage >= 70 ? GreenBrush : TaskCompletionPercentage >= 40 ? AmberBrush : RedBrush;

        public int OkrOnTrackPercent
        {
            get => _okrOnTrackPercent;
            set { _okrOnTrackPercent = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(OkrColor)); }
        }

        public Brush OkrColor => _okrOnTrackPercent >= 70 ? GreenBrush : _okrOnTrackPercent >= 40 ? AmberBrush : RedBrush;

        public Brush KpiColor => _kpisOnTarget >= 70 ? GreenBrush : _kpisOnTarget >= 40 ? AmberBrush : RedBrush;

        public int TotalKpis
        {
            get => _totalKpis;
            set { _totalKpis = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Needs Attention Section

        public int OverdueMeetingsCount
        {
            get => _overdueMeetingsCount;
            set { _overdueMeetingsCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasNoOverdueMeetings)); }
        }

        public bool HasNoOverdueMeetings => OverdueMeetingsCount == 0;

        public ObservableCollection<TeamMemberCadenceInfo> OverdueMeetingMembers
        {
            get => _overdueMeetingMembers;
            set { _overdueMeetingMembers = value; RaisePropertyChanged(); }
        }

        public int OpenActionItemsCount
        {
            get => _openActionItemsCount;
            set { _openActionItemsCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasNoOpenActionItems)); }
        }

        public bool HasNoOpenActionItems => OpenActionItemsCount == 0;

        public ObservableCollection<MeetingTask> RecentActionItems
        {
            get => _recentActionItems;
            set { _recentActionItems = value; RaisePropertyChanged(); }
        }

        public int UnresolvedConcernsCount
        {
            get => _unresolvedConcernsCount;
            set { _unresolvedConcernsCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasNoConcerns)); }
        }

        public bool HasNoConcerns => UnresolvedConcernsCount == 0;

        public ObservableCollection<AgendaItem> RecentConcerns
        {
            get => _recentConcerns;
            set { _recentConcerns = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Trajectory Alerts

        public ObservableCollection<TrajectoryAlertItem> AtRiskTrajectoryItems
        {
            get => _atRiskTrajectoryItems;
            set { _atRiskTrajectoryItems = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasAtRiskItems)); }
        }

        public bool HasAtRiskItems => AtRiskTrajectoryItems.Count > 0;

        #endregion

        #region Properties - Team Health Table

        public ObservableCollection<TeamHealthRow> TeamHealthRows
        {
            get => _teamHealthRows;
            set { _teamHealthRows = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Team Pulse Section

        public ObservableCollection<TeamMemberCadenceInfo> TeamMemberCadence
        {
            get => _teamMemberCadence;
            set { _teamMemberCadence = value; RaisePropertyChanged(); }
        }

        public string MeetingCadenceSummary
        {
            get
            {
                var onTrack = TeamMemberCadence?.Count(t => t.IsOnTrack) ?? 0;
                var total = TeamMemberCadence?.Count ?? 0;
                return $"{onTrack} of {total} on track";
            }
        }

        #endregion

        #region Properties - This Week Section

        public int UpcomingMeetings
        {
            get => _upcomingMeetings;
            set { _upcomingMeetings = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasNoUpcomingMeetings)); }
        }

        public bool HasNoUpcomingMeetings => UpcomingMeetings == 0;

        public ObservableCollection<OneOnOne> UpcomingMeetingsList
        {
            get => _upcomingMeetingsList;
            set { _upcomingMeetingsList = value; RaisePropertyChanged(); }
        }

        public int GoalsDueSoonCount
        {
            get => _goalsDueSoonCount;
            set { _goalsDueSoonCount = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasNoGoalsDueSoon)); }
        }

        public bool HasNoGoalsDueSoon => GoalsDueSoonCount == 0;

        public ObservableCollection<IndividualGoal> GoalsDueSoon
        {
            get => _goalsDueSoon;
            set { _goalsDueSoon = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Performance Section

        public int TotalTeamMembers
        {
            get => _totalTeamMembers;
            set { _totalTeamMembers = value; RaisePropertyChanged(); }
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set { _totalTasks = value; RaisePropertyChanged(); }
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set { _completedTasks = value; RaisePropertyChanged(); }
        }

        public double TaskCompletionPercentage => TotalTasks > 0 
            ? Math.Round((CompletedTasks / (double)TotalTasks) * 100, 1) 
            : 0;

        public int ActiveOkrs
        {
            get => _activeOkrs;
            set { _activeOkrs = value; RaisePropertyChanged(); }
        }

        public int KpisOnTarget
        {
            get => _kpisOnTarget;
            set { _kpisOnTarget = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(KpiColor)); }
        }

        public SeriesCollection TaskCompletionSeries
        {
            get => _taskCompletionSeries;
            set { _taskCompletionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection OkrProgressSeries
        {
            get => _okrProgressSeries;
            set { _okrProgressSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection KpiStatusSeries
        {
            get => _kpiStatusSeries;
            set { _kpiStatusSeries = value; RaisePropertyChanged(); }
        }

        public string[] OkrProgressLabels
        {
            get => _okrProgressLabels;
            set { _okrProgressLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand => _refreshCommand ??= 
            new TrackerCommand(ExecuteRefresh, _ => true);

        public ICommand NewOneOnOneCommand => _newOneOnOneCommand ??=
            new TrackerCommand(ExecuteNewOneOnOne, _ => true);

        #endregion

        #region Private Methods

        private void InitializeCharts()
        {
            TaskCompletionSeries = new SeriesCollection();
            OkrProgressSeries = new SeriesCollection();
            KpiStatusSeries = new SeriesCollection();
        }

        /// <summary>
        /// Initializes the dashboard by loading data asynchronously.
        /// Call this from the View's Loaded event, not from the constructor.
        /// </summary>
        public async Task InitializeAsync()
        {
            await RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            _logger.Debug("RefreshDataAsync started");
            try
            {
                // Load all data in parallel
                var teamTask = TrackerDataManager.Instance.GetTeamData();
                var oneOnOnesTask = TrackerDataManager.Instance.GetOneOnOnes();
                var tasksTask = TrackerDataManager.Instance.GetTasks();
                var okrsTask = TrackerDataManager.Instance.GetOKRs();
                var kpisTask = TrackerDataManager.Instance.GetKPIs();
                var projectsTask = TrackerDataManager.Instance.GetProjects();
                var feedbacksTask = TrackerDataManager.Instance.GetFeedbacks();
                var goalsTask = TrackerDataManager.Instance.GetGoals();

                await Task.WhenAll(teamTask, oneOnOnesTask, tasksTask, okrsTask, kpisTask, projectsTask, feedbacksTask, goalsTask).ConfigureAwait(false);

                // Update collections
                _teamMembers = new ObservableCollection<TeamMember>(await teamTask);
                _oneOnOnes = new ObservableCollection<OneOnOne>(await oneOnOnesTask);
                _tasks = new ObservableCollection<IndividualTask>(await tasksTask);
                _okrs = new ObservableCollection<ObjectiveKeyResult>(await okrsTask);
                _kpis = new ObservableCollection<KeyPerformanceIndicator>(await kpisTask);
                _projects = new ObservableCollection<Project>(await projectsTask);
                _feedbacks = new ObservableCollection<Feedback>(await feedbacksTask);

                // Try to load goals if available
                try
                {
                    _goals = new ObservableCollection<IndividualGoal>(await goalsTask);
                }
                catch
                {
                    _goals = new ObservableCollection<IndividualGoal>();
                }

                _logger.Info("Dashboard data refreshed: {0} team members, {1} tasks, {2} OKRs",
                    _teamMembers.Count, _tasks.Count, _okrs.Count);

                UpdateManagerMetrics();
                UpdateTeamHealthTable();
                UpdateCharts();
                
                // Load trajectory alerts (fire and forget for non-blocking UI)
                _ = LoadTrajectoryAlertsAsync();

                _logger.Debug("RefreshDataAsync completed");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in RefreshDataAsync");
                // Handle gracefully - dashboard can work with partial data
            }
        }

        private void UpdateManagerMetrics()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7 - (int)today.DayOfWeek);
            var endOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            // Basic counts
            TotalTeamMembers = _teamMembers.Count;
            TotalTasks = _tasks.Count;
            CompletedTasks = _tasks.Count(t => t.IsCompleted);
            TotalKpis = _kpis.Count;

            // =====================================================
            // NEEDS ATTENTION: Overdue 1:1s
            // =====================================================
            var meetingCadenceDays = 14; // Assume bi-weekly cadence
            var cadenceInfo = new List<TeamMemberCadenceInfo>();
            
            foreach (var member in _teamMembers)
            {
                var lastMeeting = _oneOnOnes
                    .Where(m => m.TeamMember?.Id == member.Id && m.Date <= today)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                var daysSince = lastMeeting != null 
                    ? (int)(today - lastMeeting.Date).TotalDays 
                    : 999; // Never met

                var isOverdue = daysSince > meetingCadenceDays;
                var statusColor = isOverdue 
                    ? RedBrush
                    : daysSince > meetingCadenceDays - 3 
                        ? AmberBrush
                        : GreenBrush;

                cadenceInfo.Add(new TeamMemberCadenceInfo
                {
                    FullName = member.FullName,
                    DaysSinceLastMeeting = daysSince == 999 ? "Never" : daysSince.ToString(),
                    IsOnTrack = !isOverdue,
                    CadenceStatusColor = statusColor
                });
            }

            TeamMemberCadence = new ObservableCollection<TeamMemberCadenceInfo>(
                cadenceInfo.OrderByDescending(c => c.IsOnTrack ? 0 : 1).ThenBy(c => c.FullName));
            
            OverdueMeetingMembers = new ObservableCollection<TeamMemberCadenceInfo>(
                cadenceInfo.Where(c => !c.IsOnTrack).OrderBy(c => c.FullName).Take(5));
            
            OverdueMeetingsCount = cadenceInfo.Count(c => !c.IsOnTrack);
            
            // Meeting cadence percentage
            var onTrackCount = cadenceInfo.Count(c => c.IsOnTrack);
            MeetingCadencePercent = TotalTeamMembers > 0 
                ? (int)Math.Round((onTrackCount / (double)TotalTeamMembers) * 100) 
                : 100;
            
            RaisePropertyChanged(nameof(MeetingCadenceSummary));

            // =====================================================
            // NEEDS ATTENTION: Open Action Items
            // =====================================================
            var allActionItems = _oneOnOnes
                .SelectMany(m => m.Tasks ?? new List<MeetingTask>())
                .ToList();

            _totalActionItems = allActionItems.Count;
            _completedActionItems = allActionItems.Count(t => t.IsCompleted);
            var openItems = allActionItems.Where(t => !t.IsCompleted).ToList();

            OpenActionItemsCount = openItems.Count;
            RecentActionItems = new ObservableCollection<MeetingTask>(openItems.Take(5));
            
            // Action item completion percentage
            ActionItemCompletionPercent = _totalActionItems > 0 
                ? (int)Math.Round((_completedActionItems / (double)_totalActionItems) * 100) 
                : 100;

            // =====================================================
            // NEEDS ATTENTION: Unresolved Concerns
            // =====================================================
            var allConcerns = _oneOnOnes
                .SelectMany(m => m.AgendaItems ?? new List<AgendaItem>())
                .Where(a => a.Category == AgendaItemCategory.Concern && 
                           string.IsNullOrWhiteSpace(a.Resolution))
                .ToList();

            UnresolvedConcernsCount = allConcerns.Count;
            RecentConcerns = new ObservableCollection<AgendaItem>(allConcerns.Take(5));

            // =====================================================
            // THIS WEEK: Upcoming Meetings
            // =====================================================
            UpcomingMeetings = _oneOnOnes.Count(m => m.Date >= today && m.Date <= endOfWeek);
            UpcomingMeetingsList = new ObservableCollection<OneOnOne>(
                _oneOnOnes
                    .Where(m => m.Date >= today && m.Date <= endOfWeek)
                    .OrderBy(m => m.Date)
                    .Take(8));

            // =====================================================
            // THIS WEEK: Goals Due Soon
            // =====================================================
            var goalsDue = _goals
                .Where(g => g.TargetDate.HasValue && 
                           g.TargetDate.Value >= today && 
                           g.TargetDate.Value <= endOfMonth &&
                           g.Status != GoalStatus.Completed)
                .OrderBy(g => g.TargetDate)
                .ToList();

            GoalsDueSoonCount = goalsDue.Count;
            GoalsDueSoon = new ObservableCollection<IndividualGoal>(goalsDue.Take(5));

            // =====================================================
            // PERFORMANCE: OKRs and KPIs
            // =====================================================
            var onTrackOkrs = _okrs.Count(o => o.Status == ObjectiveStatusEnum.OnTrack);
            ActiveOkrs = _okrs.Count;
            OkrOnTrackPercent = ActiveOkrs > 0 
                ? (int)Math.Round((onTrackOkrs / (double)ActiveOkrs) * 100) 
                : 0;
            
            var onTargetKpis = _kpis.Count(k => k.Status == KpiStatusEnum.OnTarget);
            KpisOnTarget = TotalKpis > 0 ? (int)Math.Round((onTargetKpis / (double)TotalKpis) * 100) : 0;
            
            RaisePropertyChanged(nameof(TaskCompletionPercentage));
            RaisePropertyChanged(nameof(TaskCompletionColor));
        }

        private void UpdateTeamHealthTable()
        {
            var today = DateTime.Today;
            var meetingCadenceDays = 14;
            var healthRows = new List<TeamHealthRow>();

            foreach (var member in _teamMembers)
            {
                // Last meeting
                var lastMeeting = _oneOnOnes
                    .Where(m => m.TeamMember?.Id == member.Id && m.Date <= today)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefault();

                var daysSince = lastMeeting != null 
                    ? (int)(today - lastMeeting.Date).TotalDays 
                    : 999;

                // Tasks for this member
                var memberTasks = _tasks.Where(t => t.Owner?.Id == member.Id && !t.IsCompleted).Count();
                
                // Goals for this member
                var memberGoals = _goals.Where(g => g.TeamMember?.Id == member.Id && g.Status != GoalStatus.Completed).Count();

                // Determine health status
                var isOverdue = daysSince > meetingCadenceDays;
                var hasManyTasks = memberTasks > 5;
                
                string status;
                Brush statusBg;
                Brush statusFg;

                if (isOverdue && hasManyTasks)
                {
                    status = "Alert";
                    statusBg = new SolidColorBrush(Color.FromArgb(30, 0xEF, 0x44, 0x44));
                    statusFg = RedBrush;
                }
                else if (isOverdue || hasManyTasks)
                {
                    status = "Watch";
                    statusBg = new SolidColorBrush(Color.FromArgb(30, 0xF5, 0x9E, 0x0B));
                    statusFg = AmberBrush;
                }
                else
                {
                    status = "Good";
                    statusBg = new SolidColorBrush(Color.FromArgb(30, 0x10, 0xB9, 0x81));
                    statusFg = GreenBrush;
                }

                healthRows.Add(new TeamHealthRow
                {
                    FullName = member.FullName,
                    Initials = member.Initials,
                    ProfileImage = member.ProfileImage,
                    PresenceEmoji = member.CombinedPresenceEmoji,
                    PresenceDisplay = member.CombinedPresenceDisplay,
                    LastMeetingDisplay = daysSince == 999 ? "Never" : daysSince == 0 ? "Today" : daysSince == 1 ? "1d ago" : $"{daysSince}d ago",
                    LastMeetingColor = isOverdue ? RedBrush : daysSince > meetingCadenceDays - 3 ? AmberBrush : (Brush)System.Windows.Application.Current.FindResource("ForegroundBrush"),
                    OpenTasks = memberTasks.ToString(),
                    TasksColor = memberTasks > 7 ? RedBrush : memberTasks > 4 ? AmberBrush : (Brush)System.Windows.Application.Current.FindResource("ForegroundBrush"),
                    ActiveGoals = memberGoals.ToString(),
                    StatusText = status,
                    StatusBackground = statusBg,
                    StatusForeground = statusFg
                });
            }

            // Sort: Alert first, then Watch, then Good
            var sortedRows = healthRows
                .OrderBy(r => r.StatusText == "Alert" ? 0 : r.StatusText == "Watch" ? 1 : 2)
                .ThenBy(r => r.FullName)
                .ToList();

            TeamHealthRows = new ObservableCollection<TeamHealthRow>(sortedRows);
        }

        private void UpdateCharts()
        {
            UpdateTaskCompletionChart();
            UpdateOkrProgressChart();
            UpdateKpiStatusChart();
        }

        private void UpdateTaskCompletionChart()
        {
            var completed = _tasks.Count(t => t.IsCompleted);
            var incomplete = _tasks.Count - completed;

            // Create new SeriesCollection to avoid LiveCharts race condition
            TaskCompletionSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Done",
                    Values = new ChartValues<ObservableValue> { new ObservableValue(completed) },
                    DataLabels = false,
                    Fill = GreenBrush
                },
                new PieSeries
                {
                    Title = "To Do",
                    Values = new ChartValues<ObservableValue> { new ObservableValue(incomplete) },
                    DataLabels = false,
                    Fill = GrayBrush
                }
            };
        }

        private void UpdateOkrProgressChart()
        {
            var onTrack = _okrs.Count(o => o.Status == ObjectiveStatusEnum.OnTrack);
            var atRisk = _okrs.Count(o => o.Status == ObjectiveStatusEnum.AtRisk);
            var offTrack = _okrs.Count(o => o.Status == ObjectiveStatusEnum.OffTrack);

            // Create new SeriesCollection to avoid LiveCharts race condition
            OkrProgressSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "On Track",
                    Values = new ChartValues<double> { onTrack },
                    Fill = GreenBrush
                },
                new ColumnSeries
                {
                    Title = "At Risk",
                    Values = new ChartValues<double> { atRisk },
                    Fill = AmberBrush
                },
                new ColumnSeries
                {
                    Title = "Off Track",
                    Values = new ChartValues<double> { offTrack },
                    Fill = RedBrush
                }
            };

            OkrProgressLabels = new[] { "OKRs" };
        }

        private void UpdateKpiStatusChart()
        {
            var onTarget = _kpis.Count(k => k.Status == KpiStatusEnum.OnTarget);
            var offTarget = _kpis.Count(k => k.Status == KpiStatusEnum.OffTarget);
            var closeToTarget = _kpis.Count(k => k.Status == KpiStatusEnum.CloseToTarget);

            // Create new SeriesCollection to avoid LiveCharts race condition
            KpiStatusSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "On Target",
                    Values = new ChartValues<ObservableValue> { new ObservableValue(onTarget) },
                    DataLabels = false,
                    Fill = GreenBrush
                },
                new PieSeries
                {
                    Title = "Close",
                    Values = new ChartValues<ObservableValue> { new ObservableValue(closeToTarget) },
                    DataLabels = false,
                    Fill = AmberBrush
                },
                new PieSeries
                {
                    Title = "Off Target",
                    Values = new ChartValues<ObservableValue> { new ObservableValue(offTarget) },
                    DataLabels = false,
                    Fill = RedBrush
                }
            };
        }

        private async void ExecuteRefresh(object? parameter)
        {
            // Note: This is a command handler, so async void is acceptable here
            // The command framework handles the async operation
            await RefreshDataAsync();
        }

        private void ExecuteNewOneOnOne(object? parameter)
        {
            DialogManager.Instance.LaunchDialogByType(DialogType.AddOneOnOne, false, () =>
            {
                _ = RefreshDataAsync();
            });
        }

        private async Task LoadTrajectoryAlertsAsync()
        {
            try
            {
                var alerts = new List<TrajectoryAlertItem>();
                var analyticsService = PredictiveAnalyticsService.Instance;

                // Check OKRs for at-risk trajectories
                var activeOkrs = _okrs.Where(o => !o.IsDeleted).Take(10).ToList();
                foreach (var okr in activeOkrs)
                {
                    try
                    {
                        var prediction = await analyticsService.AnalyzeOkrAsync(
                            okr.ObjectiveId,
                            okr.Title,
                            okr.StartDate,
                            okr.EndDate);

                        if (prediction.IsValid && prediction.Trajectory != null)
                        {
                            if (prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.Critical ||
                                prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.AtRisk)
                            {
                                alerts.Add(new TrajectoryAlertItem
                                {
                                    Title = okr.Title,
                                    EntityType = "OKR",
                                    RiskLevel = prediction.Trajectory.Risk,
                                    TrendDirection = prediction.Trend?.Direction ?? TrendAnalyzer.TrendDirection.Stable
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip items without sufficient data
                    }
                }

                // Check KPIs for at-risk trajectories
                var activeKpis = _kpis.Where(k => !k.IsDeleted).Take(10).ToList();
                foreach (var kpi in activeKpis)
                {
                    try
                    {
                        var prediction = await analyticsService.AnalyzeKpiAsync(
                            kpi.KpiId,
                            kpi.Name,
                            null, // KPIs typically don't have a target date
                            kpi.TargetValue);

                        if (prediction.IsValid && prediction.Trajectory != null)
                        {
                            if (prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.Critical ||
                                prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.AtRisk)
                            {
                                alerts.Add(new TrajectoryAlertItem
                                {
                                    Title = kpi.Name,
                                    EntityType = "KPI",
                                    RiskLevel = prediction.Trajectory.Risk,
                                    TrendDirection = prediction.Trend?.Direction ?? TrendAnalyzer.TrendDirection.Stable
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip items without sufficient data
                    }
                }

                // Update UI on dispatcher thread
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    AtRiskTrajectoryItems = new ObservableCollection<TrajectoryAlertItem>(
                        alerts.OrderByDescending(a => a.RiskLevel == TrajectoryPredictor.RiskLevel.Critical)
                              .ThenBy(a => a.Title)
                              .Take(5));
                });

                _logger.Debug("Loaded {0} trajectory alerts", alerts.Count);
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading trajectory alerts: {0}", ex.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents an at-risk item for the trajectory alerts panel.
    /// </summary>
    public class TrajectoryAlertItem
    {
        private static readonly Brush CriticalBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
        private static readonly Brush AtRiskBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));

        public string Title { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public TrajectoryPredictor.RiskLevel RiskLevel { get; set; }
        public TrendAnalyzer.TrendDirection TrendDirection { get; set; }

        public Brush RiskColor => RiskLevel == TrajectoryPredictor.RiskLevel.Critical ? CriticalBrush : AtRiskBrush;
        
        public string TrendText => TrendDirection switch
        {
            TrendAnalyzer.TrendDirection.Declining => "↓ Declining",
            TrendAnalyzer.TrendDirection.Stable => "→ Stalled",
            TrendAnalyzer.TrendDirection.Improving => "↑ Improving",
            TrendAnalyzer.TrendDirection.Insufficient => "? Data",
            _ => ""
        };
    }

    /// <summary>
    /// Helper class for team member meeting cadence display.
    /// </summary>
    public class TeamMemberCadenceInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string DaysSinceLastMeeting { get; set; } = "0";
        public bool IsOnTrack { get; set; }
        public Brush CadenceStatusColor { get; set; } = Brushes.Gray;
    }

    /// <summary>
    /// Row data for the Team Health table.
    /// </summary>
    public class TeamHealthRow
    {
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public byte[] ProfileImage { get; set; } = Array.Empty<byte>();
        public string PresenceEmoji { get; set; } = "⚪";
        public string PresenceDisplay { get; set; } = "Unknown";
        public string LastMeetingDisplay { get; set; } = "—";
        public Brush LastMeetingColor { get; set; } = Brushes.Gray;
        public string OpenTasks { get; set; } = "0";
        public Brush TasksColor { get; set; } = Brushes.Gray;
        public string ActiveGoals { get; set; } = "0";
        public string StatusText { get; set; } = "Good";
        public Brush StatusBackground { get; set; } = Brushes.Transparent;
        public Brush StatusForeground { get; set; } = Brushes.Green;
    }
}
