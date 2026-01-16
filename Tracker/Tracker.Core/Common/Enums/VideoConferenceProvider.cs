namespace Tracker.Core.Common.Enums
{
    /// <summary>
    /// Video conference platform types.
    /// Maps to video_conference_provider column (varchar stored as string).
    /// </summary>
    public enum VideoConferenceProvider
    {
        /// <summary>
        /// Microsoft Teams meeting.
        /// </summary>
        Teams,

        /// <summary>
        /// Google Meet meeting.
        /// </summary>
        GoogleMeet,

        /// <summary>
        /// Zoom meeting.
        /// </summary>
        Zoom,

        /// <summary>
        /// Cisco WebEx meeting.
        /// </summary>
        WebEx
    }
}
