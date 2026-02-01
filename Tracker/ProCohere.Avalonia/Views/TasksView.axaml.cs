using System;
using Avalonia;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Tasks browse/manage page.
/// This is the authoritative destination for browsing and managing tasks.
/// Quick Access from Pulse navigates here (not to Me page).
/// </summary>
public partial class TasksView : UserControl
{
    private TasksViewModel? _viewModel;
    
    public TasksView()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Refresh data when page becomes visible (only if authenticated)
        if (_viewModel != null && AuthService.Instance.CurrentTeamMember != null)
        {
            _ = _viewModel.RefreshAsync();
        }
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is TasksViewModel vm)
        {
            _viewModel = vm;
            
            // Pass the ViewModel to the embedded content
            TasksContent.DataContext = vm;
            
            // Load data if authenticated and visible
            if (IsVisible && AuthService.Instance.CurrentTeamMember != null)
            {
                _ = vm.RefreshAsync();
            }
        }
    }
}
