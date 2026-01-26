namespace ProCohere.Avalonia.Models.Dialogs;

/// <summary>
/// Result data from the UpdateMetricValueDialog.
/// </summary>
public class UpdateMetricValueResult
{
    public decimal NewValue { get; init; }
    public string? WhatChanged { get; init; }
}
