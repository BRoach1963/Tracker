using Avalonia.Controls;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System.Collections.Generic;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for creating or editing metrics.
/// </summary>
public partial class EditMetricDialog : Window
{
    private EditMetricDialogViewModel? _viewModel;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditMetricResult? Result => _viewModel?.Result;
    
    public EditMetricDialog()
    {
        InitializeComponent();
        
        _viewModel = new EditMetricDialogViewModel();
        DataContext = _viewModel;
        
        _viewModel.CloseRequested += () => Close();
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
