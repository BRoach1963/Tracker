using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models.Dialogs;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the UpdateMetricValueDialog.
/// </summary>
public partial class UpdateMetricValueDialogViewModel : ObservableObject
{
    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<UpdateMetricValueResult?>? CloseRequested;

    /// <summary>
    /// Gets or sets the current value display text.
    /// </summary>
    [ObservableProperty]
    private string _currentValue = string.Empty;

    /// <summary>
    /// Gets or sets whether to show the current value section.
    /// </summary>
    [ObservableProperty]
    private bool _showCurrentValue;

    /// <summary>
    /// Gets or sets the new value input text.
    /// </summary>
    [ObservableProperty]
    private string _newValue = string.Empty;

    /// <summary>
    /// Gets or sets the what changed description.
    /// </summary>
    [ObservableProperty]
    private string _whatChanged = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a manual metric (shows required indicator).
    /// </summary>
    [ObservableProperty]
    private bool _isManualMetric;

    /// <summary>
    /// Initializes the dialog with current metric info.
    /// </summary>
    public void Initialize(string? currentValue, bool isManualMetric)
    {
        if (!string.IsNullOrEmpty(currentValue))
        {
            CurrentValue = currentValue;
            ShowCurrentValue = true;
        }
        else
        {
            ShowCurrentValue = false;
        }
        
        IsManualMetric = isManualMetric;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    [RelayCommand]
    private void Update()
    {
        var newValueText = NewValue?.Trim();
        
        if (string.IsNullOrWhiteSpace(newValueText))
        {
            return;
        }

        // Try to parse as decimal
        if (!decimal.TryParse(newValueText, out var value))
        {
            return;
        }

        var result = new UpdateMetricValueResult
        {
            NewValue = value,
            WhatChanged = string.IsNullOrWhiteSpace(WhatChanged) ? null : WhatChanged.Trim()
        };

        CloseRequested?.Invoke(this, result);
    }
}
