using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
    }
}
