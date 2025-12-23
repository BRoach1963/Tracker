using System.Windows.Controls;
using System.Windows.Input;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Compact feedback list control for embedding in dialogs.
    /// </summary>
    public partial class TeamMemberFeedbackControl : UserControl
    {
        public TeamMemberFeedbackControl()
        {
            InitializeComponent();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to edit feedback
            if (DataContext is TeamMemberViewModel vm && vm.SelectedFeedback != null)
            {
                vm.EditFeedbackCommand?.Execute(null);
            }
        }
    }
}

