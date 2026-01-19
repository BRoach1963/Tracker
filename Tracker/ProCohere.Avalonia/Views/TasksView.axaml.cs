using Avalonia.Controls;
using Avalonia.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class TasksView : UserControl
{
    public TasksView()
    {
        InitializeComponent();
        DataContext = new ViewModels.TasksViewModel();
    }
    
    /// <summary>
    /// Handles task item tap to open the detail flyout.
    /// </summary>
    private void TaskItem_Tapped(object? sender, TappedEventArgs e)
    {
        // Prevent checkbox taps from triggering item selection
        if (e.Source is CheckBox)
            return;
            
        if (sender is Border { DataContext: TaskDetail task } && DataContext is TasksViewModel viewModel)
        {
            // Execute the SelectTask command
            if (viewModel.SelectTaskCommand.CanExecute(task))
            {
                viewModel.SelectTaskCommand.Execute(task);
            }
        }
    }
}
