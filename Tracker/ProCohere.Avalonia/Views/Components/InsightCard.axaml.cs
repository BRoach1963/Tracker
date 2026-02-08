using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Components;

/// <summary>
/// Insight card component for displaying AI-generated insights.
/// Raises events for dismiss, snooze, and view actions.
/// </summary>
public partial class InsightCard : UserControl
{
    /// <summary>Raised when the dismiss button is clicked.</summary>
    public event EventHandler<Insight>? DismissRequested;
    
    /// <summary>Raised when the snooze button is clicked.</summary>
    public event EventHandler<Insight>? SnoozeRequested;
    
    /// <summary>Raised when the view button is clicked.</summary>
    public event EventHandler<Insight>? ViewRequested;

    public InsightCard()
    {
        InitializeComponent();
    }

    private void Dismiss_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Insight insight)
        {
            DismissRequested?.Invoke(this, insight);
        }
    }

    private void Snooze_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Insight insight)
        {
            SnoozeRequested?.Invoke(this, insight);
        }
    }

    private void View_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Insight insight)
        {
            ViewRequested?.Invoke(this, insight);
        }
    }
}
