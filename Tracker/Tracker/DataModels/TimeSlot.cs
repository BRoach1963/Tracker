using System;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a time slot for scheduling purposes.
    /// </summary>
    public class TimeSlot
    {
        /// <summary>Gets or sets the start time.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Gets or sets the end time.</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Gets or sets whether this slot is available.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Gets the duration of the time slot.</summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>Alias for StartTime for backwards compatibility.</summary>
        public DateTime Start => StartTime;

        /// <summary>Alias for EndTime for backwards compatibility.</summary>
        public DateTime End => EndTime;
    }
}
