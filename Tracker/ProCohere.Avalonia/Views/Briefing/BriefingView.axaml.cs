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
        _viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;
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
}
