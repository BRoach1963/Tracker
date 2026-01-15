using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class ProjectsControl : UserControl
    {
        public ProjectsControl()
        {
            InitializeComponent();
        }

        #region Stat Card Click Handlers

        private void StatCard_Active_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                // Toggle: if already selected, go back to All
                if (vm.ProjectStatusFilter == "Active")
                    vm.ProjectStatusFilter = null;
                else
                    vm.ProjectStatusFilter = "Active";
            }
        }

        private void StatCard_Completed_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (vm.ProjectStatusFilter == "Completed")
                    vm.ProjectStatusFilter = null;
                else
                    vm.ProjectStatusFilter = "Completed";
            }
        }

        private void StatCard_AtRisk_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                if (vm.ProjectStatusFilter == "AtRisk")
                    vm.ProjectStatusFilter = null;
                else
                    vm.ProjectStatusFilter = "AtRisk";
            }
        }

        private void StatCard_All_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.ProjectStatusFilter = null;
            }
        }

        #endregion

        #region Filter Button Click Handlers

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.ProjectStatusFilter = null;
            }
        }

        private void FilterActive_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.ProjectStatusFilter = "Active";
            }
        }

        private void FilterCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm)
            {
                vm.ProjectStatusFilter = "Completed";
            }
        }

        #endregion

        #region Project Selection

        private void Project_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Project project)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedProject = project;
                }
            }
        }

        private void ProjectsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrackerMainViewModel vm && vm.SelectedProject != null)
            {
                vm.EditProjectCommand?.Execute(vm.SelectedProject);
            }
        }

        #endregion
    }
}
