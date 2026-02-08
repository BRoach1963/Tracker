using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ProCohere.Avalonia.Models.Reports;
using ProCohere.Avalonia.Services;
using SkiaSharp;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Reports view.
/// Provides analytics and historical data visualization.
/// 
/// Unlike Briefing/Pulse (current state), Reports show TRENDS OVER TIME with charts.
/// </summary>
public partial class ReportsViewModel : ViewModelBase
{
    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "reports_viewmodel.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Currently selected report type (tab index).
    /// 0=Overview, 1=Goals, 2=Metrics, 3=Tasks, 4=Meetings, 5=Team
    /// </summary>
    [ObservableProperty]
    private int _selectedReportIndex;

    /// <summary>
    /// Start date for report range.
    /// </summary>
    [ObservableProperty]
    private DateTime? _startDate = DateTime.Now.AddDays(-30);

    /// <summary>
    /// End date for report range.
    /// </summary>
    [ObservableProperty]
    private DateTime? _endDate = DateTime.Now;

    /// <summary>
    /// Gets the effective start date (never null).
    /// </summary>
    private DateTime EffectiveStartDate => StartDate ?? DateTime.Now.AddDays(-30);
    
    /// <summary>
    /// Gets the effective end date (never null).
    /// </summary>
    private DateTime EffectiveEndDate => EndDate ?? DateTime.Now;

    /// <summary>
    /// Whether data is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Last error message if any.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    #endregion

    #region Report Data

    [ObservableProperty]
    private OverviewReportData? _overviewData;

    [ObservableProperty]
    private GoalsReportData? _goalsData;

    [ObservableProperty]
    private MetricsReportData? _metricsData;

    [ObservableProperty]
    private TasksReportData? _tasksData;

    [ObservableProperty]
    private MeetingsReportData? _meetingsData;

    [ObservableProperty]
    private TeamReportData? _teamData;

    #endregion

    #region Chart Series

    // Overview charts
    [ObservableProperty]
    private ISeries[] _goalProgressSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _taskCompletionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _meetingMinutesSeries = Array.Empty<ISeries>();

    // Goals charts
    [ObservableProperty]
    private ISeries[] _goalHealthPieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _goalTypePieSeries = Array.Empty<ISeries>();

    // Metrics charts
    [ObservableProperty]
    private ISeries[] _metricTrendSeries = Array.Empty<ISeries>();

    // Tasks charts
    [ObservableProperty]
    private ISeries[] _taskStatusPieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _taskCompletionTrendSeries = Array.Empty<ISeries>();

    // Meetings charts
    [ObservableProperty]
    private ISeries[] _meetingTypePieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _meetingCountTrendSeries = Array.Empty<ISeries>();

    // Team charts
    [ObservableProperty]
    private ISeries[] _feedbackTypePieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _feedbackTrendSeries = Array.Empty<ISeries>();

    #endregion

    #region X Axis for Time Series

    public Axis[] TimeXAxes { get; } = new Axis[]
    {
        new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d"))
    };

    public Axis[] DefaultYAxes { get; } = new Axis[]
    {
        new Axis { MinLimit = 0 }
    };

    #endregion

    #region Quick Date Range Presets

    [RelayCommand]
    private void SetDateRange(string preset)
    {
        EndDate = DateTime.Now;
        StartDate = preset switch
        {
            "7d" => DateTime.Now.AddDays(-7),
            "30d" => DateTime.Now.AddDays(-30),
            "90d" => DateTime.Now.AddDays(-90),
            "ytd" => new DateTime(DateTime.Now.Year, 1, 1),
            "1y" => DateTime.Now.AddYears(-1),
            _ => DateTime.Now.AddDays(-30)
        };
        Log($"Date range set to {preset}: {StartDate:d} - {EndDate:d}");
        _ = LoadReportDataAsync();
    }

    #endregion

    #region Lifecycle

    partial void OnSelectedReportIndexChanged(int value)
    {
        Log($"Report tab changed to {value}");
        _ = LoadReportDataAsync();
    }

    partial void OnStartDateChanged(DateTime? value)
    {
        Log($"Start date changed to {value:d}");
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        Log($"End date changed to {value:d}");
    }

    /// <summary>
    /// Initializes the ViewModel and loads initial data.
    /// </summary>
    public async Task InitializeAsync()
    {
        Log("InitializeAsync");
        await LoadReportDataAsync();
    }

    /// <summary>
    /// Refreshes the current report.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        Log("RefreshAsync");
        await LoadReportDataAsync();
    }

    #endregion

    #region Export Commands

    /// <summary>
    /// Whether an export operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isExporting;

    /// <summary>
    /// Exports all data to Excel. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportAllDataAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting all data to: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;

            // Load all data for export
            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            var projects = await ProjectService.Instance.GetAllProjectsAsync();

            var bundle = new ExcelExportService.ExportDataBundle
            {
                TeamMembers = dashboard.TeamMembers,
                Meetings = dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end),
                Tasks = dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end),
                Goals = dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end),
                Metrics = metrics,
                Projects = projects.Where(p => p.CreatedAt >= start && p.CreatedAt <= end),
                StartDate = start,
                EndDate = end
            };

            var success = await ExcelExportService.Instance.ExportAllDataAsync(bundle, filePath);

            if (!success)
            {
                ErrorMessage = ExcelExportService.Instance.LastError ?? "Export failed";
            }
            else
            {
                Log("Export completed successfully");
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"Export error: {ex.Message}");
            ErrorMessage = $"Export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Exports current tab data to Excel. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportCurrentTabAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting current tab ({SelectedReportIndex}) to: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;
            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();

            bool success = SelectedReportIndex switch
            {
                1 => await ExcelExportService.Instance.ExportGoalsAsync(
                    dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end), filePath),
                2 => await ExcelExportService.Instance.ExportMetricsAsync(
                    await MetricsService.Instance.GetAllMetricsAsync(), filePath),
                3 => await ExcelExportService.Instance.ExportTasksAsync(
                    dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end), filePath),
                4 => await ExcelExportService.Instance.ExportMeetingsAsync(
                    dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end), filePath),
                5 => await ExcelExportService.Instance.ExportTeamMembersAsync(
                    dashboard.TeamMembers, filePath),
                _ => await ExportAllDataAsync(filePath) // Overview exports all
            };

            if (!success)
            {
                ErrorMessage = ExcelExportService.Instance.LastError ?? "Export failed";
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"Export error: {ex.Message}");
            ErrorMessage = $"Export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Gets the suggested filename for the current Excel export.
    /// </summary>
    public string GetExportFilename()
    {
        var tabName = SelectedReportIndex switch
        {
            1 => "Goals",
            2 => "Metrics",
            3 => "Tasks",
            4 => "Meetings",
            5 => "Team",
            _ => "Report"
        };

        return $"ProCohere_{tabName}_{DateTime.Now:yyyyMMdd}.xlsx";
    }

    /// <summary>
    /// Gets the suggested filename for the current PDF export.
    /// </summary>
    public string GetPdfExportFilename()
    {
        var tabName = SelectedReportIndex switch
        {
            1 => "Goals",
            2 => "Metrics",
            3 => "Tasks",
            4 => "Meetings",
            5 => "Team",
            _ => "Report"
        };

        return $"ProCohere_{tabName}_{DateTime.Now:yyyyMMdd}.pdf";
    }

    #endregion

    #region PDF Export Commands

    /// <summary>
    /// Exports all data to PDF. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportAllDataToPdfAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting all data to PDF: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;

            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            var projects = await ProjectService.Instance.GetAllProjectsAsync();

            var bundle = new ExcelExportService.ExportDataBundle
            {
                TeamMembers = dashboard.TeamMembers,
                Meetings = dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end),
                Tasks = dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end),
                Goals = dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end),
                Metrics = metrics,
                Projects = projects.Where(p => p.CreatedAt >= start && p.CreatedAt <= end),
                StartDate = start,
                EndDate = end
            };

            var success = await PdfExportService.Instance.ExportAllDataAsync(bundle, filePath);

            if (!success)
            {
                ErrorMessage = PdfExportService.Instance.LastError ?? "PDF export failed";
            }
            else
            {
                Log("PDF export completed successfully");
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"PDF export error: {ex.Message}");
            ErrorMessage = $"PDF export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Exports current tab data to PDF. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportCurrentTabToPdfAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting current tab ({SelectedReportIndex}) to PDF: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;
            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();

            bool success = SelectedReportIndex switch
            {
                1 => await PdfExportService.Instance.ExportGoalsAsync(
                    dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end), filePath),
                2 => await PdfExportService.Instance.ExportMetricsAsync(
                    await MetricsService.Instance.GetAllMetricsAsync(), filePath),
                3 => await PdfExportService.Instance.ExportTasksAsync(
                    dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end), filePath),
                4 => await PdfExportService.Instance.ExportMeetingsAsync(
                    dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end), filePath),
                5 => await PdfExportService.Instance.ExportTeamMembersAsync(
                    dashboard.TeamMembers, filePath),
                _ => await ExportAllDataToPdfAsync(filePath) // Overview exports all
            };

            if (!success)
            {
                ErrorMessage = PdfExportService.Instance.LastError ?? "PDF export failed";
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"PDF export error: {ex.Message}");
            ErrorMessage = $"PDF export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    #endregion

    #region CSV Export Commands

    /// <summary>
    /// Gets the suggested filename for CSV export based on current tab.
    /// </summary>
    public string GetCsvExportFilename()
    {
        var tabName = SelectedReportIndex switch
        {
            0 => "AllData",
            1 => "Goals",
            2 => "Metrics",
            3 => "Tasks",
            4 => "Meetings",
            5 => "Team",
            _ => "Report"
        };

        // For single tab: .csv, for all data (Overview): .zip
        var extension = SelectedReportIndex == 0 ? ".zip" : ".csv";
        return $"ProCohere_{tabName}_{DateTime.Now:yyyyMMdd}{extension}";
    }

    /// <summary>
    /// Exports all data to a ZIP containing CSV files. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportAllDataToCsvAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting all data to CSV ZIP: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;

            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();
            var metrics = await MetricsService.Instance.GetAllMetricsAsync();
            var projects = await ProjectService.Instance.GetAllProjectsAsync();

            var success = await CsvExportService.Instance.ExportAllDataAsync(
                dashboard.TeamMembers,
                dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end),
                dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end),
                dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end),
                metrics,
                projects.Where(p => p.CreatedAt >= start && p.CreatedAt <= end),
                filePath
            );

            if (!success)
            {
                ErrorMessage = CsvExportService.Instance.LastError ?? "CSV export failed";
            }
            else
            {
                Log("CSV ZIP export completed successfully");
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"CSV export error: {ex.Message}");
            ErrorMessage = $"CSV export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    /// <summary>
    /// Exports current tab data to CSV. Called from View code-behind with file path.
    /// </summary>
    public async Task<bool> ExportCurrentTabToCsvAsync(string filePath)
    {
        if (IsExporting) return false;
        IsExporting = true;

        try
        {
            Log($"Exporting current tab ({SelectedReportIndex}) to CSV: {filePath}");

            var start = EffectiveStartDate;
            var end = EffectiveEndDate;
            var dashboard = await DashboardService.Instance.LoadDashboardDataAsync();

            bool success = SelectedReportIndex switch
            {
                1 => await CsvExportService.Instance.ExportGoalsAsync(
                    dashboard.Goals.Where(g => g.CreatedAt >= start && g.CreatedAt <= end), filePath),
                2 => await CsvExportService.Instance.ExportMetricsAsync(
                    await MetricsService.Instance.GetAllMetricsAsync(), filePath),
                3 => await CsvExportService.Instance.ExportTasksAsync(
                    dashboard.Tasks.Where(t => t.CreatedAt >= start && t.CreatedAt <= end), filePath),
                4 => await CsvExportService.Instance.ExportMeetingsAsync(
                    dashboard.Meetings.Where(m => m.ScheduledAt >= start && m.ScheduledAt <= end), filePath),
                5 => await CsvExportService.Instance.ExportTeamMembersAsync(
                    dashboard.TeamMembers, filePath),
                _ => await ExportAllDataToCsvAsync(filePath) // Overview exports all as ZIP
            };

            if (!success)
            {
                ErrorMessage = CsvExportService.Instance.LastError ?? "CSV export failed";
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"CSV export error: {ex.Message}");
            ErrorMessage = $"CSV export failed: {ex.Message}";
            return false;
        }
        finally
        {
            IsExporting = false;
        }
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads report data for the selected report type and date range.
    /// </summary>
    private async Task LoadReportDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var start = EffectiveStartDate;
            var end = EffectiveEndDate;

            Log($"Loading report data: Type={SelectedReportIndex}, Range={start:d}-{end:d}");

            switch (SelectedReportIndex)
            {
                case 0: // Overview
                    OverviewData = await ReportService.Instance.GetOverviewReportAsync(start, end);
                    BuildOverviewCharts();
                    break;

                case 1: // Goals
                    GoalsData = await ReportService.Instance.GetGoalsReportAsync(start, end);
                    BuildGoalsCharts();
                    break;

                case 2: // Metrics
                    MetricsData = await ReportService.Instance.GetMetricsReportAsync(start, end);
                    BuildMetricsCharts();
                    break;

                case 3: // Tasks
                    TasksData = await ReportService.Instance.GetTasksReportAsync(start, end);
                    BuildTasksCharts();
                    break;

                case 4: // Meetings
                    MeetingsData = await ReportService.Instance.GetMeetingsReportAsync(start, end);
                    BuildMeetingsCharts();
                    break;

                case 5: // Team
                    TeamData = await ReportService.Instance.GetTeamReportAsync(start, end);
                    BuildTeamCharts();
                    break;
            }

            Log("Report data loaded successfully");
        }
        catch (Exception ex)
        {
            Log($"Error loading report: {ex.Message}");
            ErrorMessage = $"Failed to load report: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Chart Builders

    private void BuildOverviewCharts()
    {
        if (OverviewData == null) return;

        // Goal progress over time (line chart)
        GoalProgressSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Goal Progress",
                Values = ConvertToDateTimePoints(OverviewData.GoalProgressOverTime),
                Stroke = new SolidColorPaint(SKColor.Parse("#22C55E"), 3),
                Fill = new SolidColorPaint(SKColor.Parse("#22C55E").WithAlpha(50)),
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#22C55E"), 2)
            }
        };

        // Task completion over time (line chart)
        TaskCompletionSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Tasks Completed",
                Values = ConvertToDateTimePoints(OverviewData.TaskCompletionOverTime),
                Stroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 3),
                Fill = new SolidColorPaint(SKColor.Parse("#3B82F6").WithAlpha(50)),
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#3B82F6"), 2)
            }
        };

        // Meeting minutes over time (bar chart)
        MeetingMinutesSeries = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Name = "Meeting Minutes",
                Values = ConvertToDateTimePoints(OverviewData.MeetingMinutesOverTime),
                Fill = new SolidColorPaint(SKColor.Parse("#8B5CF6"))
            }
        };
    }

    private void BuildGoalsCharts()
    {
        if (GoalsData == null) return;

        // Health distribution pie chart
        var healthPieSeries = new List<ISeries>();
        foreach (var health in GoalsData.HealthDistribution)
        {
            if (health.Count > 0)
            {
                healthPieSeries.Add(new PieSeries<int>
                {
                    Name = health.Health,
                    Values = new[] { health.Count },
                    Fill = new SolidColorPaint(SKColor.Parse(health.Color)),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{health.Health}: {health.Count}"
                });
            }
        }
        GoalHealthPieSeries = healthPieSeries.ToArray();

        // Type distribution pie chart
        var typePieSeries = new List<ISeries>();
        var colors = new[] { "#3B82F6", "#22C55E", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899" };
        var colorIndex = 0;
        foreach (var type in GoalsData.TypeDistribution)
        {
            typePieSeries.Add(new PieSeries<int>
            {
                Name = type.Type,
                Values = new[] { type.Count },
                Fill = new SolidColorPaint(SKColor.Parse(colors[colorIndex % colors.Length])),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsFormatter = point => $"{type.Type}: {type.Count}"
            });
            colorIndex++;
        }
        GoalTypePieSeries = typePieSeries.ToArray();
    }

    private void BuildMetricsCharts()
    {
        if (MetricsData == null) return;

        // Multi-line chart for metric trends
        var trendSeries = new List<ISeries>();
        var colors = new[] { "#3B82F6", "#22C55E", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899", "#14B8A6" };
        var colorIndex = 0;

        foreach (var metric in MetricsData.MetricTrends)
        {
            if (metric.DataPoints.Count > 0)
            {
                var color = SKColor.Parse(colors[colorIndex % colors.Length]);
                trendSeries.Add(new LineSeries<DateTimePoint>
                {
                    Name = metric.MetricName,
                    Values = ConvertToDateTimePoints(metric.DataPoints),
                    Stroke = new SolidColorPaint(color, 2),
                    GeometrySize = 6,
                    GeometryStroke = new SolidColorPaint(color, 2),
                    Fill = null
                });
                colorIndex++;
            }
        }
        MetricTrendSeries = trendSeries.ToArray();
    }

    private void BuildTasksCharts()
    {
        if (TasksData == null) return;

        // Status distribution pie chart
        var statusPieSeries = new List<ISeries>();
        foreach (var status in TasksData.StatusDistribution)
        {
            if (status.Count > 0)
            {
                statusPieSeries.Add(new PieSeries<int>
                {
                    Name = status.Status,
                    Values = new[] { status.Count },
                    Fill = new SolidColorPaint(SKColor.Parse(status.Color)),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{status.Status}: {status.Count}"
                });
            }
        }
        TaskStatusPieSeries = statusPieSeries.ToArray();

        // Completion trend line chart
        TaskCompletionTrendSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Tasks Completed",
                Values = ConvertToDateTimePoints(TasksData.CompletionOverTime),
                Stroke = new SolidColorPaint(SKColor.Parse("#22C55E"), 3),
                Fill = new SolidColorPaint(SKColor.Parse("#22C55E").WithAlpha(50)),
                GeometrySize = 8
            }
        };
    }

    private void BuildMeetingsCharts()
    {
        if (MeetingsData == null) return;

        // Type distribution pie chart
        var typePieSeries = new List<ISeries>();
        foreach (var type in MeetingsData.TypeDistribution)
        {
            if (type.Count > 0)
            {
                typePieSeries.Add(new PieSeries<int>
                {
                    Name = type.Type,
                    Values = new[] { type.Count },
                    Fill = new SolidColorPaint(SKColor.Parse(type.Color)),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{type.Type}: {type.Count}"
                });
            }
        }
        MeetingTypePieSeries = typePieSeries.ToArray();

        // Meeting count over time (bar chart)
        MeetingCountTrendSeries = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Name = "Meetings",
                Values = ConvertToDateTimePoints(MeetingsData.MeetingCountOverTime),
                Fill = new SolidColorPaint(SKColor.Parse("#3B82F6"))
            }
        };
    }

    private void BuildTeamCharts()
    {
        if (TeamData == null) return;

        // Feedback type distribution pie chart
        var feedbackPieSeries = new List<ISeries>();
        foreach (var type in TeamData.FeedbackDistribution)
        {
            if (type.Count > 0)
            {
                feedbackPieSeries.Add(new PieSeries<int>
                {
                    Name = type.Type,
                    Values = new[] { type.Count },
                    Fill = new SolidColorPaint(SKColor.Parse(type.Color)),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = point => $"{type.Type}: {type.Count}"
                });
            }
        }
        FeedbackTypePieSeries = feedbackPieSeries.ToArray();

        // Feedback over time trend
        FeedbackTrendSeries = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Name = "Feedback",
                Values = ConvertToDateTimePoints(TeamData.FeedbackOverTime),
                Fill = new SolidColorPaint(SKColor.Parse("#22C55E"))
            }
        };
    }

    #endregion

    #region Helpers

    private static List<DateTimePoint> ConvertToDateTimePoints(List<DateValuePoint> points)
    {
        var result = new List<DateTimePoint>();
        foreach (var point in points)
        {
            result.Add(new DateTimePoint(point.Date, point.Value));
        }
        return result;
    }

    #endregion
}
