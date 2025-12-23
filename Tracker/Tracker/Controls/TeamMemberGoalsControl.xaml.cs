using System.Windows.Controls;
using System.Windows.Input;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Compact goals list control for embedding in dialogs.
    /// </summary>
    public partial class TeamMemberGoalsControl : UserControl
    {
        public TeamMemberGoalsControl()
        {
            InitializeComponent();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to edit goal
            if (DataContext is TeamMemberViewModel vm && vm.SelectedGoal != null)
            {
                vm.EditGoalCommand?.Execute(null);
            }
        }
    }
}

