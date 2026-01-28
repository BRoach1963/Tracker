using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Reusable card control for displaying a note in grid/list context.
/// Supports configurable pin indicator visibility and click events.
/// </summary>
public partial class NoteCard : UserControl
{
    /// <summary>
    /// Defines the ShowPinIndicator styled property.
    /// When true, displays the "Pinned" badge on the card.
    /// </summary>
    public static readonly StyledProperty<bool> ShowPinIndicatorProperty =
        AvaloniaProperty.Register<NoteCard, bool>(nameof(ShowPinIndicator), defaultValue: false);

    /// <summary>
    /// Gets or sets whether to show the pinned indicator badge.
    /// </summary>
    public bool ShowPinIndicator
    {
        get => GetValue(ShowPinIndicatorProperty);
        set => SetValue(ShowPinIndicatorProperty, value);
    }

    /// <summary>
    /// Event raised when the card is clicked.
    /// </summary>
    public event EventHandler<NoteCardClickedEventArgs>? CardClicked;

    public NoteCard()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowPinIndicatorProperty)
        {
            UpdatePinIndicatorVisibility();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdatePinIndicatorVisibility();
    }

    private void UpdatePinIndicatorVisibility()
    {
        if (PinIndicatorBorder != null)
        {
            PinIndicatorBorder.IsVisible = ShowPinIndicator;
        }
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is Note note)
        {
            CardClicked?.Invoke(this, new NoteCardClickedEventArgs(note));
        }
    }
}

/// <summary>
/// Event args for the NoteCard.CardClicked event.
/// </summary>
public class NoteCardClickedEventArgs : EventArgs
{
    /// <summary>
    /// The note that was clicked.
    /// </summary>
    public Note Note { get; }

    public NoteCardClickedEventArgs(Note note)
    {
        Note = note;
    }
}
