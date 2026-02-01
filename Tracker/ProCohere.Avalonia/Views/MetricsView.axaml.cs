using System;
using Avalonia;
using Avalonia.Controls;
using ProCohere.Avalonia.ViewModels;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views;

/// <summary>
/// Standalone Metrics browse/manage page.
/// This is the authoritative destination for browsing and managing metrics.
/// Quick Access from Pulse navigates here (not to Circle tabs).
/// </summary>
public partial class MetricsView : UserControl
{
    private MetricsViewModel? _viewModel;
    
    public MetricsView()
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
        if (DataContext is MetricsViewModel vm)
        {
            _viewModel = vm;
            
            // Pass the ViewModel to the embedded content
            MetricsContent.DataContext = vm;
            
            // Load data if authenticated and visible
            if (IsVisible && AuthService.Instance.CurrentTeamMember != null)
            {
                _ = vm.RefreshAsync();
            }
        }
    }
}
