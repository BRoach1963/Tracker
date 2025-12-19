using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    public partial class GoalsControl : UserControl
    {
        public GoalsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle click on a goal item to select it.
        /// </summary>
        private void GoalItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is IndividualGoal goal)
            {
                if (DataContext is TrackerMainViewModel vm)
                {
                    vm.SelectedGoal = goal;
                }
            }
        }

        /// <summary>
        /// Handle filter by status click.
        /// </summary>
        private void FilterStatus_Click(object sender, MouseButtonEventArgs e)
        {
            // Future enhancement: filter goals by status
            // For now, this is a placeholder for the filter functionality
        }
    }
}
