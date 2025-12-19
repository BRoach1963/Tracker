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
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
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
                                Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                                ShowInTaskbar = false
                            };
                            return true;
                        }
                        catch (Exception)
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
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.Reports:
                    window = new ReportsDialog(new ReportsViewModel(callback))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddOneOnOne:
                    var vm = dataObject is TeamMember tm
                        ? new OneOnOneViewModel(callback, new OneOnOne(), false, tm)
                        : new OneOnOneViewModel(callback, new OneOnOne(), false);

                    // Initialize with today's date and round to nearest quarter hour
                    vm.Data.Date = DateTime.Today;
                    var now = DateTime.Now;
                    var minutes = now.Minute;
                    var roundedMinutes = ((minutes / 15) + 1) * 15; // Round UP to next quarter hour
                    var startHour = now.Hour;
                    if (roundedMinutes >= 60)
                    {
                        roundedMinutes = 0;
                        startHour = (startHour + 1) % 24;
                    }
                    vm.Data.StartTime = new TimeSpan(startHour, roundedMinutes, 0);
                    vm.Data.EndTime = vm.Data.StartTime.Add(TimeSpan.FromMinutes(30)); // Default 30 min meeting
                    
                    window = new AddOneOnOneDialog(vm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddTask:
                    window = new AddTaskDialog(new NewTaskViewModel(callback, new IndividualTask()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddProject:
                    window = new AddProjectDialog(new NewProjectViewModel(callback, new Project()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddKPI:
                    window = new AddKPI(new NewKpiViewModel(callback, new KeyPerformanceIndicator()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddOKR:
                    window = new AddOkrDialog(new NewOkrViewModel(callback, new ObjectiveKeyResult()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true; 
                case DialogType.MainWindow:
                    window = new MainWindow(new TrackerMainViewModel())
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    return true;
                case DialogType.AddFeedback:
                    var feedbackVm = dataObject is Feedback existingFeedback
                        ? new FeedbackViewModel(callback, existingFeedback, existingFeedback.TeamMemberId, true)
                        : new FeedbackViewModel(callback, new Feedback(), 0, false);
                    window = new AddFeedbackDialog(feedbackVm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;
                case DialogType.AddGoal:
                    var goalVm = dataObject is IndividualGoal existingGoal
                        ? new GoalViewModel(callback, existingGoal, existingGoal.TeamMemberId, true)
                        : new GoalViewModel(callback, new IndividualGoal(), 0, false);
                    window = new AddGoalDialog(goalVm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                        ShowInTaskbar = false
                    };
                    return true;

                case DialogType.EditOKR:
                    if (dataObject is ObjectiveKeyResult okrToEdit)
                    {
                        window = new AddOkrDialog(new NewOkrViewModel(callback, okrToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditProject:
                    if (dataObject is Project projectToEdit)
                    {
                        window = new AddProjectDialog(new NewProjectViewModel(callback, projectToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditTask:
                    if (dataObject is IndividualTask taskToEdit)
                    {
                        window = new AddTaskDialog(new NewTaskViewModel(callback, taskToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditKPI:
                    if (dataObject is KeyPerformanceIndicator kpiToEdit)
                    {
                        window = new AddKPI(new NewKpiViewModel(callback, kpiToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.AddKeyResult:
                    if (dataObject is (KeyResult newKr, int okrIdAdd))
                    {
                        window = new AddKeyResultDialog(new KeyResultViewModel(callback, newKr, okrIdAdd, edit: false))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    else if (dataObject is int okrId)
                    {
                        window = new AddKeyResultDialog(new KeyResultViewModel(callback, new KeyResult(), okrId, edit: false))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditKeyResult:
                    if (dataObject is KeyResult krToEdit)
                    {
                        window = new AddKeyResultDialog(new KeyResultViewModel(callback, krToEdit, krToEdit.OkrId, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.AddMeasurable:
                    if (dataObject is KeyResult krForMeasurable)
                    {
                        window = new AddMeasurableDialog(new MeasurableViewModel(callback, krForMeasurable))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = new WeakReference(UiHelper.GetOwnerWindow(type)).Target as Window,
                            ShowInTaskbar = false
                        };
                        return true;
                    }
                    window = null;
                    return false;

                default:
                    MessageBoxHelper.Show($"No dialog available for type {type}", "Invalid Dialog");
                    window = null;
                    return false;
            }
        }
    }
}
