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
        public static bool TryGetWindowFromType(DialogType type, Action? callback, out BaseWindow? window)
        {
            switch (type)
            {
                case DialogType.AddTeamMember:
                    window = new AddTeamMemberDialog(new AddTeamMemberViewModel(callback, new TeamMember()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.Settings:
                    window = new SettingsDialog(new SettingsViewModel(callback))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddOneOnOne:
                    window = new AddOneOnOneDialog(new OneOnOneViewModel(callback, new OneOnOne()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddTask:
                    window = new AddTaskDialog(new NewTaskViewModel(callback, new IndividualTask()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddProject:
                    window = new AddProjectDialog(new NewProjectViewModel(callback, new Project()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddKPI:
                    window = new AddKPI(new NewKpiViewModel(callback, new KeyPerformanceIndicator()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddOKR:
                    window = new AddOkrDialog(new NewOkrViewModel(callback, new ObjectiveKeyResult()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                default:
                    MessageBox.Show($"No dialog available for type {type}", "Invalid Dialog", MessageBoxButton.OK);
                    window = null;
                    return false;
            }
        }
    }
}
