using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.ViewModels;

namespace ProCohere.Avalonia.Views;

public partial class CircleView : UserControl
{
    public CircleView()
    {
        InitializeComponent();
        DataContext = new CircleViewModel();
    }

    private void StatFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.All);
        }
    }

    private void StatFilter_Active_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.Active);
        }
    }

    private void StatFilter_NeedsAttention_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFilterCommand.Execute(TeamMemberFilter.NeedsAttention);
        }
    }

    private void MemberCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is TeamMemberDetail member)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectTeamMemberCommand.Execute(member);
            }
        }
    }

    private void MemberCard_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is TeamMemberDetail member)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.EditTeamMemberCommand.Execute(member);
            }
        }
    }

    private void MeetingCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is MeetingDetail meeting)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectMeetingCommand.Execute(meeting);
            }
        }
    }

    #region Goals Tab Handlers

    private void GoalFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.All);
        }
    }

    private void GoalFilter_OnTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.OnTrack);
        }
    }

    private void GoalFilter_AtRisk_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.AtRisk);
        }
    }

    private void GoalFilter_OffTrack_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetGoalFilterCommand.Execute(GoalFilter.OffTrack);
        }
    }

    private void GoalCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is GoalDetail goal)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectGoalCommand.Execute(goal);
            }
        }
    }

    #endregion

    #region Feedback Tab Handlers

    private void FeedbackFilter_All_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.All);
        }
    }

    private void FeedbackFilter_Praise_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Praise);
        }
    }

    private void FeedbackFilter_Constructive_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Constructive);
        }
    }

    private void FeedbackFilter_Coaching_Tapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CircleViewModel vm)
        {
            vm.SetFeedbackFilterCommand.Execute(FeedbackFilter.Coaching);
        }
    }

    private void FeedbackCard_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is FeedbackDetail feedback)
        {
            if (DataContext is CircleViewModel vm)
            {
                vm.SelectFeedbackCommand.Execute(feedback);
            }
        }
    }

    #endregion
}
