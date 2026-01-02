using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Services.MeetingPrep
{
    /// <summary>
    /// Interface for meeting prep data gatherers.
    /// Each gatherer is responsible for collecting data from a specific source.
    /// </summary>
    public interface IMeetingPrepGatherer
    {
        /// <summary>
        /// Name of the gatherer for logging and identification.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The section type this gatherer produces.
        /// </summary>
        PrepSectionType SectionType { get; }

        /// <summary>
        /// Whether this gatherer is enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gathers data and populates a prep section for the specified team member.
        /// </summary>
        /// <param name="teamMember">The team member to gather data for.</param>
        /// <param name="meetingDate">The date of the upcoming meeting.</param>
        /// <returns>A populated PrepSection or null if no relevant data.</returns>
        Task<PrepSection?> GatherAsync(TeamMember teamMember, DateTime meetingDate);
    }
}
