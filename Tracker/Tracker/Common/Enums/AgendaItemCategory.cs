using System.ComponentModel;

namespace Tracker.Common.Enums
{
    /// <summary>
    /// Categories for agenda items in 1:1 meetings.
    /// Replaces the separate DiscussionType and ConcernType enums.
    /// </summary>
    public enum AgendaItemCategory
    {
        [Description("Discussion Topic")]
        Topic = 0,

        [Description("Concern")]
        Concern = 1,

        [Description("Question")]
        Question = 2,

        [Description("Decision")]
        Decision = 3,

        [Description("Blocker")]
        Blocker = 4,

        [Description("Feedback")]
        Feedback = 5,

        [Description("Update")]
        Update = 6,

        [Description("Career Development")]
        CareerDevelopment = 7,

        [Description("Team Dynamics")]
        TeamDynamics = 8,

        [Description("Process Improvement")]
        Process = 9,

        [Description("Performance")]
        Performance = 10
    }
}

