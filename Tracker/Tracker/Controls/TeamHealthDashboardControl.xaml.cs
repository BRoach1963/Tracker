using System.Windows.Controls;
//using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Interaction logic for TeamHealthDashboardControl.xaml - Currently disabled
    /// </summary>
    public partial class TeamHealthDashboardControl : UserControl
    {
        //private readonly TeamHealthDashboardViewModel _viewModel;

        public TeamHealthDashboardControl()
        {
            InitializeComponent();

            // TeamHealth feature disabled
            //// Create view model using DI
            //_viewModel = App.ViewModelFactory.Create<TeamHealthDashboardViewModel>();
            //DataContext = _viewModel;

            // Load data when control is loaded
            //Loaded += async (s, e) => await _viewModel.LoadDataAsync();
        }
    }
}
