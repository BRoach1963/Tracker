using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : BaseWindow
    {
        public SettingsDialog(SettingsViewModel dataContext) : base(DialogType.Settings)
        {
            InitializeComponent();
            DataContext = dataContext;
        }
    }
}
