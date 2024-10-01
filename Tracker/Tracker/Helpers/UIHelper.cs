using System.Windows;
using Tracker.Common.Enums;

namespace Tracker.Helpers
{
    public static class UiHelper
    {

        public static Window? GetOwnerWindow(DialogType type)
        {
            Window? ownerWindow = null;
            switch (type)
            {
                case DialogType.AddOneOnOne:
                case DialogType.AddTeamMember:
                case DialogType.AddKPI:
                case DialogType.AddOKR:
                case DialogType.AddProject:
                case DialogType.AddTask:
                case DialogType.Settings:
                    ownerWindow = Win32UtilHelper.GetMainWindow();
                    break;
            }

            return ownerWindow;
        }
    }
}
