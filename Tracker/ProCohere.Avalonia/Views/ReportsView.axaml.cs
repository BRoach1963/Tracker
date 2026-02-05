using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Code-behind for ReportsView - handles tab click events and export file picker.
/// All state and logic lives in ReportsViewModel (MVVM).
/// </summary>
public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    #region Tab Click Handlers

    private void TabOverview_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 0;
    }

    private void TabGoals_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 1;
    }

    private void TabMetrics_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 2;
    }

    private void TabTasks_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 3;
    }

    private void TabMeetings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 4;
    }

    private void TabTeam_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            vm.SelectedReportIndex = 5;
    }

    #endregion

    #region Export Handlers

    private async void ExportCurrentTab_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filePath = await ShowSaveExcelFilePickerAsync(vm.GetExportFilename());
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportCurrentTabAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "Export Complete",
                    $"Data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] Export error: {ex.Message}");
        }
    }

    private async void ExportAllData_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filename = $"ProCohere_AllData_{DateTime.Now:yyyyMMdd}.xlsx";
            var filePath = await ShowSaveExcelFilePickerAsync(filename);
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportAllDataAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "Export Complete",
                    $"All data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] Export error: {ex.Message}");
        }
    }

    private async void ExportCurrentTabToPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filePath = await ShowSavePdfFilePickerAsync(vm.GetPdfExportFilename());
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportCurrentTabToPdfAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "PDF Export Complete",
                    $"Data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "PDF Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] PDF export error: {ex.Message}");
        }
    }

    private async void ExportAllDataToPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filename = $"ProCohere_AllData_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = await ShowSavePdfFilePickerAsync(filename);
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportAllDataToPdfAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "PDF Export Complete",
                    $"All data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "PDF Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] PDF export error: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task<string?> ShowSaveExcelFilePickerAsync(string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to Excel",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ".xlsx",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Excel Files") { Patterns = new[] { "*.xlsx" } }
            }
        });

        return file?.Path.LocalPath;
    }

    private async System.Threading.Tasks.Task<string?> ShowSavePdfFilePickerAsync(string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to PDF",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ".pdf",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("PDF Files") { Patterns = new[] { "*.pdf" } }
            }
        });

        return file?.Path.LocalPath;
    }

    private async void ExportCurrentTabToCsv_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filename = vm.GetCsvExportFilename();
            // For current tab (non-Overview), always use .csv
            if (vm.SelectedReportIndex != 0 && filename.EndsWith(".zip"))
            {
                filename = filename.Replace(".zip", ".csv");
            }

            var filePath = await ShowSaveCsvFilePickerAsync(filename);
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportCurrentTabToCsvAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "CSV Export Complete",
                    $"Data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "CSV Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] CSV export error: {ex.Message}");
        }
    }

    private async void ExportAllDataToCsv_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportsViewModel vm) return;

        try
        {
            var filename = $"ProCohere_AllData_{DateTime.Now:yyyyMMdd}.zip";
            var filePath = await ShowSaveZipFilePickerAsync(filename);
            if (string.IsNullOrEmpty(filePath)) return;

            var success = await vm.ExportAllDataToCsvAsync(filePath);
            if (success)
            {
                NotificationService.Instance.ShowSuccess(
                    "CSV Export Complete",
                    $"All data exported to {System.IO.Path.GetFileName(filePath)}");
            }
            else
            {
                NotificationService.Instance.ShowError(
                    "CSV Export Failed",
                    vm.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReportsView] CSV export error: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task<string?> ShowSaveCsvFilePickerAsync(string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to CSV",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ".csv",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("CSV Files") { Patterns = new[] { "*.csv" } }
            }
        });

        return file?.Path.LocalPath;
    }

    private async System.Threading.Tasks.Task<string?> ShowSaveZipFilePickerAsync(string suggestedFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export to ZIP",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = ".zip",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("ZIP Archives") { Patterns = new[] { "*.zip" } }
            }
        });

        return file?.Path.LocalPath;
    }

    #endregion
}
