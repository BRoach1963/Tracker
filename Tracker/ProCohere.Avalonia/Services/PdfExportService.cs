using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for exporting data to PDF format using QuestPDF.
/// Singleton pattern matching other ProCohere services.
/// </summary>
public class PdfExportService
{
    #region Singleton

    private static readonly Lazy<PdfExportService> _instance =
        new(() => new PdfExportService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static PdfExportService Instance => _instance.Value;

    private PdfExportService()
    {
        // Set QuestPDF license for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "pdf_export_service.log");

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

    private static readonly string PrimaryColor = "#3B82F6";    // Blue-500
    private static readonly string HeaderBgColor = "#1E40AF";   // Blue-800
    private static readonly string LightBgColor = "#F8FAFC";    // Slate-50
    private static readonly string BorderColor = "#E2E8F0";     // Slate-200
    private static readonly string TextColor = "#1E293B";       // Slate-800
    private static readonly string SecondaryTextColor = "#64748B"; // Slate-500

    #endregion

    #region Team Members Export

    /// <summary>
    /// Exports team members to a PDF file.
    /// </summary>
    public async Task<bool> ExportTeamMembersAsync(IEnumerable<TeamMemberDetail> members, string filePath)
    {
        try
        {
            Log($"Exporting team members to PDF: {filePath}");
            LastError = null;

            var memberList = members.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Header().Element(c => ComposeHeader(c, "Team Members Report"));
                        
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(10).Column(column =>
                            {
                                column.Spacing(10);
                                
                                // Summary
                                column.Item().Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Total Members", memberList.Count.ToString() },
                                    { "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                                }));

                                // Table
                                column.Item().Element(e => ComposeTeamMembersTable(e, memberList));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf(filePath);
            });

            Log($"Successfully exported {memberList.Count} team members to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeTeamMembersTable(IContainer container, List<TeamMemberDetail> members)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);  // Name
                columns.RelativeColumn(2);  // Email
                columns.RelativeColumn(1.5f); // Job Title
                columns.RelativeColumn(1.5f); // Manager
            });

            // Header
            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Name");
                ComposeTableHeader(header.Cell(), "Email");
                ComposeTableHeader(header.Cell(), "Job Title");
                ComposeTableHeader(header.Cell(), "Manager");
            });

            // Rows
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), member.FullName, isAlternate);
                ComposeTableCell(table.Cell(), member.Email, isAlternate);
                ComposeTableCell(table.Cell(), member.JobTitle ?? "", isAlternate);
                ComposeTableCell(table.Cell(), member.ManagerName ?? "", isAlternate);
            }
        });
    }

    #endregion

    #region Meetings Export

    /// <summary>
    /// Exports meetings to a PDF file.
    /// </summary>
    public async Task<bool> ExportMeetingsAsync(IEnumerable<MeetingDetail> meetings, string filePath)
    {
        try
        {
            Log($"Exporting meetings to PDF: {filePath}");
            LastError = null;

            var meetingList = meetings.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Header().Element(c => ComposeHeader(c, "Meetings Report"));
                        
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(10).Column(column =>
                            {
                                column.Spacing(10);
                                
                                column.Item().Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Total Meetings", meetingList.Count.ToString() },
                                    { "Completed", meetingList.Count(m => m.Status == "completed").ToString() },
                                    { "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                                }));

                                column.Item().Element(e => ComposeMeetingsTable(e, meetingList));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf(filePath);
            });

            Log($"Successfully exported {meetingList.Count} meetings to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeMeetingsTable(IContainer container, List<MeetingDetail> meetings)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);    // Title
                columns.RelativeColumn(1);    // Type
                columns.RelativeColumn(1.5f); // Date
                columns.RelativeColumn(1);    // Duration
                columns.RelativeColumn(1);    // Status
            });

            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Title");
                ComposeTableHeader(header.Cell(), "Type");
                ComposeTableHeader(header.Cell(), "Date");
                ComposeTableHeader(header.Cell(), "Duration");
                ComposeTableHeader(header.Cell(), "Status");
            });

            for (int i = 0; i < meetings.Count; i++)
            {
                var meeting = meetings[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), meeting.Title ?? "Untitled", isAlternate);
                ComposeTableCell(table.Cell(), meeting.MeetingType ?? "one_on_one", isAlternate);
                ComposeTableCell(table.Cell(), meeting.ScheduledAt?.ToString("MMM d, yyyy") ?? "", isAlternate);
                ComposeTableCell(table.Cell(), $"{meeting.DurationMinutes} min", isAlternate);
                ComposeTableCell(table.Cell(), meeting.Status ?? "", isAlternate);
            }
        });
    }

    #endregion

    #region Tasks Export

    /// <summary>
    /// Exports tasks to a PDF file.
    /// </summary>
    public async Task<bool> ExportTasksAsync(IEnumerable<TaskDetail> tasks, string filePath)
    {
        try
        {
            Log($"Exporting tasks to PDF: {filePath}");
            LastError = null;

            var taskList = tasks.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Header().Element(c => ComposeHeader(c, "Tasks Report"));
                        
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(10).Column(column =>
                            {
                                column.Spacing(10);
                                
                                var completed = taskList.Count(t => t.Status == "completed");
                                var overdue = taskList.Count(t => t.IsOverdue);
                                
                                column.Item().Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Total Tasks", taskList.Count.ToString() },
                                    { "Completed", completed.ToString() },
                                    { "Overdue", overdue.ToString() },
                                    { "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                                }));

                                column.Item().Element(e => ComposeTasksTable(e, taskList));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf(filePath);
            });

            Log($"Successfully exported {taskList.Count} tasks to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeTasksTable(IContainer container, List<TaskDetail> tasks)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);    // Title
                columns.RelativeColumn(1);    // Status
                columns.RelativeColumn(1);    // Priority
                columns.RelativeColumn(1.5f); // Due Date
            });

            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Title");
                ComposeTableHeader(header.Cell(), "Status");
                ComposeTableHeader(header.Cell(), "Priority");
                ComposeTableHeader(header.Cell(), "Due Date");
            });

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), task.Title, isAlternate);
                ComposeTableCell(table.Cell(), task.StatusDisplay, isAlternate);
                ComposeTableCell(table.Cell(), task.Priority ?? "medium", isAlternate);
                ComposeTableCell(table.Cell(), task.DueDate?.ToString("MMM d, yyyy") ?? "", isAlternate);
            }
        });
    }

    #endregion

    #region Goals Export

    /// <summary>
    /// Exports goals to a PDF file.
    /// </summary>
    public async Task<bool> ExportGoalsAsync(IEnumerable<GoalDetail> goals, string filePath)
    {
        try
        {
            Log($"Exporting goals to PDF: {filePath}");
            LastError = null;

            var goalList = goals.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Header().Element(c => ComposeHeader(c, "Goals Report"));
                        
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(10).Column(column =>
                            {
                                column.Spacing(10);
                                
                                var onTrack = goalList.Count(g => g.Health == GoalHealth.OnTrack);
                                var atRisk = goalList.Count(g => g.Health == GoalHealth.AtRisk);
                                
                                column.Item().Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Total Goals", goalList.Count.ToString() },
                                    { "On Track", onTrack.ToString() },
                                    { "At Risk", atRisk.ToString() },
                                    { "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                                }));

                                column.Item().Element(e => ComposeGoalsTable(e, goalList));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf(filePath);
            });

            Log($"Successfully exported {goalList.Count} goals to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeGoalsTable(IContainer container, List<GoalDetail> goals)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);    // Title
                columns.RelativeColumn(1);    // Type
                columns.RelativeColumn(1);    // Status
                columns.RelativeColumn(1);    // Health
                columns.RelativeColumn(1.5f); // Due Date
            });

            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Title");
                ComposeTableHeader(header.Cell(), "Type");
                ComposeTableHeader(header.Cell(), "Status");
                ComposeTableHeader(header.Cell(), "Health");
                ComposeTableHeader(header.Cell(), "Due Date");
            });

            for (int i = 0; i < goals.Count; i++)
            {
                var goal = goals[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), goal.Title, isAlternate);
                ComposeTableCell(table.Cell(), goal.GoalType.ToString(), isAlternate);
                ComposeTableCell(table.Cell(), goal.Status ?? "active", isAlternate);
                ComposeTableCell(table.Cell(), goal.Health.ToString(), isAlternate);
                ComposeTableCell(table.Cell(), goal.DueDate?.ToString("MMM d, yyyy") ?? "", isAlternate);
            }
        });
    }

    #endregion

    #region Metrics Export

    /// <summary>
    /// Exports metrics to a PDF file.
    /// </summary>
    public async Task<bool> ExportMetricsAsync(IEnumerable<MetricDetail> metrics, string filePath)
    {
        try
        {
            Log($"Exporting metrics to PDF: {filePath}");
            LastError = null;

            var metricList = metrics.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Header().Element(c => ComposeHeader(c, "Metrics Report"));
                        
                        page.Content().Element(c =>
                        {
                            c.PaddingVertical(10).Column(column =>
                            {
                                column.Spacing(10);
                                
                                column.Item().Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Total Metrics", metricList.Count.ToString() },
                                    { "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                                }));

                                column.Item().Element(e => ComposeMetricsTable(e, metricList));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf(filePath);
            });

            Log($"Successfully exported {metricList.Count} metrics to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeMetricsTable(IContainer container, List<MetricDetail> metrics)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.5f); // Name
                columns.RelativeColumn(1);    // Type
                columns.RelativeColumn(1);    // Current
                columns.RelativeColumn(1);    // Target
                columns.RelativeColumn(1);    // Trend
            });

            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Name");
                ComposeTableHeader(header.Cell(), "Type");
                ComposeTableHeader(header.Cell(), "Current");
                ComposeTableHeader(header.Cell(), "Target");
                ComposeTableHeader(header.Cell(), "Trend");
            });

            for (int i = 0; i < metrics.Count; i++)
            {
                var metric = metrics[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), metric.Name, isAlternate);
                ComposeTableCell(table.Cell(), metric.MetricType, isAlternate);
                ComposeTableCell(table.Cell(), metric.CurrentValue?.ToString("F2") ?? "-", isAlternate);
                ComposeTableCell(table.Cell(), metric.TargetValue?.ToString("F2") ?? "-", isAlternate);
                ComposeTableCell(table.Cell(), metric.Trend.ToString(), isAlternate);
            }
        });
    }

    #endregion

    #region All Data Export

    /// <summary>
    /// Exports all data to a single PDF file with multiple sections.
    /// </summary>
    public async Task<bool> ExportAllDataAsync(ExcelExportService.ExportDataBundle bundle, string filePath)
    {
        try
        {
            Log($"Exporting all data to PDF: {filePath}");
            LastError = null;

            var teamMembers = bundle.TeamMembers.ToList();
            var meetings = bundle.Meetings.ToList();
            var tasks = bundle.Tasks.ToList();
            var goals = bundle.Goals.ToList();
            var metrics = bundle.Metrics.ToList();
            var projects = bundle.Projects.ToList();

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    // Title Page
                    container.Page(page =>
                    {
                        ConfigurePage(page);
                        
                        page.Content().Element(c =>
                        {
                            c.AlignCenter().AlignMiddle().Column(column =>
                            {
                                column.Spacing(20);
                                
                                column.Item().Text("ProCohere")
                                    .FontSize(36).Bold().FontColor(PrimaryColor);
                                
                                column.Item().Text("Complete Data Export")
                                    .FontSize(24).FontColor(TextColor);
                                
                                column.Item().PaddingTop(30).Text($"Report Period: {bundle.StartDate:MMM d, yyyy} - {bundle.EndDate:MMM d, yyyy}")
                                    .FontSize(14).FontColor(SecondaryTextColor);
                                
                                column.Item().Text($"Generated: {DateTime.Now:MMMM d, yyyy h:mm tt}")
                                    .FontSize(12).FontColor(SecondaryTextColor);
                                
                                column.Item().PaddingTop(40).Element(e => ComposeSummaryBox(e, new Dictionary<string, string>
                                {
                                    { "Team Members", teamMembers.Count.ToString() },
                                    { "Meetings", meetings.Count.ToString() },
                                    { "Tasks", tasks.Count.ToString() },
                                    { "Goals", goals.Count.ToString() },
                                    { "Metrics", metrics.Count.ToString() },
                                    { "Projects", projects.Count.ToString() }
                                }));
                            });
                        });

                        page.Footer().Element(ComposeFooter);
                    });

                    // Team Members Section
                    if (teamMembers.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Team Members"));
                            page.Content().PaddingVertical(10).Element(e => ComposeTeamMembersTable(e, teamMembers));
                            page.Footer().Element(ComposeFooter);
                        });
                    }

                    // Meetings Section
                    if (meetings.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Meetings"));
                            page.Content().PaddingVertical(10).Element(e => ComposeMeetingsTable(e, meetings));
                            page.Footer().Element(ComposeFooter);
                        });
                    }

                    // Tasks Section
                    if (tasks.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Tasks"));
                            page.Content().PaddingVertical(10).Element(e => ComposeTasksTable(e, tasks));
                            page.Footer().Element(ComposeFooter);
                        });
                    }

                    // Goals Section
                    if (goals.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Goals"));
                            page.Content().PaddingVertical(10).Element(e => ComposeGoalsTable(e, goals));
                            page.Footer().Element(ComposeFooter);
                        });
                    }

                    // Metrics Section
                    if (metrics.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Metrics"));
                            page.Content().PaddingVertical(10).Element(e => ComposeMetricsTable(e, metrics));
                            page.Footer().Element(ComposeFooter);
                        });
                    }

                    // Projects Section
                    if (projects.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePage(page);
                            page.Header().Element(c => ComposeHeader(c, "Projects"));
                            page.Content().PaddingVertical(10).Element(e => ComposeProjectsTable(e, projects));
                            page.Footer().Element(ComposeFooter);
                        });
                    }
                }).GeneratePdf(filePath);
            });

            Log("Successfully exported all data to PDF");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private void ComposeProjectsTable(IContainer container, List<Project> projects)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.5f); // Name
                columns.RelativeColumn(3);    // Description
                columns.RelativeColumn(1);    // Status
                columns.RelativeColumn(1.5f); // Due Date
            });

            table.Header(header =>
            {
                ComposeTableHeader(header.Cell(), "Name");
                ComposeTableHeader(header.Cell(), "Description");
                ComposeTableHeader(header.Cell(), "Status");
                ComposeTableHeader(header.Cell(), "Due Date");
            });

            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                var isAlternate = i % 2 == 1;

                ComposeTableCell(table.Cell(), project.Name, isAlternate);
                ComposeTableCell(table.Cell(), TruncateText(project.Description ?? "", 50), isAlternate);
                ComposeTableCell(table.Cell(), project.Status, isAlternate);
                ComposeTableCell(table.Cell(), project.DueDate?.ToString("MMM d, yyyy") ?? "", isAlternate);
            }
        });
    }

    #endregion

    #region Shared Components

    private void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(40);
        page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));
    }

    private void ComposeHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(title)
                    .FontSize(20).Bold().FontColor(PrimaryColor);
                
                column.Item().PaddingTop(5).LineHorizontal(2).LineColor(PrimaryColor);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("ProCohere Report • Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private void ComposeSummaryBox(IContainer container, Dictionary<string, string> items)
    {
        container.Background(LightBgColor).Border(1).BorderColor(BorderColor).Padding(15).Row(row =>
        {
            foreach (var item in items)
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(item.Value)
                        .FontSize(18).Bold().FontColor(PrimaryColor);
                    column.Item().Text(item.Key)
                        .FontSize(10).FontColor(SecondaryTextColor);
                });
            }
        });
    }

    private void ComposeTableHeader(IContainer cell, string text)
    {
        cell.Background(HeaderBgColor).Padding(8).Text(text)
            .FontSize(10).Bold().FontColor(Colors.White);
    }

    private void ComposeTableCell(IContainer cell, string text, bool isAlternate)
    {
        var bgColor = isAlternate ? LightBgColor : "#FFFFFF";
        cell.Background(bgColor).BorderBottom(1).BorderColor(BorderColor)
            .Padding(8).Text(text).FontSize(9);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
