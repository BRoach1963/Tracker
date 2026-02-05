using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for exporting data to Excel format.
/// Singleton pattern matching other ProCohere services.
/// </summary>
public class ExcelExportService
{
    #region Singleton

    private static readonly Lazy<ExcelExportService> _instance =
        new(() => new ExcelExportService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static ExcelExportService Instance => _instance.Value;

    private ExcelExportService()
    {
        // Set EPPlus license for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "excel_export_service.log");

    public string? LastError { get; private set; }

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

    #region Color Constants

    private static readonly Color HeaderColor = Color.FromArgb(59, 130, 246); // Blue-500
    private static readonly Color HeaderTextColor = Color.White;
    private static readonly Color AlternateRowColor = Color.FromArgb(248, 250, 252); // Slate-50

    #endregion

    #region Team Members Export

    /// <summary>
    /// Exports team members to an Excel file.
    /// </summary>
    public async Task<bool> ExportTeamMembersAsync(IEnumerable<TeamMemberDetail> members, string filePath)
    {
        try
        {
            Log($"Exporting team members to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Team Members");

                // Headers
                var headers = new[] { "Name", "Email", "Job Title", "Manager", "LinkedIn", "Hire Date", "Birthday" };
                AddHeaders(worksheet, headers);

                // Data
                var memberList = members.ToList();
                for (int i = 0; i < memberList.Count; i++)
                {
                    var member = memberList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = member.FullName;
                    worksheet.Cells[row, 2].Value = member.Email;
                    worksheet.Cells[row, 3].Value = member.JobTitle ?? string.Empty;
                    worksheet.Cells[row, 4].Value = member.ManagerName ?? string.Empty;
                    worksheet.Cells[row, 5].Value = member.LinkedInUrl ?? string.Empty;
                    worksheet.Cells[row, 6].Value = member.HireDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                    worksheet.Cells[row, 7].Value = member.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty;

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {members.Count()} team members");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Meetings Export

    /// <summary>
    /// Exports meetings to an Excel file.
    /// </summary>
    public async Task<bool> ExportMeetingsAsync(IEnumerable<MeetingDetail> meetings, string filePath)
    {
        try
        {
            Log($"Exporting meetings to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Meetings");

                // Headers
                var headers = new[] { "Title", "Type", "Date", "Duration (min)", "Status", "Attendee", "Agenda Items", "Notes" };
                AddHeaders(worksheet, headers);

                // Data
                var meetingList = meetings.ToList();
                for (int i = 0; i < meetingList.Count; i++)
                {
                    var meeting = meetingList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = meeting.Title ?? "Untitled Meeting";
                    worksheet.Cells[row, 2].Value = meeting.MeetingType ?? "one_on_one";
                    worksheet.Cells[row, 3].Value = meeting.ScheduledAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
                    worksheet.Cells[row, 4].Value = meeting.DurationMinutes;
                    worksheet.Cells[row, 5].Value = meeting.Status;
                    worksheet.Cells[row, 6].Value = meeting.TeamMemberName ?? string.Empty;
                    worksheet.Cells[row, 7].Value = meeting.AgendaItems?.Count ?? 0;
                    worksheet.Cells[row, 8].Value = meeting.MyNotesCount;

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {meetings.Count()} meetings");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Tasks Export

    /// <summary>
    /// Exports tasks to an Excel file.
    /// </summary>
    public async Task<bool> ExportTasksAsync(IEnumerable<TaskDetail> tasks, string filePath)
    {
        try
        {
            Log($"Exporting tasks to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Tasks");

                // Headers
                var headers = new[] { "Title", "Description", "Status", "Priority", "Due Date", "Completed At", "Source" };
                AddHeaders(worksheet, headers);

                // Data
                var taskList = tasks.ToList();
                for (int i = 0; i < taskList.Count; i++)
                {
                    var task = taskList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = task.Title;
                    worksheet.Cells[row, 2].Value = task.Description ?? string.Empty;
                    worksheet.Cells[row, 3].Value = task.StatusDisplay;
                    worksheet.Cells[row, 4].Value = task.Priority ?? "medium";
                    worksheet.Cells[row, 5].Value = task.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                    worksheet.Cells[row, 6].Value = task.CompletedAt?.ToString("yyyy-MM-dd") ?? string.Empty;
                    worksheet.Cells[row, 7].Value = task.SourceType ?? "manual";

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {tasks.Count()} tasks");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Goals Export

    /// <summary>
    /// Exports goals to an Excel file.
    /// </summary>
    public async Task<bool> ExportGoalsAsync(IEnumerable<GoalDetail> goals, string filePath)
    {
        try
        {
            Log($"Exporting goals to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Goals");

                // Headers
                var headers = new[] { "Title", "Description", "Type", "Status", "Health", "Start Date", "Due Date" };
                AddHeaders(worksheet, headers);

                // Data
                var goalList = goals.ToList();
                for (int i = 0; i < goalList.Count; i++)
                {
                    var goal = goalList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = goal.Title;
                    worksheet.Cells[row, 2].Value = goal.Description ?? string.Empty;
                    worksheet.Cells[row, 3].Value = goal.GoalType.ToString();
                    worksheet.Cells[row, 4].Value = goal.Status ?? "active";
                    worksheet.Cells[row, 5].Value = goal.Health.ToString();
                    worksheet.Cells[row, 6].Value = goal.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                    worksheet.Cells[row, 7].Value = goal.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {goals.Count()} goals");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Metrics Export

    /// <summary>
    /// Exports metrics to an Excel file.
    /// </summary>
    public async Task<bool> ExportMetricsAsync(IEnumerable<MetricDetail> metrics, string filePath)
    {
        try
        {
            Log($"Exporting metrics to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Metrics");

                // Headers
                var headers = new[] { "Name", "Description", "Type", "Current Value", "Target Value", "Unit", "Trend", "Direction" };
                AddHeaders(worksheet, headers);

                // Data
                var metricList = metrics.ToList();
                for (int i = 0; i < metricList.Count; i++)
                {
                    var metric = metricList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = metric.Name;
                    worksheet.Cells[row, 2].Value = metric.Description ?? string.Empty;
                    worksheet.Cells[row, 3].Value = metric.MetricType;
                    worksheet.Cells[row, 4].Value = metric.CurrentValue?.ToString("F2") ?? string.Empty;
                    worksheet.Cells[row, 5].Value = metric.TargetValue?.ToString("F2") ?? string.Empty;
                    worksheet.Cells[row, 6].Value = metric.Unit ?? string.Empty;
                    worksheet.Cells[row, 7].Value = metric.Trend.ToString();
                    worksheet.Cells[row, 8].Value = metric.TargetDirection ?? string.Empty;

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {metrics.Count()} metrics");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Projects Export

    /// <summary>
    /// Exports projects to an Excel file.
    /// </summary>
    public async Task<bool> ExportProjectsAsync(IEnumerable<Project> projects, string filePath)
    {
        try
        {
            Log($"Exporting projects to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Projects");

                // Headers
                var headers = new[] { "Name", "Description", "Status", "Due Date", "Created At" };
                AddHeaders(worksheet, headers);

                // Data
                var projectList = projects.ToList();
                for (int i = 0; i < projectList.Count; i++)
                {
                    var project = projectList[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = project.Name;
                    worksheet.Cells[row, 2].Value = project.Description ?? string.Empty;
                    worksheet.Cells[row, 3].Value = project.Status;
                    worksheet.Cells[row, 4].Value = project.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                    worksheet.Cells[row, 5].Value = project.CreatedAt.ToString("yyyy-MM-dd");

                    ApplyAlternateRowStyle(worksheet, row, headers.Length);
                }

                FinalizeWorksheet(worksheet);
                package.SaveAs(new FileInfo(filePath));
            });

            Log($"Successfully exported {projects.Count()} projects");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region All Data Export

    /// <summary>
    /// Export data bundle for combined export.
    /// </summary>
    public class ExportDataBundle
    {
        public IEnumerable<TeamMemberDetail> TeamMembers { get; init; } = Enumerable.Empty<TeamMemberDetail>();
        public IEnumerable<MeetingDetail> Meetings { get; init; } = Enumerable.Empty<MeetingDetail>();
        public IEnumerable<TaskDetail> Tasks { get; init; } = Enumerable.Empty<TaskDetail>();
        public IEnumerable<GoalDetail> Goals { get; init; } = Enumerable.Empty<GoalDetail>();
        public IEnumerable<MetricDetail> Metrics { get; init; } = Enumerable.Empty<MetricDetail>();
        public IEnumerable<Project> Projects { get; init; } = Enumerable.Empty<Project>();
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
    }

    /// <summary>
    /// Exports all data to a single Excel file with multiple worksheets.
    /// </summary>
    public async Task<bool> ExportAllDataAsync(ExportDataBundle bundle, string filePath)
    {
        try
        {
            Log($"Exporting all data to: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                using var package = new ExcelPackage();

                // Summary sheet
                AddSummarySheet(package, bundle);

                // Data sheets
                AddTeamMembersSheet(package, bundle.TeamMembers);
                AddMeetingsSheet(package, bundle.Meetings);
                AddTasksSheet(package, bundle.Tasks);
                AddGoalsSheet(package, bundle.Goals);
                AddMetricsSheet(package, bundle.Metrics);
                AddProjectsSheet(package, bundle.Projects);

                package.SaveAs(new FileInfo(filePath));
            });

            Log("Successfully exported all data");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"Export failed: {ex.Message}");
            return false;
        }
    }

    private void AddSummarySheet(ExcelPackage package, ExportDataBundle bundle)
    {
        var worksheet = package.Workbook.Worksheets.Add("Summary");

        // Title
        worksheet.Cells[1, 1].Value = "ProCohere Report";
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 18;
        worksheet.Cells[1, 1].Style.Font.Color.SetColor(HeaderColor);

        // Date range
        worksheet.Cells[3, 1].Value = "Report Period:";
        worksheet.Cells[3, 1].Style.Font.Bold = true;
        worksheet.Cells[3, 2].Value = $"{bundle.StartDate:yyyy-MM-dd} to {bundle.EndDate:yyyy-MM-dd}";

        worksheet.Cells[4, 1].Value = "Generated:";
        worksheet.Cells[4, 1].Style.Font.Bold = true;
        worksheet.Cells[4, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Counts
        worksheet.Cells[6, 1].Value = "Data Summary";
        worksheet.Cells[6, 1].Style.Font.Bold = true;
        worksheet.Cells[6, 1].Style.Font.Size = 14;

        var summaryData = new[]
        {
            ("Team Members", bundle.TeamMembers.Count()),
            ("Meetings", bundle.Meetings.Count()),
            ("Tasks", bundle.Tasks.Count()),
            ("Goals", bundle.Goals.Count()),
            ("Metrics", bundle.Metrics.Count()),
            ("Projects", bundle.Projects.Count())
        };

        for (int i = 0; i < summaryData.Length; i++)
        {
            int row = 7 + i;
            worksheet.Cells[row, 1].Value = summaryData[i].Item1;
            worksheet.Cells[row, 2].Value = summaryData[i].Item2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private void AddTeamMembersSheet(ExcelPackage package, IEnumerable<TeamMemberDetail> members)
    {
        var worksheet = package.Workbook.Worksheets.Add("Team Members");
        var headers = new[] { "Name", "Email", "Job Title", "Manager", "LinkedIn", "Hire Date" };
        AddHeaders(worksheet, headers);

        var memberList = members.ToList();
        for (int i = 0; i < memberList.Count; i++)
        {
            var member = memberList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = member.FullName;
            worksheet.Cells[row, 2].Value = member.Email;
            worksheet.Cells[row, 3].Value = member.JobTitle ?? string.Empty;
            worksheet.Cells[row, 4].Value = member.ManagerName ?? string.Empty;
            worksheet.Cells[row, 5].Value = member.LinkedInUrl ?? string.Empty;
            worksheet.Cells[row, 6].Value = member.HireDate?.ToString("yyyy-MM-dd") ?? string.Empty;

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    private void AddMeetingsSheet(ExcelPackage package, IEnumerable<MeetingDetail> meetings)
    {
        var worksheet = package.Workbook.Worksheets.Add("Meetings");
        var headers = new[] { "Title", "Type", "Date", "Duration", "Status", "Attendee" };
        AddHeaders(worksheet, headers);

        var meetingList = meetings.ToList();
        for (int i = 0; i < meetingList.Count; i++)
        {
            var meeting = meetingList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = meeting.Title ?? "Untitled";
            worksheet.Cells[row, 2].Value = meeting.MeetingType ?? "one_on_one";
            worksheet.Cells[row, 3].Value = meeting.ScheduledAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
            worksheet.Cells[row, 4].Value = meeting.DurationMinutes;
            worksheet.Cells[row, 5].Value = meeting.Status;
            worksheet.Cells[row, 6].Value = meeting.TeamMemberName ?? string.Empty;

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    private void AddTasksSheet(ExcelPackage package, IEnumerable<TaskDetail> tasks)
    {
        var worksheet = package.Workbook.Worksheets.Add("Tasks");
        var headers = new[] { "Title", "Status", "Priority", "Due Date", "Completed" };
        AddHeaders(worksheet, headers);

        var taskList = tasks.ToList();
        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = task.Title;
            worksheet.Cells[row, 2].Value = task.StatusDisplay;
            worksheet.Cells[row, 3].Value = task.Priority ?? "medium";
            worksheet.Cells[row, 4].Value = task.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            worksheet.Cells[row, 5].Value = task.CompletedAt?.ToString("yyyy-MM-dd") ?? string.Empty;

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    private void AddGoalsSheet(ExcelPackage package, IEnumerable<GoalDetail> goals)
    {
        var worksheet = package.Workbook.Worksheets.Add("Goals");
        var headers = new[] { "Title", "Type", "Status", "Health", "Due Date" };
        AddHeaders(worksheet, headers);

        var goalList = goals.ToList();
        for (int i = 0; i < goalList.Count; i++)
        {
            var goal = goalList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = goal.Title;
            worksheet.Cells[row, 2].Value = goal.GoalType.ToString();
            worksheet.Cells[row, 3].Value = goal.Status ?? "active";
            worksheet.Cells[row, 4].Value = goal.Health.ToString();
            worksheet.Cells[row, 5].Value = goal.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    private void AddMetricsSheet(ExcelPackage package, IEnumerable<MetricDetail> metrics)
    {
        var worksheet = package.Workbook.Worksheets.Add("Metrics");
        var headers = new[] { "Name", "Type", "Current", "Target", "Unit", "Trend" };
        AddHeaders(worksheet, headers);

        var metricList = metrics.ToList();
        for (int i = 0; i < metricList.Count; i++)
        {
            var metric = metricList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = metric.Name;
            worksheet.Cells[row, 2].Value = metric.MetricType;
            worksheet.Cells[row, 3].Value = metric.CurrentValue?.ToString("F2") ?? string.Empty;
            worksheet.Cells[row, 4].Value = metric.TargetValue?.ToString("F2") ?? string.Empty;
            worksheet.Cells[row, 5].Value = metric.Unit ?? string.Empty;
            worksheet.Cells[row, 6].Value = metric.Trend.ToString();

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    private void AddProjectsSheet(ExcelPackage package, IEnumerable<Project> projects)
    {
        var worksheet = package.Workbook.Worksheets.Add("Projects");
        var headers = new[] { "Name", "Description", "Status", "Due Date" };
        AddHeaders(worksheet, headers);

        var projectList = projects.ToList();
        for (int i = 0; i < projectList.Count; i++)
        {
            var project = projectList[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = project.Name;
            worksheet.Cells[row, 2].Value = project.Description ?? string.Empty;
            worksheet.Cells[row, 3].Value = project.Status;
            worksheet.Cells[row, 4].Value = project.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty;

            ApplyAlternateRowStyle(worksheet, row, headers.Length);
        }

        FinalizeWorksheet(worksheet);
    }

    #endregion

    #region Helpers

    private static void AddHeaders(ExcelWorksheet worksheet, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        using var range = worksheet.Cells[1, 1, 1, headers.Length];
        range.Style.Font.Bold = true;
        range.Style.Font.Color.SetColor(HeaderTextColor);
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(HeaderColor);
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
        range.Style.Border.Bottom.Color.SetColor(Color.FromArgb(30, 64, 175)); // Blue-800
    }

    private static void ApplyAlternateRowStyle(ExcelWorksheet worksheet, int row, int columnCount)
    {
        if (row % 2 == 0)
        {
            using var range = worksheet.Cells[row, 1, row, columnCount];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(AlternateRowColor);
        }
    }

    private static void FinalizeWorksheet(ExcelWorksheet worksheet)
    {
        if (worksheet.Dimension != null)
        {
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Add thin borders to all cells
            using var range = worksheet.Cells[worksheet.Dimension.Address];
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Top.Color.SetColor(Color.FromArgb(226, 232, 240)); // Slate-200
            range.Style.Border.Bottom.Color.SetColor(Color.FromArgb(226, 232, 240));
            range.Style.Border.Left.Color.SetColor(Color.FromArgb(226, 232, 240));
            range.Style.Border.Right.Color.SetColor(Color.FromArgb(226, 232, 240));
        }

        // Freeze header row
        worksheet.View.FreezePanes(2, 1);
    }

    #endregion
}
