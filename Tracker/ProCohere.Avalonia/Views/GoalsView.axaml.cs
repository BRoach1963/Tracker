using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Goals view - displays goals with narrative-first philosophy.
/// </summary>
public partial class GoalsView : UserControl
{
    public GoalsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle goal card click to select the goal.
    /// </summary>
    private void OnGoalCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is GoalDetail goal)
        {
            if (DataContext is GoalsViewModel vm)
            {
                vm.SelectGoalCommand.Execute(goal);
            }
        }
    }
}
