using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddProjectDialog.xaml
    /// </summary>
    public partial class AddProjectDialog 
    {
        public AddProjectDialog(NewProjectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
