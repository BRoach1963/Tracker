using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the help window, managing help topic display and search.
/// Follows clean MVVM patterns with proper error handling and single responsibility.
/// </summary>
public partial class HelpWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchQuery = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<HelpTopic> _topics = new();
    
    [ObservableProperty]
    private HelpTopic? _selectedTopic;
    
    [ObservableProperty]
    private ObservableCollection<HelpTopic> _relatedTopics = new();
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private string? _errorMessage;
    
    private readonly HelpService _helpService;
    
    public ICommand SearchCommand { get; }
    public ICommand SelectTopicCommand { get; }
    public ICommand CloseCommand { get; }
    
    public HelpWindowViewModel(HelpService? helpService = null)
    {
        _helpService = helpService ?? HelpService.Instance;
        
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        SelectTopicCommand = new AsyncRelayCommand<HelpTopic>(SelectTopicAsync);
        CloseCommand = new RelayCommand<Window>(Close);
        
        _ = InitializeAsync();
    }
    
    public HelpWindowViewModel(string? initialTopicId, HelpService? helpService = null) : this(helpService)
    {
        if (!string.IsNullOrEmpty(initialTopicId))
        {
            _ = LoadInitialTopicAsync(initialTopicId);
        }
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            await _helpService.InitializeAsync();
            await LoadAllTopicsAsync();
        }
        catch (Exception ex)
        {
            HandleError("Failed to initialize help system", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task LoadInitialTopicAsync(string topicId)
    {
        try
        {
            var topic = await _helpService.GetTopicAsync(topicId);
            if (topic != null)
            {
                await SelectTopicAsync(topic);
            }
            else
            {
                ErrorMessage = $"Help topic '{topicId}' not found";
            }
        }
        catch (Exception ex)
        {
            HandleError($"Failed to load help topic '{topicId}'", ex);
        }
    }
    
    private async Task LoadAllTopicsAsync()
    {
        var allTopics = await _helpService.SearchTopicsAsync(string.Empty);
        
        Topics.Clear();
        foreach (var topic in allTopics)
        {
            Topics.Add(topic);
        }
        
        // Select first topic if none selected and topics are available
        if (SelectedTopic == null && Topics.Count > 0)
        {
            await SelectTopicAsync(Topics.First());
        }
    }
    
    private async Task SearchAsync()
    {
        if (IsLoading) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var results = await _helpService.SearchTopicsAsync(SearchQuery);
            
            Topics.Clear();
            foreach (var topic in results)
            {
                Topics.Add(topic);
            }
            
            // Auto-select first result if searching with query
            if (!string.IsNullOrWhiteSpace(SearchQuery) && Topics.Count > 0)
            {
                await SelectTopicAsync(Topics.First());
            }
        }
        catch (Exception ex)
        {
            HandleError("Search failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task SelectTopicAsync(HelpTopic? topic)
    {
        if (topic == null || IsLoading) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            // Load full content for the topic
            var fullTopic = await _helpService.GetTopicAsync(topic.Id);
            if (fullTopic != null)
            {
                SelectedTopic = fullTopic;
                await LoadRelatedTopicsAsync(fullTopic);
            }
            else
            {
                ErrorMessage = $"Failed to load topic content for '{topic.Title}'";
            }
        }
        catch (Exception ex)
        {
            HandleError($"Failed to select topic '{topic.Title}'", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task LoadRelatedTopicsAsync(HelpTopic topic)
    {
        RelatedTopics.Clear();
        
        foreach (var relatedId in topic.RelatedTopics)
        {
            try
            {
                var relatedTopic = await _helpService.GetTopicAsync(relatedId);
                if (relatedTopic != null)
                {
                    RelatedTopics.Add(relatedTopic);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HelpWindowViewModel] Failed to load related topic {relatedId}: {ex.Message}");
                // Don't show error to user for related topics - not critical
            }
        }
    }
    
    private static void Close(Window? window)
    {
        window?.Close();
    }
    
    private void HandleError(string message, Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[HelpWindowViewModel] {message}: {ex.Message}");
        ErrorMessage = message;
    }
    
    partial void OnSearchQueryChanged(string value)
    {
        // Auto-search when query changes
        if (string.IsNullOrEmpty(value))
        {
            _ = LoadAllTopicsAsync();
        }
    }
}