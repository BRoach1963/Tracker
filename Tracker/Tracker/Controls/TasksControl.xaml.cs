using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Interfaces;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class TasksControl : UserControl
    {
        public TasksControl()
        {
            InitializeComponent();
        }

        #region Stat Card Click Handlers

        private void StatCard_Open_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (vm.TaskStatusFilter == "Open")
                    vm.TaskStatusFilter = null;
                else
                    vm.TaskStatusFilter = "Open";
            }
        }

        private void StatCard_Overdue_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (vm.TaskStatusFilter == "Overdue")
                    vm.TaskStatusFilter = null;
                else
                    vm.TaskStatusFilter = "Overdue";
            }
        }

        private void StatCard_Completed_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (vm.TaskStatusFilter == "Completed")
                    vm.TaskStatusFilter = null;
                else
                    vm.TaskStatusFilter = "Completed";
            }
        }

        private void StatCard_All_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.TaskStatusFilter = null;
            }
        }

        #endregion

        #region Filter Button Click Handlers

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.TaskStatusFilter = null;
            }
        }

        private void FilterOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.TaskStatusFilter = "Open";
            }
        }

        private void FilterOverdue_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.TaskStatusFilter = "Overdue";
            }
        }

        private void FilterCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.TaskStatusFilter = "Completed";
            }
        }

        #endregion

        #region Task Selection

        /// <summary>
        /// Handle mouse down on a task card - single click selects, double click edits.
        /// </summary>
        private void Task_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ITask task)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedTask = task;
                    
                    // Double-click opens edit dialog
                    if (e.ClickCount == 2)
                    {
                        vm.EditTaskCommand?.Execute(task);
                        e.Handled = true;
                    }
                }
            }
        }

        private void Task_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is ITask task)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedTask = task;
                }
            }
        }

        private void TasksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm && vm.SelectedTask != null)
            {
                vm.EditTaskCommand?.Execute(vm.SelectedTask);
            }
        }

        #endregion
    }
}
