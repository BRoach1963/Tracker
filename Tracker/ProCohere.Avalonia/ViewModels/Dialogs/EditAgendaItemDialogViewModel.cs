using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProCohere.Avalonia.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the EditAgendaItemDialog.
/// </summary>
public partial class EditAgendaItemDialogViewModel : ObservableObject
{
    private readonly DialogAgendaItem _item;
    
    /// <summary>
    /// The result of the dialog (null if cancelled).
    /// </summary>
    public EditAgendaItemResult? Result { get; private set; }
    
    /// <summary>
    /// Raised when the dialog should close.
    /// </summary>
    public event Action? CloseRequested;
    
    /// <summary>
    /// Raised when user wants to edit a talking point (View handles dialog).
    /// </summary>
    public event Action<TalkingPoint>? EditTalkingPointRequested;
    
    #region Observable Properties
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _displayTitle = string.Empty;
    
    [ObservableProperty]
    private string _sharedContext = string.Empty;
    
    [ObservableProperty]
    private string _privateContext = string.Empty;
    
    [ObservableProperty]
    private int _visibilityIndex;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTalkingPoints))]
    private ObservableCollection<TalkingPoint> _talkingPoints = new();
    
    [ObservableProperty]
    private bool _isAddPanelVisible;
    
    [ObservableProperty]
    private string _newTalkingPointText = string.Empty;
    
    #endregion
    
    /// <summary>
    /// Whether there are any talking points (for empty state visibility).
    /// </summary>
    public bool HasTalkingPoints => TalkingPoints.Count > 0;
    
    // Visibility tag values matching XAML order: meeting (0), personal (1)
    private static readonly string[] VisibilityTags = { "meeting", "personal" };
    
    public EditAgendaItemDialogViewModel()
    {
        _item = new DialogAgendaItem();
        TalkingPoints.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasTalkingPoints));
    }
    
    public EditAgendaItemDialogViewModel(DialogAgendaItem item) : this()
    {
        _item = item;
        LoadItemData();
    }
    
    private void LoadItemData()
    {
        Title = _item.Title ?? string.Empty;
        DisplayTitle = _item.DisplayTitle ?? string.Empty;
        SharedContext = _item.SharedContext ?? string.Empty;
        PrivateContext = _item.PrivateContext ?? string.Empty;
        
        // Set visibility index
        var visibilityScope = _item.VisibilityScope ?? "meeting";
        VisibilityIndex = Array.IndexOf(VisibilityTags, visibilityScope);
        if (VisibilityIndex < 0) VisibilityIndex = 0;
        
        // Load talking points
        TalkingPoints.Clear();
        foreach (var tp in _item.TalkingPoints)
        {
            TalkingPoints.Add(new TalkingPoint
            {
                Id = tp.Id,
                Text = tp.Text,
                Discussed = tp.Discussed,
                Order = tp.Order
            });
        }
    }
    
    #region Talking Points Commands
    
    [RelayCommand]
    private void ShowAddPanel()
    {
        IsAddPanelVisible = true;
        NewTalkingPointText = string.Empty;
    }
    
    [RelayCommand]
    private void CancelAdd()
    {
        IsAddPanelVisible = false;
        NewTalkingPointText = string.Empty;
    }
    
    [RelayCommand]
    private void ConfirmAdd()
    {
        var text = NewTalkingPointText?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        AddTalkingPoint(text);
        
        IsAddPanelVisible = false;
        NewTalkingPointText = string.Empty;
    }
    
    /// <summary>
    /// Called from View when Enter is pressed in the new talking point textbox.
    /// </summary>
    public void TryAddFromTextBox()
    {
        var text = NewTalkingPointText?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            AddTalkingPoint(text);
            NewTalkingPointText = string.Empty;
        }
    }
    
    private void AddTalkingPoint(string text)
    {
        var tp = new TalkingPoint
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            Discussed = false,
            Order = TalkingPoints.Count
        };
        TalkingPoints.Add(tp);
    }
    
    [RelayCommand]
    private void RemoveTalkingPoint(TalkingPoint? tp)
    {
        if (tp == null) return;
        
        TalkingPoints.Remove(tp);
        
        // Reorder remaining points
        for (int i = 0; i < TalkingPoints.Count; i++)
        {
            TalkingPoints[i].Order = i;
        }
    }
    
    [RelayCommand]
    private void EditTalkingPoint(TalkingPoint? tp)
    {
        if (tp == null) return;
        EditTalkingPointRequested?.Invoke(tp);
    }
    
    /// <summary>
    /// Called from View after edit dialog completes.
    /// </summary>
    public void UpdateTalkingPointText(TalkingPoint tp, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText)) return;
        
        tp.Text = newText.Trim();
        
        // Force refresh by removing and re-adding (ObservableCollection doesn't detect property changes)
        var index = TalkingPoints.IndexOf(tp);
        if (index >= 0)
        {
            TalkingPoints.RemoveAt(index);
            TalkingPoints.Insert(index, tp);
        }
    }
    
    #endregion
    
    #region Dialog Commands
    
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke();
    }
    
    [RelayCommand]
    private void Save()
    {
        var title = Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }
        
        var visibility = VisibilityIndex >= 0 && VisibilityIndex < VisibilityTags.Length 
            ? VisibilityTags[VisibilityIndex] 
            : "meeting";
        
        Result = new EditAgendaItemResult
        {
            WasSaved = true,
            Title = title,
            DisplayTitle = string.IsNullOrWhiteSpace(DisplayTitle) ? null : DisplayTitle.Trim(),
            SharedContext = string.IsNullOrWhiteSpace(SharedContext) ? null : SharedContext.Trim(),
            PrivateContext = string.IsNullOrWhiteSpace(PrivateContext) ? null : PrivateContext.Trim(),
            VisibilityScope = visibility,
            TalkingPoints = TalkingPoints.ToList(),
            IsDirty = _item.Id != Guid.Empty
        };
        
        CloseRequested?.Invoke();
    }
    
    #endregion
}
