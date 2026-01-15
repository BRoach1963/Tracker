using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTeamMemberDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-team-member")]
    public partial class TeamMemberDialog : BaseWindow
    {
        public TeamMemberDialog(TeamMemberViewModel vm, DialogType type) : base(type)
        {
            DataContext = vm;
            InitializeComponent();
        }

        private void PhotoOverlay_MouseEnter(object sender, MouseEventArgs e)
        {
            var animation = new DoubleAnimation(1, System.TimeSpan.FromMilliseconds(150));
            PhotoEditOverlay.BeginAnimation(OpacityProperty, animation);
        }

        private void PhotoOverlay_MouseLeave(object sender, MouseEventArgs e)
        {
            var animation = new DoubleAnimation(0, System.TimeSpan.FromMilliseconds(150));
            PhotoEditOverlay.BeginAnimation(OpacityProperty, animation);
        }

        private void ChangePhoto_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TeamMemberViewModel vm && vm.ChooseProfilePictureCommand.CanExecute(null))
            {
                vm.ChooseProfilePictureCommand.Execute(null);
            }
        }
    }
}
