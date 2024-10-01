using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTeamMemberDialog.xaml
    /// </summary>
    public partial class TeamMemberDialog : BaseWindow
    {
        public TeamMemberDialog(TeamMemberViewModel vm, DialogType type) : base(type)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
