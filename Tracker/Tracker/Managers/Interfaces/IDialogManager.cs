using Tracker.Common.Enums;
using Tracker.Controls;

namespace Tracker.Managers.Interfaces
{
    public interface IDialogManager
    {
        void LaunchDialogByType(DialogType type, bool modal, Action callback);
        void CloseDialog(BaseWindow dialog, bool dispose = true);
    }
}
