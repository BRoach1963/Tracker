using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddKPI.xaml
    /// </summary>
    public partial class AddKPI 
    {
        public AddKPI(NewKpiViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
