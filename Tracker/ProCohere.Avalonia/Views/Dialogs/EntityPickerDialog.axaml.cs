using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models.Dialogs;
using ProCohere.Avalonia.ViewModels.Dialogs;
using System;
using System.ComponentModel;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for searching and selecting existing items (tasks, goals, metrics, projects)
/// to link to an agenda item or prep item.
/// Minimal code-behind - all business logic in ViewModel.
/// </summary>
public partial class EntityPickerDialog : Window
{
    private readonly EntityPickerDialogViewModel _viewModel;

    /// <summary>
    /// The selected result (null if cancelled).
    /// </summary>
    public EntityPickerResult? Result => _viewModel.Result;

    public EntityPickerDialog()
    {
        InitializeComponent();

        _viewModel = new EntityPickerDialogViewModel();
        DataContext = _viewModel;

        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Load data when window opens
        Opened += async (s, e) => await _viewModel.LoadItemsAsync();
    }

    /// <summary>
    /// Optional: Filter to only show specific entity types.
    /// </summary>
    public void SetAllowedTypes(params string[] types)
    {
        _viewModel.SetAllowedTypes(types);

        // Update filter panel visibility
        if (types.Length == 1)
        {
            FilterPanel.IsVisible = false;
        }
    }

    private void OnCloseRequested(object? sender, EntityPickerResult? result)
    {
        Close(result);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntityPickerDialogViewModel.CanLink))
        {
            UpdateLinkButtonState();
        }
    }

    private void UpdateLinkButtonState()
    {
        if (_viewModel.CanLink)
        {
            LinkBorder.Classes.Add("can-link");
        }
        else
        {
            LinkBorder.Classes.Remove("can-link");
        }
    }

    #region UI Event Handlers (Visual State Only)

    private void FilterBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string filter)
        {
            _viewModel.SetFilterCommand.Execute(filter);

            // Update visual state - remove selected from all, add to clicked
            AllFilterBorder.Classes.Remove("selected");
            TaskFilterBorder.Classes.Remove("selected");
            GoalFilterBorder.Classes.Remove("selected");
            MetricFilterBorder.Classes.Remove("selected");
            ProjectFilterBorder.Classes.Remove("selected");

            border.Classes.Add("selected");
        }
    }

    private void ResultsListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }

    private void LinkBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel.CanLink)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }

    private void CancelBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _viewModel.CancelCommand.Execute(null);
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnClosed(e);
    }
}
