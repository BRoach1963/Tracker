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
                case DialogType.EditTeamMember:
                case DialogType.AddKPI:
                case DialogType.EditKPI:
                case DialogType.AddOKR:
                case DialogType.EditOKR:
                case DialogType.AddProject:
                case DialogType.EditProject:
                case DialogType.AddTask:
                case DialogType.EditTask:
                case DialogType.Settings:
                case DialogType.Reports:
                case DialogType.AddFeedback:
                case DialogType.AddGoal:
                case DialogType.AddKeyResult:
                case DialogType.EditKeyResult:
                case DialogType.AddMeasurable:
                    ownerWindow = Win32UtilHelper.GetMainWindow();
                    break;
            }

            return ownerWindow;
        }
    }
}
