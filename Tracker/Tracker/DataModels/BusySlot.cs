using System;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a busy/unavailable time slot.
    /// </summary>
    public class BusySlot
    {
        /// <summary>Gets or sets the start time.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>Gets or sets the end time.</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Gets or sets the reason for being busy.</summary>
        public string? Reason { get; set; }

        /// <summary>Gets or sets the title/description of the busy slot.</summary>
        public string? Title { get; set; }

        /// <summary>Alias for StartTime for backwards compatibility.</summary>
        public DateTime Start => StartTime;

        /// <summary>Alias for EndTime for backwards compatibility.</summary>
        public DateTime End => EndTime;
    }
}
