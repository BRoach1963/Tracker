using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Popover control for selecting a project to link to.
/// </summary>
public partial class ProjectSelectorPopover : UserControl
{
    /// <summary>
    /// Raised when a project is selected.
    /// </summary>
    public event EventHandler<Project>? ProjectSelected;
    
    private string _searchText = string.Empty;
    private bool _isLoading;
    private ObservableCollection<Project> _allProjects = new();
    private ObservableCollection<Project> _filteredProjects = new();

    public ProjectSelectorPopover()
    {
        InitializeComponent();
        DataContext = this;
        
        // Focus search box when opened
        SearchBox.AttachedToVisualTree += async (s, e) =>
        {
            SearchBox.Focus();
            await LoadProjectsAsync();
        };
        
        // Filter on search text change
        SearchBox.TextChanged += (s, e) => FilterProjects();
    }
    
    /// <summary>
    /// Search text for filtering projects.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                FilterProjects();
            }
        }
    }
    
    /// <summary>
    /// Whether projects are loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            // Manual property change since we're not using [ObservableProperty]
        }
    }
    
    /// <summary>
    /// Filtered list of projects for display.
    /// </summary>
    public ObservableCollection<Project> FilteredProjects => _filteredProjects;
    
    /// <summary>
    /// Whether to show the empty state.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && FilteredProjects.Count == 0;
    
    /// <summary>
    /// Loads available projects from the service.
    /// </summary>
    private async Task LoadProjectsAsync()
    {
        IsLoading = true;
        
        try
        {
            var projects = await ProjectService.Instance.GetAllProjectsAsync();
            _allProjects.Clear();
            
            foreach (var project in projects.OrderByDescending(p => p.Status == ProjectStatus.Active)
                                             .ThenBy(p => p.Name))
            {
                _allProjects.Add(project);
            }
            
            FilterProjects();
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Filters projects based on search text.
    /// </summary>
    private void FilterProjects()
    {
        _filteredProjects.Clear();
        
        var searchLower = SearchText?.ToLowerInvariant() ?? string.Empty;
        
        var filtered = string.IsNullOrWhiteSpace(searchLower)
            ? _allProjects
            : _allProjects.Where(p => 
                p.Name.ToLowerInvariant().Contains(searchLower) ||
                (p.Description?.ToLowerInvariant().Contains(searchLower) ?? false));
        
        foreach (var project in filtered)
        {
            _filteredProjects.Add(project);
        }
    }
    
    /// <summary>
    /// Command to select a project.
    /// </summary>
    [RelayCommand]
    private void SelectProject(Project project)
    {
        ProjectSelected?.Invoke(this, project);
    }
}
