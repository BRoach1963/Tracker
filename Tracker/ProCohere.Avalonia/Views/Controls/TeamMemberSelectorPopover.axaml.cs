using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Views.Controls;

/// <summary>
/// Popover control for selecting a team member.
/// Reusable component following the same pattern as ProjectSelectorPopover.
/// </summary>
public partial class TeamMemberSelectorPopover : UserControl
{
    /// <summary>
    /// Raised when a team member is selected.
    /// </summary>
    public event EventHandler<TeamMemberDetail>? MemberSelected;
    
    private string _searchText = string.Empty;
    private bool _isLoading;
    private Guid? _excludeMemberId;
    private readonly ObservableCollection<TeamMemberDetail> _allMembers = new();
    private readonly ObservableCollection<TeamMemberDetail> _filteredMembers = new();

    public TeamMemberSelectorPopover()
    {
        InitializeComponent();
        DataContext = this;
        
        // Focus search box when opened
        SearchBox.AttachedToVisualTree += async (s, e) =>
        {
            SearchBox.Focus();
            await LoadMembersAsync();
        };
        
        // Filter on search text change
        SearchBox.TextChanged += (s, e) => FilterMembers();
    }
    
    /// <summary>
    /// Sets a team member ID to exclude from the list (e.g., current owner).
    /// </summary>
    public void SetExcludeMember(Guid? memberId)
    {
        _excludeMemberId = memberId;
    }
    
    /// <summary>
    /// Search text for filtering members.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                FilterMembers();
            }
        }
    }
    
    /// <summary>
    /// Whether members are loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => _isLoading = value;
    }
    
    /// <summary>
    /// Filtered list of team members for display.
    /// </summary>
    public ObservableCollection<TeamMemberDetail> FilteredMembers => _filteredMembers;
    
    /// <summary>
    /// Whether to show the empty state.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && FilteredMembers.Count == 0;
    
    /// <summary>
    /// Loads available team members from the service.
    /// </summary>
    private async Task LoadMembersAsync()
    {
        IsLoading = true;
        
        try
        {
            var client = AuthService.Instance.GetProCohereClient();
            if (client == null) return;
            
            // Load active team members
            var result = await client.From<TeamMemberDetail>()
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Order("first_name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            
            _allMembers.Clear();
            
            if (result.Models != null)
            {
                foreach (var member in result.Models)
                {
                    // Exclude specified member if set
                    if (_excludeMemberId.HasValue && member.Id == _excludeMemberId.Value)
                        continue;
                        
                    _allMembers.Add(member);
                }
            }
            
            FilterMembers();
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Filters members based on search text.
    /// </summary>
    private void FilterMembers()
    {
        _filteredMembers.Clear();
        
        var searchLower = SearchText?.ToLowerInvariant() ?? string.Empty;
        
        var filtered = string.IsNullOrWhiteSpace(searchLower)
            ? _allMembers
            : _allMembers.Where(m => 
                (m.FullName?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (m.Email?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (m.JobTitle?.ToLowerInvariant().Contains(searchLower) ?? false));
        
        foreach (var member in filtered)
        {
            _filteredMembers.Add(member);
        }
    }
    
    /// <summary>
    /// Command to select a team member.
    /// </summary>
    [RelayCommand]
    private void SelectMember(TeamMemberDetail member)
    {
        MemberSelected?.Invoke(this, member);
    }
}
