using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog 
    {
        public SettingsDialog(SettingsViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
