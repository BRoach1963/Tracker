using Tracker.Common.Enums;

namespace Tracker.Eventing.Messages
{
    public class DialogCloseMessage
    {
        public bool IsCanceled { get; set; }
        public DialogType DialogType { get; set; }
    }
}
