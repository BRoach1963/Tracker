using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.Win32;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the Reports page, handling analytics, visualizations, and Excel exports.
    /// </summary>
    public class ReportsViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(ReportsViewModel));

        // Report selection
        private bool _isReport1Selected = true; // Default to 1:1 Effectiveness
        private bool _isReport2Selected;
        private bool _isReport3Selected;
        private bool _isReport4Selected;
        private bool _isReport5Selected;
        private bool _isReport6Selected;
        private bool _isReport7Selected;
        private bool _isReport8Selected;
        private bool _isReport9Selected;
        private bool _isReport10Selected;
        private bool _isReport11Selected;
        private bool _isReport12Selected;

        // Filters
        private ObservableCollection<TeamMemberFilterItem> _teamMemberFilterOptions = new();
        private TeamMemberFilterItem? _selectedTeamMemberFilter;
        private ObservableCollection<string> _dateRangeOptions = new();
        private string _selectedDateRange = "Last 30 Days";

        // State
        private bool _isLoading;
        private bool _isExporting;

        // Data
        private List<TeamMember> _teamMembers = new();
        private List<OneOnOne> _oneOnOnes = new();
        private List<IndividualTask> _tasks = new();
        private List<ObjectiveKeyResult> _okrs = new();
        private List<KeyPerformanceIndicator> _kpis = new();
        private List<Project> _projects = new();
        private List<Feedback> _feedbacks = new();
        private List<IndividualGoal> _goals = new();

        // Report 1: 1:1 Effectiveness
        private int _r1_totalMeetings;
        private int _r1_avgDuration;
        private int _r1_actionsCreated;
        private int _r1_completionRate;
        private SeriesCollection _r1_meetingFrequencySeries = new();
        private ObservableCollection<string> _r1_meetingFrequencyLabels = new();
        private SeriesCollection _r1_agendaTopicsSeries = new();
        private ObservableCollection<OneOnOne> _r1_recentMeetings = new();

        // Report 2: Meeting Cadence
        private int _r2_totalTeamMembers;
        private int _r2_onTrackCount;
        private int _r2_overdueCount;
        private int _r2_neverMetCount;
        private int _r2_cadenceCompliancePercent;
        private ObservableCollection<MemberCadenceRow> _r2_memberCadenceList = new();
        private SeriesCollection _r2_cadenceDistributionSeries = new();

        // Report 3: Task Completion
        private int _r3_totalTasks;
        private int _r3_completedTasks;
        private int _r3_overdueTasksCount;
        private int _r3_completionPercent;
        private double _r3_avgDaysToComplete;
        private SeriesCollection _r3_completionTrendSeries = new();
        private ObservableCollection<string> _r3_completionTrendLabels = new();
        private SeriesCollection _r3_tasksByOwnerSeries = new();
        private ObservableCollection<string> _r3_tasksByOwnerLabels = new();
        private ObservableCollection<IndividualTask> _r3_overdueTasksList = new();

        // Report 4: Action Item Follow-Up
        private int _r4_totalActionItems;
        private int _r4_completedActionItems;
        private int _r4_pendingActionItems;
        private int _r4_completionPercent;
        private double _r4_avgDaysToComplete;
        private int _r4_carryOverCount;
        private SeriesCollection _r4_completionTrendSeries = new();
        private ObservableCollection<string> _r4_completionTrendLabels = new();
        private ObservableCollection<ActionItemRow> _r4_pendingItems = new();
        private SeriesCollection _r4_byTeamMemberSeries = new();
        private ObservableCollection<string> _r4_byTeamMemberLabels = new();

        // Report 5: OKR Progress
        private int _r5_totalOkrs;
        private int _r5_onTrackCount;
        private int _r5_atRiskCount;
        private int _r5_offTrackCount;
        private double _r5_avgProgress;
        private int _r5_totalKeyResults;
        private int _r5_completedKeyResults;
        private SeriesCollection _r5_statusDistributionSeries = new();
        private SeriesCollection _r5_progressByOwnerSeries = new();
        private ObservableCollection<string> _r5_progressByOwnerLabels = new();
        private ObservableCollection<OkrRow> _r5_okrsList = new();
        private SeriesCollection _r5_progressTrendSeries = new();
        private ObservableCollection<string> _r5_progressTrendLabels = new();

        // Report 6: KPI Performance
        private int _r6_totalKpis;
        private int _r6_onTargetCount;
        private int _r6_closeToTargetCount;
        private int _r6_offTargetCount;
        private double _r6_avgPerformance;
        private int _r6_needsAttentionCount;
        private SeriesCollection _r6_statusDistributionSeries = new();
        private SeriesCollection _r6_performanceByCategorySeries = new();
        private ObservableCollection<string> _r6_performanceByCategoryLabels = new();
        private ObservableCollection<KpiRow> _r6_kpisList = new();
        private SeriesCollection _r6_targetVsActualSeries = new();
        private ObservableCollection<string> _r6_targetVsActualLabels = new();

        // Report 7: Goal Tracker
        private int _r7_totalGoals;
        private int _r7_completedGoals;
        private int _r7_inProgressGoals;
        private int _r7_overdueGoals;
        private double _r7_avgProgress;
        private int _r7_completionPercent;
        private SeriesCollection _r7_statusDistributionSeries = new();
        private SeriesCollection _r7_goalsByCategorySeries = new();
        private ObservableCollection<string> _r7_goalsByCategoryLabels = new();
        private ObservableCollection<GoalRow> _r7_goalsList = new();
        private SeriesCollection _r7_progressByMemberSeries = new();
        private ObservableCollection<string> _r7_progressByMemberLabels = new();

        // Report 8: Project Health
        private int _r8_totalProjects;
        private int _r8_completedProjects;
        private int _r8_inProgressProjects;
        private int _r8_atRiskProjects;
        private double _r8_avgProgress;
        private int _r8_overdueCount;
        private SeriesCollection _r8_statusDistributionSeries = new();
        private SeriesCollection _r8_progressByProjectSeries = new();
        private ObservableCollection<string> _r8_progressByProjectLabels = new();
        private ObservableCollection<ProjectRow> _r8_projectsList = new();

        // Report 9: Feedback Trends
        private int _r9_totalFeedback;
        private int _r9_positiveFeedback;
        private int _r9_constructiveFeedback;
        private int _r9_recognitionCount;
        private double _r9_positiveRatio;
        private SeriesCollection _r9_typeDistributionSeries = new();
        private SeriesCollection _r9_feedbackTrendSeries = new();
        private ObservableCollection<string> _r9_feedbackTrendLabels = new();
        private SeriesCollection _r9_byRecipientSeries = new();
        private ObservableCollection<string> _r9_byRecipientLabels = new();
        private ObservableCollection<FeedbackRow> _r9_recentFeedback = new();

        // Report 10: Team Comparison
        private int _r10_totalMembers;
        private double _r10_avgTaskCompletion;
        private double _r10_avgMeetingFrequency;
        private double _r10_avgGoalProgress;
        private ObservableCollection<TeamComparisonRow> _r10_comparisonList = new();
        private SeriesCollection _r10_taskCompletionSeries = new();
        private ObservableCollection<string> _r10_taskCompletionLabels = new();
        private SeriesCollection _r10_engagementSeries = new();
        private ObservableCollection<string> _r10_engagementLabels = new();

        // Report 11: Performance Review Prep
        private string _r11_selectedMemberName = string.Empty;
        private int _r11_totalMeetings;
        private int _r11_totalTasks;
        private int _r11_completedTasks;
        private int _r11_totalGoals;
        private int _r11_completedGoals;
        private int _r11_feedbackCount;
        private int _r11_positiveFeedbackCount;
        private double _r11_taskCompletionRate;
        private double _r11_goalCompletionRate;
        private ObservableCollection<OneOnOne> _r11_meetingHistory = new();
        private ObservableCollection<FeedbackRow> _r11_feedbackHistory = new();
        private ObservableCollection<IndividualGoal> _r11_goalsList = new();
        private SeriesCollection _r11_performanceTrendSeries = new();
        private ObservableCollection<string> _r11_performanceTrendLabels = new();

        // Report 12: Executive Summary
        private int _r12_totalTeamMembers;
        private int _r12_activeMembers;
        private int _r12_meetingsThisPeriod;
        private int _r12_meetingCadencePercent;
        private int _r12_tasksTotal;
        private int _r12_tasksCompleted;
        private int _r12_taskCompletionPercent;
        private int _r12_okrsTotal;
        private int _r12_okrsOnTrack;
        private int _r12_okrHealthPercent;
        private int _r12_kpisTotal;
        private int _r12_kpisOnTarget;
        private int _r12_kpiHealthPercent;
        private int _r12_goalsTotal;
        private int _r12_goalsCompleted;
        private int _r12_goalCompletionPercent;
        private int _r12_feedbackTotal;
        private int _r12_positiveFeedback;
        private double _r12_positiveRatio;
        private string _r12_overallHealthStatus = "Good";
        private Brush _r12_overallHealthColor = Brushes.Green;
        private int _r12_overallHealthScore;
        private SeriesCollection _r12_healthTrendSeries = new();
        private ObservableCollection<string> _r12_healthTrendLabels = new();
        private SeriesCollection _r12_metricsSummarySeries = new();
        private ObservableCollection<ExecutiveSummaryItem> _r12_keyHighlights = new();
        private ObservableCollection<ExecutiveSummaryItem> _r12_areasOfConcern = new();

        // Commands
        private ICommand? _exportAllCommand;
        private ICommand? _exportCurrentReportCommand;
        private ICommand? _refreshReportCommand;

        // Legacy export commands
        private ICommand? _exportTeamMembersCommand;
        private ICommand? _exportOneOnOnesCommand;
        private ICommand? _exportTasksCommand;
        private ICommand? _exportProjectsCommand;
        private ICommand? _exportOKRsCommand;
        private ICommand? _exportKPIsCommand;

        #endregion

        #region Report Definitions

        private static readonly Dictionary<int, ReportDefinition> _reportDefinitions = new()
        {
            { 1, new ReportDefinition("1:1 Effectiveness", "Analyze the quality and outcomes of your 1:1 meetings", true, new[]
                {
                    "Meeting frequency and duration trends",
                    "Agenda topics breakdown by category",
                    "Action item creation and completion rates",
                    "Individual team member filtering",
                    "Historical comparison over time"
                })
            },
            { 2, new ReportDefinition("Meeting Cadence", "Track your 1:1 meeting frequency and consistency", false, new[]
                {
                    "Days since last 1:1 for each team member",
                    "Cadence compliance percentage",
                    "Meeting streak tracking",
                    "Schedule recommendations"
                })
            },
            { 3, new ReportDefinition("Task Completion", "Monitor task progress and completion trends", false, new[]
                {
                    "Task completion rate over time",
                    "Burndown charts",
                    "Tasks by owner and priority",
                    "Overdue task analysis"
                })
            },
            { 4, new ReportDefinition("Action Item Follow-Up", "Track follow-through on 1:1 action items", false, new[]
                {
                    "Action items created vs completed",
                    "Average time to completion",
                    "Carry-over items between meetings",
                    "Owner accountability metrics"
                })
            },
            { 5, new ReportDefinition("OKR Progress", "Track objective and key result progress across your team", true, new[]
                {
                    "OKR status distribution (On Track, At Risk, Off Track)",
                    "Progress tracking by time period",
                    "Key result completion rates",
                    "Progress breakdown by owner"
                })
            },
            { 6, new ReportDefinition("KPI Performance", "Monitor key performance indicators vs targets", true, new[]
                {
                    "KPI actual vs target comparison",
                    "Performance by category breakdown",
                    "Status distribution (On/Close/Off target)",
                    "KPIs requiring attention"
                })
            },
            { 7, new ReportDefinition("Goal Tracker", "Track individual development goals across your team", true, new[]
                {
                    "Goal completion rates",
                    "Progress by team member",
                    "Goals by category breakdown",
                    "Overdue goal tracking"
                })
            },
            { 8, new ReportDefinition("Project Health", "Overview of project status and health across your team", true, new[]
                {
                    "Project status breakdown",
                    "Progress tracking by project",
                    "Overdue project alerts",
                    "Task completion within projects"
                })
            },
            { 9, new ReportDefinition("Feedback Trends", "Analyze feedback patterns across your team", true, new[]
                {
                    "Feedback volume over time",
                    "Positive vs constructive ratio",
                    "Feedback by recipient breakdown",
                    "Recent feedback history"
                })
            },
            { 10, new ReportDefinition("Team Comparison", "Compare performance metrics across all team members", false, new[]
                {
                    "Side-by-side performance metrics",
                    "Task completion comparison",
                    "Meeting engagement comparison",
                    "Goal progress comparison"
                })
            },
            { 11, new ReportDefinition("Performance Review Prep", "Comprehensive data for performance reviews", true, new[]
                {
                    "Complete 1:1 meeting history",
                    "Task and goal achievements",
                    "Feedback received summary",
                    "Performance trend over time",
                    "Exportable review data"
                })
            },
            { 12, new ReportDefinition("Executive Summary", "High-level overview for leadership", false, new[]
                {
                    "Team health scorecard",
                    "Key metrics dashboard",
                    "Trends and highlights",
                    "Risks and recommendations",
                    "QBR-ready format"
                })
            }
        };

        #endregion

        #region Ctor

        public ReportsViewModel(Action? callback) : base(callback)
        {
            InitializeDateRangeOptions();
            _ = LoadDataAsync();
        }

        #endregion

        #region Commands

        public ICommand ExportAllCommand => _exportAllCommand ??=
            new TrackerCommand(ExecuteExportAll, CanExport);

        public ICommand ExportCurrentReportCommand => _exportCurrentReportCommand ??=
            new TrackerCommand(ExecuteExportCurrentReport, CanExport);

        public ICommand RefreshReportCommand => _refreshReportCommand ??=
            new TrackerCommand(ExecuteRefreshReport, _ => !IsLoading);

        // Legacy commands for backward compatibility
        public ICommand ExportTeamMembersCommand => _exportTeamMembersCommand ??=
            new TrackerCommand(ExecuteExportTeamMembers, CanExport);

        public ICommand ExportOneOnOnesCommand => _exportOneOnOnesCommand ??=
            new TrackerCommand(ExecuteExportOneOnOnes, CanExport);

        public ICommand ExportTasksCommand => _exportTasksCommand ??=
            new TrackerCommand(ExecuteExportTasks, CanExport);

        public ICommand ExportProjectsCommand => _exportProjectsCommand ??=
            new TrackerCommand(ExecuteExportProjects, CanExport);

        public ICommand ExportOKRsCommand => _exportOKRsCommand ??=
            new TrackerCommand(ExecuteExportOKRs, CanExport);

        public ICommand ExportKPIsCommand => _exportKPIsCommand ??=
            new TrackerCommand(ExecuteExportKPIs, CanExport);

        #endregion

        #region Properties - Report Selection

        public bool IsReport1Selected
        {
            get => _isReport1Selected;
            set { _isReport1Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport2Selected
        {
            get => _isReport2Selected;
            set { _isReport2Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport3Selected
        {
            get => _isReport3Selected;
            set { _isReport3Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport4Selected
        {
            get => _isReport4Selected;
            set { _isReport4Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport5Selected
        {
            get => _isReport5Selected;
            set { _isReport5Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport6Selected
        {
            get => _isReport6Selected;
            set { _isReport6Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport7Selected
        {
            get => _isReport7Selected;
            set { _isReport7Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport8Selected
        {
            get => _isReport8Selected;
            set { _isReport8Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport9Selected
        {
            get => _isReport9Selected;
            set { _isReport9Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport10Selected
        {
            get => _isReport10Selected;
            set { _isReport10Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport11Selected
        {
            get => _isReport11Selected;
            set { _isReport11Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        public bool IsReport12Selected
        {
            get => _isReport12Selected;
            set { _isReport12Selected = value; RaisePropertyChanged(); OnReportSelectionChanged(); }
        }

        #endregion

        #region Properties - Selected Report Info

        public string SelectedReportTitle => _reportDefinitions.TryGetValue(GetSelectedReportIndex(), out var def) ? def.Title : "Select a Report";
        public string SelectedReportDescription => _reportDefinitions.TryGetValue(GetSelectedReportIndex(), out var def) ? def.Description : "";
        public bool ShowTeamMemberFilter => _reportDefinitions.TryGetValue(GetSelectedReportIndex(), out var def) && def.SupportsTeamMemberFilter;
        public ObservableCollection<string> SelectedReportFeatures => new(_reportDefinitions.TryGetValue(GetSelectedReportIndex(), out var def) ? def.Features : Array.Empty<string>());

        public bool IsPlaceholderReport
        {
            get
            {
                var index = GetSelectedReportIndex();
                // All 12 reports are now implemented
                return index > 12;
            }
        }

        #endregion

        #region Properties - Filters

        public ObservableCollection<TeamMemberFilterItem> TeamMemberFilterOptions
        {
            get => _teamMemberFilterOptions;
            set { _teamMemberFilterOptions = value; RaisePropertyChanged(); }
        }

        public TeamMemberFilterItem? SelectedTeamMemberFilter
        {
            get => _selectedTeamMemberFilter;
            set
            {
                _selectedTeamMemberFilter = value;
                RaisePropertyChanged();
                _ = RefreshCurrentReportAsync();
            }
        }

        public ObservableCollection<string> DateRangeOptions
        {
            get => _dateRangeOptions;
            set { _dateRangeOptions = value; RaisePropertyChanged(); }
        }

        public string SelectedDateRange
        {
            get => _selectedDateRange;
            set
            {
                _selectedDateRange = value;
                RaisePropertyChanged();
                _ = RefreshCurrentReportAsync();
            }
        }

        #endregion

        #region Properties - State

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; RaisePropertyChanged(); }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set { _isExporting = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 1: 1:1 Effectiveness

        public int R1_TotalMeetings
        {
            get => _r1_totalMeetings;
            set { _r1_totalMeetings = value; RaisePropertyChanged(); }
        }

        public int R1_AvgDuration
        {
            get => _r1_avgDuration;
            set { _r1_avgDuration = value; RaisePropertyChanged(); }
        }

        public int R1_ActionsCreated
        {
            get => _r1_actionsCreated;
            set { _r1_actionsCreated = value; RaisePropertyChanged(); }
        }

        public int R1_CompletionRate
        {
            get => _r1_completionRate;
            set { _r1_completionRate = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R1_MeetingFrequencySeries
        {
            get => _r1_meetingFrequencySeries;
            set { _r1_meetingFrequencySeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R1_MeetingFrequencyLabels
        {
            get => _r1_meetingFrequencyLabels;
            set { _r1_meetingFrequencyLabels = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R1_AgendaTopicsSeries
        {
            get => _r1_agendaTopicsSeries;
            set { _r1_agendaTopicsSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<OneOnOne> R1_RecentMeetings
        {
            get => _r1_recentMeetings;
            set { _r1_recentMeetings = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 2: Meeting Cadence

        public int R2_TotalTeamMembers
        {
            get => _r2_totalTeamMembers;
            set { _r2_totalTeamMembers = value; RaisePropertyChanged(); }
        }

        public int R2_OnTrackCount
        {
            get => _r2_onTrackCount;
            set { _r2_onTrackCount = value; RaisePropertyChanged(); }
        }

        public int R2_OverdueCount
        {
            get => _r2_overdueCount;
            set { _r2_overdueCount = value; RaisePropertyChanged(); }
        }

        public int R2_NeverMetCount
        {
            get => _r2_neverMetCount;
            set { _r2_neverMetCount = value; RaisePropertyChanged(); }
        }

        public int R2_CadenceCompliancePercent
        {
            get => _r2_cadenceCompliancePercent;
            set { _r2_cadenceCompliancePercent = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<MemberCadenceRow> R2_MemberCadenceList
        {
            get => _r2_memberCadenceList;
            set { _r2_memberCadenceList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R2_CadenceDistributionSeries
        {
            get => _r2_cadenceDistributionSeries;
            set { _r2_cadenceDistributionSeries = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 3: Task Completion

        public int R3_TotalTasks
        {
            get => _r3_totalTasks;
            set { _r3_totalTasks = value; RaisePropertyChanged(); }
        }

        public int R3_CompletedTasks
        {
            get => _r3_completedTasks;
            set { _r3_completedTasks = value; RaisePropertyChanged(); }
        }

        public int R3_OverdueTasks
        {
            get => _r3_overdueTasksCount;
            set { _r3_overdueTasksCount = value; RaisePropertyChanged(); }
        }

        public int R3_CompletionPercent
        {
            get => _r3_completionPercent;
            set { _r3_completionPercent = value; RaisePropertyChanged(); }
        }

        public double R3_AvgDaysToComplete
        {
            get => _r3_avgDaysToComplete;
            set { _r3_avgDaysToComplete = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R3_CompletionTrendSeries
        {
            get => _r3_completionTrendSeries;
            set { _r3_completionTrendSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R3_CompletionTrendLabels
        {
            get => _r3_completionTrendLabels;
            set { _r3_completionTrendLabels = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R3_TasksByOwnerSeries
        {
            get => _r3_tasksByOwnerSeries;
            set { _r3_tasksByOwnerSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R3_TasksByOwnerLabels
        {
            get => _r3_tasksByOwnerLabels;
            set { _r3_tasksByOwnerLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<IndividualTask> R3_OverdueTasksList
        {
            get => _r3_overdueTasksList;
            set { _r3_overdueTasksList = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 4: Action Item Follow-Up

        public int R4_TotalActionItems
        {
            get => _r4_totalActionItems;
            set { _r4_totalActionItems = value; RaisePropertyChanged(); }
        }

        public int R4_CompletedActionItems
        {
            get => _r4_completedActionItems;
            set { _r4_completedActionItems = value; RaisePropertyChanged(); }
        }

        public int R4_PendingActionItems
        {
            get => _r4_pendingActionItems;
            set { _r4_pendingActionItems = value; RaisePropertyChanged(); }
        }

        public int R4_CompletionPercent
        {
            get => _r4_completionPercent;
            set { _r4_completionPercent = value; RaisePropertyChanged(); }
        }

        public double R4_AvgDaysToComplete
        {
            get => _r4_avgDaysToComplete;
            set { _r4_avgDaysToComplete = value; RaisePropertyChanged(); }
        }

        public int R4_CarryOverCount
        {
            get => _r4_carryOverCount;
            set { _r4_carryOverCount = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R4_CompletionTrendSeries
        {
            get => _r4_completionTrendSeries;
            set { _r4_completionTrendSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R4_CompletionTrendLabels
        {
            get => _r4_completionTrendLabels;
            set { _r4_completionTrendLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<ActionItemRow> R4_PendingItems
        {
            get => _r4_pendingItems;
            set { _r4_pendingItems = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R4_ByTeamMemberSeries
        {
            get => _r4_byTeamMemberSeries;
            set { _r4_byTeamMemberSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R4_ByTeamMemberLabels
        {
            get => _r4_byTeamMemberLabels;
            set { _r4_byTeamMemberLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 5: OKR Progress

        public int R5_TotalOkrs
        {
            get => _r5_totalOkrs;
            set { _r5_totalOkrs = value; RaisePropertyChanged(); }
        }

        public int R5_OnTrackCount
        {
            get => _r5_onTrackCount;
            set { _r5_onTrackCount = value; RaisePropertyChanged(); }
        }

        public int R5_AtRiskCount
        {
            get => _r5_atRiskCount;
            set { _r5_atRiskCount = value; RaisePropertyChanged(); }
        }

        public int R5_OffTrackCount
        {
            get => _r5_offTrackCount;
            set { _r5_offTrackCount = value; RaisePropertyChanged(); }
        }

        public double R5_AvgProgress
        {
            get => _r5_avgProgress;
            set { _r5_avgProgress = value; RaisePropertyChanged(); }
        }

        public int R5_TotalKeyResults
        {
            get => _r5_totalKeyResults;
            set { _r5_totalKeyResults = value; RaisePropertyChanged(); }
        }

        public int R5_CompletedKeyResults
        {
            get => _r5_completedKeyResults;
            set { _r5_completedKeyResults = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R5_StatusDistributionSeries
        {
            get => _r5_statusDistributionSeries;
            set { _r5_statusDistributionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R5_ProgressByOwnerSeries
        {
            get => _r5_progressByOwnerSeries;
            set { _r5_progressByOwnerSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R5_ProgressByOwnerLabels
        {
            get => _r5_progressByOwnerLabels;
            set { _r5_progressByOwnerLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<OkrRow> R5_OkrsList
        {
            get => _r5_okrsList;
            set { _r5_okrsList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R5_ProgressTrendSeries
        {
            get => _r5_progressTrendSeries;
            set { _r5_progressTrendSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R5_ProgressTrendLabels
        {
            get => _r5_progressTrendLabels;
            set { _r5_progressTrendLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 6: KPI Performance

        public int R6_TotalKpis
        {
            get => _r6_totalKpis;
            set { _r6_totalKpis = value; RaisePropertyChanged(); }
        }

        public int R6_OnTargetCount
        {
            get => _r6_onTargetCount;
            set { _r6_onTargetCount = value; RaisePropertyChanged(); }
        }

        public int R6_CloseToTargetCount
        {
            get => _r6_closeToTargetCount;
            set { _r6_closeToTargetCount = value; RaisePropertyChanged(); }
        }

        public int R6_OffTargetCount
        {
            get => _r6_offTargetCount;
            set { _r6_offTargetCount = value; RaisePropertyChanged(); }
        }

        public double R6_AvgPerformance
        {
            get => _r6_avgPerformance;
            set { _r6_avgPerformance = value; RaisePropertyChanged(); }
        }

        public int R6_NeedsAttentionCount
        {
            get => _r6_needsAttentionCount;
            set { _r6_needsAttentionCount = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R6_StatusDistributionSeries
        {
            get => _r6_statusDistributionSeries;
            set { _r6_statusDistributionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R6_PerformanceByCategorySeries
        {
            get => _r6_performanceByCategorySeries;
            set { _r6_performanceByCategorySeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R6_PerformanceByCategoryLabels
        {
            get => _r6_performanceByCategoryLabels;
            set { _r6_performanceByCategoryLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<KpiRow> R6_KpisList
        {
            get => _r6_kpisList;
            set { _r6_kpisList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R6_TargetVsActualSeries
        {
            get => _r6_targetVsActualSeries;
            set { _r6_targetVsActualSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R6_TargetVsActualLabels
        {
            get => _r6_targetVsActualLabels;
            set { _r6_targetVsActualLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 7: Goal Tracker

        public int R7_TotalGoals
        {
            get => _r7_totalGoals;
            set { _r7_totalGoals = value; RaisePropertyChanged(); }
        }

        public int R7_CompletedGoals
        {
            get => _r7_completedGoals;
            set { _r7_completedGoals = value; RaisePropertyChanged(); }
        }

        public int R7_InProgressGoals
        {
            get => _r7_inProgressGoals;
            set { _r7_inProgressGoals = value; RaisePropertyChanged(); }
        }

        public int R7_OverdueGoals
        {
            get => _r7_overdueGoals;
            set { _r7_overdueGoals = value; RaisePropertyChanged(); }
        }

        public double R7_AvgProgress
        {
            get => _r7_avgProgress;
            set { _r7_avgProgress = value; RaisePropertyChanged(); }
        }

        public int R7_CompletionPercent
        {
            get => _r7_completionPercent;
            set { _r7_completionPercent = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R7_StatusDistributionSeries
        {
            get => _r7_statusDistributionSeries;
            set { _r7_statusDistributionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R7_GoalsByCategorySeries
        {
            get => _r7_goalsByCategorySeries;
            set { _r7_goalsByCategorySeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R7_GoalsByCategoryLabels
        {
            get => _r7_goalsByCategoryLabels;
            set { _r7_goalsByCategoryLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<GoalRow> R7_GoalsList
        {
            get => _r7_goalsList;
            set { _r7_goalsList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R7_ProgressByMemberSeries
        {
            get => _r7_progressByMemberSeries;
            set { _r7_progressByMemberSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R7_ProgressByMemberLabels
        {
            get => _r7_progressByMemberLabels;
            set { _r7_progressByMemberLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 8: Project Health

        public int R8_TotalProjects
        {
            get => _r8_totalProjects;
            set { _r8_totalProjects = value; RaisePropertyChanged(); }
        }

        public int R8_CompletedProjects
        {
            get => _r8_completedProjects;
            set { _r8_completedProjects = value; RaisePropertyChanged(); }
        }

        public int R8_InProgressProjects
        {
            get => _r8_inProgressProjects;
            set { _r8_inProgressProjects = value; RaisePropertyChanged(); }
        }

        public int R8_AtRiskProjects
        {
            get => _r8_atRiskProjects;
            set { _r8_atRiskProjects = value; RaisePropertyChanged(); }
        }

        public double R8_AvgProgress
        {
            get => _r8_avgProgress;
            set { _r8_avgProgress = value; RaisePropertyChanged(); }
        }

        public int R8_OverdueCount
        {
            get => _r8_overdueCount;
            set { _r8_overdueCount = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R8_StatusDistributionSeries
        {
            get => _r8_statusDistributionSeries;
            set { _r8_statusDistributionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R8_ProgressByProjectSeries
        {
            get => _r8_progressByProjectSeries;
            set { _r8_progressByProjectSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R8_ProgressByProjectLabels
        {
            get => _r8_progressByProjectLabels;
            set { _r8_progressByProjectLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<ProjectRow> R8_ProjectsList
        {
            get => _r8_projectsList;
            set { _r8_projectsList = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 9: Feedback Trends

        public int R9_TotalFeedback
        {
            get => _r9_totalFeedback;
            set { _r9_totalFeedback = value; RaisePropertyChanged(); }
        }

        public int R9_PositiveFeedback
        {
            get => _r9_positiveFeedback;
            set { _r9_positiveFeedback = value; RaisePropertyChanged(); }
        }

        public int R9_ConstructiveFeedback
        {
            get => _r9_constructiveFeedback;
            set { _r9_constructiveFeedback = value; RaisePropertyChanged(); }
        }

        public int R9_RecognitionCount
        {
            get => _r9_recognitionCount;
            set { _r9_recognitionCount = value; RaisePropertyChanged(); }
        }

        public double R9_PositiveRatio
        {
            get => _r9_positiveRatio;
            set { _r9_positiveRatio = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R9_TypeDistributionSeries
        {
            get => _r9_typeDistributionSeries;
            set { _r9_typeDistributionSeries = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R9_FeedbackTrendSeries
        {
            get => _r9_feedbackTrendSeries;
            set { _r9_feedbackTrendSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R9_FeedbackTrendLabels
        {
            get => _r9_feedbackTrendLabels;
            set { _r9_feedbackTrendLabels = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R9_ByRecipientSeries
        {
            get => _r9_byRecipientSeries;
            set { _r9_byRecipientSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R9_ByRecipientLabels
        {
            get => _r9_byRecipientLabels;
            set { _r9_byRecipientLabels = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<FeedbackRow> R9_RecentFeedback
        {
            get => _r9_recentFeedback;
            set { _r9_recentFeedback = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 10: Team Comparison

        public int R10_TotalMembers
        {
            get => _r10_totalMembers;
            set { _r10_totalMembers = value; RaisePropertyChanged(); }
        }

        public double R10_AvgTaskCompletion
        {
            get => _r10_avgTaskCompletion;
            set { _r10_avgTaskCompletion = value; RaisePropertyChanged(); }
        }

        public double R10_AvgMeetingFrequency
        {
            get => _r10_avgMeetingFrequency;
            set { _r10_avgMeetingFrequency = value; RaisePropertyChanged(); }
        }

        public double R10_AvgGoalProgress
        {
            get => _r10_avgGoalProgress;
            set { _r10_avgGoalProgress = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<TeamComparisonRow> R10_ComparisonList
        {
            get => _r10_comparisonList;
            set { _r10_comparisonList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R10_TaskCompletionSeries
        {
            get => _r10_taskCompletionSeries;
            set { _r10_taskCompletionSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R10_TaskCompletionLabels
        {
            get => _r10_taskCompletionLabels;
            set { _r10_taskCompletionLabels = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R10_EngagementSeries
        {
            get => _r10_engagementSeries;
            set { _r10_engagementSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R10_EngagementLabels
        {
            get => _r10_engagementLabels;
            set { _r10_engagementLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 11: Performance Review Prep

        public string R11_SelectedMemberName
        {
            get => _r11_selectedMemberName;
            set { _r11_selectedMemberName = value; RaisePropertyChanged(); }
        }

        public int R11_TotalMeetings
        {
            get => _r11_totalMeetings;
            set { _r11_totalMeetings = value; RaisePropertyChanged(); }
        }

        public int R11_TotalTasks
        {
            get => _r11_totalTasks;
            set { _r11_totalTasks = value; RaisePropertyChanged(); }
        }

        public int R11_CompletedTasks
        {
            get => _r11_completedTasks;
            set { _r11_completedTasks = value; RaisePropertyChanged(); }
        }

        public int R11_TotalGoals
        {
            get => _r11_totalGoals;
            set { _r11_totalGoals = value; RaisePropertyChanged(); }
        }

        public int R11_CompletedGoals
        {
            get => _r11_completedGoals;
            set { _r11_completedGoals = value; RaisePropertyChanged(); }
        }

        public int R11_FeedbackCount
        {
            get => _r11_feedbackCount;
            set { _r11_feedbackCount = value; RaisePropertyChanged(); }
        }

        public int R11_PositiveFeedbackCount
        {
            get => _r11_positiveFeedbackCount;
            set { _r11_positiveFeedbackCount = value; RaisePropertyChanged(); }
        }

        public double R11_TaskCompletionRate
        {
            get => _r11_taskCompletionRate;
            set { _r11_taskCompletionRate = value; RaisePropertyChanged(); }
        }

        public double R11_GoalCompletionRate
        {
            get => _r11_goalCompletionRate;
            set { _r11_goalCompletionRate = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<OneOnOne> R11_MeetingHistory
        {
            get => _r11_meetingHistory;
            set { _r11_meetingHistory = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<FeedbackRow> R11_FeedbackHistory
        {
            get => _r11_feedbackHistory;
            set { _r11_feedbackHistory = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<IndividualGoal> R11_GoalsList
        {
            get => _r11_goalsList;
            set { _r11_goalsList = value; RaisePropertyChanged(); }
        }

        public SeriesCollection R11_PerformanceTrendSeries
        {
            get => _r11_performanceTrendSeries;
            set { _r11_performanceTrendSeries = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<string> R11_PerformanceTrendLabels
        {
            get => _r11_performanceTrendLabels;
            set { _r11_performanceTrendLabels = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Properties - Report 12: Executive Summary

        public int R12_TotalTeamMembers { get => _r12_totalTeamMembers; set { _r12_totalTeamMembers = value; RaisePropertyChanged(); } }
        public int R12_ActiveMembers { get => _r12_activeMembers; set { _r12_activeMembers = value; RaisePropertyChanged(); } }
        public int R12_MeetingsThisPeriod { get => _r12_meetingsThisPeriod; set { _r12_meetingsThisPeriod = value; RaisePropertyChanged(); } }
        public int R12_MeetingCadencePercent { get => _r12_meetingCadencePercent; set { _r12_meetingCadencePercent = value; RaisePropertyChanged(); } }
        public int R12_TasksTotal { get => _r12_tasksTotal; set { _r12_tasksTotal = value; RaisePropertyChanged(); } }
        public int R12_TasksCompleted { get => _r12_tasksCompleted; set { _r12_tasksCompleted = value; RaisePropertyChanged(); } }
        public int R12_TaskCompletionPercent { get => _r12_taskCompletionPercent; set { _r12_taskCompletionPercent = value; RaisePropertyChanged(); } }
        public int R12_OkrsTotal { get => _r12_okrsTotal; set { _r12_okrsTotal = value; RaisePropertyChanged(); } }
        public int R12_OkrsOnTrack { get => _r12_okrsOnTrack; set { _r12_okrsOnTrack = value; RaisePropertyChanged(); } }
        public int R12_OkrHealthPercent { get => _r12_okrHealthPercent; set { _r12_okrHealthPercent = value; RaisePropertyChanged(); } }
        public int R12_KpisTotal { get => _r12_kpisTotal; set { _r12_kpisTotal = value; RaisePropertyChanged(); } }
        public int R12_KpisOnTarget { get => _r12_kpisOnTarget; set { _r12_kpisOnTarget = value; RaisePropertyChanged(); } }
        public int R12_KpiHealthPercent { get => _r12_kpiHealthPercent; set { _r12_kpiHealthPercent = value; RaisePropertyChanged(); } }
        public int R12_GoalsTotal { get => _r12_goalsTotal; set { _r12_goalsTotal = value; RaisePropertyChanged(); } }
        public int R12_GoalsCompleted { get => _r12_goalsCompleted; set { _r12_goalsCompleted = value; RaisePropertyChanged(); } }
        public int R12_GoalCompletionPercent { get => _r12_goalCompletionPercent; set { _r12_goalCompletionPercent = value; RaisePropertyChanged(); } }
        public int R12_FeedbackTotal { get => _r12_feedbackTotal; set { _r12_feedbackTotal = value; RaisePropertyChanged(); } }
        public int R12_PositiveFeedback { get => _r12_positiveFeedback; set { _r12_positiveFeedback = value; RaisePropertyChanged(); } }
        public double R12_PositiveRatio { get => _r12_positiveRatio; set { _r12_positiveRatio = value; RaisePropertyChanged(); } }
        public string R12_OverallHealthStatus { get => _r12_overallHealthStatus; set { _r12_overallHealthStatus = value; RaisePropertyChanged(); } }
        public Brush R12_OverallHealthColor { get => _r12_overallHealthColor; set { _r12_overallHealthColor = value; RaisePropertyChanged(); } }
        public int R12_OverallHealthScore { get => _r12_overallHealthScore; set { _r12_overallHealthScore = value; RaisePropertyChanged(); } }
        public SeriesCollection R12_HealthTrendSeries { get => _r12_healthTrendSeries; set { _r12_healthTrendSeries = value; RaisePropertyChanged(); } }
        public ObservableCollection<string> R12_HealthTrendLabels { get => _r12_healthTrendLabels; set { _r12_healthTrendLabels = value; RaisePropertyChanged(); } }
        public SeriesCollection R12_MetricsSummarySeries { get => _r12_metricsSummarySeries; set { _r12_metricsSummarySeries = value; RaisePropertyChanged(); } }
        public ObservableCollection<ExecutiveSummaryItem> R12_KeyHighlights { get => _r12_keyHighlights; set { _r12_keyHighlights = value; RaisePropertyChanged(); } }
        public ObservableCollection<ExecutiveSummaryItem> R12_AreasOfConcern { get => _r12_areasOfConcern; set { _r12_areasOfConcern = value; RaisePropertyChanged(); } }

        #endregion

        #region Private Methods - Initialization

        private void InitializeDateRangeOptions()
        {
            DateRangeOptions = new ObservableCollection<string>
            {
                "Last 7 Days",
                "Last 30 Days",
                "Last 90 Days",
                "This Quarter",
                "Last Quarter",
                "This Year",
                "All Time"
            };
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                _teamMembers = (await TrackerDataManager.Instance.GetTeamData()).ToList();
                _oneOnOnes = (await TrackerDataManager.Instance.GetOneOnOnes()).ToList();
                _tasks = (await TrackerDataManager.Instance.GetTasks()).ToList();
                _okrs = (await TrackerDataManager.Instance.GetOKRs()).ToList();
                _kpis = (await TrackerDataManager.Instance.GetKPIs()).ToList();
                _projects = (await TrackerDataManager.Instance.GetProjects()).ToList();
                _feedbacks = (await TrackerDataManager.Instance.GetFeedbacks()).ToList();

                try
                {
                    _goals = (await TrackerDataManager.Instance.GetGoals()).ToList();
                }
                catch
                {
                    _goals = new List<IndividualGoal>();
                }

                // Initialize team member filter
                var filterOptions = new List<TeamMemberFilterItem>
                {
                    new TeamMemberFilterItem { Id = 0, FullName = "All Team Members" }
                };
                filterOptions.AddRange(_teamMembers.Select(tm => new TeamMemberFilterItem { Id = tm.Id, FullName = tm.FullName }));
                TeamMemberFilterOptions = new ObservableCollection<TeamMemberFilterItem>(filterOptions);
                SelectedTeamMemberFilter = filterOptions.First();

                await RefreshCurrentReportAsync();
            }
            catch (Exception ex)
            {
                _logger.Error("Error loading report data: {0}", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods - Report Selection

        private int GetSelectedReportIndex()
        {
            if (_isReport1Selected) return 1;
            if (_isReport2Selected) return 2;
            if (_isReport3Selected) return 3;
            if (_isReport4Selected) return 4;
            if (_isReport5Selected) return 5;
            if (_isReport6Selected) return 6;
            if (_isReport7Selected) return 7;
            if (_isReport8Selected) return 8;
            if (_isReport9Selected) return 9;
            if (_isReport10Selected) return 10;
            if (_isReport11Selected) return 11;
            if (_isReport12Selected) return 12;
            return 1;
        }

        private void OnReportSelectionChanged()
        {
            RaisePropertyChanged(nameof(SelectedReportTitle));
            RaisePropertyChanged(nameof(SelectedReportDescription));
            RaisePropertyChanged(nameof(ShowTeamMemberFilter));
            RaisePropertyChanged(nameof(SelectedReportFeatures));
            RaisePropertyChanged(nameof(IsPlaceholderReport));
            _ = RefreshCurrentReportAsync();
        }

        private async Task RefreshCurrentReportAsync()
        {
            var index = GetSelectedReportIndex();

            switch (index)
            {
                case 1:
                    await RefreshReport1Async();
                    break;
                case 2:
                    await RefreshReport2Async();
                    break;
                case 3:
                    await RefreshReport3Async();
                    break;
                case 4:
                    await RefreshReport4Async();
                    break;
                case 5:
                    await RefreshReport5Async();
                    break;
                case 6:
                    await RefreshReport6Async();
                    break;
                case 7:
                    await RefreshReport7Async();
                    break;
                case 8:
                    await RefreshReport8Async();
                    break;
                case 9:
                    await RefreshReport9Async();
                    break;
                case 10:
                    await RefreshReport10Async();
                    break;
                case 11:
                    await RefreshReport11Async();
                    break;
                case 12:
                    await RefreshReport12Async();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Private Methods - Report 1: 1:1 Effectiveness

        private async Task RefreshReport1Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var filteredMeetings = _oneOnOnes
                    .Where(m => m.Date >= dateRange.Start && m.Date <= dateRange.End)
                    .ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredMeetings = filteredMeetings
                        .Where(m => m.TeamMember?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                R1_TotalMeetings = filteredMeetings.Count;
                R1_AvgDuration = filteredMeetings.Count > 0
                    ? (int)filteredMeetings.Average(m => m.Duration.TotalMinutes)
                    : 0;

                var allActions = filteredMeetings.SelectMany(m => m.Tasks ?? new List<MeetingTask>()).ToList();
                R1_ActionsCreated = allActions.Count;
                R1_CompletionRate = allActions.Count > 0
                    ? (int)(allActions.Count(t => t.IsCompleted) / (double)allActions.Count * 100)
                    : 0;

                // Meeting frequency chart (by week)
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    UpdateMeetingFrequencyChart(filteredMeetings, dateRange);
                    UpdateAgendaTopicsChart(filteredMeetings);

                    // Recent meetings
                    R1_RecentMeetings = new ObservableCollection<OneOnOne>(
                        filteredMeetings.OrderByDescending(m => m.Date).Take(10));
                });
            });
        }

        private void UpdateMeetingFrequencyChart(List<OneOnOne> meetings, (DateTime Start, DateTime End) dateRange)
        {
            var labels = new List<string>();
            var values = new ChartValues<int>();

            // Group by week
            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            var currentWeekStart = startDate.AddDays(-(int)startDate.DayOfWeek);

            while (currentWeekStart <= endDate)
            {
                var weekEnd = currentWeekStart.AddDays(6);
                var count = meetings.Count(m => m.Date >= currentWeekStart && m.Date <= weekEnd);
                
                labels.Add(currentWeekStart.ToString("MMM d"));
                values.Add(count);
                
                currentWeekStart = currentWeekStart.AddDays(7);
            }

            R1_MeetingFrequencyLabels = new ObservableCollection<string>(labels);
            R1_MeetingFrequencySeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Meetings",
                    Values = values,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Fill = new SolidColorBrush(Color.FromArgb(50, 59, 130, 246)),
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 2
                }
            };
        }

        private void UpdateAgendaTopicsChart(List<OneOnOne> meetings)
        {
            var allAgendaItems = meetings.SelectMany(m => m.AgendaItems ?? new List<AgendaItem>()).ToList();
            
            var categoryGroups = allAgendaItems
                .GroupBy(a => a.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var series = new SeriesCollection();
            var colors = new[]
            {
                Color.FromRgb(59, 130, 246),   // Blue
                Color.FromRgb(16, 185, 129),   // Green
                Color.FromRgb(245, 158, 11),   // Amber
                Color.FromRgb(239, 68, 68),    // Red
                Color.FromRgb(139, 92, 246),   // Purple
                Color.FromRgb(236, 72, 153),   // Pink
            };

            var colorIndex = 0;
            foreach (var group in categoryGroups)
            {
                series.Add(new PieSeries
                {
                    Title = group.Category.ToString(),
                    Values = new ChartValues<ObservableValue> { new ObservableValue(group.Count) },
                    DataLabels = true,
                    Fill = new SolidColorBrush(colors[colorIndex % colors.Length])
                });
                colorIndex++;
            }

            R1_AgendaTopicsSeries = series;
        }

        private (DateTime Start, DateTime End) GetDateRange()
        {
            var end = DateTime.Today;
            var start = SelectedDateRange switch
            {
                "Last 7 Days" => end.AddDays(-7),
                "Last 30 Days" => end.AddDays(-30),
                "Last 90 Days" => end.AddDays(-90),
                "This Quarter" => new DateTime(end.Year, ((end.Month - 1) / 3) * 3 + 1, 1),
                "Last Quarter" => new DateTime(end.Year, ((end.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3),
                "This Year" => new DateTime(end.Year, 1, 1),
                "All Time" => DateTime.MinValue,
                _ => end.AddDays(-30)
            };

            return (start, end);
        }

        #endregion

        #region Private Methods - Report 2: Meeting Cadence

        private async Task RefreshReport2Async()
        {
            await Task.Run(() =>
            {
                var today = DateTime.Today;
                var cadenceThresholdDays = 14; // 2 weeks = on track
                var overdueThresholdDays = 21; // 3 weeks = overdue

                var memberCadenceList = new List<MemberCadenceRow>();
                int onTrack = 0, overdue = 0, neverMet = 0;

                foreach (var member in _teamMembers.Where(m => m.IsActive))
                {
                    var lastMeeting = _oneOnOnes
                        .Where(m => m.TeamMember?.Id == member.Id && m.Date <= today)
                        .OrderByDescending(m => m.Date)
                        .FirstOrDefault();

                    var daysSinceLastMeeting = lastMeeting != null
                        ? (int)(today - lastMeeting.Date).TotalDays
                        : -1; // -1 means never met

                    var status = "Never Met";
                    var statusColor = Brushes.Gray;

                    if (daysSinceLastMeeting == -1)
                    {
                        neverMet++;
                        status = "Never Met";
                        statusColor = new SolidColorBrush(Color.FromRgb(107, 114, 128)); // Gray
                    }
                    else if (daysSinceLastMeeting <= cadenceThresholdDays)
                    {
                        onTrack++;
                        status = "On Track";
                        statusColor = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    }
                    else if (daysSinceLastMeeting <= overdueThresholdDays)
                    {
                        status = "Due Soon";
                        statusColor = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
                    }
                    else
                    {
                        overdue++;
                        status = "Overdue";
                        statusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    }

                    var nextMeetingDate = _oneOnOnes
                        .Where(m => m.TeamMember?.Id == member.Id && m.Date > today)
                        .OrderBy(m => m.Date)
                        .FirstOrDefault()?.Date;

                    memberCadenceList.Add(new MemberCadenceRow
                    {
                        TeamMemberName = member.FullName,
                        DaysSinceLast = daysSinceLastMeeting == -1 ? "Never" : $"{daysSinceLastMeeting}d",
                        LastMeetingDate = lastMeeting?.Date.ToString("MMM d") ?? "—",
                        NextScheduled = nextMeetingDate?.ToString("MMM d") ?? "None",
                        Status = status,
                        StatusColor = statusColor,
                        MeetingCount = _oneOnOnes.Count(m => m.TeamMember?.Id == member.Id)
                    });
                }

                // Sort: Overdue first, then by days since last
                memberCadenceList = memberCadenceList
                    .OrderByDescending(m => m.Status == "Overdue")
                    .ThenByDescending(m => m.Status == "Due Soon")
                    .ThenByDescending(m => m.Status == "Never Met")
                    .ThenBy(m => m.DaysSinceLast == "Never" ? int.MaxValue : int.Parse(m.DaysSinceLast.TrimEnd('d')))
                    .ToList();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R2_TotalTeamMembers = _teamMembers.Count(m => m.IsActive);
                    R2_OnTrackCount = onTrack;
                    R2_OverdueCount = overdue;
                    R2_NeverMetCount = neverMet;
                    R2_CadenceCompliancePercent = R2_TotalTeamMembers > 0
                        ? (int)((double)onTrack / R2_TotalTeamMembers * 100)
                        : 0;
                    R2_MemberCadenceList = new ObservableCollection<MemberCadenceRow>(memberCadenceList);

                    // Cadence distribution pie chart
                    R2_CadenceDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "On Track",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(onTrack) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Overdue",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(overdue) },
                            Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Never Met",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(neverMet) },
                            Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                            DataLabels = true
                        }
                    };
                });
            });
        }

        #endregion

        #region Private Methods - Report 3: Task Completion

        private async Task RefreshReport3Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var filteredTasks = _tasks
                    .Where(t => t.CreatedAt >= dateRange.Start && t.CreatedAt <= dateRange.End)
                    .ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredTasks = filteredTasks
                        .Where(t => t.Owner?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                var completed = filteredTasks.Count(t => t.IsCompleted);
                var overdue = filteredTasks.Count(t => t.IsOverdue && !t.IsCompleted);
                var today = DateTime.Today;

                // Calculate average days to complete (for completed tasks)
                // Use LastModifiedAt as a proxy for completion date for completed tasks
                var completedTasks = filteredTasks.Where(t => t.IsCompleted).ToList();
                var avgDays = completedTasks.Count > 0
                    ? completedTasks.Average(t => (t.LastModifiedAt - t.CreatedAt).TotalDays)
                    : 0;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R3_TotalTasks = filteredTasks.Count;
                    R3_CompletedTasks = completed;
                    R3_OverdueTasks = overdue;
                    R3_CompletionPercent = filteredTasks.Count > 0
                        ? (int)((double)completed / filteredTasks.Count * 100)
                        : 0;
                    R3_AvgDaysToComplete = Math.Round(avgDays, 1);

                    // Completion trend chart (by week)
                    UpdateTaskCompletionTrendChart(filteredTasks, dateRange);

                    // Tasks by owner chart
                    UpdateTasksByOwnerChart(filteredTasks);

                    // Overdue tasks list
                    R3_OverdueTasksList = new ObservableCollection<IndividualTask>(
                        filteredTasks
                            .Where(t => t.IsOverdue && !t.IsCompleted)
                            .OrderBy(t => t.DueDate)
                            .Take(10));
                });
            });
        }

        private void UpdateTaskCompletionTrendChart(List<IndividualTask> tasks, (DateTime Start, DateTime End) dateRange)
        {
            var completedValues = new ChartValues<int>();
            var createdValues = new ChartValues<int>();
            var labels = new List<string>();

            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            var currentWeekStart = startDate.AddDays(-(int)startDate.DayOfWeek);

            while (currentWeekStart <= endDate)
            {
                var weekEnd = currentWeekStart.AddDays(6);
                var created = tasks.Count(t => t.CreatedAt >= currentWeekStart && t.CreatedAt <= weekEnd);
                // Use LastModifiedAt as a proxy for completion date for completed tasks
                var completed = tasks.Count(t => t.IsCompleted && 
                    t.LastModifiedAt >= currentWeekStart && t.LastModifiedAt <= weekEnd);

                labels.Add(currentWeekStart.ToString("MMM d"));
                createdValues.Add(created);
                completedValues.Add(completed);

                currentWeekStart = currentWeekStart.AddDays(7);
            }

            R3_CompletionTrendLabels = new ObservableCollection<string>(labels);
            R3_CompletionTrendSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Created",
                    Values = createdValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 2
                },
                new LineSeries
                {
                    Title = "Completed",
                    Values = completedValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 2
                }
            };
        }

        private void UpdateTasksByOwnerChart(List<IndividualTask> tasks)
        {
            var ownerGroups = tasks
                .Where(t => t.Owner != null)
                .GroupBy(t => t.Owner!.FullName)
                .Select(g => new
                {
                    Name = g.Key,
                    Completed = g.Count(t => t.IsCompleted),
                    Pending = g.Count(t => !t.IsCompleted)
                })
                .OrderByDescending(g => g.Completed + g.Pending)
                .Take(8)
                .ToList();

            var labels = ownerGroups.Select(g => g.Name).ToList();
            var completedValues = new ChartValues<int>(ownerGroups.Select(g => g.Completed));
            var pendingValues = new ChartValues<int>(ownerGroups.Select(g => g.Pending));

            R3_TasksByOwnerLabels = new ObservableCollection<string>(labels);
            R3_TasksByOwnerSeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "Completed",
                    Values = completedValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                },
                new StackedColumnSeries
                {
                    Title = "Pending",
                    Values = pendingValues,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                }
            };
        }

        #endregion

        #region Private Methods - Report 4: Action Item Follow-Up

        private async Task RefreshReport4Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var filteredMeetings = _oneOnOnes
                    .Where(m => m.Date >= dateRange.Start && m.Date <= dateRange.End)
                    .ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredMeetings = filteredMeetings
                        .Where(m => m.TeamMember?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                var allActionItems = filteredMeetings
                    .SelectMany(m => m.Tasks ?? new List<MeetingTask>())
                    .ToList();

                var completed = allActionItems.Count(a => a.IsCompleted);
                var pending = allActionItems.Count(a => !a.IsCompleted);

                // Calculate average days to complete
                // Use LastModifiedAt as a proxy for completion date for completed items
                var completedItems = allActionItems.Where(a => a.IsCompleted).ToList();
                var avgDays = completedItems.Count > 0
                    ? completedItems.Average(a => (a.LastModifiedAt - a.CreatedAt).TotalDays)
                    : 0;

                // Calculate carry-over count (items from older meetings still pending)
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                var carryOver = allActionItems.Count(a => !a.IsCompleted && a.CreatedAt < thirtyDaysAgo);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R4_TotalActionItems = allActionItems.Count;
                    R4_CompletedActionItems = completed;
                    R4_PendingActionItems = pending;
                    R4_CompletionPercent = allActionItems.Count > 0
                        ? (int)((double)completed / allActionItems.Count * 100)
                        : 0;
                    R4_AvgDaysToComplete = Math.Round(avgDays, 1);
                    R4_CarryOverCount = carryOver;

                    // Completion trend chart
                    UpdateActionItemTrendChart(filteredMeetings, dateRange);

                    // By team member chart
                    UpdateActionItemsByTeamMemberChart(filteredMeetings);

                    // Pending items list
                    var pendingItemRows = new List<ActionItemRow>();
                    foreach (var meeting in filteredMeetings)
                    {
                        var pendingTasks = meeting.Tasks?.Where(t => !t.IsCompleted) ?? Enumerable.Empty<MeetingTask>();
                        foreach (var task in pendingTasks)
                        {
                            var daysPending = (int)(DateTime.Today - task.CreatedAt).TotalDays;
                            pendingItemRows.Add(new ActionItemRow
                            {
                                Description = task.Description,
                                TeamMemberName = meeting.TeamMember?.FullName ?? "—",
                                MeetingDate = meeting.Date.ToString("MMM d"),
                                DaysPending = daysPending,
                                IsOverdue = daysPending > 14,
                                StatusColor = daysPending > 14
                                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                                    : daysPending > 7
                                        ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
                                        : new SolidColorBrush(Color.FromRgb(107, 114, 128))
                            });
                        }
                    }

                    R4_PendingItems = new ObservableCollection<ActionItemRow>(
                        pendingItemRows.OrderByDescending(i => i.DaysPending).Take(15));
                });
            });
        }

        private void UpdateActionItemTrendChart(List<OneOnOne> meetings, (DateTime Start, DateTime End) dateRange)
        {
            var createdValues = new ChartValues<int>();
            var completedValues = new ChartValues<int>();
            var labels = new List<string>();

            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            var currentWeekStart = startDate.AddDays(-(int)startDate.DayOfWeek);

            while (currentWeekStart <= endDate)
            {
                var weekEnd = currentWeekStart.AddDays(6);
                var weekMeetings = meetings.Where(m => m.Date >= currentWeekStart && m.Date <= weekEnd).ToList();
                var created = weekMeetings.Sum(m => m.Tasks?.Count ?? 0);
                var completed = weekMeetings.Sum(m => m.Tasks?.Count(t => t.IsCompleted) ?? 0);

                labels.Add(currentWeekStart.ToString("MMM d"));
                createdValues.Add(created);
                completedValues.Add(completed);

                currentWeekStart = currentWeekStart.AddDays(7);
            }

            R4_CompletionTrendLabels = new ObservableCollection<string>(labels);
            R4_CompletionTrendSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Created",
                    Values = createdValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                },
                new ColumnSeries
                {
                    Title = "Completed",
                    Values = completedValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                }
            };
        }

        private void UpdateActionItemsByTeamMemberChart(List<OneOnOne> meetings)
        {
            var memberGroups = meetings
                .Where(m => m.TeamMember != null)
                .GroupBy(m => m.TeamMember!.FullName)
                .Select(g => new
                {
                    Name = g.Key,
                    Completed = g.Sum(m => m.Tasks?.Count(t => t.IsCompleted) ?? 0),
                    Pending = g.Sum(m => m.Tasks?.Count(t => !t.IsCompleted) ?? 0)
                })
                .Where(g => g.Completed + g.Pending > 0)
                .OrderByDescending(g => g.Completed + g.Pending)
                .Take(8)
                .ToList();

            var labels = memberGroups.Select(g => g.Name).ToList();
            var completedValues = new ChartValues<int>(memberGroups.Select(g => g.Completed));
            var pendingValues = new ChartValues<int>(memberGroups.Select(g => g.Pending));

            R4_ByTeamMemberLabels = new ObservableCollection<string>(labels);
            R4_ByTeamMemberSeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "Completed",
                    Values = completedValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                },
                new StackedColumnSeries
                {
                    Title = "Pending",
                    Values = pendingValues,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                }
            };
        }

        #endregion

        #region Private Methods - Report 5: OKR Progress

        private async Task RefreshReport5Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var filteredOkrs = _okrs
                    .Where(o => o.EndDate >= dateRange.Start || o.StartDate <= dateRange.End)
                    .ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredOkrs = filteredOkrs
                        .Where(o => o.Owner?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                var onTrack = filteredOkrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OnTrack);
                var atRisk = filteredOkrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.AtRisk);
                var offTrack = filteredOkrs.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OffTrack);

                var allKeyResults = filteredOkrs.SelectMany(o => o.KeyResults ?? new List<KeyResult>()).ToList();
                var completedKrs = allKeyResults.Count(kr => kr.Progress >= 100);
                var avgProgress = filteredOkrs.Count > 0 ? filteredOkrs.Average(o => o.CompletionPercentage) : 0;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R5_TotalOkrs = filteredOkrs.Count;
                    R5_OnTrackCount = onTrack;
                    R5_AtRiskCount = atRisk;
                    R5_OffTrackCount = offTrack;
                    R5_AvgProgress = Math.Round(avgProgress, 1);
                    R5_TotalKeyResults = allKeyResults.Count;
                    R5_CompletedKeyResults = completedKrs;

                    // Status distribution pie chart
                    R5_StatusDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "On Track",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(onTrack) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "At Risk",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(atRisk) },
                            Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Off Track",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(offTrack) },
                            Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            DataLabels = true
                        }
                    };

                    // Progress by owner chart
                    UpdateOkrProgressByOwnerChart(filteredOkrs);

                    // OKRs list
                    var okrRows = filteredOkrs
                        .OrderByDescending(o => o.Status == Common.Enums.ObjectiveStatusEnum.OffTrack)
                        .ThenByDescending(o => o.Status == Common.Enums.ObjectiveStatusEnum.AtRisk)
                        .ThenBy(o => o.DaysRemaining)
                        .Take(15)
                        .Select(o => new OkrRow
                        {
                            Title = o.Title,
                            OwnerName = o.Owner?.FullName ?? "—",
                            Progress = (int)o.CompletionPercentage,
                            KeyResultCount = o.KeyResults?.Count ?? 0,
                            Status = o.Status.ToString(),
                            StatusColor = GetOkrStatusBrush(o.Status),
                            DaysRemaining = o.DaysRemaining,
                            TimePeriod = o.TimePeriodDisplay
                        })
                        .ToList();

                    R5_OkrsList = new ObservableCollection<OkrRow>(okrRows);

                    // Progress trend (by time period)
                    UpdateOkrProgressTrendChart(filteredOkrs);
                });
            });
        }

        private void UpdateOkrProgressByOwnerChart(List<ObjectiveKeyResult> okrs)
        {
            var ownerGroups = okrs
                .Where(o => o.Owner != null)
                .GroupBy(o => o.Owner!.FullName)
                .Select(g => new
                {
                    Name = g.Key,
                    AvgProgress = g.Average(o => o.CompletionPercentage),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.AvgProgress)
                .Take(8)
                .ToList();

            var labels = ownerGroups.Select(g => g.Name).ToList();
            var progressValues = new ChartValues<double>(ownerGroups.Select(g => Math.Round(g.AvgProgress, 1)));

            R5_ProgressByOwnerLabels = new ObservableCollection<string>(labels);
            R5_ProgressByOwnerSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Avg Progress %",
                    Values = progressValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    MaxColumnWidth = 50
                }
            };
        }

        private void UpdateOkrProgressTrendChart(List<ObjectiveKeyResult> okrs)
        {
            // Group OKRs by time period
            var periodGroups = okrs
                .GroupBy(o => o.TimePeriodDisplay)
                .Select(g => new
                {
                    Period = g.Key,
                    AvgProgress = g.Average(o => o.CompletionPercentage),
                    OnTrack = g.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OnTrack),
                    AtRisk = g.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.AtRisk),
                    OffTrack = g.Count(o => o.Status == Common.Enums.ObjectiveStatusEnum.OffTrack)
                })
                .Take(6)
                .ToList();

            var labels = periodGroups.Select(g => g.Period).ToList();
            var onTrackValues = new ChartValues<int>(periodGroups.Select(g => g.OnTrack));
            var atRiskValues = new ChartValues<int>(periodGroups.Select(g => g.AtRisk));
            var offTrackValues = new ChartValues<int>(periodGroups.Select(g => g.OffTrack));

            R5_ProgressTrendLabels = new ObservableCollection<string>(labels);
            R5_ProgressTrendSeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "On Track",
                    Values = onTrackValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                },
                new StackedColumnSeries
                {
                    Title = "At Risk",
                    Values = atRiskValues,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                },
                new StackedColumnSeries
                {
                    Title = "Off Track",
                    Values = offTrackValues,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                }
            };
        }

        private Brush GetOkrStatusBrush(Common.Enums.ObjectiveStatusEnum status)
        {
            return status switch
            {
                Common.Enums.ObjectiveStatusEnum.OnTrack => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Common.Enums.ObjectiveStatusEnum.AtRisk => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Common.Enums.ObjectiveStatusEnum.OffTrack => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }

        #endregion

        #region Private Methods - Report 6: KPI Performance

        private async Task RefreshReport6Async()
        {
            await Task.Run(() =>
            {
                var filteredKpis = _kpis.ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredKpis = filteredKpis
                        .Where(k => k.Owner?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                var onTarget = filteredKpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget);
                var closeToTarget = filteredKpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.CloseToTarget);
                var offTarget = filteredKpis.Count(k => k.Status == Common.Enums.KpiStatusEnum.OffTarget);
                var avgPerformance = filteredKpis.Count > 0 ? filteredKpis.Average(k => k.PercentComplete) : 0;
                var needsAttention = filteredKpis.Count(k => k.Status != Common.Enums.KpiStatusEnum.OnTarget);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R6_TotalKpis = filteredKpis.Count;
                    R6_OnTargetCount = onTarget;
                    R6_CloseToTargetCount = closeToTarget;
                    R6_OffTargetCount = offTarget;
                    R6_AvgPerformance = Math.Round(avgPerformance, 1);
                    R6_NeedsAttentionCount = needsAttention;

                    // Status distribution pie chart
                    R6_StatusDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "On Target",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(onTarget) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Close to Target",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(closeToTarget) },
                            Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Off Target",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(offTarget) },
                            Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            DataLabels = true
                        }
                    };

                    // Performance by category chart
                    UpdateKpiPerformanceByCategoryChart(filteredKpis);

                    // Target vs actual chart
                    UpdateKpiTargetVsActualChart(filteredKpis);

                    // KPIs list
                    var kpiRows = filteredKpis
                        .OrderBy(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget ? 2 : k.Status == Common.Enums.KpiStatusEnum.CloseToTarget ? 1 : 0)
                        .ThenByDescending(k => Math.Abs(k.PercentComplete - 100))
                        .Take(15)
                        .Select(k => new KpiRow
                        {
                            Name = k.Name,
                            OwnerName = k.Owner?.FullName ?? "—",
                            Value = k.Value,
                            TargetValue = k.TargetValue,
                            Unit = k.Unit,
                            PercentComplete = (int)k.PercentComplete,
                            Status = k.Status.ToString(),
                            StatusColor = GetKpiStatusBrush(k.Status),
                            Category = k.Category,
                            LastUpdated = k.LastUpdated
                        })
                        .ToList();

                    R6_KpisList = new ObservableCollection<KpiRow>(kpiRows);
                });
            });
        }

        private void UpdateKpiPerformanceByCategoryChart(List<KeyPerformanceIndicator> kpis)
        {
            var categoryGroups = kpis
                .Where(k => !string.IsNullOrEmpty(k.Category))
                .GroupBy(k => k.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    AvgPerformance = g.Average(k => k.PercentComplete),
                    OnTarget = g.Count(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget),
                    Total = g.Count()
                })
                .OrderByDescending(g => g.AvgPerformance)
                .Take(8)
                .ToList();

            if (!categoryGroups.Any())
            {
                // If no categories, group by owner instead
                categoryGroups = kpis
                    .Where(k => k.Owner != null)
                    .GroupBy(k => k.Owner!.FullName)
                    .Select(g => new
                    {
                        Category = g.Key,
                        AvgPerformance = g.Average(k => k.PercentComplete),
                        OnTarget = g.Count(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget),
                        Total = g.Count()
                    })
                    .OrderByDescending(g => g.AvgPerformance)
                    .Take(8)
                    .ToList();
            }

            var labels = categoryGroups.Select(g => g.Category).ToList();
            var performanceValues = new ChartValues<double>(categoryGroups.Select(g => Math.Round(g.AvgPerformance, 1)));

            R6_PerformanceByCategoryLabels = new ObservableCollection<string>(labels);
            R6_PerformanceByCategorySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Avg % of Target",
                    Values = performanceValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    MaxColumnWidth = 50
                }
            };
        }

        private void UpdateKpiTargetVsActualChart(List<KeyPerformanceIndicator> kpis)
        {
            // Show top 8 KPIs by importance (off target first, then by gap)
            var topKpis = kpis
                .OrderBy(k => k.Status == Common.Enums.KpiStatusEnum.OnTarget ? 2 : k.Status == Common.Enums.KpiStatusEnum.CloseToTarget ? 1 : 0)
                .ThenByDescending(k => Math.Abs(k.Value - k.TargetValue))
                .Take(8)
                .ToList();

            var labels = topKpis.Select(k => k.Name.Length > 15 ? k.Name.Substring(0, 15) + "..." : k.Name).ToList();
            var actualValues = new ChartValues<double>(topKpis.Select(k => k.Value));
            var targetValues = new ChartValues<double>(topKpis.Select(k => k.TargetValue));

            R6_TargetVsActualLabels = new ObservableCollection<string>(labels);
            R6_TargetVsActualSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Actual",
                    Values = actualValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    MaxColumnWidth = 30
                },
                new ColumnSeries
                {
                    Title = "Target",
                    Values = targetValues,
                    Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    MaxColumnWidth = 30
                }
            };
        }

        private Brush GetKpiStatusBrush(Common.Enums.KpiStatusEnum status)
        {
            return status switch
            {
                Common.Enums.KpiStatusEnum.OnTarget => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Common.Enums.KpiStatusEnum.CloseToTarget => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Common.Enums.KpiStatusEnum.OffTarget => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }

        #endregion

        #region Private Methods - Report 7: Goal Tracker

        private async Task RefreshReport7Async()
        {
            await Task.Run(() =>
            {
                var filteredGoals = _goals.ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredGoals = filteredGoals
                        .Where(g => g.TeamMemberId == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                var completed = filteredGoals.Count(g => g.Status == Common.Enums.GoalStatus.Completed);
                var inProgress = filteredGoals.Count(g => g.Status == Common.Enums.GoalStatus.InProgress);
                var overdue = filteredGoals.Count(g => g.IsOverdue);
                var avgProgress = filteredGoals.Count > 0 ? filteredGoals.Average(g => g.ProgressPercent) : 0;
                var completionPercent = filteredGoals.Count > 0 ? (int)((double)completed / filteredGoals.Count * 100) : 0;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R7_TotalGoals = filteredGoals.Count;
                    R7_CompletedGoals = completed;
                    R7_InProgressGoals = inProgress;
                    R7_OverdueGoals = overdue;
                    R7_AvgProgress = Math.Round(avgProgress, 1);
                    R7_CompletionPercent = completionPercent;

                    // Status distribution pie chart
                    var notStarted = filteredGoals.Count(g => g.Status == Common.Enums.GoalStatus.NotStarted);
                    var onHold = filteredGoals.Count(g => g.Status == Common.Enums.GoalStatus.OnHold);

                    R7_StatusDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "Completed",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(completed) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "In Progress",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(inProgress) },
                            Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Not Started",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(notStarted) },
                            Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "On Hold",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(onHold) },
                            Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                            DataLabels = true
                        }
                    };

                    // Goals by category chart
                    UpdateGoalsByCategoryChart(filteredGoals);

                    // Progress by team member chart
                    UpdateGoalsProgressByMemberChart(filteredGoals);

                    // Goals list (prioritize overdue and in-progress)
                    var goalRows = filteredGoals
                        .OrderByDescending(g => g.IsOverdue)
                        .ThenByDescending(g => g.Status == Common.Enums.GoalStatus.InProgress)
                        .ThenBy(g => g.TargetDate)
                        .Take(15)
                        .Select(g => new GoalRow
                        {
                            Title = g.Title,
                            TeamMemberName = g.TeamMember?.FullName ?? "—",
                            Category = g.Category.ToString(),
                            Progress = g.ProgressPercent,
                            Status = g.Status.ToString(),
                            StatusColor = GetGoalStatusBrush(g.Status, g.IsOverdue),
                            TargetDate = g.TargetDate?.ToString("MMM d, yyyy") ?? "—",
                            IsOverdue = g.IsOverdue,
                            DaysRemaining = g.DaysRemaining
                        })
                        .ToList();

                    R7_GoalsList = new ObservableCollection<GoalRow>(goalRows);
                });
            });
        }

        private void UpdateGoalsByCategoryChart(List<IndividualGoal> goals)
        {
            var categoryGroups = goals
                .GroupBy(g => g.Category)
                .Select(g => new
                {
                    Category = g.Key.ToString(),
                    Total = g.Count(),
                    Completed = g.Count(goal => goal.Status == Common.Enums.GoalStatus.Completed),
                    InProgress = g.Count(goal => goal.Status == Common.Enums.GoalStatus.InProgress)
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            var labels = categoryGroups.Select(g => g.Category).ToList();
            var completedValues = new ChartValues<int>(categoryGroups.Select(g => g.Completed));
            var inProgressValues = new ChartValues<int>(categoryGroups.Select(g => g.InProgress));
            var otherValues = new ChartValues<int>(categoryGroups.Select(g => g.Total - g.Completed - g.InProgress));

            R7_GoalsByCategoryLabels = new ObservableCollection<string>(labels);
            R7_GoalsByCategorySeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "Completed",
                    Values = completedValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                },
                new StackedColumnSeries
                {
                    Title = "In Progress",
                    Values = inProgressValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                },
                new StackedColumnSeries
                {
                    Title = "Other",
                    Values = otherValues,
                    Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128))
                }
            };
        }

        private void UpdateGoalsProgressByMemberChart(List<IndividualGoal> goals)
        {
            var memberGroups = goals
                .Where(g => g.TeamMember != null)
                .GroupBy(g => g.TeamMember!.FullName)
                .Select(g => new
                {
                    Name = g.Key,
                    AvgProgress = g.Average(goal => goal.ProgressPercent),
                    CompletedCount = g.Count(goal => goal.Status == Common.Enums.GoalStatus.Completed),
                    TotalCount = g.Count()
                })
                .OrderByDescending(g => g.AvgProgress)
                .Take(8)
                .ToList();

            var labels = memberGroups.Select(g => g.Name).ToList();
            var progressValues = new ChartValues<double>(memberGroups.Select(g => g.AvgProgress));

            R7_ProgressByMemberLabels = new ObservableCollection<string>(labels);
            R7_ProgressByMemberSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Avg Progress %",
                    Values = progressValues,
                    Fill = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                    MaxColumnWidth = 50
                }
            };
        }

        private Brush GetGoalStatusBrush(Common.Enums.GoalStatus status, bool isOverdue)
        {
            if (isOverdue)
                return new SolidColorBrush(Color.FromRgb(239, 68, 68));

            return status switch
            {
                Common.Enums.GoalStatus.Completed => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Common.Enums.GoalStatus.InProgress => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Common.Enums.GoalStatus.NotStarted => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Common.Enums.GoalStatus.OnHold => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Common.Enums.GoalStatus.Cancelled => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }

        #endregion

        #region Private Methods - Report 8: Project Health

        private async Task RefreshReport8Async()
        {
            await Task.Run(() =>
            {
                var filteredProjects = _projects.ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredProjects = filteredProjects
                        .Where(p => p.Owner?.Id == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                var completed = filteredProjects.Count(p => p.Status == "Completed");
                var inProgress = filteredProjects.Count(p => p.Status == "InProgress" || p.Status == "In Progress");
                var atRisk = filteredProjects.Count(p => p.IsOverdue && p.Status != "Completed");
                var avgProgress = filteredProjects.Count > 0 ? filteredProjects.Average(p => (double)p.Progress) : 0;
                var overdueCount = filteredProjects.Count(p => p.IsOverdue);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R8_TotalProjects = filteredProjects.Count;
                    R8_CompletedProjects = completed;
                    R8_InProgressProjects = inProgress;
                    R8_AtRiskProjects = atRisk;
                    R8_AvgProgress = Math.Round(avgProgress, 1);
                    R8_OverdueCount = overdueCount;

                    // Status distribution pie chart
                    var notStarted = filteredProjects.Count(p => p.Status == "NotStarted" || p.Status == "Not Started");
                    var onHold = filteredProjects.Count(p => p.Status == "OnHold" || p.Status == "On Hold");

                    R8_StatusDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "Completed",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(completed) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "In Progress",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(inProgress) },
                            Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "At Risk",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(atRisk) },
                            Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Not Started",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(notStarted) },
                            Fill = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                            DataLabels = true
                        }
                    };

                    // Progress by project chart
                    UpdateProjectProgressChart(filteredProjects);

                    // Projects list
                    var projectRows = filteredProjects
                        .OrderByDescending(p => p.IsOverdue)
                        .ThenByDescending(p => p.Status == "InProgress" || p.Status == "In Progress")
                        .ThenBy(p => p.EndDate)
                        .Take(15)
                        .Select(p => new ProjectRow
                        {
                            Name = p.Name,
                            OwnerName = p.Owner?.FullName ?? "—",
                            Progress = (int)p.Progress,
                            TotalTasks = p.TotalTasks,
                            CompletedTasks = p.CompletedTasks,
                            Status = p.Status,
                            StatusColor = GetProjectStatusBrush(p.Status, p.IsOverdue),
                            EndDate = p.EndDate?.ToString("MMM d, yyyy") ?? "—",
                            IsOverdue = p.IsOverdue,
                            DaysRemaining = p.DaysRemaining
                        })
                        .ToList();

                    R8_ProjectsList = new ObservableCollection<ProjectRow>(projectRows);
                });
            });
        }

        private void UpdateProjectProgressChart(List<Project> projects)
        {
            var topProjects = projects
                .Where(p => p.Status != "Completed" && p.Status != "Cancelled")
                .OrderBy(p => p.Progress)
                .Take(10)
                .ToList();

            var labels = topProjects.Select(p => p.Name.Length > 20 ? p.Name.Substring(0, 20) + "..." : p.Name).ToList();
            var progressValues = new ChartValues<double>(topProjects.Select(p => (double)p.Progress));

            R8_ProgressByProjectLabels = new ObservableCollection<string>(labels);
            R8_ProgressByProjectSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Progress %",
                    Values = progressValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    MaxColumnWidth = 40
                }
            };
        }

        private Brush GetProjectStatusBrush(string status, bool isOverdue)
        {
            if (isOverdue)
                return new SolidColorBrush(Color.FromRgb(239, 68, 68));

            return status switch
            {
                "Completed" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                "InProgress" or "In Progress" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                "OnHold" or "On Hold" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "NotStarted" or "Not Started" => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                "Cancelled" => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }

        #endregion

        #region Private Methods - Report 9: Feedback Trends

        private async Task RefreshReport9Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var filteredFeedback = _feedbacks
                    .Where(f => f.Date >= dateRange.Start && f.Date <= dateRange.End)
                    .ToList();

                // Apply team member filter
                if (SelectedTeamMemberFilter?.Id > 0)
                {
                    filteredFeedback = filteredFeedback
                        .Where(f => f.TeamMemberId == SelectedTeamMemberFilter.Id)
                        .ToList();
                }

                // Calculate KPIs
                var positive = filteredFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Positive);
                var constructive = filteredFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Constructive);
                var recognition = filteredFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Recognition);
                var positiveRatio = filteredFeedback.Count > 0 
                    ? (positive + recognition) / (double)filteredFeedback.Count * 100 
                    : 0;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R9_TotalFeedback = filteredFeedback.Count;
                    R9_PositiveFeedback = positive;
                    R9_ConstructiveFeedback = constructive;
                    R9_RecognitionCount = recognition;
                    R9_PositiveRatio = Math.Round(positiveRatio, 1);

                    // Type distribution pie chart
                    var coaching = filteredFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Coaching);
                    var review = filteredFeedback.Count(f => f.Type == Common.Enums.FeedbackType.PerformanceReview);

                    R9_TypeDistributionSeries = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "Positive",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(positive) },
                            Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Recognition",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(recognition) },
                            Fill = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Constructive",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(constructive) },
                            Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                            DataLabels = true
                        },
                        new PieSeries
                        {
                            Title = "Coaching",
                            Values = new ChartValues<ObservableValue> { new ObservableValue(coaching) },
                            Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                            DataLabels = true
                        }
                    };

                    // Feedback trend over time
                    UpdateFeedbackTrendChart(filteredFeedback, dateRange);

                    // Feedback by recipient
                    UpdateFeedbackByRecipientChart(filteredFeedback);

                    // Recent feedback list
                    var feedbackRows = filteredFeedback
                        .OrderByDescending(f => f.Date)
                        .Take(15)
                        .Select(f => new FeedbackRow
                        {
                            Title = f.Title,
                            TeamMemberName = f.TeamMember?.FullName ?? "—",
                            Type = f.Type.ToString(),
                            TypeColor = GetFeedbackTypeBrush(f.Type),
                            Date = f.Date.ToString("MMM d, yyyy"),
                            Context = f.Context
                        })
                        .ToList();

                    R9_RecentFeedback = new ObservableCollection<FeedbackRow>(feedbackRows);
                });
            });
        }

        private void UpdateFeedbackTrendChart(List<Feedback> feedbacks, (DateTime Start, DateTime End) dateRange)
        {
            var positiveValues = new ChartValues<int>();
            var constructiveValues = new ChartValues<int>();
            var labels = new List<string>();

            var startDate = dateRange.Start;
            var endDate = dateRange.End;
            var currentWeekStart = startDate.AddDays(-(int)startDate.DayOfWeek);

            while (currentWeekStart <= endDate)
            {
                var weekEnd = currentWeekStart.AddDays(6);
                var positive = feedbacks.Count(f => f.Date >= currentWeekStart && f.Date <= weekEnd && 
                    (f.Type == Common.Enums.FeedbackType.Positive || f.Type == Common.Enums.FeedbackType.Recognition));
                var constructive = feedbacks.Count(f => f.Date >= currentWeekStart && f.Date <= weekEnd && 
                    f.Type == Common.Enums.FeedbackType.Constructive);

                labels.Add(currentWeekStart.ToString("MMM d"));
                positiveValues.Add(positive);
                constructiveValues.Add(constructive);

                currentWeekStart = currentWeekStart.AddDays(7);
            }

            R9_FeedbackTrendLabels = new ObservableCollection<string>(labels);
            R9_FeedbackTrendSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Positive/Recognition",
                    Values = positiveValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 2
                },
                new LineSeries
                {
                    Title = "Constructive",
                    Values = constructiveValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 6,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    StrokeThickness = 2
                }
            };
        }

        private void UpdateFeedbackByRecipientChart(List<Feedback> feedbacks)
        {
            var recipientGroups = feedbacks
                .Where(f => f.TeamMember != null)
                .GroupBy(f => f.TeamMember!.FullName)
                .Select(g => new
                {
                    Name = g.Key,
                    Positive = g.Count(f => f.Type == Common.Enums.FeedbackType.Positive || f.Type == Common.Enums.FeedbackType.Recognition),
                    Constructive = g.Count(f => f.Type == Common.Enums.FeedbackType.Constructive),
                    Total = g.Count()
                })
                .OrderByDescending(g => g.Total)
                .Take(8)
                .ToList();

            var labels = recipientGroups.Select(g => g.Name).ToList();
            var positiveValues = new ChartValues<int>(recipientGroups.Select(g => g.Positive));
            var constructiveValues = new ChartValues<int>(recipientGroups.Select(g => g.Constructive));

            R9_ByRecipientLabels = new ObservableCollection<string>(labels);
            R9_ByRecipientSeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "Positive/Recognition",
                    Values = positiveValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                },
                new StackedColumnSeries
                {
                    Title = "Constructive",
                    Values = constructiveValues,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                }
            };
        }

        private Brush GetFeedbackTypeBrush(Common.Enums.FeedbackType type)
        {
            return type switch
            {
                Common.Enums.FeedbackType.Positive => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Common.Enums.FeedbackType.Recognition => new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                Common.Enums.FeedbackType.Constructive => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Common.Enums.FeedbackType.Coaching => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Common.Enums.FeedbackType.PerformanceReview => new SolidColorBrush(Color.FromRgb(236, 72, 153)),
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }

        #endregion

        #region Private Methods - Report 10: Team Comparison

        private async Task RefreshReport10Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                var activeMembers = _teamMembers.Where(m => m.IsActive).ToList();

                var comparisonRows = new List<TeamComparisonRow>();

                foreach (var member in activeMembers)
                {
                    // Calculate metrics for each team member
                    var memberTasks = _tasks.Where(t => t.Owner?.Id == member.Id && t.CreatedAt >= dateRange.Start).ToList();
                    var memberMeetings = _oneOnOnes.Where(m => m.TeamMember?.Id == member.Id && m.Date >= dateRange.Start).ToList();
                    var memberGoals = _goals.Where(g => g.TeamMemberId == member.Id).ToList();
                    var memberFeedback = _feedbacks.Where(f => f.TeamMemberId == member.Id && f.Date >= dateRange.Start).ToList();

                    var taskCompletionRate = memberTasks.Count > 0 
                        ? (double)memberTasks.Count(t => t.IsCompleted) / memberTasks.Count * 100 
                        : 0;
                    var avgGoalProgress = memberGoals.Count > 0 
                        ? memberGoals.Average(g => g.ProgressPercent) 
                        : 0;
                    var positiveFeedbackRatio = memberFeedback.Count > 0
                        ? (double)memberFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Positive || f.Type == Common.Enums.FeedbackType.Recognition) / memberFeedback.Count * 100
                        : 0;

                    comparisonRows.Add(new TeamComparisonRow
                    {
                        TeamMemberName = member.FullName,
                        TotalTasks = memberTasks.Count,
                        CompletedTasks = memberTasks.Count(t => t.IsCompleted),
                        TaskCompletionRate = Math.Round(taskCompletionRate, 1),
                        MeetingCount = memberMeetings.Count,
                        GoalCount = memberGoals.Count,
                        GoalProgress = Math.Round(avgGoalProgress, 1),
                        FeedbackCount = memberFeedback.Count,
                        PositiveFeedbackRatio = Math.Round(positiveFeedbackRatio, 1)
                    });
                }

                // Calculate averages
                var avgTaskCompletion = comparisonRows.Count > 0 ? comparisonRows.Average(r => r.TaskCompletionRate) : 0;
                var avgMeetingFreq = comparisonRows.Count > 0 ? comparisonRows.Average(r => r.MeetingCount) : 0;
                var avgGoalProg = comparisonRows.Count > 0 ? comparisonRows.Average(r => r.GoalProgress) : 0;

                // Sort by task completion rate descending
                comparisonRows = comparisonRows.OrderByDescending(r => r.TaskCompletionRate).ToList();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R10_TotalMembers = activeMembers.Count;
                    R10_AvgTaskCompletion = Math.Round(avgTaskCompletion, 1);
                    R10_AvgMeetingFrequency = Math.Round(avgMeetingFreq, 1);
                    R10_AvgGoalProgress = Math.Round(avgGoalProg, 1);
                    R10_ComparisonList = new ObservableCollection<TeamComparisonRow>(comparisonRows);

                    // Task completion chart
                    UpdateTeamTaskCompletionChart(comparisonRows);

                    // Engagement chart (meetings + feedback)
                    UpdateTeamEngagementChart(comparisonRows);
                });
            });
        }

        private void UpdateTeamTaskCompletionChart(List<TeamComparisonRow> rows)
        {
            var topRows = rows.Take(10).ToList();
            var labels = topRows.Select(r => r.TeamMemberName).ToList();
            var values = new ChartValues<double>(topRows.Select(r => r.TaskCompletionRate));

            R10_TaskCompletionLabels = new ObservableCollection<string>(labels);
            R10_TaskCompletionSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Task Completion %",
                    Values = values,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    MaxColumnWidth = 40
                }
            };
        }

        private void UpdateTeamEngagementChart(List<TeamComparisonRow> rows)
        {
            var topRows = rows.OrderByDescending(r => r.MeetingCount + r.FeedbackCount).Take(10).ToList();
            var labels = topRows.Select(r => r.TeamMemberName).ToList();
            var meetingValues = new ChartValues<int>(topRows.Select(r => r.MeetingCount));
            var feedbackValues = new ChartValues<int>(topRows.Select(r => r.FeedbackCount));

            R10_EngagementLabels = new ObservableCollection<string>(labels);
            R10_EngagementSeries = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Title = "1:1 Meetings",
                    Values = meetingValues,
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                },
                new StackedColumnSeries
                {
                    Title = "Feedback Given",
                    Values = feedbackValues,
                    Fill = new SolidColorBrush(Color.FromRgb(139, 92, 246))
                }
            };
        }

        #endregion

        #region Private Methods - Report 11: Performance Review Prep

        private async Task RefreshReport11Async()
        {
            await Task.Run(() =>
            {
                // This report requires a specific team member to be selected
                if (SelectedTeamMemberFilter == null || SelectedTeamMemberFilter.Id == 0)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        R11_SelectedMemberName = "Please select a team member";
                        R11_TotalMeetings = 0;
                        R11_TotalTasks = 0;
                        R11_CompletedTasks = 0;
                        R11_TotalGoals = 0;
                        R11_CompletedGoals = 0;
                        R11_FeedbackCount = 0;
                        R11_PositiveFeedbackCount = 0;
                        R11_TaskCompletionRate = 0;
                        R11_GoalCompletionRate = 0;
                        R11_MeetingHistory = new ObservableCollection<OneOnOne>();
                        R11_FeedbackHistory = new ObservableCollection<FeedbackRow>();
                        R11_GoalsList = new ObservableCollection<IndividualGoal>();
                        R11_PerformanceTrendSeries = new SeriesCollection();
                        R11_PerformanceTrendLabels = new ObservableCollection<string>();
                    });
                    return;
                }

                var memberId = SelectedTeamMemberFilter.Id;
                var memberName = SelectedTeamMemberFilter.FullName;

                // Get all data for this team member
                var memberMeetings = _oneOnOnes.Where(m => m.TeamMember?.Id == memberId).OrderByDescending(m => m.Date).ToList();
                var memberTasks = _tasks.Where(t => t.Owner?.Id == memberId).ToList();
                var memberGoals = _goals.Where(g => g.TeamMemberId == memberId).ToList();
                var memberFeedback = _feedbacks.Where(f => f.TeamMemberId == memberId).OrderByDescending(f => f.Date).ToList();

                var completedTasks = memberTasks.Count(t => t.IsCompleted);
                var completedGoals = memberGoals.Count(g => g.Status == Common.Enums.GoalStatus.Completed);
                var taskCompletionRate = memberTasks.Count > 0 ? (double)completedTasks / memberTasks.Count * 100 : 0;
                var goalCompletionRate = memberGoals.Count > 0 ? (double)completedGoals / memberGoals.Count * 100 : 0;
                var positiveFeedback = memberFeedback.Count(f => f.Type == Common.Enums.FeedbackType.Positive || f.Type == Common.Enums.FeedbackType.Recognition);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    R11_SelectedMemberName = memberName;
                    R11_TotalMeetings = memberMeetings.Count;
                    R11_TotalTasks = memberTasks.Count;
                    R11_CompletedTasks = completedTasks;
                    R11_TotalGoals = memberGoals.Count;
                    R11_CompletedGoals = completedGoals;
                    R11_FeedbackCount = memberFeedback.Count;
                    R11_PositiveFeedbackCount = positiveFeedback;
                    R11_TaskCompletionRate = Math.Round(taskCompletionRate, 1);
                    R11_GoalCompletionRate = Math.Round(goalCompletionRate, 1);

                    // Meeting history
                    R11_MeetingHistory = new ObservableCollection<OneOnOne>(memberMeetings.Take(20));

                    // Feedback history
                    var feedbackRows = memberFeedback.Take(15).Select(f => new FeedbackRow
                    {
                        Title = f.Title,
                        TeamMemberName = memberName,
                        Type = f.Type.ToString(),
                        TypeColor = GetFeedbackTypeBrush(f.Type),
                        Date = f.Date.ToString("MMM d, yyyy"),
                        Context = f.Context
                    }).ToList();
                    R11_FeedbackHistory = new ObservableCollection<FeedbackRow>(feedbackRows);

                    // Goals list
                    R11_GoalsList = new ObservableCollection<IndividualGoal>(memberGoals.OrderByDescending(g => g.Status == Common.Enums.GoalStatus.InProgress));

                    // Performance trend chart (task completion by month)
                    UpdatePerformanceTrendChart(memberTasks, memberMeetings);
                });
            });
        }

        private void UpdatePerformanceTrendChart(List<IndividualTask> tasks, List<OneOnOne> meetings)
        {
            var tasksByMonth = new Dictionary<string, (int Created, int Completed)>();
            var meetingsByMonth = new Dictionary<string, int>();

            // Last 6 months
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Today.AddMonths(-i);
                var monthKey = month.ToString("MMM yyyy");
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var created = tasks.Count(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd);
                var completed = tasks.Count(t => t.IsCompleted && t.LastModifiedAt >= monthStart && t.LastModifiedAt <= monthEnd);
                var meetingCount = meetings.Count(m => m.Date >= monthStart && m.Date <= monthEnd);

                tasksByMonth[monthKey] = (created, completed);
                meetingsByMonth[monthKey] = meetingCount;
            }

            var labels = tasksByMonth.Keys.ToList();
            var completedValues = new ChartValues<int>(tasksByMonth.Values.Select(v => v.Completed));
            var meetingValues = new ChartValues<int>(meetingsByMonth.Values);

            R11_PerformanceTrendLabels = new ObservableCollection<string>(labels);
            R11_PerformanceTrendSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Tasks Completed",
                    Values = completedValues,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    MaxColumnWidth = 30
                },
                new LineSeries
                {
                    Title = "1:1 Meetings",
                    Values = meetingValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Fill = Brushes.Transparent,
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 2
                }
            };
        }

        #endregion

        #region Private Methods - Report 12: Executive Summary

        private async Task RefreshReport12Async()
        {
            await Task.Run(() =>
            {
                var dateRange = GetDateRange();
                
                // Team metrics
                var activeMembers = _teamMembers.Where(m => m.IsActive).ToList();
                R12_TotalTeamMembers = _teamMembers.Count;
                R12_ActiveMembers = activeMembers.Count;
                
                // Meeting cadence
                var meetings = _oneOnOnes
                    .Where(m => m.Date >= dateRange.Start && m.Date <= dateRange.End)
                    .ToList();
                R12_MeetingsThisPeriod = meetings.Count;
                
                // Calculate cadence: % of team members who had at least one meeting in period
                var membersWithMeetings = meetings.Select(m => m.TeamMember?.Id).Distinct().Count();
                R12_MeetingCadencePercent = activeMembers.Count > 0 
                    ? (int)(membersWithMeetings / (double)activeMembers.Count * 100) 
                    : 0;
                
                // Task completion
                var tasks = _tasks.ToList();
                R12_TasksTotal = tasks.Count;
                R12_TasksCompleted = tasks.Count(t => t.IsCompleted);
                R12_TaskCompletionPercent = tasks.Count > 0 
                    ? (int)(R12_TasksCompleted / (double)tasks.Count * 100) 
                    : 0;
                
                // OKR health
                var okrs = _okrs.ToList();
                R12_OkrsTotal = okrs.Count;
                R12_OkrsOnTrack = okrs.Count(o => o.Status == ObjectiveStatusEnum.OnTrack);
                R12_OkrHealthPercent = okrs.Count > 0 
                    ? (int)(R12_OkrsOnTrack / (double)okrs.Count * 100) 
                    : 0;
                
                // KPI health  
                var kpis = _kpis.ToList();
                R12_KpisTotal = kpis.Count;
                R12_KpisOnTarget = kpis.Count(k => k.Value >= k.TargetValue);
                R12_KpiHealthPercent = kpis.Count > 0 
                    ? (int)(R12_KpisOnTarget / (double)kpis.Count * 100) 
                    : 0;
                
                // Goal completion
                var goals = _goals.ToList();
                R12_GoalsTotal = goals.Count;
                R12_GoalsCompleted = goals.Count(g => g.Status == Common.Enums.GoalStatus.Completed);
                R12_GoalCompletionPercent = goals.Count > 0 
                    ? (int)(R12_GoalsCompleted / (double)goals.Count * 100) 
                    : 0;
                
                // Feedback summary
                var feedback = _feedbacks
                    .Where(f => f.Date >= dateRange.Start && f.Date <= dateRange.End)
                    .ToList();
                R12_FeedbackTotal = feedback.Count;
                R12_PositiveFeedback = feedback.Count(f => f.Type == Common.Enums.FeedbackType.Positive || f.Type == Common.Enums.FeedbackType.Recognition);
                R12_PositiveRatio = feedback.Count > 0 
                    ? Math.Round(R12_PositiveFeedback / (double)feedback.Count * 100, 1) 
                    : 0;
                
                // Calculate overall health score (weighted average)
                var weights = new[] { 0.25, 0.20, 0.20, 0.20, 0.15 };
                var scores = new[] { R12_MeetingCadencePercent, R12_TaskCompletionPercent, R12_OkrHealthPercent, R12_KpiHealthPercent, R12_GoalCompletionPercent };
                R12_OverallHealthScore = (int)(weights.Zip(scores, (w, s) => w * s).Sum());
                
                // Determine health status and color
                if (R12_OverallHealthScore >= 80)
                {
                    R12_OverallHealthStatus = "Excellent";
                    R12_OverallHealthColor = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else if (R12_OverallHealthScore >= 60)
                {
                    R12_OverallHealthStatus = "Good";
                    R12_OverallHealthColor = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                }
                else if (R12_OverallHealthScore >= 40)
                {
                    R12_OverallHealthStatus = "Needs Attention";
                    R12_OverallHealthColor = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }
                else
                {
                    R12_OverallHealthStatus = "At Risk";
                    R12_OverallHealthColor = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
                
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Metrics summary pie chart
                    UpdateExecutiveMetricsChart();
                    
                    // Health trend chart
                    UpdateExecutiveHealthTrendChart();
                    
                    // Key highlights
                    UpdateKeyHighlights();
                    
                    // Areas of concern
                    UpdateAreasOfConcern();
                });
            });
        }

        private void UpdateExecutiveMetricsChart()
        {
            R12_MetricsSummarySeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = $"Meetings ({R12_MeetingCadencePercent}%)",
                    Values = new ChartValues<int> { R12_MeetingCadencePercent },
                    Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    DataLabels = false
                },
                new PieSeries
                {
                    Title = $"Tasks ({R12_TaskCompletionPercent}%)",
                    Values = new ChartValues<int> { R12_TaskCompletionPercent },
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    DataLabels = false
                },
                new PieSeries
                {
                    Title = $"OKRs ({R12_OkrHealthPercent}%)",
                    Values = new ChartValues<int> { R12_OkrHealthPercent },
                    Fill = new SolidColorBrush(Color.FromRgb(139, 92, 246)),
                    DataLabels = false
                },
                new PieSeries
                {
                    Title = $"KPIs ({R12_KpiHealthPercent}%)",
                    Values = new ChartValues<int> { R12_KpiHealthPercent },
                    Fill = new SolidColorBrush(Color.FromRgb(236, 72, 153)),
                    DataLabels = false
                },
                new PieSeries
                {
                    Title = $"Goals ({R12_GoalCompletionPercent}%)",
                    Values = new ChartValues<int> { R12_GoalCompletionPercent },
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    DataLabels = false
                }
            };
        }

        private void UpdateExecutiveHealthTrendChart()
        {
            // Calculate health scores for last 6 periods
            var labels = new List<string>();
            var healthValues = new ChartValues<int>();
            
            for (int i = 5; i >= 0; i--)
            {
                var periodStart = DateTime.Today.AddDays(-30 * (i + 1));
                var periodEnd = DateTime.Today.AddDays(-30 * i);
                labels.Add(periodEnd.ToString("MMM"));
                
                // Calculate period health (simplified - use task completion as proxy)
                var periodTasks = _tasks.Count(t => t.CreatedAt <= periodEnd);
                var periodCompleted = _tasks.Count(t => t.IsCompleted && t.LastModifiedAt <= periodEnd);
                var periodHealth = periodTasks > 0 ? (int)(periodCompleted / (double)periodTasks * 100) : 50;
                
                healthValues.Add(Math.Min(100, Math.Max(0, periodHealth)));
            }
            
            R12_HealthTrendLabels = new ObservableCollection<string>(labels);
            R12_HealthTrendSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Team Health",
                    Values = healthValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    Fill = new SolidColorBrush(Color.FromArgb(50, 59, 130, 246)),
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 3
                }
            };
        }

        private void UpdateKeyHighlights()
        {
            var highlights = new List<ExecutiveSummaryItem>();
            
            if (R12_TaskCompletionPercent >= 80)
                highlights.Add(new ExecutiveSummaryItem("High task completion", $"{R12_TaskCompletionPercent}% of tasks completed", "✓", Brushes.Green));
            
            if (R12_MeetingCadencePercent >= 90)
                highlights.Add(new ExecutiveSummaryItem("Strong meeting cadence", $"{R12_MeetingCadencePercent}% of team engaged in 1:1s", "✓", Brushes.Green));
            
            if (R12_OkrHealthPercent >= 70)
                highlights.Add(new ExecutiveSummaryItem("OKRs on track", $"{R12_OkrsOnTrack}/{R12_OkrsTotal} OKRs are on track or achieved", "✓", Brushes.Green));
            
            if (R12_PositiveRatio >= 60)
                highlights.Add(new ExecutiveSummaryItem("Positive feedback culture", $"{R12_PositiveRatio}% positive feedback ratio", "✓", Brushes.Green));
            
            if (R12_GoalCompletionPercent >= 50)
                highlights.Add(new ExecutiveSummaryItem("Goal progress", $"{R12_GoalsCompleted}/{R12_GoalsTotal} goals completed", "✓", Brushes.Green));
            
            if (highlights.Count == 0)
                highlights.Add(new ExecutiveSummaryItem("Data building", "Continue tracking to see highlights", "📊", Brushes.Gray));
            
            R12_KeyHighlights = new ObservableCollection<ExecutiveSummaryItem>(highlights.Take(5));
        }

        private void UpdateAreasOfConcern()
        {
            var concerns = new List<ExecutiveSummaryItem>();
            
            if (R12_MeetingCadencePercent < 60)
                concerns.Add(new ExecutiveSummaryItem("Low meeting cadence", $"Only {R12_MeetingCadencePercent}% of team have had recent 1:1s", "⚠", Brushes.Orange));
            
            if (R12_TaskCompletionPercent < 50)
                concerns.Add(new ExecutiveSummaryItem("Task backlog growing", $"Only {R12_TaskCompletionPercent}% of tasks completed", "⚠", Brushes.Orange));
            
            var overdueOkrs = _okrs.Count(o => o.Status == ObjectiveStatusEnum.OffTrack);
            if (overdueOkrs > 0)
                concerns.Add(new ExecutiveSummaryItem("OKRs at risk", $"{overdueOkrs} OKRs are off track", "⚠", Brushes.Red));
            
            var offTargetKpis = _kpis.Count(k => k.Value < k.TargetValue * 0.7);
            if (offTargetKpis > 0)
                concerns.Add(new ExecutiveSummaryItem("KPIs below target", $"{offTargetKpis} KPIs significantly below target", "⚠", Brushes.Red));
            
            var overdueGoals = _goals.Count(g => g.TargetDate.HasValue && g.TargetDate.Value < DateTime.Today && g.Status != Common.Enums.GoalStatus.Completed);
            if (overdueGoals > 0)
                concerns.Add(new ExecutiveSummaryItem("Overdue goals", $"{overdueGoals} goals past due date", "⚠", Brushes.Orange));
            
            if (concerns.Count == 0)
                concerns.Add(new ExecutiveSummaryItem("No major concerns", "All metrics within acceptable ranges", "✓", Brushes.Green));
            
            R12_AreasOfConcern = new ObservableCollection<ExecutiveSummaryItem>(concerns.Take(5));
        }

        #endregion

        #region Private Methods - Commands

        private bool CanExport(object? parameter)
        {
            return !IsExporting;
        }

        private void ExecuteRefreshReport(object? parameter)
        {
            _ = LoadDataAsync();
        }

        private string GetSaveFilePath(string defaultFileName)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = "xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        private async void ExecuteExportCurrentReport(object? parameter)
        {
            var index = GetSelectedReportIndex();
            var reportName = _reportDefinitions.TryGetValue(index, out var def) ? def.Title.Replace(" ", "_") : "Report";

            await ExportAsync(async () =>
            {
                // For now, export filtered 1:1 data for Report 1
                if (index == 1)
                {
                    var dateRange = GetDateRange();
                    var filteredMeetings = _oneOnOnes
                        .Where(m => m.Date >= dateRange.Start && m.Date <= dateRange.End);

                    if (SelectedTeamMemberFilter?.Id > 0)
                    {
                        filteredMeetings = filteredMeetings.Where(m => m.TeamMember?.Id == SelectedTeamMemberFilter.Id);
                    }

                    var filePath = GetSaveFilePath(reportName);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        ExcelExportService.ExportOneOnOnes(filteredMeetings.ToList(), filePath);
                        NotificationManager.Instance.ShowSuccess("Export Complete", $"Report exported to {Path.GetFileName(filePath)}");
                    }
                }
                else
                {
                    NotificationManager.Instance.ShowInfo("Coming Soon", "Export for this report is not yet available.");
                }

                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportAll(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("TrackerReport");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportAllData(_teamMembers, _oneOnOnes, _tasks, _projects, _okrs, _kpis, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Complete report exported to {Path.GetFileName(filePath)}");
                }

                await Task.CompletedTask;
            });
        }

        // Legacy export methods
        private async void ExecuteExportTeamMembers(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("TeamMembers");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportTeamMembers(_teamMembers, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Team members exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportOneOnOnes(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("OneOnOnes");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportOneOnOnes(_oneOnOnes, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"1:1 meetings exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportTasks(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("Tasks");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportTasks(_tasks, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Tasks exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportProjects(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("Projects");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportProjects(_projects, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Projects exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportOKRs(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("OKRs");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportOKRs(_okrs, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"OKRs exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async void ExecuteExportKPIs(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var filePath = GetSaveFilePath("KPIs");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportKPIs(_kpis, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"KPIs exported to {Path.GetFileName(filePath)}");
                }
                await Task.CompletedTask;
            });
        }

        private async Task ExportAsync(Func<Task> exportAction)
        {
            IsExporting = true;
            try
            {
                await Task.Run(async () => await exportAction());
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Export Failed", $"An error occurred during export: {ex.Message}");
            }
            finally
            {
                IsExporting = false;
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Defines metadata for a report type.
    /// </summary>
    public class ReportDefinition
    {
        public string Title { get; }
        public string Description { get; }
        public bool SupportsTeamMemberFilter { get; }
        public string[] Features { get; }

        public ReportDefinition(string title, string description, bool supportsTeamMemberFilter, string[] features)
        {
            Title = title;
            Description = description;
            SupportsTeamMemberFilter = supportsTeamMemberFilter;
            Features = features;
        }
    }

    /// <summary>
    /// Item for team member filter dropdown.
    /// </summary>
    public class TeamMemberFilterItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Row data for Meeting Cadence report.
    /// </summary>
    public class MemberCadenceRow
    {
        public string TeamMemberName { get; set; } = string.Empty;
        public string DaysSinceLast { get; set; } = "—";
        public string LastMeetingDate { get; set; } = "—";
        public string NextScheduled { get; set; } = "None";
        public string Status { get; set; } = "Unknown";
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public int MeetingCount { get; set; }
    }

    /// <summary>
    /// Row data for Action Item Follow-Up report.
    /// </summary>
    public class ActionItemRow
    {
        public string Description { get; set; } = string.Empty;
        public string TeamMemberName { get; set; } = string.Empty;
        public string MeetingDate { get; set; } = "—";
        public int DaysPending { get; set; }
        public bool IsOverdue { get; set; }
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }

    /// <summary>
    /// Row data for OKR Progress report.
    /// </summary>
    public class OkrRow
    {
        public string Title { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public int KeyResultCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public int DaysRemaining { get; set; }
        public string TimePeriod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Row data for KPI Performance report.
    /// </summary>
    public class KpiRow
    {
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public double Value { get; set; }
        public double TargetValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int PercentComplete { get; set; }
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public string Category { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string ValueDisplay => string.IsNullOrEmpty(Unit) ? $"{Value:N1}" : $"{Value:N1} {Unit}";
        public string TargetDisplay => string.IsNullOrEmpty(Unit) ? $"{TargetValue:N1}" : $"{TargetValue:N1} {Unit}";
    }

    /// <summary>
    /// Row data for Goal Tracker report.
    /// </summary>
    public class GoalRow
    {
        public string Title { get; set; } = string.Empty;
        public string TeamMemberName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public string TargetDate { get; set; } = "—";
        public bool IsOverdue { get; set; }
        public int? DaysRemaining { get; set; }
        public string DaysRemainingDisplay => DaysRemaining.HasValue 
            ? (DaysRemaining.Value < 0 ? $"{Math.Abs(DaysRemaining.Value)}d overdue" : $"{DaysRemaining.Value}d left")
            : "—";
    }

    /// <summary>
    /// Row data for Project Health report.
    /// </summary>
    public class ProjectRow
    {
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int Progress { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public string EndDate { get; set; } = "—";
        public bool IsOverdue { get; set; }
        public int? DaysRemaining { get; set; }
        public string TaskProgress => $"{CompletedTasks}/{TotalTasks}";
        public string DaysRemainingDisplay => DaysRemaining.HasValue 
            ? (DaysRemaining.Value < 0 ? $"{Math.Abs(DaysRemaining.Value)}d overdue" : $"{DaysRemaining.Value}d left")
            : "—";
    }

    /// <summary>
    /// Row data for Feedback Trends report.
    /// </summary>
    public class FeedbackRow
    {
        public string Title { get; set; } = string.Empty;
        public string TeamMemberName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Brush TypeColor { get; set; } = Brushes.Gray;
        public string Date { get; set; } = "—";
        public string Context { get; set; } = string.Empty;
    }

    /// <summary>
    /// Row data for Team Comparison report.
    /// </summary>
    public class TeamComparisonRow
    {
        public string TeamMemberName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double TaskCompletionRate { get; set; }
        public int MeetingCount { get; set; }
        public int GoalCount { get; set; }
        public double GoalProgress { get; set; }
        public int FeedbackCount { get; set; }
        public double PositiveFeedbackRatio { get; set; }
    }

    /// <summary>
    /// Item for Executive Summary highlights and concerns.
    /// </summary>
    public class ExecutiveSummaryItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public Brush IconColor { get; set; } = Brushes.Gray;

        public ExecutiveSummaryItem() { }

        public ExecutiveSummaryItem(string title, string description, string icon, Brush iconColor)
        {
            Title = title;
            Description = description;
            Icon = icon;
            IconColor = iconColor;
        }
    }

    #endregion
}
