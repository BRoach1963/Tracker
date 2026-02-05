using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for exporting data to CSV format.
/// Singleton pattern matching other ProCohere services.
/// </summary>
public class CsvExportService
{
    #region Singleton

    private static readonly Lazy<CsvExportService> _instance =
        new(() => new CsvExportService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static CsvExportService Instance => _instance.Value;

    private CsvExportService() { }

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "csv_export_service.log");

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

    #region CSV Utilities

    /// <summary>
    /// Escapes a value for CSV format (handles quotes, commas, newlines).
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If value contains comma, quote, or newline, wrap in quotes and escape existing quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Creates a CSV line from an array of values.
    /// </summary>
    private static string ToCsvLine(params string?[] values)
    {
        return string.Join(",", values.Select(EscapeCsvValue));
    }

    #endregion

    #region Team Members Export

    /// <summary>
    /// Exports team members to a CSV file.
    /// </summary>
    public async Task<bool> ExportTeamMembersAsync(IEnumerable<TeamMemberDetail> members, string filePath)
    {
        try
        {
            Log($"Exporting team members to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Name", "Email", "Job Title", "Manager", "LinkedIn", "Hire Date", "Birthday"));

                // Data rows
                foreach (var member in members)
                {
                    sb.AppendLine(ToCsvLine(
                        member.FullName,
                        member.Email,
                        member.JobTitle,
                        member.ManagerName,
                        member.LinkedInUrl,
                        member.HireDate?.ToString("yyyy-MM-dd"),
                        member.Birthday?.ToString("yyyy-MM-dd")
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Team members CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting team members to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for team members (for ZIP export).
    /// </summary>
    private string GenerateTeamMembersCsv(IEnumerable<TeamMemberDetail> members)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Name", "Email", "Job Title", "Manager", "LinkedIn", "Hire Date", "Birthday"));

        foreach (var member in members)
        {
            sb.AppendLine(ToCsvLine(
                member.FullName,
                member.Email,
                member.JobTitle,
                member.ManagerName,
                member.LinkedInUrl,
                member.HireDate?.ToString("yyyy-MM-dd"),
                member.Birthday?.ToString("yyyy-MM-dd")
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Meetings Export

    /// <summary>
    /// Exports meetings to a CSV file.
    /// </summary>
    public async Task<bool> ExportMeetingsAsync(IEnumerable<MeetingDetail> meetings, string filePath)
    {
        try
        {
            Log($"Exporting meetings to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Title", "Type", "Date", "Duration (min)", "Status", "Attendee", "Agenda Items", "Notes"));

                // Data rows
                foreach (var meeting in meetings)
                {
                    sb.AppendLine(ToCsvLine(
                        meeting.Title ?? "Untitled Meeting",
                        meeting.MeetingType ?? "one_on_one",
                        meeting.ScheduledAt?.ToString("yyyy-MM-dd HH:mm"),
                        meeting.DurationMinutes.ToString(),
                        meeting.Status,
                        meeting.TeamMemberName,
                        (meeting.AgendaItems?.Count ?? 0).ToString(),
                        meeting.MyNotesCount.ToString()
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Meetings CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting meetings to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for meetings (for ZIP export).
    /// </summary>
    private string GenerateMeetingsCsv(IEnumerable<MeetingDetail> meetings)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Title", "Type", "Date", "Duration (min)", "Status", "Attendee", "Agenda Items", "Notes"));

        foreach (var meeting in meetings)
        {
            sb.AppendLine(ToCsvLine(
                meeting.Title ?? "Untitled Meeting",
                meeting.MeetingType ?? "one_on_one",
                meeting.ScheduledAt?.ToString("yyyy-MM-dd HH:mm"),
                meeting.DurationMinutes.ToString(),
                meeting.Status,
                meeting.TeamMemberName,
                (meeting.AgendaItems?.Count ?? 0).ToString(),
                meeting.MyNotesCount.ToString()
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Tasks Export

    /// <summary>
    /// Exports tasks to a CSV file.
    /// </summary>
    public async Task<bool> ExportTasksAsync(IEnumerable<TaskDetail> tasks, string filePath)
    {
        try
        {
            Log($"Exporting tasks to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Title", "Description", "Status", "Priority", "Due Date", "Completed At", "Source"));

                // Data rows
                foreach (var task in tasks)
                {
                    sb.AppendLine(ToCsvLine(
                        task.Title,
                        task.Description,
                        task.StatusDisplay,
                        task.Priority ?? "medium",
                        task.DueDate?.ToString("yyyy-MM-dd"),
                        task.CompletedAt?.ToString("yyyy-MM-dd"),
                        task.SourceType ?? "manual"
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Tasks CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting tasks to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for tasks (for ZIP export).
    /// </summary>
    private string GenerateTasksCsv(IEnumerable<TaskDetail> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Title", "Description", "Status", "Priority", "Due Date", "Completed At", "Source"));

        foreach (var task in tasks)
        {
            sb.AppendLine(ToCsvLine(
                task.Title,
                task.Description,
                task.StatusDisplay,
                task.Priority ?? "medium",
                task.DueDate?.ToString("yyyy-MM-dd"),
                task.CompletedAt?.ToString("yyyy-MM-dd"),
                task.SourceType ?? "manual"
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Goals Export

    /// <summary>
    /// Exports goals to a CSV file.
    /// </summary>
    public async Task<bool> ExportGoalsAsync(IEnumerable<GoalDetail> goals, string filePath)
    {
        try
        {
            Log($"Exporting goals to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Title", "Description", "Type", "Status", "Health", "Start Date", "Due Date"));

                // Data rows
                foreach (var goal in goals)
                {
                    sb.AppendLine(ToCsvLine(
                        goal.Title,
                        goal.Description,
                        goal.GoalType.ToString(),
                        goal.Status ?? "active",
                        goal.Health.ToString(),
                        goal.StartDate?.ToString("yyyy-MM-dd"),
                        goal.DueDate?.ToString("yyyy-MM-dd")
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Goals CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting goals to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for goals (for ZIP export).
    /// </summary>
    private string GenerateGoalsCsv(IEnumerable<GoalDetail> goals)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Title", "Description", "Type", "Status", "Health", "Start Date", "Due Date"));

        foreach (var goal in goals)
        {
            sb.AppendLine(ToCsvLine(
                goal.Title,
                goal.Description,
                goal.GoalType.ToString(),
                goal.Status ?? "active",
                goal.Health.ToString(),
                goal.StartDate?.ToString("yyyy-MM-dd"),
                goal.DueDate?.ToString("yyyy-MM-dd")
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Metrics Export

    /// <summary>
    /// Exports metrics to a CSV file.
    /// </summary>
    public async Task<bool> ExportMetricsAsync(IEnumerable<MetricDetail> metrics, string filePath)
    {
        try
        {
            Log($"Exporting metrics to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Name", "Description", "Type", "Current Value", "Target Value", "Unit", "Trend", "Direction"));

                // Data rows
                foreach (var metric in metrics)
                {
                    sb.AppendLine(ToCsvLine(
                        metric.Name,
                        metric.Description,
                        metric.MetricType,
                        metric.CurrentValue?.ToString("F2"),
                        metric.TargetValue?.ToString("F2"),
                        metric.Unit,
                        metric.Trend.ToString(),
                        metric.TargetDirection
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Metrics CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting metrics to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for metrics (for ZIP export).
    /// </summary>
    private string GenerateMetricsCsv(IEnumerable<MetricDetail> metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Name", "Description", "Type", "Current Value", "Target Value", "Unit", "Trend", "Direction"));

        foreach (var metric in metrics)
        {
            sb.AppendLine(ToCsvLine(
                metric.Name,
                metric.Description,
                metric.MetricType,
                metric.CurrentValue?.ToString("F2"),
                metric.TargetValue?.ToString("F2"),
                metric.Unit,
                metric.Trend.ToString(),
                metric.TargetDirection
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Projects Export

    /// <summary>
    /// Exports projects to a CSV file.
    /// </summary>
    public async Task<bool> ExportProjectsAsync(IEnumerable<Project> projects, string filePath)
    {
        try
        {
            Log($"Exporting projects to CSV: {filePath}");
            LastError = null;

            await Task.Run(() =>
            {
                var sb = new StringBuilder();

                // Header row (matches ExcelExportService)
                sb.AppendLine(ToCsvLine("Name", "Description", "Status", "Due Date", "Created At"));

                // Data rows
                foreach (var project in projects)
                {
                    sb.AppendLine(ToCsvLine(
                        project.Name,
                        project.Description,
                        project.Status,
                        project.DueDate?.ToString("yyyy-MM-dd"),
                        project.CreatedAt.ToString("yyyy-MM-dd")
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });

            Log($"Projects CSV export complete: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting projects to CSV: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates CSV content for projects (for ZIP export).
    /// </summary>
    private string GenerateProjectsCsv(IEnumerable<Project> projects)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToCsvLine("Name", "Description", "Status", "Due Date", "Created At"));

        foreach (var project in projects)
        {
            sb.AppendLine(ToCsvLine(
                project.Name,
                project.Description,
                project.Status,
                project.DueDate?.ToString("yyyy-MM-dd"),
                project.CreatedAt.ToString("yyyy-MM-dd")
            ));
        }

        return sb.ToString();
    }

    #endregion

    #region Export All Data

    /// <summary>
    /// Exports all data to a ZIP file containing individual CSV files.
    /// </summary>
    public async Task<bool> ExportAllDataAsync(
        IEnumerable<TeamMemberDetail> teamMembers,
        IEnumerable<MeetingDetail> meetings,
        IEnumerable<TaskDetail> tasks,
        IEnumerable<GoalDetail> goals,
        IEnumerable<MetricDetail> metrics,
        IEnumerable<Project> projects,
        string zipFilePath)
    {
        try
        {
            Log($"Exporting all data to ZIP: {zipFilePath}");
            LastError = null;

            await Task.Run(() =>
            {
                // Delete existing file if it exists
                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);

                using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);

                // Add team members CSV
                var teamMembersCsv = GenerateTeamMembersCsv(teamMembers);
                AddCsvToZip(archive, "TeamMembers.csv", teamMembersCsv);

                // Add meetings CSV
                var meetingsCsv = GenerateMeetingsCsv(meetings);
                AddCsvToZip(archive, "Meetings.csv", meetingsCsv);

                // Add tasks CSV
                var tasksCsv = GenerateTasksCsv(tasks);
                AddCsvToZip(archive, "Tasks.csv", tasksCsv);

                // Add goals CSV
                var goalsCsv = GenerateGoalsCsv(goals);
                AddCsvToZip(archive, "Goals.csv", goalsCsv);

                // Add metrics CSV
                var metricsCsv = GenerateMetricsCsv(metrics);
                AddCsvToZip(archive, "Metrics.csv", metricsCsv);

                // Add projects CSV
                var projectsCsv = GenerateProjectsCsv(projects);
                AddCsvToZip(archive, "Projects.csv", projectsCsv);

                // Add summary/readme
                var summary = GenerateSummaryReadme(
                    teamMembers.Count(),
                    meetings.Count(),
                    tasks.Count(),
                    goals.Count(),
                    metrics.Count(),
                    projects.Count()
                );
                AddCsvToZip(archive, "README.txt", summary);
            });

            Log($"All data ZIP export complete: {zipFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ERROR exporting all data to ZIP: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a CSV string to the ZIP archive.
    /// </summary>
    private static void AddCsvToZip(ZipArchive archive, string fileName, string content)
    {
        var entry = archive.CreateEntry(fileName);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    /// <summary>
    /// Generates a summary README for the ZIP export.
    /// </summary>
    private static string GenerateSummaryReadme(
        int teamMemberCount,
        int meetingCount,
        int taskCount,
        int goalCount,
        int metricCount,
        int projectCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ProCohere Data Export");
        sb.AppendLine("=====================");
        sb.AppendLine();
        sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("Contents:");
        sb.AppendLine($"  - TeamMembers.csv: {teamMemberCount} records");
        sb.AppendLine($"  - Meetings.csv: {meetingCount} records");
        sb.AppendLine($"  - Tasks.csv: {taskCount} records");
        sb.AppendLine($"  - Goals.csv: {goalCount} records");
        sb.AppendLine($"  - Metrics.csv: {metricCount} records");
        sb.AppendLine($"  - Projects.csv: {projectCount} records");
        sb.AppendLine();
        sb.AppendLine("Total Records: " + (teamMemberCount + meetingCount + taskCount + goalCount + metricCount + projectCount));
        sb.AppendLine();
        sb.AppendLine("All CSV files use UTF-8 encoding with comma separators.");
        sb.AppendLine("Values containing commas, quotes, or newlines are properly escaped.");

        return sb.ToString();
    }

    #endregion
}
