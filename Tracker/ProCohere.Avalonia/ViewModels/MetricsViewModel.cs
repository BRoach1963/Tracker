using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Metrics sub-tab within Pulse.
/// Implements signals-not-targets philosophy for metric observation.
/// 
/// Philosophy: "Metrics are signals that tell a story, NOT targets to chase."
/// NO progress bars, percentages, or red/yellow/green status indicators.
/// </summary>
public partial class MetricsViewModel : ViewModelBase
{
    #region Loading State

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Scope Filter

    /// <summary>
    /// Metric scope filter: 0=My Metrics, 1=Team Metrics, 2=All Metrics
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScopeMyMetrics))]
    [NotifyPropertyChangedFor(nameof(IsScopeTeamMetrics))]
    [NotifyPropertyChangedFor(nameof(IsScopeAllMetrics))]
    private int _selectedScope = 0;

    public bool IsScopeMyMetrics => SelectedScope == 0;
    public bool IsScopeTeamMetrics => SelectedScope == 1;
    public bool IsScopeAllMetrics => SelectedScope == 2;

    [RelayCommand]
    private async Task SetScope(string scopeIndex)
    {
        if (int.TryParse(scopeIndex, out var index))
        {
            SelectedScope = index;
            await LoadMetricsAsync();
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// Active metrics for display.
    /// </summary>
    public ObservableCollection<MetricDetail> Metrics { get; } = new();

    #endregion

    #region Selection State

    [ObservableProperty]
    private MetricDetail? _selectedMetric;

    [ObservableProperty]
    private bool _isDetailFlyoutOpen;

    [ObservableProperty]
    private bool _isEditorFlyoutOpen;

    [ObservableProperty]
    private MetricDetail? _editingMetric;

    #endregion

    #region Stats

    [ObservableProperty]
    private int _activeMetricsCount;

    [ObservableProperty]
    private int _trendingUpCount;

    [ObservableProperty]
    private int _trendingDownCount;

    #endregion

    public MetricsViewModel()
    {
        // Load metrics on initialization
        _ = LoadMetricsAsync();
    }

    #region Commands

    [RelayCommand]
    private async Task LoadMetricsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // TODO: Implement actual Supabase query via MetricsService
            // For now, clear and show empty state
            Metrics.Clear();
            
            // Placeholder stats
            ActiveMetricsCount = 0;
            TrendingUpCount = 0;
            TrendingDownCount = 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load metrics: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectMetric(MetricDetail metric)
    {
        SelectedMetric = metric;
        IsDetailFlyoutOpen = true;
        IsEditorFlyoutOpen = false;
    }

    [RelayCommand]
    private void CloseDetailFlyout()
    {
        IsDetailFlyoutOpen = false;
        SelectedMetric = null;
    }

    [RelayCommand]
    private void CreateNewMetric()
    {
        EditingMetric = new MetricDetail
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Description = string.Empty,
            Lifecycle = "active"
        };
        IsEditorFlyoutOpen = true;
        IsDetailFlyoutOpen = false;
    }

    [RelayCommand]
    private void EditMetric(MetricDetail metric)
    {
        EditingMetric = metric;
        IsEditorFlyoutOpen = true;
        IsDetailFlyoutOpen = false;
    }

    [RelayCommand]
    private void CloseEditorFlyout()
    {
        IsEditorFlyoutOpen = false;
        EditingMetric = null;
    }

    [RelayCommand]
    private async Task SaveMetricAsync()
    {
        if (EditingMetric == null) return;

        try
        {
            IsLoading = true;

            // TODO: Implement actual save via MetricsService
            // if (EditingMetric.Id == Guid.Empty)
            //     await MetricsService.CreateMetricAsync(EditingMetric);
            // else
            //     await MetricsService.UpdateMetricAsync(EditingMetric);

            CloseEditorFlyout();
            await LoadMetricsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save metric: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteMetricAsync(MetricDetail metric)
    {
        try
        {
            // TODO: Implement actual delete via MetricsService
            // await MetricsService.DeleteMetricAsync(metric.Id);

            Metrics.Remove(metric);
            if (SelectedMetric?.Id == metric.Id)
            {
                CloseDetailFlyout();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete metric: {ex.Message}";
        }
    }

    #endregion
}
