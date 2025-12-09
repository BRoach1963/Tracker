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
                worksheet.Cells[row, 7].Value = member.LinkedInProfile;
                worksheet.Cells[row, 8].Value = member.CreatedAt.ToString("yyyy-MM-dd");
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports 1:1 meetings to an Excel file.
        /// </summary>
        public static void ExportOneOnOnes(List<OneOnOne> oneOnOnes, string filePath)
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
            worksheet.Cells[1, 6].Value = "Action Items";
            worksheet.Cells[1, 7].Value = "Follow-up Items";
            worksheet.Cells[1, 8].Value = "Linked Tasks";
            worksheet.Cells[1, 9].Value = "Linked OKRs";
            worksheet.Cells[1, 10].Value = "Linked KPIs";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < oneOnOnes.Count; i++)
            {
                var meeting = oneOnOnes[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = meeting.Id;
                worksheet.Cells[row, 2].Value = meeting.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = meeting.TeamMember != null 
                    ? $"{meeting.TeamMember.FirstName} {meeting.TeamMember.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 4].Value = meeting.Status.ToString();
                worksheet.Cells[row, 5].Value = meeting.Description;
                worksheet.Cells[row, 6].Value = meeting.ActionItems?.Count(a => !a.IsDeleted) ?? 0;
                worksheet.Cells[row, 7].Value = meeting.FollowUpItems?.Count(f => !f.IsDeleted) ?? 0;
                worksheet.Cells[row, 8].Value = meeting.LinkedTasks?.Count(lt => !lt.IsDeleted) ?? 0;
                worksheet.Cells[row, 9].Value = meeting.LinkedOkrs?.Count(lo => !lo.IsDeleted) ?? 0;
                worksheet.Cells[row, 10].Value = meeting.LinkedKpis?.Count(lk => !lk.IsDeleted) ?? 0;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports tasks to an Excel file.
        /// </summary>
        public static void ExportTasks(List<IndividualTask> tasks, string filePath)
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
                worksheet.Cells[row, 4].Value = task.DueDate != default(DateTime) 
                    ? task.DueDate.ToString("yyyy-MM-dd") 
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
            worksheet.Cells[1, 6].Value = "End Date";
            worksheet.Cells[1, 7].Value = "Budget";
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
                worksheet.Cells[row, 1].Value = project.ID;
                worksheet.Cells[row, 2].Value = project.Name;
                worksheet.Cells[row, 3].Value = project.Description;
                worksheet.Cells[row, 4].Value = project.Status.ToString();
                worksheet.Cells[row, 5].Value = project.StartDate != default(DateTime) 
                    ? project.StartDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = project.EndDate?.ToString("yyyy-MM-dd") ?? "N/A";
                worksheet.Cells[row, 7].Value = project.Budget != decimal.MinValue 
                    ? project.Budget.ToString("C") 
                    : "N/A";
                worksheet.Cells[row, 8].Value = project.Owner != null 
                    ? $"{project.Owner.FirstName} {project.Owner.LastName}".Trim() 
                    : "N/A";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports OKRs to an Excel file.
        /// </summary>
        public static void ExportOKRs(List<ObjectiveKeyResult> okrs, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("OKRs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Title";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Start Date";
            worksheet.Cells[1, 6].Value = "End Date";
            worksheet.Cells[1, 7].Value = "Completion %";
            worksheet.Cells[1, 8].Value = "Owner";
            worksheet.Cells[1, 9].Value = "Discussed In Meetings";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < okrs.Count; i++)
            {
                var okr = okrs[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = okr.ObjectiveId;
                worksheet.Cells[row, 2].Value = okr.Title;
                worksheet.Cells[row, 3].Value = okr.Description;
                worksheet.Cells[row, 4].Value = okr.Status.ToString();
                worksheet.Cells[row, 5].Value = okr.StartDate != default(DateTime) 
                    ? okr.StartDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = okr.EndDate != default(DateTime) 
                    ? okr.EndDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 7].Value = okr.CompletionPercentage;
                worksheet.Cells[row, 8].Value = okr.Owner != null 
                    ? $"{okr.Owner.FirstName} {okr.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 9].Value = okr.MeetingCount;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports KPIs to an Excel file.
        /// </summary>
        public static void ExportKPIs(List<KeyPerformanceIndicator> kpis, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("KPIs");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Value";
            worksheet.Cells[1, 5].Value = "Target Value";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "Last Updated";
            worksheet.Cells[1, 8].Value = "Owner";
            worksheet.Cells[1, 9].Value = "Discussed In Meetings";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data
            for (int i = 0; i < kpis.Count; i++)
            {
                var kpi = kpis[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = kpi.KpiId;
                worksheet.Cells[row, 2].Value = kpi.Name;
                worksheet.Cells[row, 3].Value = kpi.Description;
                worksheet.Cells[row, 4].Value = kpi.Value;
                worksheet.Cells[row, 5].Value = kpi.TargetValue;
                worksheet.Cells[row, 6].Value = kpi.Status.ToString();
                worksheet.Cells[row, 7].Value = kpi.LastUpdated != default(DateTime) 
                    ? kpi.LastUpdated.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 8].Value = kpi.Owner != null 
                    ? $"{kpi.Owner.FirstName} {kpi.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 9].Value = kpi.MeetingCount;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        /// <summary>
        /// Exports all data to a single Excel file with multiple worksheets.
        /// </summary>
        public static void ExportAllData(
            List<TeamMember> teamMembers,
            List<OneOnOne> oneOnOnes,
            List<IndividualTask> tasks,
            List<Project> projects,
            List<ObjectiveKeyResult> okrs,
            List<KeyPerformanceIndicator> kpis,
            string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            
            // Create summary sheet
            var summarySheet = package.Workbook.Worksheets.Add("Summary");
            summarySheet.Cells[1, 1].Value = "Tracker Report";
            summarySheet.Cells[1, 1].Style.Font.Bold = true;
            summarySheet.Cells[1, 1].Style.Font.Size = 16;
            summarySheet.Cells[3, 1].Value = "Generated:";
            summarySheet.Cells[3, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            summarySheet.Cells[5, 1].Value = "Team Members:";
            summarySheet.Cells[5, 2].Value = teamMembers.Count;
            summarySheet.Cells[6, 1].Value = "1:1 Meetings:";
            summarySheet.Cells[6, 2].Value = oneOnOnes.Count;
            summarySheet.Cells[7, 1].Value = "Tasks:";
            summarySheet.Cells[7, 2].Value = tasks.Count;
            summarySheet.Cells[8, 1].Value = "Projects:";
            summarySheet.Cells[8, 2].Value = projects.Count;
            summarySheet.Cells[9, 1].Value = "OKRs:";
            summarySheet.Cells[9, 2].Value = okrs.Count;
            summarySheet.Cells[10, 1].Value = "KPIs:";
            summarySheet.Cells[10, 2].Value = kpis.Count;
            summarySheet.Cells.AutoFitColumns();

            // Export each data type to separate sheets
            ExportTeamMembers(teamMembers, filePath); // This creates a new file, so we'll recreate
            ExportOneOnOnes(oneOnOnes, filePath);
            ExportTasks(tasks, filePath);
            ExportProjects(projects, filePath);
            ExportOKRs(okrs, filePath);
            ExportKPIs(kpis, filePath);

            // Actually, let's do it properly - add all sheets to one package
            package.Dispose();
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
            summary.Cells[6, 1].Value = "1:1 Meetings:";
            summary.Cells[6, 2].Value = oneOnOnes.Count;
            summary.Cells[7, 1].Value = "Tasks:";
            summary.Cells[7, 2].Value = tasks.Count;
            summary.Cells[8, 1].Value = "Projects:";
            summary.Cells[8, 2].Value = projects.Count;
            summary.Cells[9, 1].Value = "OKRs:";
            summary.Cells[9, 2].Value = okrs.Count;
            summary.Cells[10, 1].Value = "KPIs:";
            summary.Cells[10, 2].Value = kpis.Count;
            summary.Cells.AutoFitColumns();

            // Add data sheets using helper methods
            AddTeamMembersSheet(allPackage, teamMembers);
            AddOneOnOnesSheet(allPackage, oneOnOnes);
            AddTasksSheet(allPackage, tasks);
            AddProjectsSheet(allPackage, projects);
            AddOKRsSheet(allPackage, okrs);
            AddKPIsSheet(allPackage, kpis);

            allPackage.SaveAs(new FileInfo(filePath));
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
                worksheet.Cells[row, 7].Value = member.LinkedInProfile;
                worksheet.Cells[row, 8].Value = member.CreatedAt.ToString("yyyy-MM-dd");
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddOneOnOnesSheet(ExcelPackage package, List<OneOnOne> oneOnOnes)
        {
            var worksheet = package.Workbook.Worksheets.Add("1:1 Meetings");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Date";
            worksheet.Cells[1, 3].Value = "Team Member";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Description";
            worksheet.Cells[1, 6].Value = "Action Items";
            worksheet.Cells[1, 7].Value = "Follow-up Items";
            worksheet.Cells[1, 8].Value = "Linked Tasks";
            worksheet.Cells[1, 9].Value = "Linked OKRs";
            worksheet.Cells[1, 10].Value = "Linked KPIs";

            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < oneOnOnes.Count; i++)
            {
                var meeting = oneOnOnes[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = meeting.Id;
                worksheet.Cells[row, 2].Value = meeting.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = meeting.TeamMember != null 
                    ? $"{meeting.TeamMember.FirstName} {meeting.TeamMember.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 4].Value = meeting.Status.ToString();
                worksheet.Cells[row, 5].Value = meeting.Description;
                worksheet.Cells[row, 6].Value = meeting.ActionItems?.Count(a => !a.IsDeleted) ?? 0;
                worksheet.Cells[row, 7].Value = meeting.FollowUpItems?.Count(f => !f.IsDeleted) ?? 0;
                worksheet.Cells[row, 8].Value = meeting.LinkedTasks?.Count(lt => !lt.IsDeleted) ?? 0;
                worksheet.Cells[row, 9].Value = meeting.LinkedOkrs?.Count(lo => !lo.IsDeleted) ?? 0;
                worksheet.Cells[row, 10].Value = meeting.LinkedKpis?.Count(lk => !lk.IsDeleted) ?? 0;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddTasksSheet(ExcelPackage package, List<IndividualTask> tasks)
        {
            var worksheet = package.Workbook.Worksheets.Add("Tasks");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "Status";
            worksheet.Cells[1, 4].Value = "Due Date";
            worksheet.Cells[1, 5].Value = "Owner";
            worksheet.Cells[1, 6].Value = "Is Completed";
            worksheet.Cells[1, 7].Value = "Discussed In Meetings";

            using (var range = worksheet.Cells[1, 1, 1, 7])
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
                worksheet.Cells[row, 4].Value = task.DueDate != default(DateTime) 
                    ? task.DueDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 5].Value = task.Owner != null 
                    ? $"{task.Owner.FirstName} {task.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 6].Value = task.IsCompleted ? "Yes" : "No";
                worksheet.Cells[row, 7].Value = task.MeetingCount;
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
            worksheet.Cells[1, 6].Value = "End Date";
            worksheet.Cells[1, 7].Value = "Budget";
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
                worksheet.Cells[row, 1].Value = project.ID;
                worksheet.Cells[row, 2].Value = project.Name;
                worksheet.Cells[row, 3].Value = project.Description;
                worksheet.Cells[row, 4].Value = project.Status.ToString();
                worksheet.Cells[row, 5].Value = project.StartDate != default(DateTime) 
                    ? project.StartDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = project.EndDate?.ToString("yyyy-MM-dd") ?? "N/A";
                worksheet.Cells[row, 7].Value = project.Budget != decimal.MinValue 
                    ? project.Budget.ToString("C") 
                    : "N/A";
                worksheet.Cells[row, 8].Value = project.Owner != null 
                    ? $"{project.Owner.FirstName} {project.Owner.LastName}".Trim() 
                    : "N/A";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddOKRsSheet(ExcelPackage package, List<ObjectiveKeyResult> okrs)
        {
            var worksheet = package.Workbook.Worksheets.Add("OKRs");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Title";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Start Date";
            worksheet.Cells[1, 6].Value = "End Date";
            worksheet.Cells[1, 7].Value = "Completion %";
            worksheet.Cells[1, 8].Value = "Owner";
            worksheet.Cells[1, 9].Value = "Discussed In Meetings";

            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < okrs.Count; i++)
            {
                var okr = okrs[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = okr.ObjectiveId;
                worksheet.Cells[row, 2].Value = okr.Title;
                worksheet.Cells[row, 3].Value = okr.Description;
                worksheet.Cells[row, 4].Value = okr.Status.ToString();
                worksheet.Cells[row, 5].Value = okr.StartDate != default(DateTime) 
                    ? okr.StartDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 6].Value = okr.EndDate != default(DateTime) 
                    ? okr.EndDate.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 7].Value = okr.CompletionPercentage;
                worksheet.Cells[row, 8].Value = okr.Owner != null 
                    ? $"{okr.Owner.FirstName} {okr.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 9].Value = okr.MeetingCount;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private static void AddKPIsSheet(ExcelPackage package, List<KeyPerformanceIndicator> kpis)
        {
            var worksheet = package.Workbook.Worksheets.Add("KPIs");
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Value";
            worksheet.Cells[1, 5].Value = "Target Value";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "Last Updated";
            worksheet.Cells[1, 8].Value = "Owner";
            worksheet.Cells[1, 9].Value = "Discussed In Meetings";

            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < kpis.Count; i++)
            {
                var kpi = kpis[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = kpi.KpiId;
                worksheet.Cells[row, 2].Value = kpi.Name;
                worksheet.Cells[row, 3].Value = kpi.Description;
                worksheet.Cells[row, 4].Value = kpi.Value;
                worksheet.Cells[row, 5].Value = kpi.TargetValue;
                worksheet.Cells[row, 6].Value = kpi.Status.ToString();
                worksheet.Cells[row, 7].Value = kpi.LastUpdated != default(DateTime) 
                    ? kpi.LastUpdated.ToString("yyyy-MM-dd") 
                    : "N/A";
                worksheet.Cells[row, 8].Value = kpi.Owner != null 
                    ? $"{kpi.Owner.FirstName} {kpi.Owner.LastName}".Trim() 
                    : "N/A";
                worksheet.Cells[row, 9].Value = kpi.MeetingCount;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }
}

