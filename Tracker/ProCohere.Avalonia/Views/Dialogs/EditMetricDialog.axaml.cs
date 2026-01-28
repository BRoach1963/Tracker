using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.Services;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing metrics.
/// </summary>
public partial class EditMetricDialog : Window
{
    private EditMetricDialogViewModel? _viewModel;
    private bool _forceClose;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMetricResult? Result => _viewModel?.Result;
    
    public EditMetricDialog()
    {
        InitializeComponent();
        
        _viewModel = new EditMetricDialogViewModel();
        DataContext = _viewModel;
        
        // Provide dialog service for confirmations
        _viewModel.SetDialogService(new DialogService(this));
        
        // Close window when ViewModel requests it (approved close path)
        _viewModel.CloseRequested += () =>
        {
            _forceClose = true;
            Close();
        };
    }
    
    /// <summary>
    /// Handle window closing to show confirmation if there are unsaved changes.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }
        
        if (_viewModel?.HasUnsavedChanges == true)
        {
            e.Cancel = true;
            
            if (_viewModel.CancelCommand.CanExecute(null))
            {
                await _viewModel.CancelCommand.ExecuteAsync(null);
            }
        }
        else
        {
            base.OnClosing(e);
        }
    }
    
    /// <summary>
    /// Load an existing metric for editing.
    /// </summary>
    public void LoadMetric(MetricDetail metric)
    {
        _viewModel?.LoadMetric(metric);
    }
    
    /// <summary>
    /// Set the list of team members for the owner dropdown.
    /// </summary>
    public void SetTeamMembers(IEnumerable<TeamMemberDetail> teamMembers)
    {
        _viewModel?.SetTeamMembers(teamMembers);
    }
}
