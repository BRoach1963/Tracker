using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views.Pulse;

public partial class MetricsTabView : UserControl
{
    private MetricsViewModel? _viewModel;
    private MetricsViewModel? ViewModel => DataContext as MetricsViewModel;
    
    public MetricsTabView()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to wire up events
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel
        if (_viewModel != null)
        {
            _viewModel.CreateMetricDialogRequested -= OnCreateMetricDialogRequested;
            _viewModel.EditMetricDialogRequested -= OnEditMetricDialogRequested;
            _viewModel.UpdateValueDialogRequested -= OnUpdateValueDialogRequested;
        }
        
        // Subscribe to new ViewModel
        _viewModel = ViewModel;
        if (_viewModel != null)
        {
            _viewModel.CreateMetricDialogRequested += OnCreateMetricDialogRequested;
            _viewModel.EditMetricDialogRequested += OnEditMetricDialogRequested;
            _viewModel.UpdateValueDialogRequested += OnUpdateValueDialogRequested;
        }
    }

    #region Dialog Event Handlers

    private async void OnCreateMetricDialogRequested(object? sender, EventArgs e)
    {
        var result = await AppDialogService.ShowCreateMetricAsync();
        if (result != null && _viewModel != null)
        {
            await _viewModel.OnMetricSavedAsync(result);
        }
    }

    private async void OnEditMetricDialogRequested(object? sender, MetricDetail metric)
    {
        var result = await AppDialogService.ShowEditMetricAsync(metric);
        if (result != null && _viewModel != null)
        {
            await _viewModel.OnMetricSavedAsync(result);
        }
    }

    private async void OnUpdateValueDialogRequested(object? sender, MetricDetail metric)
    {
        var result = await AppDialogService.ShowUpdateMetricValueAsync(metric);
        if (result != null && _viewModel != null)
        {
            await _viewModel.UpdateMetricValueAsync(metric.Id, result.NewValue, result.WhatChanged);
        }
    }

    #endregion

    #region Filter Handlers

    private void FilterAll_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetLifecycleFilterCommand.Execute(null);
    }

    private void FilterActive_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetLifecycleFilterCommand.Execute("active");
    }

    private void FilterTrendingUp_Tapped(object? sender, TappedEventArgs e)
    {
        // TODO: Add trending filter to ViewModel if needed
        // For now, this is informational only
    }

    private void FilterTrendingDown_Tapped(object? sender, TappedEventArgs e)
    {
        // TODO: Add trending filter to ViewModel if needed
        // For now, this is informational only
    }

    #endregion

    #region Scope Handlers

    private void ScopeIndividual_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetScopeCommand.Execute("0");
    }

    private void ScopeTeam_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetScopeCommand.Execute("1");
    }

    private void ScopeOrg_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetScopeCommand.Execute("2");
    }

    private void ScopeAll_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetScopeCommand.Execute("3");
    }

    #endregion

    #region Tab Handlers

    private void TabDetails_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetDetailTabCommand.Execute("0");
    }

    private void TabHistory_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetDetailTabCommand.Execute("1");
    }

    private void TabTrend_Tapped(object? sender, TappedEventArgs e)
    {
        ViewModel?.SetDetailTabCommand.Execute("2");
    }

    #endregion

    #region Card Selection

    private void MetricCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is MetricDetail metric)
        {
            ViewModel?.SelectMetricCommand.Execute(metric);
        }
    }

    #endregion
}
