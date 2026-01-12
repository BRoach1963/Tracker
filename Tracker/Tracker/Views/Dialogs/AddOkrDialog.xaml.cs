using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddOkrDialog.xaml
    /// Dialog for creating/editing Goals.
    /// </summary>
    [HelpContext("dialogs/add-goal")]
    public partial class AddOkrDialog 
    {
        public AddOkrDialog(NewGoalViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
