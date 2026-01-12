using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.Help.Attributes;
using Tracker.Services;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for AddMeetingDialog.xaml
    /// </summary>
    [HelpContext("dialogs/add-one-on-one")]
    public partial class AddMeetingDialog
    {
        public AddMeetingDialog(MeetingViewModel vm) : base(DialogType.AddMeeting)
        {
            InitializeComponent();
            DataContext = vm;
        }

        /// <summary>
        /// Handles time slot selection from the scheduling assistant.
        /// </summary>
        private void SchedulingAssistant_TimeSlotSelected(object sender, TimeSlotSelectedEventArgs e)
        {
            if (DataContext is MeetingViewModel vm)
            {
                var slot = e.SelectedSlot;
                vm.ApplySelectedTimeSlot(slot.Start.Date, slot.Start.TimeOfDay, slot.End.TimeOfDay);
                
                // Optionally hide the scheduling assistant after selection
                vm.IsSchedulingAssistantVisible = false;
            }
        }
    }
}
