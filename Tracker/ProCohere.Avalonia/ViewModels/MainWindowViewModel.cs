using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// Main window ViewModel - manages navigation and application state.
/// Child ViewModels are owned here for proper MVVM composition.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    #region Child ViewModels
    
    /// <summary>
    /// ViewModel for the Pulse page.
    /// </summary>
    public PulseViewModel PulseViewModel { get; }
    
    /// <summary>
    /// ViewModel for the standalone Goals browse page.
    /// </summary>
    public GoalsViewModel GoalsViewModel { get; }
    
    /// <summary>
    /// ViewModel for the standalone Metrics browse page.
    /// </summary>
    public MetricsViewModel MetricsViewModel { get; }
    
    /// <summary>
    /// ViewModel for the standalone Tasks browse page.
    /// </summary>
    public TasksViewModel TasksViewModel { get; }
    
    #endregion
    
    #region Navigation

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(PageIconData))]
    private NavigationItem _selectedNavigation = NavigationItem.Briefing;

    [ObservableProperty]
    private string _selectedSubNavigation = string.Empty;

    [ObservableProperty]
    private bool _isNavigationExpanded = true;

    #endregion

    #region Page Title

    /// <summary>
    /// Gets the page title based on current navigation.
    /// </summary>
    public string PageTitle => SelectedNavigation switch
    {
        NavigationItem.Briefing => "Briefing",
        NavigationItem.Me => "Me",
        NavigationItem.Circle => "Circle",
        NavigationItem.Projects => "Projects",
        NavigationItem.Pulse => "Pulse",
        NavigationItem.Goals => "Goals",
        NavigationItem.Metrics => "Metrics",
        NavigationItem.Tasks => "Tasks",
        NavigationItem.Chronicle => "Chronicle",
        NavigationItem.Settings => "Settings",
        _ => SelectedNavigation.ToString()
    };

    /// <summary>
    /// Gets the page icon path data based on current navigation.
    /// </summary>
    public string PageIconData => SelectedNavigation switch
    {
        NavigationItem.Briefing => "M5 15H3v4c0 1.1.9 2 2 2h4v-2H5v-4zM5 5h4V3H5c-1.1 0-2 .9-2 2v4h2V5zm14-2h-4v2h4v4h2V5c0-1.1-.9-2-2-2zm0 16h-4v2h4c1.1 0 2-.9 2-2v-4h-2v4zM12 8c-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4-1.79-4-4-4zm0 6c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z",
        NavigationItem.Me => "M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,5.5A2.5,2.5 0 0,0 9.5,8A2.5,2.5 0 0,0 12,10.5A2.5,2.5 0 0,0 14.5,8A2.5,2.5 0 0,0 12,5.5M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14M12,15.5C8.04,15.5 5.5,17.08 5.5,18V18.5H18.5V18C18.5,17.08 15.96,15.5 12,15.5Z",
        NavigationItem.Circle => "M14.75 15c.966 0 1.75.784 1.75 1.75l-.001.962c.117 2.19-1.511 3.297-4.432 3.297-2.91 0-4.567-1.09-4.567-3.259v-1c0-.966.784-1.75 1.75-1.75h5.5Zm0 1.5h-5.5a.25.25 0 0 0-.25.25v1c0 1.176.887 1.759 3.067 1.759 2.168 0 2.995-.564 2.933-1.757V16.75a.25.25 0 0 0-.25-.25ZM12 8a3 3 0 1 1 0 6 3 3 0 0 1 0-6Zm0 1.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3ZM18.5 9a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5Zm-13 0a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5Zm13 1.5a1 1 0 1 0 0 2 1 1 0 0 0 0-2Zm-13 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2Z",
        NavigationItem.Projects => "M22 11V3h-7v3H9V3H2v8h7V8h2v10H9v-3H2v8h7v-3h6v3h7v-8h-7v3h-2V8h2v3h7zM7 9H4V5h3v4zm0 12H4v-4h3v4zm13-12h-3V5h3v4zm0 12h-3v-4h3v4z",
        NavigationItem.Pulse => "M5.25 3A2.25 2.25 0 0 0 3 5.25v13.5A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V5.25A2.25 2.25 0 0 0 18.75 3H5.25ZM4.5 5.25a.75.75 0 0 1 .75-.75h13.5a.75.75 0 0 1 .75.75v13.5a.75.75 0 0 1-.75.75H5.25a.75.75 0 0 1-.75-.75V5.25Zm3.22 7.47 2.25-2.25a.75.75 0 0 1 1.06 0l1.72 1.72 3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L10.5 12.06l-1.72 1.72a.75.75 0 0 1-1.06-1.06Z",
        NavigationItem.Goals => "M5,21L7.5,13L1,9H8.5L11,1L13.5,9H21L14.5,13L17,21L11,16L5,21Z",
        NavigationItem.Metrics => "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z",
        NavigationItem.Tasks => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M9,13V19H7V13H9M15,15V19H13V15H15M11,11V19H9V11H11M13,13V19H11V13H13",
        NavigationItem.Chronicle => "M12 6.042A8.967 8.967 0 0 0 6 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 0 1 6 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 0 1 6-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0 0 18 18a8.967 8.967 0 0 0-6 2.292m0-14.25v14.25",
        NavigationItem.Settings => "M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.03 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.67 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.03 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z",
        _ => ""
    };

    #endregion

    #region Search

    [ObservableProperty]
    private string _searchText = string.Empty;

    #endregion

    #region User Info

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private string _userDisplayName = string.Empty;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _userInitials = string.Empty;

    [ObservableProperty]
    private string? _userAvatarUrl;

    [ObservableProperty]
    private bool _isUserMenuOpen;

    /// <summary>
    /// Raised when the user signs out.
    /// </summary>
    public event Action? SignOutRequested;

    /// <summary>
    /// Raised when the user wants to edit their profile. 
    /// The view should show the EditAccountDialog.
    /// </summary>
    public event Action? EditProfileRequested;

    #endregion

    #region Theme

    [ObservableProperty]
    private bool _isDarkTheme = false;  // Start in light mode (Pro Cohere default)

    #endregion

    #region Manager Status

    /// <summary>
    /// True if current user has direct reports (is a manager).
    /// Used to control visibility of Circle navigation.
    /// </summary>
    [ObservableProperty]
    private bool _hasDirectReports = false;

    #endregion

    #region Status

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Database proof - shows profile data loaded from Supabase.
    /// </summary>
    [ObservableProperty]
    private string _databaseProof = "Loading from database...";

    #endregion

    public MainWindowViewModel()
    {
        // Initialize child ViewModels
        PulseViewModel = new PulseViewModel();
        GoalsViewModel = new GoalsViewModel();
        MetricsViewModel = new MetricsViewModel();
        TasksViewModel = new TasksViewModel();
        
        // Wire up Quick Access navigation events from Pulse
        PulseViewModel.NavigateToGoalsRequested += (_, _) => SelectedNavigation = NavigationItem.Goals;
        PulseViewModel.NavigateToMetricsRequested += (_, _) => SelectedNavigation = NavigationItem.Metrics;
        PulseViewModel.NavigateToTasksRequested += (_, _) => SelectedNavigation = NavigationItem.Tasks;
        
        // Wire up back navigation from browse pages
        GoalsViewModel.NavigateBackRequested += (_, _) => SelectedNavigation = NavigationItem.Pulse;
        MetricsViewModel.NavigateBackRequested += (_, _) => SelectedNavigation = NavigationItem.Pulse;
        TasksViewModel.NavigateBackRequested += (_, _) => SelectedNavigation = NavigationItem.Pulse;
        
        LoadUserInfo();
        // Load profile from database async
        _ = LoadProfileFromDatabaseAsync();
    }

    /// <summary>
    /// Converts a string to Title Case (first letter of each word capitalized).
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + 
                          (words[i].Length > 1 ? words[i][1..].ToLower() : string.Empty);
            }
        }
        return string.Join(" ", words);
    }

    /// <summary>
    /// Loads profile from the Supabase database to prove connectivity.
    /// </summary>
    private async Task LoadProfileFromDatabaseAsync()
    {
        try
        {
            DatabaseProof = "🔄 Loading from Supabase...";
            
            // Get the auth user ID we're searching for
            var authUser = AuthService.Instance.CurrentUser;
            var authUserId = authUser?.Id ?? "null";
            
            DatabaseProof = $"🔄 Querying for auth ID:\n{authUserId}";
            
            var profile = await AuthService.Instance.LoadUserProfileAsync();
            
            if (profile != null)
            {
                // Update user info from database
                if (!string.IsNullOrEmpty(profile.DisplayName))
                {
                    UserDisplayName = ToTitleCase(profile.DisplayName);
                }
                if (!string.IsNullOrEmpty(profile.FirstName) || !string.IsNullOrEmpty(profile.LastName))
                {
                    UserDisplayName = ToTitleCase($"{profile.FirstName} {profile.LastName}".Trim());
                }
                UserInitials = profile.Initials;
                UserAvatarUrl = profile.AvatarUrl;
                
                // Determine if user is a manager (has direct reports) based on role
                // Admin and Manager roles can see Circle, others cannot
                var currentRole = AuthService.Instance.CurrentRole;
                var roleName = currentRole?.Name?.ToLowerInvariant() ?? "";
                HasDirectReports = roleName == "admin" || roleName == "manager";
                
                // Get data counts
                DatabaseProof = $"✅ Profile loaded! Getting data counts...";
                var counts = await AuthService.Instance.GetDataCountsAsync();
                var dataError = AuthService.Instance.LastDataCountError;
                
                // Build the proof string with data counts
                var proofBuilder = new System.Text.StringBuilder();
                proofBuilder.AppendLine("✅ DATABASE CONNECTED!");
                proofBuilder.AppendLine();
                proofBuilder.AppendLine($"👤 Profile: {profile.FirstName} {profile.LastName}");
                proofBuilder.AppendLine($"📧 Email: {profile.Email}");
                proofBuilder.AppendLine($"🆔 User ID: {profile.Id}");
                proofBuilder.AppendLine($"💼 {profile.JobTitle ?? "(no title)"} @ {profile.Company ?? "(no company)"}");
                proofBuilder.AppendLine();
                proofBuilder.AppendLine("📊 YOUR DATA:");
                proofBuilder.AppendLine("━━━━━━━━━━━━━━━━━━");
                proofBuilder.AppendLine($"👥 Team Members: {counts.TeamMembers}");
                proofBuilder.AppendLine($"📅 Meetings: {counts.Meetings}");
                proofBuilder.AppendLine($"🎯 Goals: {counts.Goals}");
                proofBuilder.AppendLine($"✅ Tasks: {counts.Tasks}");
                proofBuilder.AppendLine("━━━━━━━━━━━━━━━━━━");
                
                if (!string.IsNullOrEmpty(dataError))
                {
                    proofBuilder.AppendLine();
                    proofBuilder.AppendLine($"⚠️ Data errors: {dataError}");
                }
                
                DatabaseProof = proofBuilder.ToString();
            }
            else
            {
                var authId = authUser?.Id ?? "null";
                var error = AuthService.Instance.LastProfileError ?? "No error info";
                DatabaseProof = $"⚠️ No profile found in database.\n\n" +
                               $"Auth User ID: {authId}\n" +
                               $"Auth Email: {authUser?.Email ?? "null"}\n\n" +
                               $"Error: {error}\n\n" +
                               $"Check Supabase Table Editor:\n" +
                               $"SELECT * FROM users\n" +
                               $"WHERE supabase_auth_id = '{authId}'";
            }
        }
        catch (Exception ex)
        {
            DatabaseProof = $"❌ DATABASE ERROR:\n{ex.Message}\n\n{ex.StackTrace?.Split('\n').FirstOrDefault()}";
        }
    }

    private void LoadUserInfo()
    {
        var session = AuthService.Instance.CurrentSession;
        if (session?.User != null)
        {
            UserEmail = session.User.Email ?? string.Empty;
            
            // Try to get display name from user metadata
            var metadata = session.User.UserMetadata;
            string rawName;
            if (metadata != null && metadata.TryGetValue("full_name", out var fullName))
            {
                rawName = fullName?.ToString() ?? string.Empty;
            }
            else
            {
                // Fallback to email prefix
                rawName = UserEmail.Split('@')[0];
            }
            
            // Properly capitalize the name
            UserDisplayName = ToTitleCase(rawName);

            // Generate initials
            var parts = UserDisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                UserInitials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                UserInitials = parts[0][..2].ToUpper();
            }
            else
            {
                UserInitials = "PC";
            }
        }
    }

    #region Commands

    [RelayCommand]
    private void NavigateTo(NavigationItem item)
    {
        SelectedNavigation = item;
        SelectedSubNavigation = string.Empty;
    }

    [RelayCommand]
    private void NavigateToSub(string subItem)
    {
        SelectedSubNavigation = subItem;
    }

    [RelayCommand]
    private void ToggleNavigation()
    {
        IsNavigationExpanded = !IsNavigationExpanded;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // Theme switching will be handled by the view
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        try
        {
            // Clear stored credentials
            var credentialService = new WindowsCredentialService();
            credentialService.ClearSession();
            
            // Sign out from Supabase
            await AuthService.Instance.SignOutAsync();
            
            // Notify the view to navigate to login
            SignOutRequested?.Invoke();
        }
        catch (Exception)
        {
            // Still navigate to login even if sign out fails
            SignOutRequested?.Invoke();
        }
    }

    [RelayCommand]
    private void OpenSearch()
    {
        // TODO: Implement command palette / search
        StatusMessage = "Search coming soon...";
    }

    [RelayCommand]
    private void OpenHelp()
    {
        // TODO: Open help / AI assistant
        StatusMessage = "Help coming soon...";
    }

    [RelayCommand]
    private void EditProfile()
    {
        // Request the view to show the EditAccountDialog
        EditProfileRequested?.Invoke();
    }

    /// <summary>
    /// Refreshes user display info after profile edit.
    /// </summary>
    public async Task RefreshUserInfoAsync()
    {
        await LoadProfileFromDatabaseAsync();
    }

    #endregion
}

/// <summary>
/// Main navigation items.
/// </summary>
public enum NavigationItem
{
    Briefing,   // Dashboard - what's relevant now (today/week view)
    Me,         // Personal hub - my tasks, goals, meetings, feedback (ALL users)
    Circle,     // Team view (MANAGERS ONLY) - team activity, attention needed
    Projects,   // Initiatives that organize long-term work
    Pulse,      // Synthesis hub - signals and quick access
    Goals,      // Goals browse/manage page (standalone destination)
    Metrics,    // Metrics browse/manage page (standalone destination)
    Tasks,      // Tasks browse/manage page (standalone destination)
    Chronicle,  // Notes, Reports
    Settings    // App settings
}
