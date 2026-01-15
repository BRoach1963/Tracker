using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTaskDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-task")]
    public partial class AddTaskDialog
    {
        public AddTaskDialog(NewTaskViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
