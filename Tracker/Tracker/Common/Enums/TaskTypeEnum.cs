using System.ComponentModel;

namespace Tracker.Common.Enums
{
    public enum TaskTypeEnum
    {
        [Description("Action Item")]
        ActionItem = 0,

        [Description("Follow Up")]
        FollowUp = 1,

        [Description("Individual")]
        Individual = 2,
    }
}
