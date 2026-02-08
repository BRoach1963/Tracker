using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Views.Dialogs;
using ProCohere.Avalonia.Attributes;
using System;

namespace ProCohere.Avalonia.Views.Briefing;

[HelpContext("briefing", ContextName = "BriefingView")]
public partial class BriefingView : UserControl
{
    public BriefingView()
    {
        InitializeComponent();
        
        // DataContext is set via binding in MainWindow.axaml to MainWindowViewModel.BriefingViewModel
        // Subscribe to dialog events when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is BriefingViewModel viewModel)
        {
            // Subscribe to dialog events
            viewModel.CreateTaskDialogRequested += OnCreateTaskDialogRequested;
            viewModel.CreateMeetingDialogRequested += OnCreateMeetingDialogRequested;
            viewModel.CreateGoalDialogRequested += OnCreateGoalDialogRequested;
            viewModel.CreateNoteDialogRequested += OnCreateNoteDialogRequested;
        }
    }

    /// <summary>
    /// Exposes ViewModel for event subscriptions (e.g., navigation events).
    /// </summary>
    public BriefingViewModel? GetViewModel() => DataContext as BriefingViewModel;

    private async void OnCreateTaskDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateTaskAsync(window);
        
        if (result.Success && result.Task != null && DataContext is BriefingViewModel viewModel)
        {
            viewModel.OnTaskSaved(result.Task);
        }
    }

    private async void OnCreateMeetingDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateMeetingAsync(window);
        
        if (result.Success && result.Meeting != null && DataContext is BriefingViewModel viewModel)
        {
            viewModel.OnMeetingSaved(result.Meeting);
        }
    }

    private async void OnCreateGoalDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var result = await AppDialogService.ShowCreateGoalAsync(window);
        
        if (result.Success && result.Goal != null && DataContext is BriefingViewModel viewModel)
        {
            viewModel.OnGoalSaved(result.Goal);
        }
    }

    private async void OnCreateNoteDialogRequested(object? sender, EventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null) return;

        var dialog = new AddNoteDialog();
        var result = await dialog.ShowDialog<AddNoteResult?>(window);

        if (result != null && !string.IsNullOrWhiteSpace(result.Content))
        {
            var note = new Note
            {
                Title = result.Title,
                Content = result.Content
            };

            var created = await NotesService.Instance.CreateNoteAsync(note);
            if (created != null)
            {
                NotificationService.Instance.ShowSuccess("Note Created", "Your note has been saved.");
            }
        }
    }
}
