using System.Windows.Input;
using Tracker.DataModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog for sending quick messages via Teams or Email.
    /// </summary>
    public partial class QuickMessageDialog
    {
        public QuickMessageDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates and shows a Quick Message dialog.
        /// </summary>
        /// <param name="recipient">The team member to message.</param>
        /// <param name="relatedMeeting">Optional related 1:1 meeting for context.</param>
        /// <param name="owner">Optional owner window.</param>
        public static void ShowDialog(TeamMember recipient, Meeting? relatedMeeting = null, System.Windows.Window? owner = null)
        {
            QuickMessageDialog? dialog = null;

            var viewModel = new QuickMessageViewModel(() =>
            {
                dialog?.Close();
            });

            viewModel.Initialize(recipient, relatedMeeting);

            dialog = new QuickMessageDialog
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

