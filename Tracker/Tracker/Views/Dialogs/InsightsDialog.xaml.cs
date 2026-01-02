using System.Windows;
using Tracker.Controls;
using Tracker.ViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Dialog window to display all AI-generated insights.
    /// </summary>
    public partial class InsightsDialog : BaseWindow
    {
        private static InsightsDialog? _instance;
        
        public InsightsDialog()
        {
            InitializeComponent();
            InsightPanel.DataContext = new InsightPanelViewModel();
        }

        private new void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Shows the insights dialog as a singleton.
        /// </summary>
        public static void ShowInsights(Window? owner = null)
        {
            if (_instance != null && _instance.IsLoaded)
            {
                _instance.Activate();
                return;
            }

            _instance = new InsightsDialog();
            if (owner != null)
            {
                _instance.Owner = owner;
            }
            _instance.Show();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            _instance = null;
        }
    }
}
