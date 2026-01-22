using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Progressive disclosure card for agenda items.
/// Displays collapsed/expanded views with notes and outcomes tabs.
/// </summary>
public partial class AgendaItemCard : UserControl
{
    private bool _isExpanded;
    private string _activeTab = "outcomes";
    private List<AgendaItemOutcomeDetail> _outcomes = new();
    private List<AgendaItemOutcomeDetail> _notes = new();

    #region Events

    /// <summary>
    /// Raised when user wants to create a task from this agenda item.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? CreateTaskRequested;

    /// <summary>
    /// Raised when user wants to record a decision.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? RecordDecisionRequested;

    /// <summary>
    /// Raised when user wants to capture feedback.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? CaptureFeedbackRequested;

    /// <summary>
    /// Raised when user wants to add a note.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? AddNoteRequested;

    /// <summary>
    /// Raised when status changes.
    /// </summary>
    public event EventHandler<(MeetingAgendaItem Item, string NewStatus)>? StatusChanged;

    /// <summary>
    /// Raised when user wants to defer the item (needs to select anchor person).
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? DeferRequested;

    #endregion

    public AgendaItemCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    #region Properties

    /// <summary>
    /// Gets the current agenda item from DataContext.
    /// </summary>
    private MeetingAgendaItem? AgendaItem => DataContext as MeetingAgendaItem;

    /// <summary>
    /// Whether the card is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            UpdateExpandedState();
        }
    }

    #endregion

    #region Expand/Collapse

    private void OnExpandClick(object? sender, RoutedEventArgs e)
    {
        IsExpanded = !IsExpanded;
        
        // Load outcomes on first expand
        if (IsExpanded && _outcomes.Count == 0 && AgendaItem != null)
        {
            _ = LoadOutcomesAsync();
        }
    }

    private void UpdateExpandedState()
    {
        var expandedSection = this.FindControl<StackPanel>("ExpandedSection");
        var expandIcon = this.FindControl<PathIcon>("ExpandIcon");

        if (expandedSection != null)
        {
            expandedSection.IsVisible = _isExpanded;
        }

        if (expandIcon != null)
        {
            // Rotate icon when expanded
            expandIcon.Data = _isExpanded
                ? global::Avalonia.Media.Geometry.Parse("M7.41,15.41L12,10.83L16.59,15.41L18,14L12,8L6,14L7.41,15.41Z") // Up arrow
                : global::Avalonia.Media.Geometry.Parse("M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z"); // Down arrow
        }
    }

    #endregion

    #region Tab Switching

    private void OnOutcomesTabClick(object? sender, RoutedEventArgs e)
    {
        _activeTab = "outcomes";
        UpdateTabVisibility();
    }

    private void OnNotesTabClick(object? sender, RoutedEventArgs e)
    {
        _activeTab = "notes";
        UpdateTabVisibility();
    }

    private void UpdateTabVisibility()
    {
        var outcomesTabButton = this.FindControl<Button>("OutcomesTabButton");
        var notesTabButton = this.FindControl<Button>("NotesTabButton");
        var outcomesTabContent = this.FindControl<StackPanel>("OutcomesTabContent");
        var notesTabContent = this.FindControl<StackPanel>("NotesTabContent");

        if (outcomesTabButton != null)
        {
            outcomesTabButton.Classes.Set("selected", _activeTab == "outcomes");
        }
        if (notesTabButton != null)
        {
            notesTabButton.Classes.Set("selected", _activeTab == "notes");
        }
        if (outcomesTabContent != null)
        {
            outcomesTabContent.IsVisible = _activeTab == "outcomes";
        }
        if (notesTabContent != null)
        {
            notesTabContent.IsVisible = _activeTab == "notes";
        }
    }

    #endregion

    #region Status Changes

    private async void OnSetOpen(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem == null) return;
        
        var success = await MeetingAgendaItemService.Instance.UpdateStatusAsync(AgendaItem.Id, "open");
        if (success)
        {
            AgendaItem.Status = "open";
            AgendaItem.IsCompleted = false;
            StatusChanged?.Invoke(this, (AgendaItem, "open"));
        }
    }

    private async void OnSetDiscussed(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem == null) return;
        
        var success = await MeetingAgendaItemService.Instance.UpdateStatusAsync(AgendaItem.Id, "discussed");
        if (success)
        {
            AgendaItem.Status = "discussed";
            AgendaItem.IsCompleted = true;
            StatusChanged?.Invoke(this, (AgendaItem, "discussed"));
        }
    }

    private void OnSetDeferred(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem == null) return;
        
        // Raise event - parent handles showing the deferral dialog to select anchor person
        DeferRequested?.Invoke(this, AgendaItem);
    }

    private async void OnSetDropped(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem == null) return;
        
        var success = await MeetingAgendaItemService.Instance.UpdateStatusAsync(AgendaItem.Id, "dropped");
        if (success)
        {
            AgendaItem.Status = "dropped";
            AgendaItem.IsCompleted = true;
            StatusChanged?.Invoke(this, (AgendaItem, "dropped"));
        }
    }

    #endregion

    #region Outcome Actions

    private void OnCreateTask(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            CreateTaskRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnRecordDecision(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            RecordDecisionRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnCaptureFeedback(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            CaptureFeedbackRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnAddNote(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            AddNoteRequested?.Invoke(this, AgendaItem);
        }
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads outcomes for the current agenda item.
    /// </summary>
    public async Task LoadOutcomesAsync()
    {
        if (AgendaItem == null) return;

        try
        {
            var allOutcomes = await AgendaItemOutcomeService.Instance.GetOutcomesForAgendaItemAsync(AgendaItem.Id);
            
            // Separate notes from other outcomes
            _outcomes = allOutcomes.FindAll(o => o.OutcomeType != OutcomeType.NotesAdded);
            _notes = allOutcomes.FindAll(o => o.OutcomeType == OutcomeType.NotesAdded);

            // Update UI
            UpdateOutcomesList();
            UpdateNotesList();
            UpdateBadgeCounts();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AgendaItemCard.LoadOutcomesAsync ERROR: {ex.Message}");
        }
    }

    private void UpdateOutcomesList()
    {
        var outcomesList = this.FindControl<ItemsControl>("OutcomesList");
        var noOutcomesText = this.FindControl<TextBlock>("NoOutcomesText");

        if (outcomesList != null)
        {
            outcomesList.ItemsSource = _outcomes;
        }
        if (noOutcomesText != null)
        {
            noOutcomesText.IsVisible = _outcomes.Count == 0;
        }
    }

    private void UpdateNotesList()
    {
        var notesList = this.FindControl<ItemsControl>("NotesList");
        var noNotesText = this.FindControl<TextBlock>("NoNotesText");

        if (notesList != null)
        {
            notesList.ItemsSource = _notes;
        }
        if (noNotesText != null)
        {
            noNotesText.IsVisible = _notes.Count == 0;
        }
    }

    private void UpdateBadgeCounts()
    {
        var outcomesCountBadge = this.FindControl<Border>("OutcomesCountBadge");
        var outcomesCountText = this.FindControl<TextBlock>("OutcomesCountText");
        var notesCountBadge = this.FindControl<Border>("NotesCountBadge");
        var notesCountText = this.FindControl<TextBlock>("NotesCountText");

        if (outcomesCountBadge != null && outcomesCountText != null)
        {
            outcomesCountBadge.IsVisible = _outcomes.Count > 0;
            outcomesCountText.Text = _outcomes.Count.ToString();
        }
        if (notesCountBadge != null && notesCountText != null)
        {
            notesCountBadge.IsVisible = _notes.Count > 0;
            notesCountText.Text = _notes.Count.ToString();
        }
    }

    /// <summary>
    /// Refreshes the outcomes display.
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadOutcomesAsync();
    }

    /// <summary>
    /// Adds an outcome to the display (optimistic UI update).
    /// </summary>
    public void AddOutcome(AgendaItemOutcomeDetail outcome)
    {
        if (outcome.OutcomeType == OutcomeType.NotesAdded)
        {
            _notes.Insert(0, outcome);
            UpdateNotesList();
        }
        else
        {
            _outcomes.Insert(0, outcome);
            UpdateOutcomesList();
        }
        UpdateBadgeCounts();
    }

    #endregion

    #region Lifecycle

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        // Reset state when data context changes
        _isExpanded = false;
        _activeTab = "outcomes";
        _outcomes.Clear();
        _notes.Clear();
        
        UpdateExpandedState();
        UpdateTabVisibility();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // Initialize tab selection
        UpdateTabVisibility();
    }

    #endregion
}
