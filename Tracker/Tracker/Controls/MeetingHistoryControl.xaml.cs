using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Factories;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Displays meeting history for a team member.
    /// </summary>
    public partial class MeetingHistoryControl : UserControl
    {
        public MeetingHistoryControl()
        {
            InitializeComponent();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is Meeting meeting)
            {
                // DialogFactory will handle creating the correct ViewModel with the meeting data
                if (DialogFactory.TryGetWindowFromType(DialogType.AddOneOnOne, null, out var dialog, meeting))
                {
                    dialog?.Show();
                }
            }
        }
    }
}

