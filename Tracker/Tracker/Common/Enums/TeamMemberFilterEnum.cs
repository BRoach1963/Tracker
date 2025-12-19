using System.ComponentModel;

namespace Tracker.Common.Enums
{
    /// <summary>
    /// Filter options for the Team Members page.
    /// </summary>
    public enum TeamMemberFilterEnum
    {
        [Description("All")]
        All = 0,

        [Description("Active")]
        Active = 1,

        [Description("Inactive")]
        Inactive = 2,

        [Description("1:1 On Track")]
        OneOnOneOnTrack = 3,

        [Description("1:1 Overdue")]
        OneOnOneOverdue = 4,

        [Description("Has Open Tasks")]
        HasOpenTasks = 5,

        [Description("Needs Attention")]
        NeedsAttention = 6
    }
}

