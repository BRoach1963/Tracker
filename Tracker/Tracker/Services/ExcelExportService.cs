using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Tracker.DataModels;

namespace Tracker.Services
{
    /// <summary>
    /// Service for exporting data to Excel format.
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Exports team members to an Excel file.
        /// </summary>
        public static void ExportTeamMembers(List<TeamMember> teamMembers, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Team Members");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "First Name";
            worksheet.Cells[1, 3].Value = "Last Name";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Role";
            worksheet.Cells[1, 6].Value = "Specialty";
            worksheet.Cells[1, 7].Value = "LinkedIn Profile";
            worksheet.Cells[1, 8].Value = "Created Date";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < teamMembers.Count; i++)
            {
                var member = teamMembers[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = member.Id;
                worksheet.Cells[row, 2].Value = member.FirstName;
                worksheet.Cells[row, 3].Value = member.LastName;
                worksheet.Cells[row, 4].Value = member.Email;
                worksheet.Cells[row, 5].Value = member.Role;
                worksheet.Cells[row, 6].Value = member.Specialty;
                worksheet.Cells[row, 7].Value = member.LinkedInUrl;
                worksheet.Cells[row, 8].Value = member.CreatedAt.ToString("yyyy-MM-dd");
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports 1:1 meetings to an Excel file.
        /// </summary>
        public static void ExportMeetings(List<Meeting> meetings, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("1:1 Meetings");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Date";
            worksheet.Cells[1, 3].Value = "Team Member";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Description";
            worksheet.Cells[1, 6].Value = "Tasks";
            worksheet.Cells[1, 7].Value = "Agenda Items";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < meetings.Count; i++)
            {
                var meeting = meetings[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = meeting.Id;
                worksheet.Cells[row, 2].Value = meeting.ScheduledAt.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = meeting.Report != null 
                    ? $"{meeting.Report.FirstName} {meeting.Report.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 4].Value = meeting.Status.ToString();
                worksheet.Cells[row, 5].Value = meeting.Description;
                worksheet.Cells[row, 6].Value = meeting.Tasks?.Count(t => !t.IsDeleted) ?? 0;
                worksheet.Cells[row, 7].Value = meeting.AgendaItems?.Count(a => !a.IsDeleted) ?? 0;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports tasks to an Excel file.
        /// </summary>
        public static void ExportTasks(List<TrackerTask> tasks, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Tasks");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Status";
            worksheet.Cells[1, 4].Value = "Due Date";
            worksheet.Cells[1, 5].Value = "Owner";
            worksheet.Cells[1, 6].Value = "Is Completed";
            worksheet.Cells[1, 7].Value = "Discussed In Meetings";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = task.Id;
                worksheet.Cells[row, 2].Value = task.Description;
                worksheet.Cells[row, 3].Value = task.Status.ToString();
                worksheet.Cells[row, 4].Value = task.DueDate.HasValue 
                    ? task.DueDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 5].Value = task.Owner != null 
                    ? $"{task.Owner.FirstName} {task.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 6].Value = task.IsCompleted ? "Yes" : "No";
                worksheet.Cells[row, 7].Value = task.MeetingCount;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports projects to an Excel file.
        /// </summary>
        public static void ExportProjects(List<Project> projects, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Projects");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Start Date";
            worksheet.Cells[1, 6].Value = "Target End Date";
            worksheet.Cells[1, 7].Value = "Progress";
            worksheet.Cells[1, 8].Value = "Owner";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = project.Id;
                worksheet.Cells[row, 2].Value = project.Name;
                worksheet.Cells[row, 3].Value = project.Description;
                worksheet.Cells[row, 4].Value = project.Status.ToString();
                worksheet.Cells[row, 5].Value = project.StartDate.HasValue 
                    ? project.StartDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = project.TargetEndDate?.ToString("yyyy-MM-dd") ?? "N/A";
                worksheet.Cells[row, 7].Value = project.ProgressPercent.ToString("F1") + "%";
                worksheet.Cells[row, 8].Value = project.Owner != null 
                    ? $"{project.Owner.FirstName} {project.Owner.LastName}".Trim() 
                    : "N/A";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports goals to an Excel file.
        /// </summary>
        public static void ExportGoals(List<Goal> goals, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("OKRs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Title";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Progress";
            worksheet.Cells[1, 6].Value = "Key Results";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < goals.Count; i++)
            {
                var goal = goals[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = goal.Id;
                worksheet.Cells[row, 2].Value = goal.Title;
                worksheet.Cells[row, 3].Value = goal.Description;
                worksheet.Cells[row, 4].Value = goal.Status.ToString();
                worksheet.Cells[row, 5].Value = goal.EffectiveProgress.ToString("F1") + "%";
                worksheet.Cells[row, 6].Value = goal.Targets?.Count ?? 0;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports metrics to an Excel file.
        /// </summary>
        public static void ExportMetrics(List<Metric> metrics, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("KPIs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Current Value";
            worksheet.Cells[1, 5].Value = "Target Value";
            worksheet.Cells[1, 6].Value = "Data Sources";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < metrics.Count; i++)
            {
                var metric = metrics[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = metric.Id;
                worksheet.Cells[row, 2].Value = metric.Name;
                worksheet.Cells[row, 3].Value = metric.Description;
                worksheet.Cells[row, 4].Value = metric.CurrentValue.ToString("F2");
                worksheet.Cells[row, 5].Value = metric.TargetValue?.ToString("F2") ?? "N/A";
                worksheet.Cells[row, 6].Value = metric.DataSources?.Count ?? 0;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports all data to a single Excel file with multiple worksheets.
        /// </summary>
        public static void ExportAllData(
            List<TeamMember> teamMembers,
            List<Meeting> meetings,
            List<TrackerTask> tasks,
            List<Project> projects,
            List<Goal> goals,
            List<Metric> metrics,
            string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var allPackage = new ExcelPackage();
            
            // Summary
            var summary = allPackage.Workbook.Worksheets.Add("Summary");
            summary.Cells[1, 1].Value = "Tracker Report";
            summary.Cells[1, 1].Style.Font.Bold = true;
            summary.Cells[1, 1].Style.Font.Size = 16;
            summary.Cells[3, 1].Value = "Generated:";
            summary.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            summary.Cells[5, 1].Value = "Team Members:";
            summary.Cells[5, 2].Value = teamMembers.Count;
            summary.Cells[6, 1].Value = "Meetings:";
            summary.Cells[6, 2].Value = meetings.Count;
            summary.Cells[7, 1].Value = "Tasks:";
            summary.Cells[7, 2].Value = tasks.Count;
            summary.Cells[8, 1].Value = "Projects:";
            summary.Cells[8, 2].Value = projects.Count;
            summary.Cells[9, 1].Value = "Goals:";
            summary.Cells[9, 2].Value = goals.Count;
            summary.Cells[10, 1].Value = "Metrics:";
            summary.Cells[10, 2].Value = metrics.Count;
            summary.Cells.AutoFitColumns();

            // Add data sheets
            AddTeamMembersSheet(allPackage, teamMembers);
            AddMeetingsSheet(allPackage, meetings);
            AddTasksSheet(allPackage, tasks);
            AddProjectsSheet(allPackage, projects);

            allPackage.SaveAs(new FileInfo(filePath));
        }

        private static void AddMeetingsSheet(ExcelPackage package, List<Meeting> meetings)
        {
            var worksheet = package.Workbook.Worksheets.Add("Meetings");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Date";
            worksheet.Cells[1, 3].Value = "Team Member";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Description";

            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < meetings.Count; i++)
            {
                var meeting = meetings[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = meeting.Id;
                worksheet.Cells[row, 2].Value = meeting.ScheduledAt.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = meeting.Report != null 
                    ? $"{meeting.Report.FirstName} {meeting.Report.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 4].Value = meeting.Status.ToString();
                worksheet.Cells[row, 5].Value = meeting.Description;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddTeamMembersSheet(ExcelPackage package, List<TeamMember> teamMembers)
        {
            var worksheet = package.Workbook.Worksheets.Add("Team Members");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "First Name";
            worksheet.Cells[1, 3].Value = "Last Name";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Role";
            worksheet.Cells[1, 6].Value = "Specialty";
            worksheet.Cells[1, 7].Value = "LinkedIn Profile";
            worksheet.Cells[1, 8].Value = "Created Date";

            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < teamMembers.Count; i++)
            {
                var member = teamMembers[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = member.Id;
                worksheet.Cells[row, 2].Value = member.FirstName;
                worksheet.Cells[row, 3].Value = member.LastName;
                worksheet.Cells[row, 4].Value = member.Email;
                worksheet.Cells[row, 5].Value = member.Role;
                worksheet.Cells[row, 6].Value = member.Specialty;
                worksheet.Cells[row, 7].Value = member.LinkedInUrl;
                worksheet.Cells[row, 8].Value = member.CreatedAt.ToString("yyyy-MM-dd");
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddTasksSheet(ExcelPackage package, List<TrackerTask> tasks)
        {
            var worksheet = package.Workbook.Worksheets.Add("Tasks");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Status";
            worksheet.Cells[1, 4].Value = "Due Date";
            worksheet.Cells[1, 5].Value = "Owner";
            worksheet.Cells[1, 6].Value = "Is Completed";

            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = task.Id;
                worksheet.Cells[row, 2].Value = task.Description;
                worksheet.Cells[row, 3].Value = task.Status.ToString();
                worksheet.Cells[row, 4].Value = task.DueDate.HasValue 
                    ? task.DueDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 5].Value = task.Owner != null 
                    ? $"{task.Owner.FirstName} {task.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 6].Value = task.IsCompleted ? "Yes" : "No";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddProjectsSheet(ExcelPackage package, List<Project> projects)
        {
            var worksheet = package.Workbook.Worksheets.Add("Projects");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Start Date";
            worksheet.Cells[1, 6].Value = "Target End Date";
            worksheet.Cells[1, 7].Value = "Progress";
            worksheet.Cells[1, 8].Value = "Owner";

            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = project.Id;
                worksheet.Cells[row, 2].Value = project.Name;
                worksheet.Cells[row, 3].Value = project.Description;
                worksheet.Cells[row, 4].Value = project.Status.ToString();
                worksheet.Cells[row, 5].Value = project.StartDate.HasValue 
                    ? project.StartDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = project.TargetEndDate?.ToString("yyyy-MM-dd") ?? "N/A";
                worksheet.Cells[row, 7].Value = project.ProgressPercent.ToString("F1") + "%";
                worksheet.Cells[row, 8].Value = project.Owner != null 
                    ? $"{project.Owner.FirstName} {project.Owner.LastName}".Trim() 
                    : "N/A";
            }
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }
}

