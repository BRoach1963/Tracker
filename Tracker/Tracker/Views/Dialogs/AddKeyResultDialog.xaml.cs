using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddKeyResultDialog.xaml
    /// Dialog for creating/editing Targets.
    /// </summary>
    [HelpContext("dialogs/add-target")]
    public partial class AddKeyResultDialog
    {
        public AddKeyResultDialog(TargetViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

