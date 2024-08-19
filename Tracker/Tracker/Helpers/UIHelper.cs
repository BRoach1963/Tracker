using System.Windows;
using Tracker.Common.Enums;

namespace Tracker.Helpers
{
    public static class UIHelper
    {

        public static Window GetOwnerWindow(DialogType type)
        {
            Window ownerWindow = null;
            switch (type)
            {
                case DialogType.AddTeamMember:
                    ownerWindow = Win32UtilHelper.GetMainWindow();
                    break;
            }

            return ownerWindow;
        }
    }
}
