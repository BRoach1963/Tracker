using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Tracker.Command;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.Managers;
using Tracker.Services;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the Reports dialog, handling Excel exports and data visualization.
    /// </summary>
    public class ReportsViewModel : BaseDialogViewModel
    {
        #region Fields

        private ICommand? _exportTeamMembersCommand;
        private ICommand? _exportOneOnOnesCommand;
        private ICommand? _exportTasksCommand;
        private ICommand? _exportProjectsCommand;
        private ICommand? _exportOKRsCommand;
        private ICommand? _exportKPIsCommand;
        private ICommand? _exportAllCommand;
        private bool _isExporting;

        #endregion

        #region Ctor

        public ReportsViewModel(Action? callback) : base(callback)
        {
        }

        #endregion

        #region Commands

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

        public ICommand ExportAllCommand => _exportAllCommand ??= 
            new TrackerCommand(ExecuteExportAll, CanExport);

        #endregion

        #region Properties

        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                _isExporting = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private bool CanExport(object? parameter)
        {
            return !IsExporting;
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

        private async void ExecuteExportTeamMembers(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var teamMembers = await TrackerDataManager.Instance.GetTeamData();
                var filePath = GetSaveFilePath("TeamMembers");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportTeamMembers(teamMembers, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Team members exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportOneOnOnes(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes();
                var filePath = GetSaveFilePath("OneOnOnes");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportOneOnOnes(oneOnOnes, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"1:1 meetings exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportTasks(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var tasks = await TrackerDataManager.Instance.GetTasks();
                var filePath = GetSaveFilePath("Tasks");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportTasks(tasks, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Tasks exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportProjects(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var projects = await TrackerDataManager.Instance.GetProjects();
                var filePath = GetSaveFilePath("Projects");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportProjects(projects, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Projects exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportOKRs(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                var filePath = GetSaveFilePath("OKRs");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportOKRs(okrs, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"OKRs exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportKPIs(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var kpis = await TrackerDataManager.Instance.GetKPIs();
                var filePath = GetSaveFilePath("KPIs");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportKPIs(kpis, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"KPIs exported to {Path.GetFileName(filePath)}");
                }
            });
        }

        private async void ExecuteExportAll(object? parameter)
        {
            await ExportAsync(async () =>
            {
                var teamMembers = await TrackerDataManager.Instance.GetTeamData();
                var oneOnOnes = await TrackerDataManager.Instance.GetOneOnOnes();
                var tasks = await TrackerDataManager.Instance.GetTasks();
                var projects = await TrackerDataManager.Instance.GetProjects();
                var okrs = await TrackerDataManager.Instance.GetOKRs();
                var kpis = await TrackerDataManager.Instance.GetKPIs();

                var filePath = GetSaveFilePath("TrackerReport");
                if (!string.IsNullOrEmpty(filePath))
                {
                    ExcelExportService.ExportAllData(teamMembers, oneOnOnes, tasks, projects, okrs, kpis, filePath);
                    NotificationManager.Instance.ShowSuccess("Export Complete", $"Complete report exported to {Path.GetFileName(filePath)}");
                }
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
}

