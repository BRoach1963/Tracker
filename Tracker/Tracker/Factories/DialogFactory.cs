using System.Windows;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.ViewModels.DialogViewModels;
using Tracker.Views.Dialogs;

namespace Tracker.Factories
{
    public static class DialogFactory
    {
        public static bool TryGetWindowFromType(DialogType type, Action callback, out BaseWindow window)
        {
            window = null;
            switch (type)
            {
                case DialogType.AddTeamMember:
                    window = new AddTeamMemberDialog(new AddTeamMemberViewModel(callback, new TeamMember()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = (Window)new WeakReference(UIHelper.GetOwnerWindow(type)).Target
                    };
                    return true;
                case DialogType.Settings:
                    window = new SettingsDialog(new SettingsViewModel(callback))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = (Window)new WeakReference(UIHelper.GetOwnerWindow(type)).Target
                    };
                    return true;
                default:
                    MessageBox.Show($"No dialog available for type {type}", "Invalid Dialog", MessageBoxButton.OK);
                    return false;
            }

            return false;
        }
    }
}
