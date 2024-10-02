using System.Windows;
using Syncfusion.Windows.Shared;
using Tracker.Common.Enums;
using Tracker.Controls;
using Tracker.DataModels;
using Tracker.Helpers;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;
using Tracker.Views.Dialogs;

namespace Tracker.Factories
{
    public static class DialogFactory
    {
        public static bool TryGetWindowFromType(DialogType type, Action? callback, out BaseWindow? window, object? dataObject)
        {
            switch (type)
            {
                case DialogType.AddTeamMember:
                    window = new TeamMemberDialog(new TeamMemberViewModel(callback, new TeamMember()), type)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;

                case DialogType.EditTeamMember:
                    if (dataObject is TeamMember teamMember)
                    {
                        try
                        {
                            window = new TeamMemberDialog(new TeamMemberViewModel(callback, teamMember, true), type)
                            {
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                            };
                            return true;
                        }
                        catch (Exception e)
                        {
                            window = null;
                            return false; 
                        }
                    }
                    window = null;
                    return false;
                case DialogType.Settings:
                    window = new SettingsDialog(new SettingsViewModel(callback))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window
                    };
                    return true;
                case DialogType.AddOneOnOne:
                    var vm = dataObject is TeamMember tm
                        ? new OneOnOneViewModel(callback, new OneOnOne(), false, tm)
                        : new OneOnOneViewModel(callback, new OneOnOne(), false);

                    vm.Data.Date = DateTime.Now;
                    window = new AddOneOnOneDialog(vm)
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
                case DialogType.MainWindow:
                    window = new MainWindow(new TrackerMainViewModel())
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
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
