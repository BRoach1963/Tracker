using System.Windows;
using System.Windows.Input;
using Tracker.Controls;
using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    [HelpContext("dialogs/add-feedback")]
    public partial class AddFeedbackDialog : BaseWindow
    {
        public AddFeedbackDialog()
        {
            InitializeComponent();
        }

        public AddFeedbackDialog(FeedbackViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        // Use base class methods: OnDragHandleMouseDown, Close_Click, 
        // Minimize_Click, Maximize_Click, Restore_Click are inherited from BaseWindow
    }
}
