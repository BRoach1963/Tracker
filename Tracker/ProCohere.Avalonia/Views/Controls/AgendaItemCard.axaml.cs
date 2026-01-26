using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ProCohere.Avalonia.Models;

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
    /// Raised when user wants to set status to Open.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? SetOpenRequested;

    /// <summary>
    /// Raised when user wants to set status to Discussed.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? SetDiscussedRequested;

    /// <summary>
    /// Raised when user wants to set status to Dropped.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? SetDroppedRequested;

    /// <summary>
    /// Raised when user wants to defer the item (needs to select anchor person).
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? DeferRequested;

    /// <summary>
    /// Raised when outcomes need to be loaded for this item.
    /// </summary>
    public event EventHandler<MeetingAgendaItem>? LoadOutcomesRequested;

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
        
        // Request outcomes load on first expand
        if (IsExpanded && _outcomes.Count == 0 && AgendaItem != null)
        {
            LoadOutcomesRequested?.Invoke(this, AgendaItem);
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

    private void OnSetOpen(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            SetOpenRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnSetDiscussed(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            SetDiscussedRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnSetDeferred(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            DeferRequested?.Invoke(this, AgendaItem);
        }
    }

    private void OnSetDropped(object? sender, RoutedEventArgs e)
    {
        if (AgendaItem != null)
        {
            SetDroppedRequested?.Invoke(this, AgendaItem);
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
    /// Sets the outcomes for display. Called by parent after loading from service.
    /// </summary>
    public void SetOutcomes(List<AgendaItemOutcomeDetail> allOutcomes)
    {
        // Separate notes from other outcomes
        _outcomes = allOutcomes.FindAll(o => o.OutcomeType != OutcomeType.NotesAdded);
        _notes = allOutcomes.FindAll(o => o.OutcomeType == OutcomeType.NotesAdded);

        // Update UI
        UpdateOutcomesList();
        UpdateNotesList();
        UpdateBadgeCounts();
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
    /// Requests a refresh of outcomes from the parent.
    /// </summary>
    public void RequestRefresh()
    {
        if (AgendaItem != null)
        {
            LoadOutcomesRequested?.Invoke(this, AgendaItem);
        }
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
