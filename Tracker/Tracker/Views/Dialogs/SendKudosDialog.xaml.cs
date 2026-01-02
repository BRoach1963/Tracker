using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for sending kudos/recognition to team members.
    /// </summary>
    public partial class SendKudosDialog
    {
        public SendKudosDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates and shows a Send Kudos dialog.
        /// </summary>
        /// <param name="owner">Optional owner window.</param>
        public static void Show(System.Windows.Window? owner = null)
        {
            Show(null, owner);
        }

        /// <summary>
        /// Creates and shows a Send Kudos dialog with a pre-selected team member.
        /// </summary>
        /// <param name="preselectedMember">Team member to pre-select.</param>
        /// <param name="owner">Optional owner window.</param>
        public static void Show(TeamMember? preselectedMember, System.Windows.Window? owner = null)
        {
            SendKudosDialog? dialog = null;

            var viewModel = new SendKudosViewModel(() =>
            {
                dialog?.Close();
            }, preselectedMember);

            dialog = new SendKudosDialog
            {
                DataContext = viewModel,
                Owner = owner
            };

            dialog.ShowDialog();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
