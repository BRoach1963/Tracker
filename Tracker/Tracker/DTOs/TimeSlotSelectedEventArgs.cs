using System;

namespace Tracker.DTOs
{
    /// <summary>
    /// Event arguments for time slot selection from the scheduling assistant.
    /// NOTE: This is a DTO for event handling, NOT a database entity.
    /// </summary>
    public class TimeSlotSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// The selected time slot.
        /// </summary>
        public TimeSlot SelectedSlot { get; }

        /// <summary>
        /// Creates a new TimeSlotSelectedEventArgs.
        /// </summary>
        /// <param name="selectedSlot">The selected time slot.</param>
        public TimeSlotSelectedEventArgs(TimeSlot selectedSlot)
        {
            SelectedSlot = selectedSlot ?? throw new ArgumentNullException(nameof(selectedSlot));
        }
    }
}
