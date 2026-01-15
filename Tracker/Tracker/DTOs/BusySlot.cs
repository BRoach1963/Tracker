using System;

namespace Tracker.DTOs
{
    /// <summary>
    /// Represents a busy/unavailable time slot.
    /// NOTE: This is a DTO for scheduling calculations, NOT a database entity.
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
    }
}
