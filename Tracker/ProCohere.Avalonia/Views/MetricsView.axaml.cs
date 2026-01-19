using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Metrics view - displays metrics with signals-not-targets philosophy.
/// </summary>
public partial class MetricsView : UserControl
{
    public MetricsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle metric row click to select the metric.
    /// </summary>
    private void OnMetricRowPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is MetricDetail metric)
        {
            if (DataContext is MetricsViewModel vm)
            {
                vm.SelectMetricCommand.Execute(metric);
            }
        }
    }
}
