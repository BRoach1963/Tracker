using System;
using Avalonia;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.Attributes;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Goals browse/manage page.
/// This is the authoritative destination for browsing and managing goals.
/// Quick Access from Pulse navigates here (not to Circle tabs).
/// </summary>
[HelpContext("goals", ContextName = "GoalsView")]
public partial class GoalsView : UserControl
{
    private GoalsViewModel? _viewModel;
    
    public GoalsView()
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
        if (DataContext is GoalsViewModel vm)
        {
            _viewModel = vm;
            
            // Pass the ViewModel to the embedded content
            GoalsContent.DataContext = vm;
            
            // Load data if authenticated and visible
            if (IsVisible && AuthService.Instance.CurrentTeamMember != null)
            {
                _ = vm.RefreshAsync();
            }
        }
    }
}
