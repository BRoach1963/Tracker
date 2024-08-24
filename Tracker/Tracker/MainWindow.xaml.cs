using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Managers;
using Tracker.ViewModels;

namespace Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new TrackerMainViewModel();
        }
 
    }
}