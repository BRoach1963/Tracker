using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// UserControl for displaying goal details in a flyout panel with vertical tabs.
/// Shows Overview, Targets, and Tasks tabs.
/// </summary>
public partial class GoalDetailFlyout : UserControl
{
    public GoalDetailFlyout()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles click on a linked metric to navigate to the Metrics tab.
    /// </summary>
    private void LinkedMetric_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is MetricDetail metric)
        {
            // Get the CircleViewModel from parent hierarchy
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent.DataContext is CircleViewModel vm)
                {
                    vm.NavigateToMetricCommand.Execute(metric);
                    e.Handled = true;
                    return;
                }
                parent = (parent as Control)?.Parent;
            }
        }
    }
}
