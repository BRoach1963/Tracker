using System.ComponentModel;
using System.Runtime.Serialization;

namespace Tracker.Common.Enums
{
    public enum EngineeringSpecialtyEnum
    {
        [Description("full")]
        [EnumMember(Value = "full")]
        FullStack = 0,

        [Description("web_ui")]
        [EnumMember(Value = "web_ui")]
        WebUI = 1,

        [Description("back")]
        [EnumMember(Value = "back")]
        Backend = 2,

        [Description("android")]
        [EnumMember(Value = "android")]
        Android = 3,

        [Description("ios")]
        [EnumMember(Value = "ios")]
        iOS = 4,

        [Description("mac_os")]
        [EnumMember(Value = "mac_os")]
        Mac = 5,

        [Description("windows")]
        [EnumMember(Value = "windows")]
        Windows = 6,

        [Description("data_science")]
        [EnumMember(Value = "data_science")]
        DataScience = 7,

        [Description("ai")]
        [EnumMember(Value = "ai")]
        AI = 8,
    }
}
