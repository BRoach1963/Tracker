using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.Common.Enums;

namespace Tracker.Eventing.Messages
{
    public class PropertyChangedMessage
    {
        public PropertyChangedEnum ChangedProperty { get; set; }
        public bool RefreshData { get; set; }
    }
}
