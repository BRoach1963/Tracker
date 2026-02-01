using Avalonia.Controls;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using System;

namespace ProCohere.Avalonia.Views.Briefing;

public partial class BriefingView : UserControl
{
    private readonly BriefingViewModel _viewModel;
    
    public BriefingView()
    {
        InitializeComponent();
        _viewModel = new BriefingViewModel();
        DataContext = _viewModel;
        
        // Subscribe to dialog events
        _viewModel.CreateTaskDialogRequested += OnCreateTaskDialogRequested;
        _viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;
        _viewModel.CreateGoalDialogRequested += OnCreateGoalDialogRequested;
        _viewModel.CreateNoteDialogRequested += OnCreateNoteDialogRequested;
    }

    private async void OnCreateTaskDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateTaskAsync(window);
        
        if (result.Success && result.Task != null)
        {
            _viewModel.OnTaskSaved(result.Task);
        }
    }

    private async void OnCreateMeetingDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateMeetingAsync(window);
        
        if (result.Success && result.Meeting != null)
        {
            _viewModel.OnMeetingSaved(result.Meeting);
        }
    }

    private async void OnCreateGoalDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateGoalAsync(window);
        
        if (result.Success && result.Goal != null)
        {
            // Refresh goals list to include the new goal
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private async void OnCreateNoteDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        // TODO: Implement note creation dialog when available
        // For now, show a notification that this feature is coming
        NotificationService.Instance.ShowInfo("Coming Soon", "Note creation will be available soon.");
    }
}
