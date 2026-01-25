using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProCohere.Avalonia.Views.Dialogs;

/// <summary>
/// Result from the edit agenda item dialog.
/// </summary>
public class EditAgendaItemResult
{
    public bool WasSaved { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DisplayTitle { get; set; }
    public string? SharedContext { get; set; }
    public string? PrivateContext { get; set; }
    public string VisibilityScope { get; set; } = "meeting";
    public List<TalkingPoint> TalkingPoints { get; set; } = new();
}

/// <summary>
/// Dialog for editing agenda item details including context, talking points, and visibility.
/// </summary>
public partial class EditAgendaItemDialog : Window
{
    private readonly DialogAgendaItem _item;
    private readonly ObservableCollection<TalkingPoint> _talkingPoints = new();
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditAgendaItemResult? Result { get; private set; }
    
    public EditAgendaItemDialog()
    {
        InitializeComponent();
        _item = new DialogAgendaItem();
        TalkingPointsControl.ItemsSource = _talkingPoints;
    }
    
    public EditAgendaItemDialog(DialogAgendaItem item) : this()
    {
        _item = item;
        LoadItemData();
    }
    
    private void LoadItemData()
    {
        TitleTextBox.Text = _item.Title;
        DisplayTitleTextBox.Text = _item.DisplayTitle;
        SharedContextTextBox.Text = _item.SharedContext;
        PrivateContextTextBox.Text = _item.PrivateContext;
        
        // Set visibility combo
        for (int i = 0; i < VisibilityComboBox.Items.Count; i++)
        {
            if (VisibilityComboBox.Items[i] is ComboBoxItem cbi && 
                cbi.Tag?.ToString() == _item.VisibilityScope)
            {
                VisibilityComboBox.SelectedIndex = i;
                break;
            }
        }
        
        // Load talking points
        _talkingPoints.Clear();
        foreach (var tp in _item.TalkingPoints)
        {
            _talkingPoints.Add(new TalkingPoint
            {
                Id = tp.Id,
                Text = tp.Text,
                Discussed = tp.Discussed,
                Order = tp.Order
            });
        }
        UpdateTalkingPointsEmptyState();
    }
    
    #region Talking Points
    
    private void AddTalkingPoint_Click(object? sender, RoutedEventArgs e)
    {
        AddTalkingPointPanel.IsVisible = true;
        NewTalkingPointTextBox.Text = "";
        NewTalkingPointTextBox.Focus();
    }
    
    private void CancelAddTalkingPoint_Click(object? sender, RoutedEventArgs e)
    {
        AddTalkingPointPanel.IsVisible = false;
        NewTalkingPointTextBox.Text = "";
    }
    
    private void ConfirmAddTalkingPoint_Click(object? sender, RoutedEventArgs e)
    {
        var text = NewTalkingPointTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        AddTalkingPoint(text);
        
        AddTalkingPointPanel.IsVisible = false;
        NewTalkingPointTextBox.Text = "";
    }
    
    private void NewTalkingPointTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = NewTalkingPointTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                AddTalkingPoint(text);
                NewTalkingPointTextBox.Text = "";
            }
        }
        else if (e.Key == Key.Escape)
        {
            CancelAddTalkingPoint_Click(sender, e);
        }
    }
    
    private void AddTalkingPoint(string text)
    {
        var tp = new TalkingPoint
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            Discussed = false,
            Order = _talkingPoints.Count
        };
        _talkingPoints.Add(tp);
        UpdateTalkingPointsEmptyState();
    }
    
    private void RemoveTalkingPoint_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TalkingPoint tp)
        {
            _talkingPoints.Remove(tp);
            // Reorder remaining points
            for (int i = 0; i < _talkingPoints.Count; i++)
            {
                _talkingPoints[i].Order = i;
            }
            UpdateTalkingPointsEmptyState();
        }
    }
    
    private void UpdateTalkingPointsEmptyState()
    {
        TalkingPointsEmptyState.IsVisible = _talkingPoints.Count == 0;
    }
    
    #endregion
    
    #region Dialog Actions
    
    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
    
    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            // Focus the title field if empty
            TitleTextBox.Focus();
            return;
        }
        
        var visibilityItem = VisibilityComboBox.SelectedItem as ComboBoxItem;
        var visibility = visibilityItem?.Tag?.ToString() ?? "meeting";
        
        Result = new EditAgendaItemResult
        {
            WasSaved = true,
            Title = title,
            DisplayTitle = string.IsNullOrWhiteSpace(DisplayTitleTextBox.Text) ? null : DisplayTitleTextBox.Text.Trim(),
            SharedContext = string.IsNullOrWhiteSpace(SharedContextTextBox.Text) ? null : SharedContextTextBox.Text.Trim(),
            PrivateContext = string.IsNullOrWhiteSpace(PrivateContextTextBox.Text) ? null : PrivateContextTextBox.Text.Trim(),
            VisibilityScope = visibility,
            TalkingPoints = _talkingPoints.ToList()
        };
        
        Close();
    }
    
    #endregion
}
