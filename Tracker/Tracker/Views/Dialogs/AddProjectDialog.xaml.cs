using Tracker.Help.Attributes;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddProjectDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-project")]
    public partial class AddProjectDialog
    {
        public AddProjectDialog(NewProjectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
