namespace Tracker.Common.Enums
{
    /// <summary>
    /// Response status for meeting attendees.
    /// Maps to PostgreSQL attendee_response values.
    /// </summary>
    public enum AttendeeResponse
    {
        /// <summary>Attendee has not responded yet.</summary>
        Pending = 0,

        /// <summary>Attendee has accepted the meeting.</summary>
        Accepted = 1,

        /// <summary>Attendee has declined the meeting.</summary>
        Declined = 2,

        /// <summary>Attendee has tentatively accepted.</summary>
        Tentative = 3
    }
}
