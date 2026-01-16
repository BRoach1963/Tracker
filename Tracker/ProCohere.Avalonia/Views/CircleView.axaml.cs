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
}
