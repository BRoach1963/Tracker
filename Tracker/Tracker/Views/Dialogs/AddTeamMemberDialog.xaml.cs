using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTeamMemberDialog.xaml
    /// </summary>
    public partial class AddTeamMemberDialog : BaseWindow
    {
        public AddTeamMemberDialog(AddTeamMemberViewModel vm) : base(DialogType.AddTeamMember)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
