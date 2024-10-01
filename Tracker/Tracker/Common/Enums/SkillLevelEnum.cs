using System.ComponentModel;
using System.Runtime.Serialization;

namespace Tracker.Common.Enums
{
    public enum SkillLevelEnum
    {
        [Description("junior")]
        [EnumMember(Value = "junior")]
        Junior = 0,

        [Description("mid")]
        [EnumMember(Value = "mid")]
        Mid = 1,

        [Description("senior")]
        [EnumMember(Value = "senior")]
        Senior = 2,

        [Description("principle")]
        [EnumMember(Value = "principle")]
        Principle = 3,
    }
}
