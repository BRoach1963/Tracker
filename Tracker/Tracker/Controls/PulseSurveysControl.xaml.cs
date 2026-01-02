using System.Windows.Controls;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Pulse Surveys control for managing engagement surveys.
    /// </summary>
    public partial class PulseSurveysControl : UserControl
    {
        public PulseSurveysControl()
        {
            InitializeComponent();
            DataContext = new PulseSurveysViewModel();
        }
    }
}
