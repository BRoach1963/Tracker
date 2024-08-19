using System.ComponentModel;
using System.Runtime.Serialization;

namespace Tracker.Common.Enums
{
    public enum RoleEnum
    {
        [Description("engineer")]
        [EnumMember(Value="engineer")]
        Engineer = 0,

        [Description("qa")]
        [EnumMember(Value = "qa")] 
        QA = 1,

        [Description("sales")]
        [EnumMember(Value = "sales")]
        Sales = 2,

        [Description("mgr")]
        [EnumMember(Value = "mgr")]
        Manager = 3,

        [Description("principal")]
        [EnumMember(Value = "principal")]
        Principal = 4,

        [Description("architect")]
        [EnumMember(Value = "architect")]
        Architect = 5,

    }
}
