using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Dialog for updating a metric's value.
/// </summary>
public partial class UpdateMetricValueDialog : Window
{
    /// <summary>
    /// Result of the dialog - the new value data if updated, null if cancelled.
    /// </summary>
    public UpdateMetricValueResult? Result { get; private set; }

    public UpdateMetricValueDialog()
    {
        InitializeComponent();
        
        // Focus the value field
        NewValueTextBox.AttachedToVisualTree += (s, e) => NewValueTextBox.Focus();
    }

    /// <summary>
    /// Initializes the dialog with current metric info.
    /// </summary>
    public void Initialize(string? currentValue, bool isManualMetric)
    {
        if (!string.IsNullOrEmpty(currentValue))
        {
            CurrentValueText.Text = currentValue;
            CurrentValueBorder.IsVisible = true;
        }
        else
        {
            CurrentValueBorder.IsVisible = false;
        }
        
        RequiredLabel.IsVisible = isManualMetric;
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }

    private void UpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        var newValue = NewValueTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(newValue))
        {
            NewValueTextBox.Focus();
            return;
        }

        // Try to parse as decimal
        if (!decimal.TryParse(newValue, out var value))
        {
            // Could show error, but for now just refocus
            NewValueTextBox.Focus();
            return;
        }

        Result = new UpdateMetricValueResult
        {
            NewValue = value,
            WhatChanged = WhatChangedTextBox.Text?.Trim()
        };

        Close();
    }
}

/// <summary>
/// Result data from the UpdateMetricValueDialog.
/// </summary>
public class UpdateMetricValueResult
{
    public decimal NewValue { get; init; }
    public string? WhatChanged { get; init; }
}
