using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Represents a linkable item from the database (task, goal, etc).
/// </summary>
public partial class LinkableItem : ObservableObject
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    
    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Reusable control for staging work items during project creation.
/// Supports creating new items (title-only) and selecting existing items.
/// MVVM-compliant: binds to ViewModel collections via StyledProperties.
/// </summary>
public partial class WorkItemStagingControl : UserControl
{
    #region Styled Properties
    
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<WorkItemStagingControl, string>(nameof(Header), "Items");
    
    public static readonly StyledProperty<string> NewItemPlaceholderProperty =
        AvaloniaProperty.Register<WorkItemStagingControl, string>(nameof(NewItemPlaceholder), "Add item...");
    
    public static readonly StyledProperty<string> SearchPlaceholderProperty =
        AvaloniaProperty.Register<WorkItemStagingControl, string>(nameof(SearchPlaceholder), "Search...");
    
    public static readonly StyledProperty<ObservableCollection<string>?> NewItemTitlesProperty =
        AvaloniaProperty.Register<WorkItemStagingControl, ObservableCollection<string>?>(nameof(NewItemTitles));
    
    public static readonly StyledProperty<ObservableCollection<LinkableItem>?> AvailableItemsProperty =
        AvaloniaProperty.Register<WorkItemStagingControl, ObservableCollection<LinkableItem>?>(nameof(AvailableItems));
    
    /// <summary>
    /// Header text (e.g., "Tasks" or "Goals").
    /// </summary>
    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    /// <summary>
    /// Placeholder for the new item input.
    /// </summary>
    public string NewItemPlaceholder
    {
        get => GetValue(NewItemPlaceholderProperty);
        set => SetValue(NewItemPlaceholderProperty, value);
    }
    
    /// <summary>
    /// Placeholder for the search input.
    /// </summary>
    public string SearchPlaceholder
    {
        get => GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }
    
    /// <summary>
    /// Bound to ViewModel's collection of new item titles (title-only, to be created).
    /// </summary>
    public ObservableCollection<string>? NewItemTitles
    {
        get => GetValue(NewItemTitlesProperty);
        set => SetValue(NewItemTitlesProperty, value);
    }
    
    /// <summary>
    /// Bound to ViewModel's collection of available items for linking.
    /// Selection state lives in the LinkableItem.IsSelected property.
    /// </summary>
    public ObservableCollection<LinkableItem>? AvailableItems
    {
        get => GetValue(AvailableItemsProperty);
        set => SetValue(AvailableItemsProperty, value);
    }
    
    #endregion
    
    /// <summary>
    /// Filtered available items based on search (internal, for display).
    /// </summary>
    public ObservableCollection<LinkableItem> FilteredItems { get; } = new();
    
    private TextBox? _newItemInput;
    private TextBox? _searchInput;
    
    public WorkItemStagingControl()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _newItemInput = this.FindControl<TextBox>("NewItemInput");
        _searchInput = this.FindControl<TextBox>("SearchInput");
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == AvailableItemsProperty)
        {
            // Unwire old collection
            if (change.OldValue is ObservableCollection<LinkableItem> oldCollection)
            {
                oldCollection.CollectionChanged -= OnAvailableItemsCollectionChanged;
            }
            
            // Wire up new collection
            if (change.NewValue is ObservableCollection<LinkableItem> newCollection)
            {
                newCollection.CollectionChanged += OnAvailableItemsCollectionChanged;
                FilterAvailableItems();
            }
        }
    }
    
    private void OnAvailableItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        FilterAvailableItems();
    }
    
    /// <summary>
    /// Adds a new item title to the ViewModel's collection.
    /// </summary>
    private void AddNewItem(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        if (NewItemTitles == null) return;
        
        var trimmed = title.Trim();
        
        // Check for duplicates
        if (NewItemTitles.Any(t => t.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return;
        
        NewItemTitles.Add(trimmed);
        
        if (_newItemInput != null)
        {
            _newItemInput.Text = string.Empty;
            _newItemInput.Focus();
        }
    }
    
    /// <summary>
    /// Removes a new item title from the ViewModel's collection.
    /// </summary>
    private void RemoveNewItem(string title)
    {
        NewItemTitles?.Remove(title);
    }
    
    /// <summary>
    /// Toggles selection of an existing item (selection state lives in the LinkableItem).
    /// </summary>
    private void ToggleExistingItem(LinkableItem item)
    {
        item.IsSelected = !item.IsSelected;
    }
    
    /// <summary>
    /// Filters available items based on search text.
    /// </summary>
    private void FilterAvailableItems()
    {
        FilteredItems.Clear();
        
        var items = AvailableItems;
        if (items == null) return;
        
        var searchText = _searchInput?.Text ?? string.Empty;
        
        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? items
            : items.Where(i => 
                i.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }
    }
    
    /// <summary>
    /// Clears all staged items (new and selected).
    /// </summary>
    public void Clear()
    {
        NewItemTitles?.Clear();
        
        if (AvailableItems != null)
        {
            foreach (var item in AvailableItems)
            {
                item.IsSelected = false;
            }
        }
        
        FilterAvailableItems();
        
        if (_newItemInput != null) _newItemInput.Text = string.Empty;
        if (_searchInput != null) _searchInput.Text = string.Empty;
    }
    
    #region Event Handlers
    
    private void OnNewItemKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _newItemInput != null)
        {
            AddNewItem(_newItemInput.Text ?? string.Empty);
            e.Handled = true;
        }
    }
    
    private void OnAddNewItemClick(object? sender, RoutedEventArgs e)
    {
        if (_newItemInput != null)
        {
            AddNewItem(_newItemInput.Text ?? string.Empty);
        }
    }
    
    private void OnRemoveNewItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string title)
        {
            RemoveNewItem(title);
        }
    }
    
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterAvailableItems();
    }
    
    private void OnLinkableItemClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is LinkableItem item)
        {
            ToggleExistingItem(item);
        }
    }
    
    #endregion
}