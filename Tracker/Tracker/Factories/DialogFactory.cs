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
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
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
                                Owner = UiHelper.GetOwnerWindow(type),
                                ShowInTaskbar = true
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
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.Reports:
                    window = new ReportsDialog(new ReportsViewModel(callback))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddOneOnOne:
                    // Check if editing an existing meeting or creating a new one
                    MeetingViewModel vm;
                    if (dataObject is Meeting existingMeeting)
                    {
                        // Edit existing meeting - pass the report team member
                        vm = new MeetingViewModel(callback, existingMeeting, true, existingMeeting.Report);
                    }
                    else if (dataObject is TeamMember tm)
                    {
                        // New meeting for specific team member
                        vm = new MeetingViewModel(callback, new Meeting(), false, tm);
                    }
                    else
                    {
                        // New meeting
                        vm = new MeetingViewModel(callback, new Meeting(), false);
                    }

                    // Initialize with today's scheduled time (only for new meetings)
                    if (dataObject is not Meeting)
                    {
                        var now = DateTime.Now;
                        var minutes = now.Minute;
                        var roundedMinutes = ((minutes / 15) + 1) * 15; // Round UP to next quarter hour
                        var startHour = now.Hour;
                        if (roundedMinutes >= 60)
                        {
                            roundedMinutes = 0;
                            startHour = (startHour + 1) % 24;
                        }
                        // Set ScheduledAt to today at the rounded time
                        vm.Data.ScheduledAt = DateTime.Today.AddHours(startHour).AddMinutes(roundedMinutes);
                        vm.Data.DurationMinutes = 30; // Default 30 min meeting
                    }
                    
                    window = new AddOneOnOneDialog(vm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddTask:
                    window = new AddTaskDialog(new NewTaskViewModel(callback, new TrackerTask()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddProject:
                    window = new AddProjectDialog(new NewProjectViewModel(callback, new Project()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddKPI:
                    window = new AddKPI(new NewMetricViewModel(callback, new Metric()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddOKR:
                    window = new AddOkrDialog(new NewGoalViewModel(callback, new Goal()))
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
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
                        ? new FeedbackViewModel(callback, existingFeedback, existingFeedback.ToTeamMemberId, true)
                        : new FeedbackViewModel(callback, new Feedback(), Guid.Empty, false);
                    window = new AddFeedbackDialog(feedbackVm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;
                case DialogType.AddGoal:
                    var goalVm = dataObject is DevelopmentGoal existingGoal
                        ? new GoalViewModel(callback, existingGoal, existingGoal.TeamMemberId, true)
                        : new GoalViewModel(callback, new DevelopmentGoal(), Guid.Empty, false);
                    window = new AddGoalDialog(goalVm)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = UiHelper.GetOwnerWindow(type),
                        ShowInTaskbar = true
                    };
                    return true;

                case DialogType.EditOKR:
                    if (dataObject is Goal okrToEdit)
                    {
                        window = new AddOkrDialog(new NewGoalViewModel(callback, okrToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
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
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditTask:
                    if (dataObject is TrackerTask taskToEdit)
                    {
                        window = new AddTaskDialog(new NewTaskViewModel(callback, taskToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditKPI:
                    if (dataObject is Metric kpiToEdit)
                    {
                        window = new AddKPI(new NewMetricViewModel(callback, kpiToEdit, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.AddKeyResult:
                    if (dataObject is (Target newKr, Guid goalIdAdd))
                    {
                        window = new AddKeyResultDialog(new TargetViewModel(callback, newKr, goalIdAdd, edit: false))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    else if (dataObject is Guid goalId)
                    {
                        window = new AddKeyResultDialog(new TargetViewModel(callback, new Target(), goalId, edit: false))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.EditKeyResult:
                    if (dataObject is Target krToEdit)
                    {
                        window = new AddKeyResultDialog(new TargetViewModel(callback, krToEdit, krToEdit.GoalId, edit: true))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
                        };
                        return true;
                    }
                    window = null;
                    return false;

                case DialogType.AddMeasurable:
                    if (dataObject is Target targetForMeasurable)
                    {
                        window = new AddMeasurableDialog(new MeasurableViewModel(callback, targetForMeasurable))
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = UiHelper.GetOwnerWindow(type),
                            ShowInTaskbar = true
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
