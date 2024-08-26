using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
