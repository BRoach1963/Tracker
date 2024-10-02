using System.ComponentModel;

namespace Tracker.Common.Enums
{
    public enum ConcernType
    {
        [Description("performance")]
        Performance = 0,

        [Description("project")]
        Project = 1,

        [Description("process")]
        Process = 2,

        [Description("personal")]
        Personal = 3,

        [Description("other")]
        Other = 4
    }
}
