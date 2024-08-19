using Tracker.Common.Enums;
using Tracker.Controls;

namespace Tracker.Managers.Interfaces
{
    public interface IDialogFactory
    {
        bool TryGetWindowFromType(DialogType type, Action callback, out BaseWindow window);
    }
}
